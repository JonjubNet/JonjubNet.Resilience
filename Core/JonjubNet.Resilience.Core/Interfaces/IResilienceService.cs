using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace JonjubNet.Resilience.Core.Interfaces
{
    /// <summary>
    /// Interfaz para el servicio de resiliencia genérico
    /// </summary>
    public interface IResilienceService
    {
        /// <summary>
        /// Ejecuta una operación con todos los patrones de resiliencia aplicados
        /// </summary>
        /// <typeparam name="T">Tipo de retorno de la operación</typeparam>
        /// <param name="operation">Operación a ejecutar</param>
        /// <param name="operationName">Nombre de la operación para logging</param>
        /// <param name="serviceName">Nombre del servicio (opcional)</param>
        /// <param name="context">Contexto adicional para logging (opcional)</param>
        /// <returns>Resultado de la operación</returns>
        Task<T> ExecuteWithResilienceAsync<T>(
            Func<Task<T>> operation,
            string operationName,
            string serviceName = "Default",
            Dictionary<string, object>? context = null);

        /// <summary>
        /// Ejecuta una operación HTTP con resiliencia
        /// </summary>
        /// <param name="httpOperation">Operación HTTP a ejecutar</param>
        /// <param name="operationName">Nombre de la operación para logging</param>
        /// <param name="serviceName">Nombre del servicio (opcional)</param>
        /// <param name="context">Contexto adicional para logging (opcional)</param>
        /// <returns>Respuesta HTTP</returns>
        Task<HttpResponseMessage> ExecuteHttpWithResilienceAsync(
            Func<Task<HttpResponseMessage>> httpOperation,
            string operationName,
            string serviceName = "HttpClient",
            Dictionary<string, object>? context = null);

        /// <summary>
        /// Ejecuta una operación de base de datos con resiliencia
        /// </summary>
        /// <typeparam name="T">Tipo de retorno de la operación</typeparam>
        /// <param name="databaseOperation">Operación de base de datos a ejecutar</param>
        /// <param name="operationName">Nombre de la operación para logging</param>
        /// <param name="context">Contexto adicional para logging (opcional)</param>
        /// <returns>Resultado de la operación</returns>
        Task<T> ExecuteDatabaseWithResilienceAsync<T>(
            Func<Task<T>> databaseOperation,
            string operationName,
            Dictionary<string, object>? context = null);

        /// <summary>
        /// Ejecuta una operación con fallback
        /// </summary>
        /// <typeparam name="T">Tipo de retorno de la operación</typeparam>
        /// <param name="primaryOperation">Operación principal</param>
        /// <param name="fallbackOperation">Operación de fallback</param>
        /// <param name="operationName">Nombre de la operación para logging</param>
        /// <param name="serviceName">Nombre del servicio (opcional)</param>
        /// <param name="context">Contexto adicional para logging (opcional)</param>
        /// <returns>Resultado de la operación principal o fallback</returns>
        Task<T> ExecuteWithFallbackAsync<T>(
            Func<Task<T>> primaryOperation,
            Func<Task<T>> fallbackOperation,
            string operationName,
            string serviceName = "Default",
            Dictionary<string, object>? context = null);
    }
}

