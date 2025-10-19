using JonjubNet.Resilience.Interfaces;
using JonjubNet.Resilience.Configuration;
using JonjubNet.Resilience.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http;
using Polly;
using Polly.Extensions.Http;
using System;
using System.Net.Http;

namespace JonjubNet.Resilience
{
    public static class ServiceExtensions
    {
        /// <summary>
        /// Agrega la infraestructura de resiliencia al contenedor de dependencias
        /// </summary>
        /// <param name="services">Colección de servicios</param>
        /// <param name="configuration">Configuración de la aplicación</param>
        /// <returns>Colección de servicios para chaining</returns>
        public static IServiceCollection AddResilienceInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            // Configurar opciones de resiliencia
            services.Configure<ResilienceConfiguration>(configuration.GetSection(ResilienceConfiguration.SectionName));

            // Registrar el servicio de resiliencia
            services.AddScoped<IResilienceService, ResilienceService>();

            // Configurar HttpClient con políticas de resiliencia
            services.AddHttpClient("ResilientHttpClient");

            return services;
        }

        /// <summary>
        /// Agrega la infraestructura de resiliencia con configuración personalizada
        /// </summary>
        /// <param name="services">Colección de servicios</param>
        /// <param name="configuration">Configuración de la aplicación</param>
        /// <param name="configureOptions">Acción para configurar opciones adicionales</param>
        /// <returns>Colección de servicios para chaining</returns>
        public static IServiceCollection AddResilienceInfrastructure(this IServiceCollection services, IConfiguration configuration, Action<ResilienceConfiguration> configureOptions)
        {
            // Configurar opciones de resiliencia
            services.Configure<ResilienceConfiguration>(configuration.GetSection(ResilienceConfiguration.SectionName));
            services.Configure(configureOptions);

            // Registrar el servicio de resiliencia
            services.AddScoped<IResilienceService, ResilienceService>();

            // Configurar HttpClient con políticas de resiliencia
            services.AddHttpClient("ResilientHttpClient");

            return services;
        }

    }
}