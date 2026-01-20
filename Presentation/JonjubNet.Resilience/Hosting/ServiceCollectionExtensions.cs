using JonjubNet.Resilience.Core.Configuration;
using JonjubNet.Resilience.Polly;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace JonjubNet.Resilience.Hosting
{
    /// <summary>
    /// Extensiones para registrar la infraestructura de resiliencia de JonjubNet
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Agrega la infraestructura de resiliencia de JonjubNet al contenedor de dependencias
        /// Lee la configuración de "JonjubNet:Resilience" en appsettings.json
        /// </summary>
        /// <param name="services">Colección de servicios</param>
        /// <param name="configuration">Configuración de la aplicación</param>
        /// <returns>Colección de servicios para chaining</returns>
        public static IServiceCollection AddJonjubNetResilience(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            // Leer configuración de "JonjubNet:Resilience"
            services.Configure<ResilienceConfiguration>(
                configuration.GetSection("JonjubNet:Resilience"));

            // Registrar la implementación de Polly (incluye IResilienceService e IResilienceClient)
            services.AddPollyResilience();

            return services;
        }
    }
}
