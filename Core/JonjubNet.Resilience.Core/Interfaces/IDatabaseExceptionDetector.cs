using System;

namespace JonjubNet.Resilience.Core.Interfaces
{
    /// <summary>
    /// Interfaz para detectar excepciones transitorias de base de datos
    /// </summary>
    public interface IDatabaseExceptionDetector
    {
        /// <summary>
        /// Determina si una excepción de base de datos es transitoria y debe ser reintentada
        /// </summary>
        /// <param name="exception">Excepción a evaluar</param>
        /// <returns>True si la excepción es transitoria y debe ser reintentada</returns>
        bool IsTransient(Exception exception);

        /// <summary>
        /// Determina si una excepción es de conexión a base de datos
        /// </summary>
        /// <param name="exception">Excepción a evaluar</param>
        /// <returns>True si la excepción es de conexión</returns>
        bool IsConnectionException(Exception exception);
    }
}

