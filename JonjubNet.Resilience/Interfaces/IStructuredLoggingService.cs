using System;
using System.Collections.Generic;

namespace JonjubNet.Resilience.Interfaces
{
    /// <summary>
    /// Interfaz para el servicio de logging estructurado genérico
    /// </summary>
    public interface IStructuredLoggingService
    {
        /// <summary>
        /// Registra un mensaje de información
        /// </summary>
        /// <param name="message">Mensaje a registrar</param>
        /// <param name="operationName">Nombre de la operación</param>
        /// <param name="category">Categoría del log</param>
        /// <param name="context">Contexto adicional</param>
        void LogInformation(string message, string operationName, string category, Dictionary<string, object>? context = null);

        /// <summary>
        /// Registra un mensaje de advertencia
        /// </summary>
        /// <param name="message">Mensaje a registrar</param>
        /// <param name="operationName">Nombre de la operación</param>
        /// <param name="category">Categoría del log</param>
        /// <param name="userId">ID del usuario (opcional)</param>
        /// <param name="context">Contexto adicional</param>
        /// <param name="exception">Excepción asociada (opcional)</param>
        void LogWarning(string message, string operationName, string category, string? userId = null, Dictionary<string, object>? context = null, Exception? exception = null);

        /// <summary>
        /// Registra un mensaje de error
        /// </summary>
        /// <param name="message">Mensaje a registrar</param>
        /// <param name="operationName">Nombre de la operación</param>
        /// <param name="category">Categoría del log</param>
        /// <param name="userId">ID del usuario (opcional)</param>
        /// <param name="context">Contexto adicional</param>
        /// <param name="exception">Excepción asociada (opcional)</param>
        void LogError(string message, string operationName, string category, string? userId = null, Dictionary<string, object>? context = null, Exception? exception = null);
    }
}
