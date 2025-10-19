# Implementación en Aplicaciones .NET

## Pasos para Implementar JonjubNet.Resilience en tu Aplicación

### 1. Instalar el Paquete NuGet

```bash
cd "ruta/a/tu/proyecto"
dotnet add package JonjubNet.Resilience --source "ruta/a/los/paquetes"
```

### 2. Actualizar Program.cs

Agregar la siguiente línea en `Program.cs`:

```csharp
using JonjubNet.Resilience;

// En el método Main o en la configuración de servicios
builder.Services.AddResilienceInfrastructure(builder.Configuration);
```

### 3. Actualizar appsettings.json

Agregar la configuración de resiliencia a tu `appsettings.json`:

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
      "DurationOfBreakSeconds": 60,
      "EnableAdvancedCircuitBreaker": false,
      "FailureThresholdRatio": 0.5,
      "MinimumThroughputForAdvanced": 10
    },
    "Retry": {
      "Enabled": true,
      "MaxRetryAttempts": 3,
      "BaseDelayMilliseconds": 1000,
      "MaxDelayMilliseconds": 30000,
      "BackoffStrategy": "Exponential",
      "JitterFactor": 0.1,
      "RetryableStatusCodes": [408, 429, 500, 502, 503, 504],
      "RetryableExceptionTypes": ["HttpRequestException", "TaskCanceledException", "TimeoutException"]
    },
    "Timeout": {
      "Enabled": true,
      "DefaultTimeoutSeconds": 30,
      "DatabaseTimeoutSeconds": 15,
      "ExternalApiTimeoutSeconds": 10,
      "CacheTimeoutSeconds": 5,
      "EnableTimeoutPerOperation": true
    },
    "Bulkhead": {
      "Enabled": true,
      "MaxConcurrency": 10,
      "MaxQueuedActions": 20,
      "Services": {
        "Database": {
          "MaxConcurrency": 5,
          "MaxQueuedActions": 10
        },
        "HttpClient": {
          "MaxConcurrency": 8,
          "MaxQueuedActions": 15
        },
        "Cache": {
          "MaxConcurrency": 3,
          "MaxQueuedActions": 5
        }
      }
    },
    "Fallback": {
      "Enabled": true,
      "EnableCacheFallback": true,
      "EnableDefaultResponseFallback": true,
      "CacheFallbackTtlSeconds": 300,
      "Services": {
        "MetricsService": {
          "EnableCacheFallback": true,
          "EnableDefaultResponse": true,
          "DefaultResponseJson": "{}",
          "CacheTtlSeconds": 300
        },
        "LoggingService": {
          "EnableCacheFallback": false,
          "EnableDefaultResponse": true,
          "DefaultResponseJson": "{}",
          "CacheTtlSeconds": 0
        }
      }
    }
  }
}
```

### 4. Implementar IStructuredLoggingService

Crear un servicio que implemente `IStructuredLoggingService` en tu proyecto:

```csharp
using JonjubNet.Resilience.Interfaces;
using Microsoft.Extensions.Logging;

namespace MiAplicacion.Shared.Services
{
    public class StructuredLoggingService : IStructuredLoggingService
    {
        private readonly ILogger<StructuredLoggingService> _logger;

        public StructuredLoggingService(ILogger<StructuredLoggingService> logger)
        {
            _logger = logger;
        }

        public void LogInformation(string message, string operationName, string category, Dictionary<string, object>? context = null)
        {
            _logger.LogInformation("{Message} | Operation: {OperationName} | Category: {Category}", 
                message, operationName, category);
        }

        public void LogWarning(string message, string operationName, string category, string? userId = null, Dictionary<string, object>? context = null, Exception? exception = null)
        {
            _logger.LogWarning(exception, "{Message} | Operation: {OperationName} | Category: {Category} | UserId: {UserId}", 
                message, operationName, category, userId);
        }

        public void LogError(string message, string operationName, string category, string? userId = null, Dictionary<string, object>? context = null, Exception? exception = null)
        {
            _logger.LogError(exception, "{Message} | Operation: {OperationName} | Category: {Category} | UserId: {UserId}", 
                message, operationName, category, userId);
        }
    }
}
```

### 5. Registrar el Servicio de Logging

En tu `ServiceExtensions.cs` de Shared, agregar:

```csharp
using JonjubNet.Resilience.Interfaces;
using MiAplicacion.Shared.Services;

// En el método AddSharedInfrastructure
services.AddScoped<IStructuredLoggingService, StructuredLoggingService>();
```

### 6. Usar en tus Servicios

Ejemplo de uso en un servicio existente:

```csharp
using JonjubNet.Resilience.Interfaces;

public class MiServicio
{
    private readonly IResilienceService _resilienceService;
    private readonly IRepositoryAsync<MiEntidad> _repository;

    public MiServicio(
        IResilienceService resilienceService,
        IRepositoryAsync<MiEntidad> repository)
    {
        _resilienceService = resilienceService;
        _repository = repository;
    }

    public async Task<MiEntidad> GetDataAsync(int id)
    {
        return await _resilienceService.ExecuteDatabaseWithResilienceAsync(
            async () => await _repository.GetByIdAsync(id),
            "GetData",
            new Dictionary<string, object>
            {
                ["DataId"] = id,
                ["Operation"] = "GetData"
            }
        );
    }

    public async Task<MiEntidad> CreateDataAsync(MiEntidad data)
    {
        return await _resilienceService.ExecuteDatabaseWithResilienceAsync(
            async () => await _repository.AddAsync(data),
            "CreateData",
            new Dictionary<string, object>
            {
                ["DataName"] = data.Name,
                ["Operation"] = "CreateData"
            }
        );
    }
}
```

### 7. Usar con HttpClient

Para llamadas HTTP externas:

```csharp
public class ExternalApiService
{
    private readonly IResilienceService _resilienceService;
    private readonly HttpClient _httpClient;

    public ExternalApiService(
        IResilienceService resilienceService,
        IHttpClientFactory httpClientFactory)
    {
        _resilienceService = resilienceService;
        _httpClient = httpClientFactory.CreateClient("ResilientHttpClient");
    }

    public async Task<string> GetExternalDataAsync(string endpoint)
    {
        var response = await _resilienceService.ExecuteHttpWithResilienceAsync(
            async () => await _httpClient.GetAsync(endpoint),
            "GetExternalData",
            "ExternalApi",
            new Dictionary<string, object>
            {
                ["Endpoint"] = endpoint,
                ["Operation"] = "GetExternalData"
            }
        );

        return await response.Content.ReadAsStringAsync();
    }
}
```

### 8. Usar con Fallback

Para operaciones que pueden fallar:

```csharp
public async Task<string> GetDataWithFallbackAsync(int id)
{
    return await _resilienceService.ExecuteWithFallbackAsync(
        // Operación principal
        async () =>
        {
            var response = await _httpClient.GetAsync($"/api/data/{id}");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
        },
        // Operación de fallback
        async () =>
        {
            // Retornar datos por defecto o desde cache
            return "{\"id\": " + id + ", \"name\": \"Default Data\", \"fallback\": true}";
        },
        "GetDataWithFallback",
        "ExternalApi",
        new Dictionary<string, object>
        {
            ["DataId"] = id,
            ["Operation"] = "GetDataWithFallback"
        }
    );
}
```

## Verificación

1. Compila el proyecto: `dotnet build`
2. Ejecuta el proyecto: `dotnet run`
3. Verifica que no hay errores en los logs
4. Prueba las operaciones que usan resiliencia

## Beneficios

- **Circuit Breaker**: Protege contra fallos en cascada
- **Retry**: Reintentos automáticos para operaciones temporales
- **Timeout**: Control de tiempo de espera
- **Fallback**: Respuestas alternativas cuando las operaciones fallan
- **Logging**: Trazabilidad completa de las operaciones
- **Configuración**: Ajuste fino por servicio y operación
