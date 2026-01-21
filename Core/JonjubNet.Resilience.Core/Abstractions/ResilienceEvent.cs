using System;

namespace JonjubNet.Resilience.Abstractions
{
    /// <summary>
    /// Evento emitido por el componente de resiliencia. Opcional: se envía a <see cref="IResilienceEventSink"/> si está registrado.
    /// El microservicio puede implementar el sink y enviar logs/métricas/traces a su stack de observabilidad.
    /// </summary>
    public sealed class ResilienceEvent
    {
        /// <summary>Tipo de evento: Retry, CircuitBreakerOpened, CircuitBreakerClosed, CircuitBreakerHalfOpened, Timeout, OperationStarted, OperationSucceeded, OperationFailed.</summary>
        public string EventType { get; init; } = string.Empty;

        /// <summary>Nombre del pipeline.</summary>
        public string PipelineName { get; init; } = string.Empty;

        /// <summary>Mensaje descriptivo.</summary>
        public string Message { get; init; } = string.Empty;

        /// <summary>Excepción asociada, si aplica.</summary>
        public Exception? Exception { get; init; }

        /// <summary>Momento del evento.</summary>
        public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;

        /// <summary>Duración de la operación, si aplica.</summary>
        public TimeSpan? Duration { get; init; }
    }
}
