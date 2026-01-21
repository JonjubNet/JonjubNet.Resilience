# Changelog

Todos los cambios notables de este proyecto serán documentados en este archivo.

El formato está basado en [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
y este proyecto adhiere a [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.0.13] - 2025-01-13

### Agregado
- **`JonjubNet.Resilience.Abstractions`**: `IResilienceClient` con `ExecuteAsync(string pipelineName, Func<CancellationToken, Task> action, CancellationToken ct)` y `ExecuteAsync<T>(...)` para orquestación sin lógica de resiliencia en el microservicio.
- **`IResilienceEventSink`** y **`ResilienceEvent`**: abstracción opcional para eventos; el componente funciona sin sink. El consumidor puede implementar el sink y enviar a su stack de observabilidad.
- **`PipelineNames`**: constantes `DatabaseRead`, `DatabaseWrite`, `DatabaseDelete`, `HttpExternal` para uso en código y en `JonjubNet:Resilience:Pipelines`.
- **`JonjubNet.Resilience.Hosting`**: `ServiceCollectionExtensions.AddJonjubNetResilience(IServiceCollection, IConfiguration)` — API estable para DI; registra opciones, pipelines y `IResilienceClient`.

### Cambiado
- **`ResilienceClient`**: implementa `JonjubNet.Resilience.Abstractions.IResilienceClient`; reemplazo de `ILogger` por `IResilienceEventSink` (opcional); parámetros `action` y `ct`.
- **`appsettings.example.json`**: pipelines de ejemplo con `DatabaseRead`, `DatabaseWrite`, `DatabaseDelete`, `HttpExternal`.

### Eliminado
- **`JonjubNet.Resilience.Core.Interfaces.IResilienceClient`** (sustituido por `JonjubNet.Resilience.Abstractions.IResilienceClient`).

### Arquitectura
- **Sin dependencia de JonjubNet.Observability**: el componente no referencia `ILoggingClient` ni `IMetricsClient`. Observabilidad vía `IResilienceEventSink` opcional en el consumidor.

---

## [1.0.12] - 2025-01-12

### Corregido
- Eliminada dependencia directa con `IStructuredLoggingService` para mejor desacoplamiento
- Componente ahora usa `ILogger<T>` estándar de .NET para logging interno
- Corrección en tests de integración para eliminar referencias a servicios de observabilidad
- Mejora en el workflow de GitHub Actions para ejecución de tests

### Mejorado
- Arquitectura más desacoplada: el componente maneja errores de forma óptima sin dependencias externas
- Mejor separación de responsabilidades: observabilidad queda en manos del servicio consumidor

## [1.0.0] - 2024-12-XX (Primer Release Público)

### Agregado
- Implementación inicial del servicio de resiliencia
- Soporte para patrones: Circuit Breaker, Retry, Timeout, Bulkhead, Fallback
- Configuración flexible via appsettings.json
- Integración nativa con ASP.NET Core
- Interfaz genérica para servicio de logging estructurado
- Implementación por defecto del servicio de resiliencia
- Soporte para operaciones HTTP con resiliencia
- Soporte para operaciones de base de datos con resiliencia
- Soporte para operaciones con fallback
- Documentación completa con ejemplos de uso
- Scripts de build para Windows y Linux/Mac

### Características Técnicas
- Basado en Polly para máximo rendimiento
- Soporte para .NET 10.0
- Configuración via IConfiguration
- Inyección de dependencias nativa
- Logging estructurado integrado (integración con JonjubNet.Observability)
- Configuración por servicio y operación
- Pipelines de resiliencia personalizables
- Estrategias de backoff configurables
- Circuit breaker avanzado
- Timeout por tipo de operación

### Arquitectura
- Arquitectura Hexagonal (Core, Infrastructure, Presentation)
- Thread-safe (ConcurrentDictionary, Interlocked)
- Optimizado para performance (string interning, pre-allocación)
- Sin memory leaks (límites de tamaño)
- Sin race conditions (operaciones atómicas)

### Soporte Multi-Database
- SQL Server, PostgreSQL, MySQL, Oracle
- Entity Framework Core (sin dependencias directas)
- Detección inteligente de excepciones transitorias

### Testing
- 34+ tests unitarios
- Tests de integración
- Cobertura: ~80-85%

### Documentación
- README completo con análisis técnico
- Guía de implementación
- API Reference
- Ejemplos avanzados
- Mejores prácticas
- Documentación XML completa para IntelliSense
