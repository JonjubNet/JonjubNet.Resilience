# Ejemplos Avanzados - JonjubNet.Resilience

> **Versión:** 1.0.0 | **Última actualización:** Diciembre 2024

---

## 📋 Tabla de Contenidos

1. [Combinación de Patrones](#combinación-de-patrones)
2. [Configuración Dinámica](#configuración-dinámica)
3. [Manejo de Excepciones Personalizado](#manejo-de-excepciones-personalizado)
4. [Integración con Entity Framework Core](#integración-con-entity-framework-core)
5. [Microservicios](#microservicios)
6. [Circuit Breaker Avanzado](#circuit-breaker-avanzado)

---

## Combinación de Patrones

### Retry + Circuit Breaker + Timeout

```csharp
public class AdvancedResilienceService
{
    private readonly IResilienceService _resilienceService;

    public async Task<string> GetDataWithFullResilienceAsync()
    {
        return await _resilienceService.ExecuteWithResilienceAsync(
            async () =>
            {
                // Esta operación tendrá:
                // 1. Retry con backoff exponencial
                // 2. Circuit Breaker para proteger contra fallos en cascada
                // 3. Timeout para evitar operaciones colgadas
                var httpClient = new HttpClient();
                var response = await httpClient.GetAsync("https://api.example.com/data");
                return await response.Content.ReadAsStringAsync();
            },
            "GetDataWithFullResilience",
            "HttpClient"
        );
    }
}
```

**Configuración:**
```json
{
  "Resilience": {
    "Retry": {
      "Enabled": true,
      "MaxRetryAttempts": 3,
      "BackoffStrategy": "Exponential"
    },
    "CircuitBreaker": {
      "Enabled": true,
      "FailureThreshold": 5,
      "DurationOfBreakSeconds": 60
    },
    "Timeout": {
      "Enabled": true,
      "ExternalApiTimeoutSeconds": 10
    }
  }
}
```

---

## Configuración Dinámica

### Hot-Reload de Configuración

```csharp
public class ResilientService
{
    private readonly IResilienceService _resilienceService;
    private readonly IOptionsMonitor<ResilienceConfiguration> _configMonitor;

    public ResilientService(
        IResilienceService resilienceService,
        IOptionsMonitor<ResilienceConfiguration> configMonitor)
    {
        _resilienceService = resilienceService;
        _configMonitor = configMonitor;
        
        // Escuchar cambios en la configuración
        _configMonitor.OnChange(config =>
        {
            Console.WriteLine($"Resilience configuration updated: Enabled={config.Enabled}");
        });
    }
}
```

---

## Manejo de Excepciones Personalizado

### Detector de Excepciones Personalizado

```csharp
public class CustomDatabaseExceptionDetector : IDatabaseExceptionDetector
{
    public bool IsTransient(Exception exception)
    {
        // Lógica personalizada para detectar excepciones transitorias
        if (exception is CustomTransientException)
            return true;
            
        // Delegar al detector por defecto para bases de datos estándar
        var defaultDetector = new DatabaseExceptionDetector();
        return defaultDetector.IsTransient(exception);
    }

    public bool IsConnectionException(Exception exception)
    {
        // Lógica personalizada
        var defaultDetector = new DatabaseExceptionDetector();
        return defaultDetector.IsConnectionException(exception);
    }
}

// Registrar en Program.cs
builder.Services.AddSingleton<IDatabaseExceptionDetector, CustomDatabaseExceptionDetector>();
```

---

## Integración con Entity Framework Core

### Operaciones de Base de Datos con Resiliencia

```csharp
public class UserRepository
{
    private readonly ApplicationDbContext _context;
    private readonly IResilienceService _resilienceService;

    public UserRepository(
        ApplicationDbContext context,
        IResilienceService resilienceService)
    {
        _context = context;
        _resilienceService = resilienceService;
    }

    public async Task<List<User>> GetUsersAsync()
    {
        return await _resilienceService.ExecuteDatabaseWithResilienceAsync(
            async () =>
            {
                return await _context.Users
                    .Where(u => u.IsActive)
                    .ToListAsync();
            },
            "GetUsers"
        );
    }

    public async Task<User> CreateUserAsync(User user)
    {
        return await _resilienceService.ExecuteDatabaseWithResilienceAsync(
            async () =>
            {
                _context.Users.Add(user);
                await _context.SaveChangesAsync();
                return user;
            },
            "CreateUser"
        );
    }
}
```

---

## Microservicios

### Configuración por Microservicio

```json
{
  "Resilience": {
    "Services": {
      "UserService": {
        "Enabled": true,
        "Retry": {
          "MaxRetryAttempts": 5,
          "BaseDelayMilliseconds": 500
        },
        "CircuitBreaker": {
          "FailureThreshold": 3,
          "DurationOfBreakSeconds": 30
        }
      },
      "OrderService": {
        "Enabled": true,
        "Retry": {
          "MaxRetryAttempts": 3,
          "BaseDelayMilliseconds": 1000
        },
        "Timeout": {
          "DefaultTimeoutSeconds": 20
        }
      }
    }
  }
}
```

**Uso:**
```csharp
// Usar configuración específica para UserService
var users = await _resilienceService.ExecuteWithResilienceAsync(
    async () => await userServiceClient.GetUsersAsync(),
    "GetUsers",
    "UserService"
);

// Usar configuración específica para OrderService
var orders = await _resilienceService.ExecuteWithResilienceAsync(
    async () => await orderServiceClient.GetOrdersAsync(),
    "GetOrders",
    "OrderService"
);
```

---

## Circuit Breaker Avanzado

### Circuit Breaker con Ratio de Fallos

```json
{
  "Resilience": {
    "CircuitBreaker": {
      "Enabled": true,
      "EnableAdvancedCircuitBreaker": true,
      "FailureThresholdRatio": 0.5,
      "MinimumThroughputForAdvanced": 10,
      "SamplingDurationSeconds": 30,
      "DurationOfBreakSeconds": 60
    }
  }
}
```

**Comportamiento:**
- El circuit breaker se abre cuando el ratio de fallos supera el 50%
- Requiere un mínimo de 10 operaciones en la ventana de muestreo
- Se cierra automáticamente después de 60 segundos

---

**Última actualización:** Diciembre 2024
