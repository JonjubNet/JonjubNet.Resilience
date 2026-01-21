using JonjubNet.Resilience.Abstractions;
using JonjubNet.Resilience.Core.Configuration;
using JonjubNet.Resilience.Core.Interfaces;
using Microsoft.Extensions.Options;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace JonjubNet.Resilience.Polly.Services
{
    /// <summary>
    /// Cliente de resiliencia que ejecuta operaciones usando pipelines nombrados.
    /// Implementa <see cref="IResilienceClient"/>; la lógica de retry/circuit breaker/timeout vive en el componente.
    /// Emite eventos a <see cref="IResilienceEventSink"/> si está registrado; funciona sin sink.
    /// </summary>
    public class ResilienceClient : IResilienceClient
    {
        private const int MaxPipelineCacheSize = 100;

        private readonly ResilienceConfiguration _configuration;
        private readonly IDatabaseExceptionDetector _exceptionDetector;
        private readonly IEnumerable<IResilienceEventSink> _sinks;
        private readonly ConcurrentDictionary<string, ResiliencePipeline> _pipelines = new();
        private long _pipelineCount = 0;

        public ResilienceClient(
            IOptions<ResilienceConfiguration> configuration,
            IDatabaseExceptionDetector? exceptionDetector = null,
            IEnumerable<IResilienceEventSink>? sinks = null)
        {
            _configuration = configuration.Value;
            _exceptionDetector = exceptionDetector ?? new DatabaseExceptionDetector();
            _sinks = sinks ?? Array.Empty<IResilienceEventSink>();
        }

        /// <inheritdoc />
        public async Task ExecuteAsync(string pipelineName, Func<CancellationToken, Task> action, CancellationToken ct = default)
        {
            await ExecuteAsync(pipelineName, async (c) =>
            {
                await action(c);
                return true;
            }, ct);
        }

        /// <inheritdoc />
        public async Task<T> ExecuteAsync<T>(string pipelineName, Func<CancellationToken, Task<T>> action, CancellationToken ct = default)
        {
            if (!_configuration.Enabled)
            {
                return await action(ct);
            }

            var pipeline = GetOrCreatePipeline(pipelineName);
            var start = DateTimeOffset.UtcNow;

            try
            {
                Emit(new ResilienceEvent
                {
                    EventType = "OperationStarted",
                    PipelineName = pipelineName,
                    Message = $"Executing with pipeline '{pipelineName}'. Resilience enabled: {_configuration.Enabled}",
                    Timestamp = start
                });

                var result = await pipeline.ExecuteAsync(async (c) => await action(c), ct);

                Emit(new ResilienceEvent
                {
                    EventType = "OperationSucceeded",
                    PipelineName = pipelineName,
                    Message = $"Operation with pipeline '{pipelineName}' completed successfully",
                    Timestamp = DateTimeOffset.UtcNow,
                    Duration = DateTimeOffset.UtcNow - start
                });

                return result;
            }
            catch (Exception ex)
            {
                Emit(new ResilienceEvent
                {
                    EventType = "OperationFailed",
                    PipelineName = pipelineName,
                    Message = $"Operation with pipeline '{pipelineName}' failed",
                    Exception = ex,
                    Timestamp = DateTimeOffset.UtcNow,
                    Duration = DateTimeOffset.UtcNow - start
                });
                throw;
            }
        }

        private ResiliencePipeline GetOrCreatePipeline(string pipelineName)
        {
            if (_pipelines.TryGetValue(pipelineName, out var existingPipeline))
                return existingPipeline;

            var currentCount = Interlocked.Read(ref _pipelineCount);
            if (currentCount >= MaxPipelineCacheSize)
            {
                Emit(new ResilienceEvent
                {
                    EventType = "OperationFailed",
                    PipelineName = pipelineName,
                    Message = $"Pipeline cache limit reached ({MaxPipelineCacheSize}). Using default pipeline for '{pipelineName}'",
                    Timestamp = DateTimeOffset.UtcNow
                });
                return CreateDefaultPipeline();
            }

            var pipeline = CreatePipelineFromConfiguration(pipelineName);
            if (_pipelines.TryAdd(pipelineName, pipeline))
            {
                Interlocked.Increment(ref _pipelineCount);
                return pipeline;
            }

            return _pipelines[pipelineName];
        }

        private ResiliencePipeline CreatePipelineFromConfiguration(string pipelineName)
        {
            if (!_configuration.Pipelines.TryGetValue(pipelineName, out var pipelineConfig))
            {
                Emit(new ResilienceEvent
                {
                    EventType = "OperationStarted",
                    PipelineName = pipelineName,
                    Message = $"Pipeline '{pipelineName}' not found in configuration. Using default pipeline.",
                    Timestamp = DateTimeOffset.UtcNow
                });
                return CreateDefaultPipeline();
            }

            if (!pipelineConfig.Enabled)
            {
                Emit(new ResilienceEvent
                {
                    EventType = "OperationStarted",
                    PipelineName = pipelineName,
                    Message = $"Pipeline '{pipelineName}' is disabled. Using default pipeline.",
                    Timestamp = DateTimeOffset.UtcNow
                });
                return CreateDefaultPipeline();
            }

            var pipelineBuilder = new ResiliencePipelineBuilder();

            if (pipelineConfig.Retry != null)
            {
                var delay = pipelineConfig.Retry.DelayMilliseconds > 0
                    ? TimeSpan.FromMilliseconds(pipelineConfig.Retry.DelayMilliseconds)
                    : TimeSpan.FromSeconds(pipelineConfig.Retry.DelaySeconds);

                pipelineBuilder.AddRetry(new RetryStrategyOptions
                {
                    ShouldHandle = new PredicateBuilder()
                        .Handle<Exception>(ex =>
                        {
                            if (pipelineConfig.Retry.HandleDeadlocks || pipelineConfig.Retry.HandleTimeouts)
                            {
                                return _exceptionDetector.IsTransient(ex) ||
                                       (pipelineConfig.Retry.HandleTimeouts && ex is TimeoutException) ||
                                       (pipelineConfig.Retry.HandleDeadlocks && ex is InvalidOperationException);
                            }
                            return _exceptionDetector.IsTransient(ex) ||
                                   ex is TimeoutException ||
                                   ex is TaskCanceledException;
                        }),
                    MaxRetryAttempts = pipelineConfig.Retry.MaxRetryAttempts,
                    Delay = delay,
                    MaxDelay = TimeSpan.FromSeconds(30),
                    BackoffType = GetBackoffType(pipelineConfig.Retry.BackoffType),
                    OnRetry = args =>
                    {
                        Emit(new ResilienceEvent
                        {
                            EventType = "Retry",
                            PipelineName = pipelineName,
                            Message = $"Retry attempt {args.AttemptNumber} after {args.RetryDelay.TotalMilliseconds}ms. Reason: {args.Outcome.Exception?.Message}",
                            Exception = args.Outcome.Exception,
                            Timestamp = DateTimeOffset.UtcNow
                        });
                        return ValueTask.CompletedTask;
                    }
                });
            }

            if (pipelineConfig.CircuitBreaker != null && pipelineConfig.CircuitBreaker.Enabled)
            {
                pipelineBuilder.AddCircuitBreaker(new CircuitBreakerStrategyOptions
                {
                    ShouldHandle = new PredicateBuilder()
                        .Handle<Exception>(ex => _exceptionDetector.IsTransient(ex) ||
                                                _exceptionDetector.IsConnectionException(ex)),
                    FailureRatio = pipelineConfig.CircuitBreaker.FailureRatio,
                    SamplingDuration = TimeSpan.FromSeconds(pipelineConfig.CircuitBreaker.SamplingDurationSeconds),
                    MinimumThroughput = pipelineConfig.CircuitBreaker.MinimumThroughput,
                    BreakDuration = TimeSpan.FromSeconds(pipelineConfig.CircuitBreaker.BreakDurationSeconds),
                    OnOpened = args =>
                    {
                        Emit(new ResilienceEvent
                        {
                            EventType = "CircuitBreakerOpened",
                            PipelineName = pipelineName,
                            Message = $"Circuit breaker opened for {args.BreakDuration.TotalMilliseconds}ms. Reason: {args.Outcome.Exception?.Message}",
                            Exception = args.Outcome.Exception,
                            Timestamp = DateTimeOffset.UtcNow
                        });
                        return ValueTask.CompletedTask;
                    },
                    OnClosed = args =>
                    {
                        Emit(new ResilienceEvent
                        {
                            EventType = "CircuitBreakerClosed",
                            PipelineName = pipelineName,
                            Message = "Circuit breaker closed - service is healthy again",
                            Timestamp = DateTimeOffset.UtcNow
                        });
                        return ValueTask.CompletedTask;
                    },
                    OnHalfOpened = args =>
                    {
                        Emit(new ResilienceEvent
                        {
                            EventType = "CircuitBreakerHalfOpened",
                            PipelineName = pipelineName,
                            Message = "Circuit breaker half-opened - testing service health",
                            Timestamp = DateTimeOffset.UtcNow
                        });
                        return ValueTask.CompletedTask;
                    }
                });
            }

            if (pipelineConfig.Timeout != null && pipelineConfig.Timeout.Enabled)
            {
                pipelineBuilder.AddTimeout(TimeSpan.FromSeconds(pipelineConfig.Timeout.TimeoutSeconds));
            }

            return pipelineBuilder.Build();
        }

        private ResiliencePipeline CreateDefaultPipeline()
        {
            var pipelineBuilder = new ResiliencePipelineBuilder();

            if (_configuration.Retry.Enabled)
            {
                pipelineBuilder.AddRetry(new RetryStrategyOptions
                {
                    ShouldHandle = new PredicateBuilder()
                        .Handle<Exception>(ex => _exceptionDetector.IsTransient(ex)),
                    MaxRetryAttempts = _configuration.Retry.MaxRetryAttempts,
                    Delay = TimeSpan.FromMilliseconds(_configuration.Retry.BaseDelayMilliseconds),
                    MaxDelay = TimeSpan.FromMilliseconds(_configuration.Retry.MaxDelayMilliseconds),
                    BackoffType = GetBackoffType(_configuration.Retry.BackoffStrategy)
                });
            }

            if (_configuration.CircuitBreaker.Enabled)
            {
                pipelineBuilder.AddCircuitBreaker(new CircuitBreakerStrategyOptions
                {
                    ShouldHandle = new PredicateBuilder()
                        .Handle<Exception>(ex => _exceptionDetector.IsTransient(ex) ||
                                                _exceptionDetector.IsConnectionException(ex)),
                    FailureRatio = _configuration.CircuitBreaker.EnableAdvancedCircuitBreaker
                        ? _configuration.CircuitBreaker.FailureThresholdRatio
                        : 0.5,
                    SamplingDuration = TimeSpan.FromSeconds(_configuration.CircuitBreaker.SamplingDurationSeconds),
                    MinimumThroughput = _configuration.CircuitBreaker.EnableAdvancedCircuitBreaker
                        ? _configuration.CircuitBreaker.MinimumThroughputForAdvanced
                        : _configuration.CircuitBreaker.MinimumThroughput,
                    BreakDuration = TimeSpan.FromSeconds(_configuration.CircuitBreaker.DurationOfBreakSeconds)
                });
            }

            if (_configuration.Timeout.Enabled)
            {
                pipelineBuilder.AddTimeout(TimeSpan.FromSeconds(_configuration.Timeout.DefaultTimeoutSeconds));
            }

            return pipelineBuilder.Build();
        }

        private static DelayBackoffType GetBackoffType(string strategy)
        {
            if (string.Equals(strategy, "exponential", StringComparison.OrdinalIgnoreCase))
                return DelayBackoffType.Exponential;
            if (string.Equals(strategy, "linear", StringComparison.OrdinalIgnoreCase))
                return DelayBackoffType.Linear;
            if (string.Equals(strategy, "fixed", StringComparison.OrdinalIgnoreCase))
                return DelayBackoffType.Constant;
            return DelayBackoffType.Exponential;
        }

        private void Emit(ResilienceEvent evt)
        {
            foreach (var s in _sinks)
            {
                try
                {
                    s.OnEvent(evt);
                }
                catch
                {
                    // No romper el pipeline por fallos en el sink
                }
            }
        }
    }
}
