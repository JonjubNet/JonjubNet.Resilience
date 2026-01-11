using JonjubNet.Resilience.Core.Interfaces;
using JonjubNet.Resilience.Core.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace JonjubNet.Resilience.Polly.Services
{
    /// <summary>
    /// Implementación de servicio de resiliencia usando Polly
    /// </summary>
    public class ResilienceService : IResilienceService
    {
        // Constantes para evitar allocations y mejorar string interning
        private const string DefaultServiceName = "Default";
        private const string HttpClientServiceName = "HttpClient";
        private const string DatabaseServiceName = "Database";
        private const string CacheServiceName = "Cache";
        private const string ResilienceCategory = "Resilience";
        private const int MaxPipelineCacheSize = 100; // Límite para evitar desbordamiento de memoria

        private readonly ILogger<ResilienceService> _logger;
        private readonly ResilienceConfiguration _configuration;
        private readonly IStructuredLoggingService _loggingService;
        private readonly IDatabaseExceptionDetector _exceptionDetector;
        private readonly ConcurrentDictionary<string, ResiliencePipeline> _pipelines = new();
        private long _pipelineCount = 0; // Contador thread-safe para límite de memoria

        public ResilienceService(
            ILogger<ResilienceService> logger,
            IOptions<ResilienceConfiguration> configuration,
            IStructuredLoggingService loggingService,
            IDatabaseExceptionDetector? exceptionDetector = null)
        {
            _logger = logger;
            _configuration = configuration.Value;
            _loggingService = loggingService;
            _exceptionDetector = exceptionDetector ?? new DatabaseExceptionDetector();
            InitializePipelines();
        }

        /// <summary>
        /// Ejecuta una operación con todos los patrones de resiliencia aplicados
        /// </summary>
        public async Task<T> ExecuteWithResilienceAsync<T>(
            Func<Task<T>> operation,
            string operationName,
            string serviceName = DefaultServiceName,
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
                    ResilienceCategory,
                    enrichedContext);

                var result = await pipeline.ExecuteAsync(async (cancellationToken) =>
                {
                    return await operation();
                });

                _loggingService.LogInformation(
                    $"Operation '{operationName}' completed successfully",
                    operationName,
                    ResilienceCategory,
                    enrichedContext);

                return result;
            }
            catch (Exception ex)
            {
                _loggingService.LogError(
                    $"Operation '{operationName}' failed after applying resilience patterns: {ex.Message}",
                    operationName,
                    ResilienceCategory,
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
            string serviceName = HttpClientServiceName,
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
                DatabaseServiceName,
                context);
        }

        /// <summary>
        /// Ejecuta una operación con fallback
        /// </summary>
        public async Task<T> ExecuteWithFallbackAsync<T>(
            Func<Task<T>> primaryOperation,
            Func<Task<T>> fallbackOperation,
            string operationName,
            string serviceName = DefaultServiceName,
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
                            ResilienceCategory,
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
                    ResilienceCategory,
                    null,
                    enrichedContext,
                    ex);

                throw;
            }
        }

        private void InitializePipelines()
        {
            // Pipelines predefinidos (thread-safe con ConcurrentDictionary)
            _pipelines.TryAdd(DefaultServiceName, CreateDefaultPipeline());
            _pipelines.TryAdd(HttpClientServiceName, CreateHttpClientPipeline());
            _pipelines.TryAdd(DatabaseServiceName, CreateDatabasePipeline());
            _pipelines.TryAdd(CacheServiceName, CreateCachePipeline());
            
            // Inicializar contador
            Interlocked.Exchange(ref _pipelineCount, (long)_pipelines.Count);
        }

        private ResiliencePipeline CreateDefaultPipeline()
        {
            var pipelineBuilder = new ResiliencePipelineBuilder();

            // Agregar retry
            if (_configuration.Retry.Enabled)
            {
                pipelineBuilder.AddRetry(new RetryStrategyOptions
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
                pipelineBuilder.AddCircuitBreaker(new CircuitBreakerStrategyOptions
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
                pipelineBuilder.AddRetry(new RetryStrategyOptions
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
                pipelineBuilder.AddCircuitBreaker(new CircuitBreakerStrategyOptions
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

        /// <summary>
        /// Crea un pipeline de resiliencia optimizado para operaciones de base de datos
        /// Soporta: SQL Server, PostgreSQL, MySQL, Oracle
        /// </summary>
        private ResiliencePipeline CreateDatabasePipeline()
        {
            var pipelineBuilder = new ResiliencePipelineBuilder();

            // Retry para Database con manejo específico de excepciones
            if (_configuration.Retry.Enabled)
            {
                pipelineBuilder.AddRetry(new RetryStrategyOptions
                {
                    ShouldHandle = new PredicateBuilder()
                        .Handle<Exception>(ex => _exceptionDetector.IsTransient(ex)),
                    MaxRetryAttempts = _configuration.Retry.MaxRetryAttempts,
                    Delay = TimeSpan.FromMilliseconds(_configuration.Retry.BaseDelayMilliseconds),
                    MaxDelay = TimeSpan.FromMilliseconds(_configuration.Retry.MaxDelayMilliseconds),
                    BackoffType = GetBackoffType(_configuration.Retry.BackoffStrategy),
                    OnRetry = args =>
                    {
                        var exceptionType = args.Outcome.Exception?.GetType().Name ?? "Unknown";
                        _logger.LogWarning(
                            "Database retry attempt {RetryAttempt} for operation after {Delay}ms. Exception: {ExceptionType}, Reason: {Reason}",
                            args.AttemptNumber,
                            args.RetryDelay.TotalMilliseconds,
                            exceptionType,
                            args.Outcome.Exception?.Message);
                        return ValueTask.CompletedTask;
                    }
                });
            }

            // Circuit Breaker para Database
            if (_configuration.CircuitBreaker.Enabled)
            {
                pipelineBuilder.AddCircuitBreaker(new CircuitBreakerStrategyOptions
                {
                    ShouldHandle = new PredicateBuilder()
                        .Handle<Exception>(ex => _exceptionDetector.IsTransient(ex) || _exceptionDetector.IsConnectionException(ex)),
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
                            "Database circuit breaker opened for {Duration}ms. Reason: {Reason}",
                            args.BreakDuration.TotalMilliseconds,
                            args.Outcome.Exception?.Message);
                        return ValueTask.CompletedTask;
                    },
                    OnClosed = args =>
                    {
                        _logger.LogInformation("Database circuit breaker closed - database is healthy again");
                        return ValueTask.CompletedTask;
                    },
                    OnHalfOpened = args =>
                    {
                        _logger.LogInformation("Database circuit breaker half-opened - testing database health");
                        return ValueTask.CompletedTask;
                    }
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
                pipelineBuilder.AddRetry(new RetryStrategyOptions
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
            // Intentar obtener pipeline existente (thread-safe)
            if (_pipelines.TryGetValue(serviceName, out var existingPipeline))
            {
                return existingPipeline;
            }

            // Verificar límite de memoria antes de crear nuevo pipeline
            var currentCount = Interlocked.Read(ref _pipelineCount);
            if (currentCount >= MaxPipelineCacheSize)
            {
                _logger.LogWarning(
                    "Pipeline cache limit reached ({MaxSize}). Using default pipeline for '{ServiceName}'",
                    MaxPipelineCacheSize,
                    serviceName);
                return _pipelines[DefaultServiceName];
            }

            // Crear pipeline personalizado solo si no existe (evitar race condition)
            var customPipeline = CreateDefaultPipeline();
            if (_pipelines.TryAdd(serviceName, customPipeline))
            {
                Interlocked.Increment(ref _pipelineCount);
                return customPipeline;
            }

            // Si otro thread ya lo creó, usar el existente
            return _pipelines[serviceName];
        }

        private static DelayBackoffType GetBackoffType(string strategy)
        {
            // Usar StringComparison.OrdinalIgnoreCase para evitar allocations de ToLowerInvariant
            if (string.Equals(strategy, "exponential", StringComparison.OrdinalIgnoreCase))
                return DelayBackoffType.Exponential;
            if (string.Equals(strategy, "linear", StringComparison.OrdinalIgnoreCase))
                return DelayBackoffType.Linear;
            if (string.Equals(strategy, "fixed", StringComparison.OrdinalIgnoreCase))
                return DelayBackoffType.Constant;
            
            return DelayBackoffType.Exponential;
        }

        private Dictionary<string, object> EnrichContext(
            Dictionary<string, object>? context,
            string operationName,
            string serviceName)
        {
            // Reutilizar contexto existente si está disponible para evitar allocations
            if (context != null)
            {
                context["OperationName"] = operationName;
                context["ServiceName"] = serviceName;
                context["ResilienceEnabled"] = _configuration.Enabled;
                context["Timestamp"] = DateTime.UtcNow;
                return context;
            }

            // Crear nuevo contexto solo si es necesario (capacidad inicial optimizada)
            var enrichedContext = new Dictionary<string, object>(4)
            {
                ["OperationName"] = operationName,
                ["ServiceName"] = serviceName,
                ["ResilienceEnabled"] = _configuration.Enabled,
                ["Timestamp"] = DateTime.UtcNow
            };
            return enrichedContext;
        }
    }
}

