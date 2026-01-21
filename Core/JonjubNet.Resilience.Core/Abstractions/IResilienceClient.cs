using System;
using System.Threading;
using System.Threading.Tasks;

namespace JonjubNet.Resilience.Abstractions
{
    /// <summary>
    /// Cliente de resiliencia para ejecutar operaciones con pipelines nombrados.
    /// El microservicio solo orquesta (DI + llamar al cliente); retry, circuit breaker y timeout viven en el componente.
    /// </summary>
    public interface IResilienceClient
    {
        /// <summary>
        /// Ejecuta una operación sin retorno con resiliencia usando un pipeline nombrado.
        /// </summary>
        /// <param name="pipelineName">Nombre del pipeline (use <see cref="PipelineNames"/> para nombres estándar).</param>
        /// <param name="action">Operación a ejecutar.</param>
        /// <param name="ct">Token de cancelación.</param>
        Task ExecuteAsync(string pipelineName, Func<CancellationToken, Task> action, CancellationToken ct = default);

        /// <summary>
        /// Ejecuta una operación con retorno con resiliencia usando un pipeline nombrado.
        /// </summary>
        /// <typeparam name="T">Tipo de retorno de la operación.</typeparam>
        /// <param name="pipelineName">Nombre del pipeline (use <see cref="PipelineNames"/> para nombres estándar).</param>
        /// <param name="action">Operación a ejecutar.</param>
        /// <param name="ct">Token de cancelación.</param>
        /// <returns>Resultado de la operación.</returns>
        Task<T> ExecuteAsync<T>(string pipelineName, Func<CancellationToken, Task<T>> action, CancellationToken ct = default);
    }
}
