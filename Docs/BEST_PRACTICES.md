# Mejores Prácticas - JonjubNet.Resilience

> **Versión:** 1.0.0 | **Última actualización:** Diciembre 2024

---

## 📋 Tabla de Contenidos

1. [Configuración](#configuración)
2. [Uso de Patrones](#uso-de-patrones)
3. [Performance](#performance)
4. [Monitoreo](#monitoreo)
5. [Seguridad](#seguridad)
6. [Testing](#testing)

---

## Configuración

### ✅ DO: Usar Configuración por Tipo de Operación

```csharp
// ✅ CORRECTO: Usar pipelines especializados
await _resilienceService.ExecuteDatabaseWithResilienceAsync(
    async () => await dbOperation(),
    "DatabaseOperation"
);

await _resilienceService.ExecuteHttpWithResilienceAsync(
    async () => await httpOperation(),
    "HttpOperation"
);
```

### ❌ DON'T: Usar Pipeline Genérico para Todo

```csharp
// ❌ INCORRECTO: No usar pipeline genérico para operaciones específicas
await _resilienceService.ExecuteWithResilienceAsync(
    async () => await dbOperation(),
    "DatabaseOperation",
    "Default" // ❌ No usar Default para operaciones de DB
);
```

### ✅ DO: Configurar Timeouts Apropiados

```json
{
  "Timeout": {
    "DatabaseTimeoutSeconds": 15,      // ✅ Tiempo apropiado para DB
    "ExternalApiTimeoutSeconds": 10,   // ✅ Tiempo apropiado para APIs
    "CacheTimeoutSeconds": 5           // ✅ Tiempo apropiado para Cache
  }
}
```

---

## Uso de Patrones

### ✅ DO: Usar Retry para Operaciones Idempotentes

```csharp
// ✅ CORRECTO: GET es idempotente, seguro para retry
await _resilienceService.ExecuteWithResilienceAsync(
    async () => await httpClient.GetAsync("https://api.example.com/data"),
    "GetData"
);
```

### ❌ DON'T: Usar Retry para Operaciones No Idempotentes

```csharp
// ❌ INCORRECTO: POST puede crear duplicados
await _resilienceService.ExecuteWithResilienceAsync(
    async () => await httpClient.PostAsync("https://api.example.com/users", content),
    "CreateUser" // ❌ No usar retry para operaciones que crean recursos
);
```

### ✅ DO: Usar Fallback para Operaciones Críticas

```csharp
// ✅ CORRECTO: Fallback para operaciones críticas
var data = await _resilienceService.ExecuteWithFallbackAsync(
    async () => await primaryDataSource.GetDataAsync(),
    async () => await cacheDataSource.GetDataAsync(),
    "GetCriticalData"
);
```

---

## Performance

### ✅ DO: Reutilizar Instancias de IResilienceService

```csharp
// ✅ CORRECTO: Inyectar como dependencia
public class MyService
{
    private readonly IResilienceService _resilienceService;
    
    public MyService(IResilienceService resilienceService)
    {
        _resilienceService = resilienceService; // ✅ Reutilizar instancia
    }
}
```

### ❌ DON'T: Crear Nuevas Instancias

```csharp
// ❌ INCORRECTO: No crear nuevas instancias
var service = new ResilienceService(...); // ❌ No hacer esto
```

### ✅ DO: Usar Contexto para Logging

```csharp
// ✅ CORRECTO: Proporcionar contexto para mejor logging
var context = new Dictionary<string, object>
{
    ["UserId"] = userId,
    ["RequestId"] = requestId
};

await _resilienceService.ExecuteWithResilienceAsync(
    async () => await operation(),
    "OperationName",
    "ServiceName",
    context // ✅ Contexto para logging
);
```

---

## Monitoreo

### ✅ DO: Monitorear Circuit Breakers

```csharp
// ✅ CORRECTO: Revisar logs cuando circuit breakers se abren
// Los logs automáticamente registran cuando circuit breakers se abren/cierran
```

**Logs esperados:**
```
[Error] Database circuit breaker opened for 60000ms. Reason: Too many failures
[Information] Database circuit breaker closed - database is healthy again
```

### ✅ DO: Monitorear Retries

```csharp
// ✅ CORRECTO: Los retries se registran automáticamente
// Revisar logs para identificar operaciones que requieren muchos retries
```

**Logs esperados:**
```
[Warning] Database retry attempt 1 for operation after 1000ms. Exception: SqlException, Reason: Timeout expired
```

---

## Seguridad

### ✅ DO: Validar Configuración

```csharp
// ✅ CORRECTO: Validar configuración en startup
public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
{
    var resilienceConfig = configuration.GetSection("Resilience").Get<ResilienceConfiguration>();
    
    if (resilienceConfig?.Retry?.MaxRetryAttempts > 10)
    {
        throw new InvalidOperationException("MaxRetryAttempts should not exceed 10");
    }
    
    services.AddResilienceInfrastructure(configuration);
}
```

### ❌ DON'T: Exponer Información Sensible en Logs

```csharp
// ❌ INCORRECTO: No incluir información sensible en contexto
var context = new Dictionary<string, object>
{
    ["Password"] = password, // ❌ NUNCA hacer esto
    ["CreditCard"] = creditCard // ❌ NUNCA hacer esto
};
```

---

## Testing

### ✅ DO: Mockear IResilienceService en Tests

```csharp
// ✅ CORRECTO: Mockear para tests unitarios
var mockResilienceService = new Mock<IResilienceService>();
mockResilienceService
    .Setup(x => x.ExecuteWithResilienceAsync(It.IsAny<Func<Task<string>>>(), It.IsAny<string>()))
    .ReturnsAsync("test-result");
```

### ✅ DO: Usar Tests de Integración

```csharp
// ✅ CORRECTO: Tests de integración con configuración real
[Fact]
public async Task ResilienceService_ShouldWorkEndToEnd()
{
    var services = new ServiceCollection();
    services.AddResilienceInfrastructure(configuration);
    var serviceProvider = services.BuildServiceProvider();
    var resilienceService = serviceProvider.GetRequiredService<IResilienceService>();
    
    var result = await resilienceService.ExecuteWithResilienceAsync(
        async () => await Task.FromResult("success"),
        "TestOperation"
    );
    
    Assert.Equal("success", result);
}
```

---

## Resumen de Mejores Prácticas

| Práctica | Descripción |
|----------|-------------|
| **Usar Pipelines Especializados** | Usa `ExecuteDatabaseWithResilienceAsync` para DB, `ExecuteHttpWithResilienceAsync` para HTTP |
| **Configurar Timeouts Apropiados** | Diferentes timeouts para diferentes tipos de operaciones |
| **Retry Solo para Idempotentes** | No uses retry para operaciones que crean o modifican recursos |
| **Usar Fallback** | Proporciona alternativas para operaciones críticas |
| **Monitorear Logs** | Revisa logs de circuit breakers y retries regularmente |
| **Validar Configuración** | Valida configuración en startup |
| **No Exponer Información Sensible** | Nunca incluyas passwords, credit cards, etc. en contexto |

---

**Última actualización:** Diciembre 2024
