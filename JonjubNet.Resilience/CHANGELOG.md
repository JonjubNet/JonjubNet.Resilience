# Changelog

Todos los cambios notables de este proyecto serán documentados en este archivo.

El formato está basado en [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
y este proyecto adhiere a [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.0.0] - 2024-01-15

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
- Soporte para .NET 8.0
- Configuración via IConfiguration
- Inyección de dependencias nativa
- Logging estructurado integrado
- Configuración por servicio y operación
- Pipelines de resiliencia personalizables
- Estrategias de backoff configurables
- Circuit breaker avanzado
- Timeout por tipo de operación

### Configuración
- Sección de configuración: "Resilience"
- Patrones: CircuitBreaker, Retry, Timeout, Bulkhead, Fallback
- Configuración por servicio específico
- Estrategias de retry: Exponential, Linear, Fixed
- Configuración de timeouts por tipo de operación
- Configuración de circuit breaker avanzado

### Documentación
- README.md completo con ejemplos
- Comentarios XML en todo el código
- Guía de implementación completa
- Instrucciones de construcción
- Licencia MIT incluida
