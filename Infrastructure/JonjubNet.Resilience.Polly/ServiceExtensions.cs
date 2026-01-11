using JonjubNet.Resilience.Core.Interfaces;
using JonjubNet.Resilience.Polly.Services;
using Microsoft.Extensions.DependencyInjection;

namespace JonjubNet.Resilience.Polly
{
    /// <summary>
    /// Extensiones para registrar la implementación de Polly
    /// </summary>
    public static class ServiceExtensions
    {
        /// <summary>
        /// Agrega la implementación de resiliencia usando Polly
        /// </summary>
        /// <param name="services">Colección de servicios</param>
        /// <returns>Colección de servicios para chaining</returns>
        public static IServiceCollection AddPollyResilience(this IServiceCollection services)
        {
            // Registrar el detector de excepciones
            services.AddSingleton<IDatabaseExceptionDetector, DatabaseExceptionDetector>();

            // Registrar el servicio de resiliencia
            services.AddScoped<IResilienceService, ResilienceService>();

            return services;
        }
    }
}

