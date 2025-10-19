# Instrucciones para Construir el Paquete NuGet

## Prerrequisitos

- .NET 8.0 SDK o superior
- Visual Studio 2022 o VS Code

## Pasos para Construir el Paquete

### 1. Restaurar Dependencias

```bash
dotnet restore
```

### 2. Compilar el Proyecto

```bash
dotnet build --configuration Release
```

### 3. Generar el Paquete NuGet

```bash
dotnet pack --configuration Release --output ./packages
```

El paquete se generará en la carpeta `./packages` con el nombre `JonjubNet.Resilience.1.0.0.nupkg`.

## Instalación Local

### Opción 1: Instalar desde archivo local

```bash
dotnet add package JonjubNet.Resilience --source ./packages
```

### Opción 2: Agregar a fuente local de NuGet

```bash
# Agregar la carpeta como fuente local
dotnet nuget add source ./packages --name "LocalPackages"

# Instalar el paquete
dotnet add package JonjubNet.Resilience --source "LocalPackages"
```

## Uso en el Proyecto CatalogMaster

### 1. Agregar Referencia al Paquete

```bash
cd "D:\Onuar\Proyecto\Net Core\JonjubNet\Sevices\CatalogMaster"
dotnet add package JonjubNet.Resilience --source "D:\Onuar\Proyecto\Net Core\JonjubNet\Component\JonjubNet.Resilience\packages"
```

### 2. Actualizar Program.cs

```csharp
using JonjubNet.Resilience;

var builder = WebApplication.CreateBuilder(args);

// Agregar infraestructura de resiliencia
builder.Services.AddResilienceInfrastructure(builder.Configuration);
```

### 3. Configurar appsettings.json

Copiar la configuración de `Examples/CatalogMasterConfiguration.json` a tu `appsettings.json`.

### 4. Usar en Servicios

```csharp
public class MiServicio
{
    private readonly IResilienceService _resilienceService;

    public MiServicio(IResilienceService resilienceService)
    {
        _resilienceService = resilienceService;
    }

    public async Task<string> MiOperacionAsync()
    {
        return await _resilienceService.ExecuteWithResilienceAsync(
            async () => {
                // Tu lógica aquí
                return "resultado";
            },
            "MiOperacion",
            "MiServicio"
        );
    }
}
```

## Publicación a NuGet.org (Opcional)

Si deseas publicar el paquete a NuGet.org:

### 1. Obtener API Key

- Ve a [NuGet.org](https://www.nuget.org)
- Inicia sesión y ve a "API Keys"
- Crea una nueva API Key

### 2. Publicar

```bash
dotnet nuget push ./packages/JonjubNet.Resilience.1.0.0.nupkg --api-key YOUR_API_KEY --source https://api.nuget.org/v3/index.json
```

## Estructura del Paquete

```
JonjubNet.Resilience.1.0.0.nupkg
├── lib/net8.0/
│   ├── JonjubNet.Resilience.dll
│   └── JonjubNet.Resilience.xml (documentación)
├── JonjubNet.Resilience.nuspec
├── README.md
└── icon.png
```

## Versiones

Para actualizar la versión del paquete, modifica la propiedad `<Version>` en `JonjubNet.Resilience.csproj`:

```xml
<Version>1.0.1</Version>
```

Luego ejecuta `dotnet pack` nuevamente.
