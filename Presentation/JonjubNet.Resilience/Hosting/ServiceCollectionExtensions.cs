using JonjubNet.Resilience.Core.Configuration;
using JonjubNet.Resilience.Polly;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace JonjubNet.Resilience.Hosting
{
    /// <summary>
    /// Extensiones para registrar la infraestructura de resiliencia de JonjubNet.
    /// El microservicio solo orquesta (DI + inyectar IResilienceClient); retry, circuit breaker y timeout viven en el componente.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Agrega la infraestructura de resiliencia de JonjubNet al contenedor de dependencias.
        /// Lee la configuración de "JonjubNet:Resilience" (Pipelines, Retry, CircuitBreaker, Timeout) en appsettings.
        /// Registra IResilienceClient (JonjubNet.Resilience.Abstractions) listo para inyectar. Opcional: registre IResilienceEventSink para eventos.
        /// </summary>
        /// <param name="services">Colección de servicios</param>
        /// <param name="configuration">Configuración de la aplicación</param>
        /// <returns>Colección de servicios para chaining</returns>
        public static IServiceCollection AddJonjubNetResilience(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            services.Configure<ResilienceConfiguration>(
                configuration.GetSection("JonjubNet:Resilience"));
            services.AddPollyResilience();
            return services;
        }
    }
}
