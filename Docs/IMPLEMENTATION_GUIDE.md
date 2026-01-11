# Guía de Implementación - JonjubNet.Resilience

> **Versión:** 1.0.0 | **Última actualización:** Diciembre 2024  
> **Componente:** JonjubNet.Resilience  
> **Nivel:** Producción Enterprise

---

## 📋 Tabla de Contenidos

1. [Introducción](#introducción)
2. [Instalación y Configuración Inicial](#instalación-y-configuración-inicial)
3. [Patrones de Resiliencia](#patrones-de-resiliencia)
4. [Uso Básico](#uso-básico)
5. [Configuración Avanzada](#configuración-avanzada)
6. [Soporte Multi-Database](#soporte-multi-database)
7. [Integración con Observabilidad](#integración-con-observabilidad)
8. [Mejores Prácticas](#mejores-prácticas)
9. [Troubleshooting](#troubleshooting)
10. [Recursos Adicionales](#recursos-adicionales)

---

## Introducción

**JonjubNet.Resilience** es una biblioteca de resiliencia de nivel empresarial que implementa patrones como Circuit Breaker, Retry, Timeout, Bulkhead y Fallback usando Polly. Esta guía te llevará paso a paso a través de la implementación completa.

### 🎯 Objetivo de esta Guía

Esta guía está diseñada para:
- ✅ Desarrolladores que implementan resiliencia por primera vez
- ✅ Equipos que migran de otras soluciones
- ✅ Arquitectos que evalúan el componente
- ✅ DevOps que configuran la infraestructura

### ✨ Características Principales

| Característica | Descripción | Beneficio |
|----------------|-------------|-----------|
| **Circuit Breaker** | Protección contra fallos en cascada | Previene sobrecarga de servicios fallidos |
| **Retry** | Reintentos automáticos con backoff | Maneja errores transitorios automáticamente |
| **Timeout** | Control de tiempo de espera | Evita operaciones que se cuelgan |
| **Fallback** | Estrategias de respaldo | Proporciona alternativas cuando falla la operación principal |
| **Multi-Database** | Soporte para 4 bases de datos | Detección inteligente de excepciones transitorias |
| **Thread-Safe** | ConcurrentDictionary, Interlocked | Seguro para uso en aplicaciones multi-thread |
| **Performance** | Optimizaciones de GC, string interning | Bajo overhead en operaciones críticas |

---

## Instalación y Configuración Inicial

### 📦 Paso 1: Instalar el Paquete NuGet

#### Opción A: NuGet Package Manager
```powershell
Install-Package JonjubNet.Resilience -Version 1.0.11
```

#### Opción B: .NET CLI
```bash
dotnet add package JonjubNet.Resilience --version 1.0.11
```

#### Opción C: PackageReference
```xml
<ItemGroup>
  <PackageReference Include="JonjubNet.Resilience" Version="1.0.11" />
</ItemGroup>
```

### ⚙️ Paso 2: Configurar en `Program.cs`

```csharp
using JonjubNet.Resilience;

var builder = WebApplication.CreateBuilder(args);

// Agregar infraestructura de resiliencia
builder.Services.AddResilienceInfrastructure(builder.Configuration);

var app = builder.Build();
app.Run();
```

### 📝 Paso 3: Configurar en `appsettings.json`

```json
{
  "Resilience": {
    "Enabled": true,
    "ServiceName": "MiAplicacion",
    "Environment": "Development",
    "CircuitBreaker": {
      "Enabled": true,
      "FailureThreshold": 5,
      "SamplingDurationSeconds": 30,
      "MinimumThroughput": 2,
      "DurationOfBreakSeconds": 60
    },
    "Retry": {
      "Enabled": true,
      "MaxRetryAttempts": 3,
      "BaseDelayMilliseconds": 1000,
      "MaxDelayMilliseconds": 30000,
      "BackoffStrategy": "Exponential"
    },
    "Timeout": {
      "Enabled": true,
      "DefaultTimeoutSeconds": 30,
      "DatabaseTimeoutSeconds": 15,
      "ExternalApiTimeoutSeconds": 10
    }
  }
}
```

---

## Patrones de Resiliencia

### Circuit Breaker

El Circuit Breaker protege contra fallos en cascada abriendo el circuito cuando se detectan demasiados fallos.

**Configuración:**
```json
{
  "CircuitBreaker": {
    "Enabled": true,
    "FailureThreshold": 5,
    "SamplingDurationSeconds": 30,
    "MinimumThroughput": 2,
    "DurationOfBreakSeconds": 60,
    "EnableAdvancedCircuitBreaker": false,
    "FailureThresholdRatio": 0.5,
    "MinimumThroughputForAdvanced": 10
  }
}
```

**Uso:**
```csharp
var result = await _resilienceService.ExecuteWithResilienceAsync(
    async () => await httpClient.GetAsync("https://api.ejemplo.com/datos"),
    "ObtenerDatos",
    "HttpClient"
);
```

### Retry

El patrón Retry reintenta operaciones que fallan temporalmente con diferentes estrategias de backoff.

**Estrategias disponibles:**
- **Exponential**: Backoff exponencial (recomendado)
- **Linear**: Backoff lineal
- **Fixed**: Retraso fijo

**Configuración:**
```json
{
  "Retry": {
    "Enabled": true,
    "MaxRetryAttempts": 3,
    "BaseDelayMilliseconds": 1000,
    "MaxDelayMilliseconds": 30000,
    "BackoffStrategy": "Exponential",
    "JitterFactor": 0.1
  }
}
```

### Timeout

El patrón Timeout limita el tiempo de espera de las operaciones.

**Configuración:**
```json
{
  "Timeout": {
    "Enabled": true,
    "DefaultTimeoutSeconds": 30,
    "DatabaseTimeoutSeconds": 15,
    "ExternalApiTimeoutSeconds": 10,
    "CacheTimeoutSeconds": 5
  }
}
```

### Fallback

El patrón Fallback proporciona respuestas alternativas cuando las operaciones fallan.

**Uso:**
```csharp
var result = await _resilienceService.ExecuteWithFallbackAsync(
    async () => await primaryOperation(),
    async () => await fallbackOperation(),
    "OperacionConFallback"
);
```

---

## Uso Básico

### Operaciones HTTP

```csharp
public class MiServicio
{
    private readonly IResilienceService _resilienceService;

    public MiServicio(IResilienceService resilienceService)
    {
        _resilienceService = resilienceService;
    }

    public async Task<string> ObtenerDatosAsync()
    {
        return await _resilienceService.ExecuteHttpWithResilienceAsync(
            async () =>
            {
                var httpClient = new HttpClient();
                var response = await httpClient.GetAsync("https://api.ejemplo.com/datos");
                return response;
            },
            "ObtenerDatos",
            "HttpClient"
        );
    }
}
```

### Operaciones de Base de Datos

```csharp
public async Task<List<Usuario>> ObtenerUsuariosAsync()
{
    return await _resilienceService.ExecuteDatabaseWithResilienceAsync(
        async () =>
        {
            return await _context.Usuarios.ToListAsync();
        },
        "ObtenerUsuarios"
    );
}
```

---

## Configuración Avanzada

### Configuración por Servicio

```json
{
  "Resilience": {
    "Services": {
      "Database": {
        "Enabled": true,
        "Retry": {
          "MaxRetryAttempts": 5,
          "BaseDelayMilliseconds": 500
        },
        "Timeout": {
          "DefaultTimeoutSeconds": 10
        }
      },
      "HttpClient": {
        "Enabled": true,
        "CircuitBreaker": {
          "FailureThreshold": 3,
          "DurationOfBreakSeconds": 30
        }
      }
    }
  }
}
```

### Configuración Programática

```csharp
builder.Services.AddResilienceInfrastructure(builder.Configuration, options =>
{
    options.Enabled = true;
    options.ServiceName = "MiAplicacion";
    options.Retry.MaxRetryAttempts = 5;
    options.CircuitBreaker.FailureThreshold = 3;
});
```

---

## Soporte Multi-Database

El componente soporta detección de excepciones transitorias para:

- **SQL Server**: Códigos de error -2, -1, 2, 53, 121, 1205, 1222, 8645, 8651, 4060
- **PostgreSQL**: SQLState 08, 40, 53
- **MySQL**: Códigos 1205, 1213, 2006, 2013, 1040, 1041
- **Oracle**: ORA-00054, ORA-00060, ORA-04021, ORA-00604

**No requiere dependencias directas** de frameworks de base de datos. Usa reflexión para detectar excepciones.

---

## Integración con Observabilidad

El componente se integra con `JonjubNet.Observability` a través de `IStructuredLoggingService`:

```csharp
// En tu Program.cs
builder.Services.AddJonjubNetObservability(builder.Configuration);
builder.Services.AddResilienceInfrastructure(builder.Configuration);
```

Todas las operaciones de resiliencia se registran automáticamente en el sistema de logging estructurado.

---

## Mejores Prácticas

1. **Usa pipelines especializados**: `ExecuteDatabaseWithResilienceAsync` para DB, `ExecuteHttpWithResilienceAsync` para HTTP
2. **Configura timeouts apropiados**: Diferentes timeouts para DB, HTTP, Cache
3. **Monitorea circuit breakers**: Revisa logs cuando se abren circuitos
4. **Usa fallback cuando sea posible**: Proporciona alternativas para operaciones críticas
5. **Evita retries en operaciones idempotentes**: Solo retry en operaciones seguras

---

## Troubleshooting

### Circuit Breaker siempre abierto

**Problema:** El circuit breaker se abre y nunca se cierra.

**Solución:** Verifica que el servicio esté funcionando correctamente. El circuit breaker se cierra automáticamente después de `DurationOfBreakSeconds`.

### Retries no funcionan

**Problema:** Las operaciones no se reintentan.

**Solución:** Verifica que `Retry.Enabled` esté en `true` y que las excepciones sean transitorias.

### Timeout muy corto

**Problema:** Las operaciones fallan por timeout prematuro.

**Solución:** Aumenta los valores de timeout en la configuración según el tipo de operación.

---

## Recursos Adicionales

- [README Principal](../README.md)
- [API Reference](API_REFERENCE.md)
- [Ejemplos Avanzados](ADVANCED_EXAMPLES.md)
- [Best Practices](BEST_PRACTICES.md)
- [CHANGELOG](../Presentation/JonjubNet.Resilience/CHANGELOG.md)

---

**Última actualización:** Diciembre 2024
