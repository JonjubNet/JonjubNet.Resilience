using System.Collections.Generic;

namespace JonjubNet.Resilience.Core.Configuration
{
    /// <summary>
    /// Configuración genérica para patrones de resiliencia
    /// </summary>
    public class ResilienceConfiguration
    {
        public const string SectionName = "Resilience";

        public bool Enabled { get; set; } = true;
        public string ServiceName { get; set; } = string.Empty;
        public string Environment { get; set; } = string.Empty;

        /// <summary>
        /// Configuración de Circuit Breaker
        /// </summary>
        public CircuitBreakerConfiguration CircuitBreaker { get; set; } = new();

        /// <summary>
        /// Configuración de Retry
        /// </summary>
        public RetryConfiguration Retry { get; set; } = new();

        /// <summary>
        /// Configuración de Timeout
        /// </summary>
        public TimeoutConfiguration Timeout { get; set; } = new();

        /// <summary>
        /// Configuración de Bulkhead
        /// </summary>
        public BulkheadConfiguration Bulkhead { get; set; } = new();

        /// <summary>
        /// Configuración de Fallback
        /// </summary>
        public FallbackConfiguration Fallback { get; set; } = new();

        /// <summary>
        /// Configuración específica por servicio
        /// </summary>
        public Dictionary<string, ServiceResilienceConfiguration> Services { get; set; } = new();

        /// <summary>
        /// Configuración de pipelines nombrados
        /// </summary>
        public Dictionary<string, PipelineConfiguration> Pipelines { get; set; } = new();
    }

    /// <summary>
    /// Configuración de un pipeline de resiliencia nombrado
    /// </summary>
    public class PipelineConfiguration
    {
        public bool Enabled { get; set; } = true;
        
        /// <summary>
        /// Configuración de Retry para este pipeline
        /// </summary>
        public PipelineRetryConfiguration? Retry { get; set; }

        /// <summary>
        /// Configuración de Circuit Breaker para este pipeline
        /// </summary>
        public PipelineCircuitBreakerConfiguration? CircuitBreaker { get; set; }

        /// <summary>
        /// Configuración de Timeout para este pipeline
        /// </summary>
        public PipelineTimeoutConfiguration? Timeout { get; set; }
    }

    /// <summary>
    /// Configuración de Retry para un pipeline
    /// </summary>
    public class PipelineRetryConfiguration
    {
        public int MaxRetryAttempts { get; set; } = 3;
        public int DelaySeconds { get; set; } = 1;
        public int DelayMilliseconds { get; set; } = 0; // Si se especifica, tiene prioridad sobre DelaySeconds
        public string BackoffType { get; set; } = "Exponential"; // Exponential, Linear, Fixed
        public double JitterPercent { get; set; } = 0.1;
        public bool HandleDeadlocks { get; set; } = false;
        public bool HandleTimeouts { get; set; } = false;
    }

    /// <summary>
    /// Configuración de Circuit Breaker para un pipeline
    /// </summary>
    public class PipelineCircuitBreakerConfiguration
    {
        public bool Enabled { get; set; } = true;
        public double FailureRatio { get; set; } = 0.5;
        public int SamplingDurationSeconds { get; set; } = 10;
        public int MinimumThroughput { get; set; } = 5;
        public int BreakDurationSeconds { get; set; } = 30;
    }

    /// <summary>
    /// Configuración de Timeout para un pipeline
    /// </summary>
    public class PipelineTimeoutConfiguration
    {
        public bool Enabled { get; set; } = true;
        public int TimeoutSeconds { get; set; } = 30;
    }

    /// <summary>
    /// Configuración de Circuit Breaker
    /// </summary>
    public class CircuitBreakerConfiguration
    {
        public bool Enabled { get; set; } = true;
        public int FailureThreshold { get; set; } = 5;
        public int SamplingDurationSeconds { get; set; } = 30;
        public int MinimumThroughput { get; set; } = 2;
        public int DurationOfBreakSeconds { get; set; } = 60;
        public bool EnableAdvancedCircuitBreaker { get; set; } = false;
        public double FailureThresholdRatio { get; set; } = 0.5;
        public int MinimumThroughputForAdvanced { get; set; } = 10;
    }

    /// <summary>
    /// Configuración de Retry
    /// </summary>
    public class RetryConfiguration
    {
        public bool Enabled { get; set; } = true;
        public int MaxRetryAttempts { get; set; } = 3;
        public int BaseDelayMilliseconds { get; set; } = 1000;
        public int MaxDelayMilliseconds { get; set; } = 30000;
        public string BackoffStrategy { get; set; } = "Exponential"; // Linear, Exponential, Fixed
        public double JitterFactor { get; set; } = 0.1; // 0.0 to 1.0
        public List<int> RetryableStatusCodes { get; set; } = new() { 408, 429, 500, 502, 503, 504 };
        public List<string> RetryableExceptionTypes { get; set; } = new() 
        { 
            "HttpRequestException", 
            "TaskCanceledException", 
            "TimeoutException" 
        };
    }

    /// <summary>
    /// Configuración de Timeout
    /// </summary>
    public class TimeoutConfiguration
    {
        public bool Enabled { get; set; } = true;
        public int DefaultTimeoutSeconds { get; set; } = 30;
        public int DatabaseTimeoutSeconds { get; set; } = 15;
        public int ExternalApiTimeoutSeconds { get; set; } = 10;
        public int CacheTimeoutSeconds { get; set; } = 5;
        public bool EnableTimeoutPerOperation { get; set; } = true;
    }

    /// <summary>
    /// Configuración de Bulkhead
    /// </summary>
    public class BulkheadConfiguration
    {
        public bool Enabled { get; set; } = true;
        public int MaxConcurrency { get; set; } = 10;
        public int MaxQueuedActions { get; set; } = 20;
        public Dictionary<string, BulkheadServiceConfiguration> Services { get; set; } = new();
    }

    /// <summary>
    /// Configuración específica de Bulkhead por servicio
    /// </summary>
    public class BulkheadServiceConfiguration
    {
        public int MaxConcurrency { get; set; } = 5;
        public int MaxQueuedActions { get; set; } = 10;
    }

    /// <summary>
    /// Configuración de Fallback
    /// </summary>
    public class FallbackConfiguration
    {
        public bool Enabled { get; set; } = true;
        public bool EnableCacheFallback { get; set; } = true;
        public bool EnableDefaultResponseFallback { get; set; } = true;
        public int CacheFallbackTtlSeconds { get; set; } = 300; // 5 minutes
        public Dictionary<string, FallbackServiceConfiguration> Services { get; set; } = new();
    }

    /// <summary>
    /// Configuración específica de Fallback por servicio
    /// </summary>
    public class FallbackServiceConfiguration
    {
        public bool EnableCacheFallback { get; set; } = true;
        public bool EnableDefaultResponse { get; set; } = true;
        public string DefaultResponseJson { get; set; } = string.Empty;
        public int CacheTtlSeconds { get; set; } = 300;
    }

    /// <summary>
    /// Configuración de resiliencia específica por servicio
    /// </summary>
    public class ServiceResilienceConfiguration
    {
        public bool Enabled { get; set; } = true;
        public CircuitBreakerConfiguration CircuitBreaker { get; set; } = new();
        public RetryConfiguration Retry { get; set; } = new();
        public TimeoutConfiguration Timeout { get; set; } = new();
        public BulkheadServiceConfiguration Bulkhead { get; set; } = new();
        public FallbackServiceConfiguration Fallback { get; set; } = new();
    }
}

