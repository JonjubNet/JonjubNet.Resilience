namespace JonjubNet.Resilience.Abstractions
{
    /// <summary>
    /// Abstracción opcional para recibir eventos de resiliencia. El componente funciona sin sink (null/no registrado).
    /// El microservicio puede implementar esta interfaz y registrarla en DI para enviar logs/métricas/traces a su stack (ej. JonjubNet.Observability).
    /// </summary>
    public interface IResilienceEventSink
    {
        /// <summary>
        /// Recibe un evento de resiliencia. La implementación no debe bloquear; puede encolar para procesamiento asíncrono.
        /// </summary>
        /// <param name="evt">Evento emitido por el componente.</param>
        void OnEvent(ResilienceEvent evt);
    }
}
