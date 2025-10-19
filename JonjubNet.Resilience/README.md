# JonjubNet.Resilience

Una biblioteca de resiliencia para aplicaciones .NET que implementa patrones como Circuit Breaker, Retry, Timeout, Bulkhead y Fallback usando Polly.

## Características

- **Circuit Breaker**: Protege contra fallos en cascada
- **Retry**: Reintentos automáticos con diferentes estrategias de backoff
- **Timeout**: Control de tiempo de espera para operaciones
- **Bulkhead**: Aislamiento de recursos
- **Fallback**: Estrategias de respaldo cuando las operaciones fallan
- **Configuración flexible**: Configuración por servicio y operación
- **Logging estructurado**: Integración con sistemas de logging

## Instalación

```bash
dotnet add package JonjubNet.Resilience
```

## Uso Básico

### 1. Configuración en appsettings.json

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

### 2. Registro en Program.cs

```csharp
using JonjubNet.Resilience;

var builder = WebApplication.CreateBuilder(args);

// Agregar infraestructura de resiliencia
builder.Services.AddResilienceInfrastructure(builder.Configuration);
```

### 3. Uso en servicios

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
        return await _resilienceService.ExecuteWithResilienceAsync(
            async () =>
            {
                // Tu lógica de negocio aquí
                var httpClient = new HttpClient();
                var response = await httpClient.GetAsync("https://api.ejemplo.com/datos");
                return await response.Content.ReadAsStringAsync();
            },
            "ObtenerDatos",
            "ApiExterna"
        );
    }

    public async Task<string> ObtenerDatosConFallbackAsync()
    {
        return await _resilienceService.ExecuteWithFallbackAsync(
            async () =>
            {
                // Operación principal
                var httpClient = new HttpClient();
                var response = await httpClient.GetAsync("https://api.ejemplo.com/datos");
                return await response.Content.ReadAsStringAsync();
            },
            async () =>
            {
                // Operación de fallback
                return "Datos por defecto";
            },
            "ObtenerDatosConFallback",
            "ApiExterna"
        );
    }
}
```

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

## Patrones de Resiliencia

### Circuit Breaker
- **Propósito**: Evita llamadas a servicios que están fallando
- **Configuración**: `FailureThreshold`, `SamplingDurationSeconds`, `DurationOfBreakSeconds`

### Retry
- **Propósito**: Reintenta operaciones que fallan temporalmente
- **Estrategias**: Exponential, Linear, Fixed
- **Configuración**: `MaxRetryAttempts`, `BaseDelayMilliseconds`, `BackoffStrategy`

### Timeout
- **Propósito**: Limita el tiempo de espera de las operaciones
- **Configuración**: `DefaultTimeoutSeconds`, `DatabaseTimeoutSeconds`, `ExternalApiTimeoutSeconds`

### Fallback
- **Propósito**: Proporciona respuestas alternativas cuando las operaciones fallan
- **Configuración**: `EnableCacheFallback`, `EnableDefaultResponseFallback`

## Logging

La biblioteca integra con sistemas de logging estructurado. Asegúrate de implementar `IStructuredLoggingService` en tu aplicación:

```csharp
public class MiLoggingService : IStructuredLoggingService
{
    private readonly ILogger<MiLoggingService> _logger;

    public MiLoggingService(ILogger<MiLoggingService> logger)
    {
        _logger = logger;
    }

    public void LogInformation(string message, string operationName, string category, Dictionary<string, object>? context = null)
    {
        _logger.LogInformation("{Message} | Operation: {OperationName} | Category: {Category}", 
            message, operationName, category);
    }

    // Implementar otros métodos...
}
```

## Licencia

MIT License - ver archivo LICENSE para más detalles.
