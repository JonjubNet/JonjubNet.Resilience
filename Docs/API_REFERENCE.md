# API Reference - JonjubNet.Resilience

> **Versión:** 1.0.0 | **Última actualización:** Diciembre 2024

---

## 📋 Tabla de Contenidos

1. [IResilienceService](#iresilienceservice)
2. [IDatabaseExceptionDetector](#idatabaseexceptiondetector)
3. [ResilienceConfiguration](#resilienceconfiguration)
4. [ServiceExtensions](#serviceextensions)

---

## IResilienceService

Interfaz principal para ejecutar operaciones con patrones de resiliencia.

### ExecuteWithResilienceAsync&lt;T&gt;

Ejecuta una operación con todos los patrones de resiliencia aplicados.

```csharp
Task<T> ExecuteWithResilienceAsync<T>(
    Func<Task<T>> operation,
    string operationName,
    string serviceName = "Default",
    Dictionary<string, object>? context = null)
```

**Parámetros:**
- `operation`: Operación a ejecutar
- `operationName`: Nombre de la operación para logging
- `serviceName`: Nombre del servicio (opcional, default: "Default")
- `context`: Contexto adicional para logging (opcional)

**Retorna:** Resultado de la operación

**Ejemplo:**
```csharp
var result = await _resilienceService.ExecuteWithResilienceAsync(
    async () => await GetDataAsync(),
    "GetData",
    "MyService"
);
```

### ExecuteHttpWithResilienceAsync

Ejecuta una operación HTTP con resiliencia.

```csharp
Task<HttpResponseMessage> ExecuteHttpWithResilienceAsync(
    Func<Task<HttpResponseMessage>> httpOperation,
    string operationName,
    string serviceName = "HttpClient",
    Dictionary<string, object>? context = null)
```

**Parámetros:**
- `httpOperation`: Operación HTTP a ejecutar
- `operationName`: Nombre de la operación para logging
- `serviceName`: Nombre del servicio (opcional, default: "HttpClient")
- `context`: Contexto adicional para logging (opcional)

**Retorna:** Respuesta HTTP

**Ejemplo:**
```csharp
var response = await _resilienceService.ExecuteHttpWithResilienceAsync(
    async () => await httpClient.GetAsync("https://api.example.com/data"),
    "GetDataFromApi"
);
```

### ExecuteDatabaseWithResilienceAsync&lt;T&gt;

Ejecuta una operación de base de datos con resiliencia.

```csharp
Task<T> ExecuteDatabaseWithResilienceAsync<T>(
    Func<Task<T>> databaseOperation,
    string operationName,
    Dictionary<string, object>? context = null)
```

**Parámetros:**
- `databaseOperation`: Operación de base de datos a ejecutar
- `operationName`: Nombre de la operación para logging
- `context`: Contexto adicional para logging (opcional)

**Retorna:** Resultado de la operación

**Ejemplo:**
```csharp
var users = await _resilienceService.ExecuteDatabaseWithResilienceAsync(
    async () => await _context.Users.ToListAsync(),
    "GetUsers"
);
```

### ExecuteWithFallbackAsync&lt;T&gt;

Ejecuta una operación con fallback.

```csharp
Task<T> ExecuteWithFallbackAsync<T>(
    Func<Task<T>> primaryOperation,
    Func<Task<T>> fallbackOperation,
    string operationName,
    string serviceName = "Default",
    Dictionary<string, object>? context = null)
```

**Parámetros:**
- `primaryOperation`: Operación principal
- `fallbackOperation`: Operación de fallback
- `operationName`: Nombre de la operación para logging
- `serviceName`: Nombre del servicio (opcional, default: "Default")
- `context`: Contexto adicional para logging (opcional)

**Retorna:** Resultado de la operación principal o fallback

**Ejemplo:**
```csharp
var result = await _resilienceService.ExecuteWithFallbackAsync(
    async () => await primaryOperation(),
    async () => await fallbackOperation(),
    "GetDataWithFallback"
);
```

---

## IDatabaseExceptionDetector

Interfaz para detectar excepciones de base de datos transitorias.

### IsTransient

Determina si una excepción de base de datos es transitoria y debe ser reintentada.

```csharp
bool IsTransient(Exception exception)
```

**Parámetros:**
- `exception`: Excepción a evaluar

**Retorna:** `true` si la excepción es transitoria, `false` en caso contrario

**Soporta:**
- SQL Server
- PostgreSQL
- MySQL
- Oracle
- Entity Framework Core

### IsConnectionException

Determina si una excepción es de conexión a base de datos.

```csharp
bool IsConnectionException(Exception exception)
```

**Parámetros:**
- `exception`: Excepción a evaluar

**Retorna:** `true` si la excepción es de conexión, `false` en caso contrario

---

## ResilienceConfiguration

Configuración principal de resiliencia.

### Propiedades

```csharp
public bool Enabled { get; set; }
public string ServiceName { get; set; }
public string Environment { get; set; }
public CircuitBreakerConfiguration CircuitBreaker { get; set; }
public RetryConfiguration Retry { get; set; }
public TimeoutConfiguration Timeout { get; set; }
public BulkheadConfiguration Bulkhead { get; set; }
public FallbackConfiguration Fallback { get; set; }
public Dictionary<string, ServiceResilienceConfiguration> Services { get; set; }
```

### CircuitBreakerConfiguration

```csharp
public bool Enabled { get; set; }
public int FailureThreshold { get; set; }
public int SamplingDurationSeconds { get; set; }
public int MinimumThroughput { get; set; }
public int DurationOfBreakSeconds { get; set; }
public bool EnableAdvancedCircuitBreaker { get; set; }
public double FailureThresholdRatio { get; set; }
public int MinimumThroughputForAdvanced { get; set; }
```

### RetryConfiguration

```csharp
public bool Enabled { get; set; }
public int MaxRetryAttempts { get; set; }
public int BaseDelayMilliseconds { get; set; }
public int MaxDelayMilliseconds { get; set; }
public string BackoffStrategy { get; set; } // "Exponential", "Linear", "Fixed"
public double JitterFactor { get; set; }
public List<int> RetryableStatusCodes { get; set; }
public List<string> RetryableExceptionTypes { get; set; }
```

### TimeoutConfiguration

```csharp
public bool Enabled { get; set; }
public int DefaultTimeoutSeconds { get; set; }
public int DatabaseTimeoutSeconds { get; set; }
public int ExternalApiTimeoutSeconds { get; set; }
public int CacheTimeoutSeconds { get; set; }
public bool EnableTimeoutPerOperation { get; set; }
```

---

## ServiceExtensions

Extensiones para registrar la infraestructura de resiliencia.

### AddResilienceInfrastructure

Agrega la infraestructura de resiliencia al contenedor de dependencias.

```csharp
IServiceCollection AddResilienceInfrastructure(
    this IServiceCollection services,
    IConfiguration configuration)
```

**Parámetros:**
- `services`: Colección de servicios
- `configuration`: Configuración de la aplicación

**Retorna:** Colección de servicios para chaining

**Ejemplo:**
```csharp
builder.Services.AddResilienceInfrastructure(builder.Configuration);
```

### AddResilienceInfrastructure (con configuración personalizada)

Agrega la infraestructura de resiliencia con configuración personalizada.

```csharp
IServiceCollection AddResilienceInfrastructure(
    this IServiceCollection services,
    IConfiguration configuration,
    Action<ResilienceConfiguration> configureOptions)
```

**Parámetros:**
- `services`: Colección de servicios
- `configuration`: Configuración de la aplicación
- `configureOptions`: Acción para configurar opciones adicionales

**Retorna:** Colección de servicios para chaining

**Ejemplo:**
```csharp
builder.Services.AddResilienceInfrastructure(builder.Configuration, options =>
{
    options.Retry.MaxRetryAttempts = 5;
    options.CircuitBreaker.FailureThreshold = 3;
});
```

---

**Última actualización:** Diciembre 2024
