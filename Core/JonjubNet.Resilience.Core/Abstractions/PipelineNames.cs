namespace JonjubNet.Resilience.Abstractions
{
    /// <summary>
    /// Nombres estándar de pipelines. Use estos valores en <see cref="IResilienceClient.ExecuteAsync"/> y en appsettings (JonjubNet:Resilience:Pipelines).
    /// </summary>
    public static class PipelineNames
    {
        /// <summary>Operaciones de lectura en base de datos.</summary>
        public const string DatabaseRead = "DatabaseRead";

        /// <summary>Operaciones de escritura en base de datos.</summary>
        public const string DatabaseWrite = "DatabaseWrite";

        /// <summary>Operaciones de eliminación en base de datos.</summary>
        public const string DatabaseDelete = "DatabaseDelete";

        /// <summary>Llamadas HTTP a servicios externos.</summary>
        public const string HttpExternal = "HttpExternal";
    }
}
