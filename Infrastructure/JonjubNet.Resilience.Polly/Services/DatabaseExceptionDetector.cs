using System;
using System.Collections.Generic;
using JonjubNet.Resilience.Core.Interfaces;

namespace JonjubNet.Resilience.Polly.Services
{
    /// <summary>
    /// Implementación de detección de excepciones de base de datos usando reflexión
    /// Soporta: SQL Server, PostgreSQL, MySQL, Oracle, Entity Framework Core
    /// No requiere dependencias directas de frameworks de base de datos
    /// Thread-safe, stateless, optimizado para rendimiento y GC
    /// </summary>
    public class DatabaseExceptionDetector : IDatabaseExceptionDetector
    {
        // Constantes para string interning y evitar allocations
        private const string DbUpdateConcurrencyExceptionName = "DbUpdateConcurrencyException";
        private const string DbUpdateExceptionName = "DbUpdateException";
        private const string SqlExceptionName = "SqlException";
        private const string NpgsqlExceptionName = "NpgsqlException";
        private const string PostgresExceptionName = "PostgresException";
        private const string MySqlExceptionName = "MySqlException";
        private const string OracleExceptionName = "OracleException";
        private const string InnerExceptionPropertyName = "InnerException";
        private const string NumberPropertyName = "Number";
        private const string SqlStatePropertyName = "SqlState";

        // Códigos de error transitorios pre-calculados (readonly para thread-safety)
        private static readonly HashSet<int> TransientSqlServerErrorNumbers = new()
        {
            -2, -1, 2, 53, 121, 1205, 1222, 8645, 8651, 4060
        };

        private static readonly HashSet<uint> TransientMySqlErrorCodes = new()
        {
            1205U, 1213U, 2006U, 2013U, 1040U, 1041U
        };

        // Patrones de mensaje pre-internados (readonly para thread-safety)
        private static readonly string[] ConnectionMessagePatterns = new[]
        {
            "connection",
            "network",
            "timeout",
            "unable to connect",
            "server was not found",
            "could not open connection",
            "gone away",
            "lost connection",
            "resource busy"
        };

        private static readonly string[] OracleErrorPatterns = new[]
        {
            "ora-00054",
            "ora-00060",
            "ora-04021",
            "ora-00604"
        };
        /// <summary>
        /// Determina si una excepción de base de datos es transitoria y debe ser reintentada
        /// Thread-safe, stateless, optimizado para rendimiento
        /// </summary>
        public bool IsTransient(Exception exception)
        {
            if (exception == null)
                return false;

            // Cachear GetType().Name una sola vez
            var exceptionTypeName = exception.GetType().Name;

            // Excepciones de timeout siempre son transitorias (verificación rápida sin reflexión)
            if (exception is TimeoutException || exception is TaskCanceledException)
                return true;

            // Excepciones de EF Core (usando reflexión para evitar dependencia directa)
            // Verificar DbUpdateConcurrencyException primero (es más específico)
            if (string.Equals(exceptionTypeName, DbUpdateConcurrencyExceptionName, StringComparison.Ordinal))
            {
                return false; // Nunca reintentar errores de concurrencia (optimistic concurrency)
            }

            // Verificar DbUpdateException (más genérico)
            if (string.Equals(exceptionTypeName, DbUpdateExceptionName, StringComparison.Ordinal))
            {
                // Revisar la excepción interna usando reflexión
                var innerExceptionProperty = exception.GetType().GetProperty(InnerExceptionPropertyName);
                if (innerExceptionProperty != null)
                {
                    var innerException = innerExceptionProperty.GetValue(exception) as Exception;
                    if (innerException != null)
                        return IsTransient(innerException);
                }
                
                // Si no hay InnerException, no es transitoria
                return false;
            }

            // SQL Server
            if (string.Equals(exceptionTypeName, SqlExceptionName, StringComparison.Ordinal))
            {
                return IsTransientSqlServerError(exception);
            }

            // PostgreSQL
            if (string.Equals(exceptionTypeName, NpgsqlExceptionName, StringComparison.Ordinal) || 
                string.Equals(exceptionTypeName, PostgresExceptionName, StringComparison.Ordinal))
            {
                return IsTransientPostgreSqlError(exception);
            }

            // MySQL
            if (string.Equals(exceptionTypeName, MySqlExceptionName, StringComparison.Ordinal))
            {
                return IsTransientMySqlError(exception);
            }

            // Oracle
            if (string.Equals(exceptionTypeName, OracleExceptionName, StringComparison.Ordinal))
            {
                return IsTransientOracleError(exception);
            }

            // Por defecto, si es una excepción de base de datos conocida, no es transitoria
            // Solo reintentar excepciones de conexión/timeout
            return false;
        }

        /// <summary>
        /// Determina si una excepción es de conexión a base de datos
        /// Optimizado para evitar allocations de ToLowerInvariant
        /// </summary>
        public bool IsConnectionException(Exception exception)
        {
            if (exception == null)
                return false;

            var message = exception.Message;
            if (string.IsNullOrEmpty(message))
                return false;

            // Usar StringComparison.OrdinalIgnoreCase para evitar ToLowerInvariant (mejor rendimiento)
            foreach (var pattern in ConnectionMessagePatterns)
            {
                if (message.Contains(pattern, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Determina si un error de SQL Server es transitorio
        /// Optimizado para evitar allocations innecesarias
        /// </summary>
        private static bool IsTransientSqlServerError(Exception exception)
        {
            try
            {
                // Usar reflexión para acceder a propiedades específicas de SqlException
                var errorNumberProperty = exception.GetType().GetProperty(NumberPropertyName);
                if (errorNumberProperty != null)
                {
                    var errorNumber = (int?)errorNumberProperty.GetValue(exception);
                    if (errorNumber.HasValue && TransientSqlServerErrorNumbers.Contains(errorNumber.Value))
                    {
                        return true;
                    }
                }

                // Verificar por mensaje si no se puede obtener el número de error
                return CheckMessagePatterns(exception.Message);
            }
            catch
            {
                // Si hay error al acceder a las propiedades, asumir que no es transitorio
                return false;
            }
        }

        /// <summary>
        /// Determina si un error de PostgreSQL es transitorio
        /// Optimizado para evitar allocations innecesarias
        /// </summary>
        private static bool IsTransientPostgreSqlError(Exception exception)
        {
            try
            {
                // Usar reflexión para acceder a propiedades específicas de NpgsqlException/PostgresException
                var sqlStateProperty = exception.GetType().GetProperty(SqlStatePropertyName);
                if (sqlStateProperty != null)
                {
                    var sqlState = sqlStateProperty.GetValue(exception)?.ToString();
                    if (!string.IsNullOrEmpty(sqlState))
                    {
                        // Códigos SQLState transitorios (usar StringComparison.Ordinal para mejor rendimiento)
                        return sqlState.StartsWith("08", StringComparison.Ordinal) ||  // Connection exceptions
                               sqlState.StartsWith("40", StringComparison.Ordinal) ||  // Transaction rollback
                               sqlState.StartsWith("53", StringComparison.Ordinal);    // Insufficient resources
                    }
                }

                // Verificar por mensaje
                return CheckMessagePatterns(exception.Message);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Determina si un error de MySQL es transitorio
        /// Optimizado para evitar allocations innecesarias
        /// </summary>
        private static bool IsTransientMySqlError(Exception exception)
        {
            try
            {
                // Usar reflexión para acceder a propiedades específicas de MySqlException
                var errorCodeProperty = exception.GetType().GetProperty(NumberPropertyName);
                if (errorCodeProperty != null)
                {
                    var errorCode = (uint?)errorCodeProperty.GetValue(exception);
                    if (errorCode.HasValue && TransientMySqlErrorCodes.Contains(errorCode.Value))
                    {
                        return true;
                    }
                }

                // Verificar por mensaje
                return CheckMessagePatterns(exception.Message);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Determina si un error de Oracle es transitorio
        /// Optimizado para evitar allocations innecesarias
        /// </summary>
        private static bool IsTransientOracleError(Exception exception)
        {
            try
            {
                var message = exception.Message;
                if (string.IsNullOrEmpty(message))
                    return false;
                
                // Verificar códigos de error Oracle comunes
                foreach (var pattern in OracleErrorPatterns)
                {
                    if (message.Contains(pattern, StringComparison.OrdinalIgnoreCase))
                        return true;
                }

                // Verificar por mensaje genérico
                return CheckMessagePatterns(message);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Método helper compartido para verificar patrones de mensaje
        /// Evita código duplicado y optimiza rendimiento
        /// </summary>
        private static bool CheckMessagePatterns(string? message)
        {
            if (string.IsNullOrEmpty(message))
                return false;

            foreach (var pattern in ConnectionMessagePatterns)
            {
                if (message.Contains(pattern, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }
    }
}

