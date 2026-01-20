using System;
using System.Threading;
using System.Threading.Tasks;

namespace JonjubNet.Resilience.Core.Interfaces
{
    /// <summary>
    /// Cliente de resiliencia para ejecutar operaciones con pipelines nombrados
    /// </summary>
    public interface IResilienceClient
    {
        /// <summary>
        /// Ejecuta una operación con resiliencia usando un pipeline nombrado
        /// </summary>
        /// <typeparam name="T">Tipo de retorno de la operación</typeparam>
        /// <param name="pipelineName">Nombre del pipeline de resiliencia</param>
        /// <param name="operation">Operación a ejecutar</param>
        /// <param name="cancellationToken">Token de cancelación</param>
        /// <returns>Resultado de la operación</returns>
        Task<T> ExecuteAsync<T>(
            string pipelineName,
            Func<CancellationToken, Task<T>> operation,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Ejecuta una operación sin retorno con resiliencia usando un pipeline nombrado
        /// </summary>
        /// <param name="pipelineName">Nombre del pipeline de resiliencia</param>
        /// <param name="operation">Operación a ejecutar</param>
        /// <param name="cancellationToken">Token de cancelación</param>
        Task ExecuteAsync(
            string pipelineName,
            Func<CancellationToken, Task> operation,
            CancellationToken cancellationToken = default);
    }
}
