using JonjubNet.Resilience.Interfaces;
using JonjubNet.Resilience.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace JonjubNet.Resilience.Services
{
    /// <summary>
    /// Servicio genérico de resiliencia que implementa múltiples patrones
    /// </summary>
    public class ResilienceService : IResilienceService
    {
        private readonly ILogger<ResilienceService> _logger;
        private readonly ResilienceConfiguration _configuration;
        private readonly IStructuredLoggingService _loggingService;
        private readonly Dictionary<string, ResiliencePipeline> _pipelines = new();

        public ResilienceService(
            ILogger<ResilienceService> logger,
            IOptions<ResilienceConfiguration> configuration,
            IStructuredLoggingService loggingService)
        {
            _logger = logger;
            _configuration = configuration.Value;
            _loggingService = loggingService;
            InitializePipelines();
        }

        /// <summary>
        /// Ejecuta una operación con todos los patrones de resiliencia aplicados
        /// </summary>
        public async Task<T> ExecuteWithResilienceAsync<T>(
            Func<Task<T>> operation,
            string operationName,
            string serviceName = "Default",
            Dictionary<string, object>? context = null)
        {
            if (!_configuration.Enabled)
            {
                return await operation();
            }

            var pipeline = GetOrCreatePipeline(serviceName);
            var enrichedContext = EnrichContext(context, operationName, serviceName);

            try
            {
                _loggingService.LogInformation(
                    $"Executing operation '{operationName}' with resilience patterns",
                    operationName,
                    "Resilience",
                    enrichedContext);

                var result = await pipeline.ExecuteAsync(async (cancellationToken) =>
                {
                    return await operation();
                });

                _loggingService.LogInformation(
                    $"Operation '{operationName}' completed successfully",
                    operationName,
                    "Resilience",
                    enrichedContext);

                return result;
            }
            catch (Exception ex)
            {
                _loggingService.LogError(
                    $"Operation '{operationName}' failed after applying resilience patterns: {ex.Message}",
                    operationName,
                    "Resilience",
                    null,
                    enrichedContext,
                    ex);

                throw;
            }
        }

        /// <summary>
        /// Ejecuta una operación HTTP con resiliencia
        /// </summary>
        public async Task<HttpResponseMessage> ExecuteHttpWithResilienceAsync(
            Func<Task<HttpResponseMessage>> httpOperation,
            string operationName,
            string serviceName = "HttpClient",
            Dictionary<string, object>? context = null)
        {
            return await ExecuteWithResilienceAsync(
                httpOperation,
                operationName,
                serviceName,
                context);
        }

        /// <summary>
        /// Ejecuta una operación de base de datos con resiliencia
        /// </summary>
        public async Task<T> ExecuteDatabaseWithResilienceAsync<T>(
            Func<Task<T>> databaseOperation,
            string operationName,
            Dictionary<string, object>? context = null)
        {
            return await ExecuteWithResilienceAsync(
                databaseOperation,
                operationName,
                "Database",
                context);
        }

        /// <summary>
        /// Ejecuta una operación con fallback
        /// </summary>
        public async Task<T> ExecuteWithFallbackAsync<T>(
            Func<Task<T>> primaryOperation,
            Func<Task<T>> fallbackOperation,
            string operationName,
            string serviceName = "Default",
            Dictionary<string, object>? context = null)
        {
            if (!_configuration.Enabled || !_configuration.Fallback.Enabled)
            {
                try
                {
                    return await primaryOperation();
                }
                catch
                {
                    return await fallbackOperation();
                }
            }

            var pipeline = GetOrCreatePipeline(serviceName);
            var enrichedContext = EnrichContext(context, operationName, serviceName);

            try
            {
                return await pipeline.ExecuteAsync(async (cancellationToken) =>
                {
                    try
                    {
                        return await primaryOperation();
                    }
                    catch (Exception ex)
                    {
                        _loggingService.LogWarning(
                            $"Primary operation '{operationName}' failed, attempting fallback: {ex.Message}",
                            operationName,
                            "Resilience",
                            null,
                            enrichedContext,
                            ex);

                        return await fallbackOperation();
                    }
                });
            }
            catch (Exception ex)
            {
                _loggingService.LogError(
                    $"Both primary and fallback operations failed for '{operationName}': {ex.Message}",
                    operationName,
                    "Resilience",
                    null,
                    enrichedContext,
                    ex);

                throw;
            }
        }

        private void InitializePipelines()
        {
            // Pipeline por defecto
            _pipelines["Default"] = CreateDefaultPipeline();

            // Pipeline para HttpClient
            _pipelines["HttpClient"] = CreateHttpClientPipeline();

            // Pipeline para Database
            _pipelines["Database"] = CreateDatabasePipeline();

            // Pipeline para Cache
            _pipelines["Cache"] = CreateCachePipeline();
        }

        private ResiliencePipeline CreateDefaultPipeline()
        {
            var pipelineBuilder = new ResiliencePipelineBuilder();

            // Agregar retry
            if (_configuration.Retry.Enabled)
            {
                pipelineBuilder.AddRetry(new Polly.Retry.RetryStrategyOptions
                {
                    ShouldHandle = new PredicateBuilder().Handle<HttpRequestException>()
                                                         .Handle<TaskCanceledException>()
                                                         .Handle<TimeoutException>(),
                    MaxRetryAttempts = _configuration.Retry.MaxRetryAttempts,
                    Delay = TimeSpan.FromMilliseconds(_configuration.Retry.BaseDelayMilliseconds),
                    MaxDelay = TimeSpan.FromMilliseconds(_configuration.Retry.MaxDelayMilliseconds),
                    BackoffType = GetBackoffType(_configuration.Retry.BackoffStrategy),
                    OnRetry = args =>
                    {
                        _logger.LogWarning(
                            "Retry attempt {RetryAttempt} for operation after {Delay}ms. Reason: {Reason}",
                            args.AttemptNumber,
                            args.RetryDelay.TotalMilliseconds,
                            args.Outcome.Exception?.Message);
                        return ValueTask.CompletedTask;
                    }
                });
            }

            // Agregar circuit breaker
            if (_configuration.CircuitBreaker.Enabled)
            {
                pipelineBuilder.AddCircuitBreaker(new Polly.CircuitBreaker.CircuitBreakerStrategyOptions
                {
                    ShouldHandle = new PredicateBuilder().Handle<HttpRequestException>()
                                                         .Handle<TaskCanceledException>()
                                                         .Handle<TimeoutException>(),
                    FailureRatio = _configuration.CircuitBreaker.EnableAdvancedCircuitBreaker 
                        ? _configuration.CircuitBreaker.FailureThresholdRatio 
                        : 0.5,
                    SamplingDuration = TimeSpan.FromSeconds(_configuration.CircuitBreaker.SamplingDurationSeconds),
                    MinimumThroughput = _configuration.CircuitBreaker.EnableAdvancedCircuitBreaker 
                        ? _configuration.CircuitBreaker.MinimumThroughputForAdvanced 
                        : _configuration.CircuitBreaker.MinimumThroughput,
                    BreakDuration = TimeSpan.FromSeconds(_configuration.CircuitBreaker.DurationOfBreakSeconds),
                    OnOpened = args =>
                    {
                        _logger.LogError(
                            "Circuit breaker opened for {Duration}ms. Reason: {Reason}",
                            args.BreakDuration.TotalMilliseconds,
                            args.Outcome.Exception?.Message);
                        return ValueTask.CompletedTask;
                    },
                    OnClosed = args =>
                    {
                        _logger.LogInformation("Circuit breaker closed - service is healthy again");
                        return ValueTask.CompletedTask;
                    },
                    OnHalfOpened = args =>
                    {
                        _logger.LogInformation("Circuit breaker half-opened - testing service health");
                        return ValueTask.CompletedTask;
                    }
                });
            }

            // Agregar timeout
            if (_configuration.Timeout.Enabled)
            {
                pipelineBuilder.AddTimeout(TimeSpan.FromSeconds(_configuration.Timeout.DefaultTimeoutSeconds));
            }

            return pipelineBuilder.Build();
        }

        private ResiliencePipeline CreateHttpClientPipeline()
        {
            var pipelineBuilder = new ResiliencePipelineBuilder();

            // Retry para HTTP
            if (_configuration.Retry.Enabled)
            {
                pipelineBuilder.AddRetry(new Polly.Retry.RetryStrategyOptions
                {
                    ShouldHandle = new PredicateBuilder().Handle<HttpRequestException>()
                                                         .Handle<TaskCanceledException>(),
                    MaxRetryAttempts = _configuration.Retry.MaxRetryAttempts,
                    Delay = TimeSpan.FromMilliseconds(_configuration.Retry.BaseDelayMilliseconds),
                    MaxDelay = TimeSpan.FromMilliseconds(_configuration.Retry.MaxDelayMilliseconds),
                    BackoffType = GetBackoffType(_configuration.Retry.BackoffStrategy)
                });
            }

            // Circuit breaker para HTTP
            if (_configuration.CircuitBreaker.Enabled)
            {
                pipelineBuilder.AddCircuitBreaker(new Polly.CircuitBreaker.CircuitBreakerStrategyOptions
                {
                    ShouldHandle = new PredicateBuilder().Handle<HttpRequestException>()
                                                         .Handle<TaskCanceledException>(),
                    FailureRatio = 0.5,
                    SamplingDuration = TimeSpan.FromSeconds(30),
                    MinimumThroughput = 2,
                    BreakDuration = TimeSpan.FromSeconds(60)
                });
            }

            // Timeout para HTTP
            if (_configuration.Timeout.Enabled)
            {
                pipelineBuilder.AddTimeout(TimeSpan.FromSeconds(_configuration.Timeout.ExternalApiTimeoutSeconds));
            }

            return pipelineBuilder.Build();
        }

        private ResiliencePipeline CreateDatabasePipeline()
        {
            var pipelineBuilder = new ResiliencePipelineBuilder();

            // Retry para Database
            if (_configuration.Retry.Enabled)
            {
                pipelineBuilder.AddRetry(new Polly.Retry.RetryStrategyOptions
                {
                    ShouldHandle = new PredicateBuilder().Handle<Exception>(),
                    MaxRetryAttempts = _configuration.Retry.MaxRetryAttempts,
                    Delay = TimeSpan.FromMilliseconds(_configuration.Retry.BaseDelayMilliseconds),
                    MaxDelay = TimeSpan.FromMilliseconds(_configuration.Retry.MaxDelayMilliseconds),
                    BackoffType = GetBackoffType(_configuration.Retry.BackoffStrategy)
                });
            }

            // Timeout para Database
            if (_configuration.Timeout.Enabled)
            {
                pipelineBuilder.AddTimeout(TimeSpan.FromSeconds(_configuration.Timeout.DatabaseTimeoutSeconds));
            }

            return pipelineBuilder.Build();
        }

        private ResiliencePipeline CreateCachePipeline()
        {
            var pipelineBuilder = new ResiliencePipelineBuilder();

            // Retry para Cache
            if (_configuration.Retry.Enabled)
            {
                pipelineBuilder.AddRetry(new Polly.Retry.RetryStrategyOptions
                {
                    ShouldHandle = new PredicateBuilder().Handle<Exception>(),
                    MaxRetryAttempts = 2, // Menos reintentos para cache
                    Delay = TimeSpan.FromMilliseconds(500),
                    MaxDelay = TimeSpan.FromMilliseconds(2000),
                    BackoffType = DelayBackoffType.Exponential
                });
            }

            // Timeout para Cache
            if (_configuration.Timeout.Enabled)
            {
                pipelineBuilder.AddTimeout(TimeSpan.FromSeconds(_configuration.Timeout.CacheTimeoutSeconds));
            }

            return pipelineBuilder.Build();
        }

        private ResiliencePipeline GetOrCreatePipeline(string serviceName)
        {
            if (_pipelines.TryGetValue(serviceName, out var existingPipeline))
            {
                return existingPipeline;
            }

            // Crear pipeline personalizado para el servicio
            var customPipeline = CreateDefaultPipeline();
            _pipelines[serviceName] = customPipeline;
            return customPipeline;
        }

        private DelayBackoffType GetBackoffType(string strategy)
        {
            return strategy.ToLowerInvariant() switch
            {
                "exponential" => DelayBackoffType.Exponential,
                "linear" => DelayBackoffType.Linear,
                "fixed" => DelayBackoffType.Constant,
                _ => DelayBackoffType.Exponential
            };
        }

        private Dictionary<string, object> EnrichContext(
            Dictionary<string, object>? context,
            string operationName,
            string serviceName)
        {
            var enrichedContext = context ?? new Dictionary<string, object>();
            enrichedContext["OperationName"] = operationName;
            enrichedContext["ServiceName"] = serviceName;
            enrichedContext["ResilienceEnabled"] = _configuration.Enabled;
            enrichedContext["Timestamp"] = DateTime.UtcNow;
            return enrichedContext;
        }
    }
}