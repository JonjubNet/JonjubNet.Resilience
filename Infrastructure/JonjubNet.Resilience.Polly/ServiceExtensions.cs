using JonjubNet.Resilience.Abstractions;
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
        /// Agrega la implementación de resiliencia usando Polly.
        /// Registra <see cref="IResilienceClient"/> (Abstractions) e IResilienceService (legacy).
        /// </summary>
        /// <param name="services">Colección de servicios</param>
        /// <returns>Colección de servicios para chaining</returns>
        public static IServiceCollection AddPollyResilience(this IServiceCollection services)
        {
            services.AddSingleton<IDatabaseExceptionDetector, DatabaseExceptionDetector>();
            services.AddScoped<IResilienceService, ResilienceService>();
            services.AddScoped<IResilienceClient, ResilienceClient>();
            return services;
        }
    }
}

