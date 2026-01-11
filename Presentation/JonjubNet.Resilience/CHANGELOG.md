# Changelog

Todos los cambios notables de este proyecto serán documentados en este archivo.

El formato está basado en [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
y este proyecto adhiere a [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

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
