using JonjubNet.Resilience.Core.Configuration;
using JonjubNet.Resilience.Core.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace JonjubNet.Resilience.Polly.Services
{
    /// <summary>
    /// Cliente de resiliencia que ejecuta operaciones usando pipelines nombrados
    /// </summary>
    public class ResilienceClient : IResilienceClient
    {
        private const int MaxPipelineCacheSize = 100;
        private const string ResilienceCategory = "Resilience";

        private readonly ILogger<ResilienceClient> _logger;
        private readonly ResilienceConfiguration _configuration;
        private readonly IDatabaseExceptionDetector _exceptionDetector;
        private readonly ConcurrentDictionary<string, ResiliencePipeline> _pipelines = new();
        private long _pipelineCount = 0;

        public ResilienceClient(
            ILogger<ResilienceClient> logger,
            IOptions<ResilienceConfiguration> configuration,
            IDatabaseExceptionDetector? exceptionDetector = null)
        {
            _logger = logger;
            _configuration = configuration.Value;
            _exceptionDetector = exceptionDetector ?? new DatabaseExceptionDetector();
        }

        /// <summary>
        /// Ejecuta una operación con resiliencia usando un pipeline nombrado
        /// </summary>
        public async Task<T> ExecuteAsync<T>(
            string pipelineName,
            Func<CancellationToken, Task<T>> operation,
            CancellationToken cancellationToken = default)
        {
            if (!_configuration.Enabled)
            {
                return await operation(cancellationToken);
            }

            var pipeline = GetOrCreatePipeline(pipelineName);
            
            try
            {
                _logger.LogInformation(
                    "Executing operation with pipeline '{PipelineName}'. Resilience enabled: {ResilienceEnabled}",
                    pipelineName,
                    _configuration.Enabled);

                var result = await pipeline.ExecuteAsync(async (ct) =>
                {
                    return await operation(ct);
                }, cancellationToken);

                _logger.LogInformation(
                    "Operation with pipeline '{PipelineName}' completed successfully",
                    pipelineName);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Operation with pipeline '{PipelineName}' failed",
                    pipelineName);

                throw;
            }
        }

        /// <summary>
        /// Ejecuta una operación sin retorno con resiliencia usando un pipeline nombrado
        /// </summary>
        public async Task ExecuteAsync(
            string pipelineName,
            Func<CancellationToken, Task> operation,
            CancellationToken cancellationToken = default)
        {
            await ExecuteAsync(pipelineName, async (ct) =>
            {
                await operation(ct);
                return true;
            }, cancellationToken);
        }

        private ResiliencePipeline GetOrCreatePipeline(string pipelineName)
        {
            // Intentar obtener pipeline existente (thread-safe)
            if (_pipelines.TryGetValue(pipelineName, out var existingPipeline))
            {
                return existingPipeline;
            }

            // Verificar límite de memoria antes de crear nuevo pipeline
            var currentCount = Interlocked.Read(ref _pipelineCount);
            if (currentCount >= MaxPipelineCacheSize)
            {
                _logger.LogWarning(
                    "Pipeline cache limit reached ({MaxSize}). Using default pipeline for '{PipelineName}'",
                    MaxPipelineCacheSize,
                    pipelineName);
                return CreateDefaultPipeline();
            }

            // Crear pipeline basado en configuración
            var pipeline = CreatePipelineFromConfiguration(pipelineName);
            if (_pipelines.TryAdd(pipelineName, pipeline))
            {
                Interlocked.Increment(ref _pipelineCount);
                return pipeline;
            }

            // Si otro thread ya lo creó, usar el existente
            return _pipelines[pipelineName];
        }

        private ResiliencePipeline CreatePipelineFromConfiguration(string pipelineName)
        {
            // Buscar configuración del pipeline
            if (!_configuration.Pipelines.TryGetValue(pipelineName, out var pipelineConfig))
            {
                _logger.LogWarning(
                    "Pipeline '{PipelineName}' not found in configuration. Using default pipeline.",
                    pipelineName);
                return CreateDefaultPipeline();
            }

            if (!pipelineConfig.Enabled)
            {
                _logger.LogInformation(
                    "Pipeline '{PipelineName}' is disabled. Using default pipeline.",
                    pipelineName);
                return CreateDefaultPipeline();
            }

            var pipelineBuilder = new ResiliencePipelineBuilder();

            // Configurar Retry
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
                            // Si HandleDeadlocks o HandleTimeouts están habilitados, usar el detector
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
                        _logger.LogWarning(
                            "Retry attempt {RetryAttempt} for pipeline '{PipelineName}' after {Delay}ms. Reason: {Reason}",
                            args.AttemptNumber,
                            args.RetryDelay.TotalMilliseconds,
                            pipelineName,
                            args.Outcome.Exception?.Message);
                        return ValueTask.CompletedTask;
                    }
                });
            }

            // Configurar Circuit Breaker
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
                        _logger.LogError(
                            "Circuit breaker opened for pipeline '{PipelineName}' for {Duration}ms. Reason: {Reason}",
                            pipelineName,
                            args.BreakDuration.TotalMilliseconds,
                            args.Outcome.Exception?.Message);
                        return ValueTask.CompletedTask;
                    },
                    OnClosed = args =>
                    {
                        _logger.LogInformation("Circuit breaker closed for pipeline '{PipelineName}' - service is healthy again", pipelineName);
                        return ValueTask.CompletedTask;
                    },
                    OnHalfOpened = args =>
                    {
                        _logger.LogInformation("Circuit breaker half-opened for pipeline '{PipelineName}' - testing service health", pipelineName);
                        return ValueTask.CompletedTask;
                    }
                });
            }

            // Configurar Timeout
            if (pipelineConfig.Timeout != null && pipelineConfig.Timeout.Enabled)
            {
                pipelineBuilder.AddTimeout(TimeSpan.FromSeconds(pipelineConfig.Timeout.TimeoutSeconds));
            }

            return pipelineBuilder.Build();
        }

        private ResiliencePipeline CreateDefaultPipeline()
        {
            var pipelineBuilder = new ResiliencePipelineBuilder();

            // Usar configuración global si no hay configuración específica del pipeline
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
    }
}
