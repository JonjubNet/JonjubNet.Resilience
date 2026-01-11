# Script para publicar JonjubNet.Resilience a GitHub
# Autor: Onuar Jiménez
# Empresa: JonjubNet

param(
    [string]$GitHubUsername = "",
    [string]$RepositoryName = "JonjubNet.Resilience",
    [string]$Version = "1.0.0"
)

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Publicar a GitHub - JonjubNet.Resilience" -ForegroundColor Cyan
Write-Host "Versión: $Version" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Verificar que estamos en el directorio correcto
if (-not (Test-Path "JonjubNet.Resilience.slnx")) {
    Write-Host "ERROR: No se encontró JonjubNet.Resilience.slnx" -ForegroundColor Red
    Write-Host "Ejecuta este script desde la raíz del proyecto" -ForegroundColor Red
    exit 1
}

# Verificar que git está instalado
try {
    $gitVersion = git --version
    Write-Host "✓ Git encontrado: $gitVersion" -ForegroundColor Green
} catch {
    Write-Host "ERROR: Git no está instalado" -ForegroundColor Red
    exit 1
}

# Verificar estado de git
Write-Host ""
Write-Host "Verificando estado de Git..." -ForegroundColor Yellow

if (-not (Test-Path ".git")) {
    Write-Host "Inicializando repositorio Git..." -ForegroundColor Yellow
    git init
    Write-Host "✓ Repositorio Git inicializado" -ForegroundColor Green
} else {
    Write-Host "✓ Repositorio Git ya existe" -ForegroundColor Green
}

# Verificar .gitignore
if (-not (Test-Path ".gitignore")) {
    Write-Host "ADVERTENCIA: .gitignore no encontrado" -ForegroundColor Yellow
    Write-Host "Se recomienda crear un .gitignore antes de continuar" -ForegroundColor Yellow
}

# Verificar cambios sin commitear
$gitStatus = git status --porcelain
if ($gitStatus) {
    Write-Host ""
    Write-Host "Cambios detectados:" -ForegroundColor Yellow
    Write-Host $gitStatus
    Write-Host ""
    $response = Read-Host "¿Deseas agregar y commitear todos los cambios? (S/N)"
    if ($response -eq "S" -or $response -eq "s") {
        git add .
        $commitMessage = Read-Host "Mensaje de commit (Enter para usar default)"
        if ([string]::IsNullOrWhiteSpace($commitMessage)) {
            $commitMessage = "Prepare release v$Version"
        }
        git commit -m $commitMessage
        Write-Host "✓ Cambios commiteados" -ForegroundColor Green
    }
} else {
    Write-Host "✓ No hay cambios pendientes" -ForegroundColor Green
}

# Verificar remote
Write-Host ""
Write-Host "Verificando remote de GitHub..." -ForegroundColor Yellow
$remotes = git remote -v

if (-not $remotes) {
    if ([string]::IsNullOrWhiteSpace($GitHubUsername)) {
        $GitHubUsername = Read-Host "Ingresa tu usuario de GitHub"
    }
    
    Write-Host ""
    Write-Host "Opciones de conexión:" -ForegroundColor Yellow
    Write-Host "1. HTTPS (recomendado para principiantes)"
    Write-Host "2. SSH (requiere configuración previa)"
    $connectionType = Read-Host "Selecciona opción (1 o 2)"
    
    if ($connectionType -eq "1") {
        $remoteUrl = "https://github.com/$GitHubUsername/$RepositoryName.git"
    } else {
        $remoteUrl = "git@github.com:$GitHubUsername/$RepositoryName.git"
    }
    
    Write-Host "Agregando remote: $remoteUrl" -ForegroundColor Yellow
    git remote add origin $remoteUrl
    Write-Host "✓ Remote agregado" -ForegroundColor Green
} else {
    Write-Host "✓ Remote ya configurado" -ForegroundColor Green
    Write-Host $remotes
}

# Verificar branch
$currentBranch = git branch --show-current
Write-Host ""
Write-Host "Branch actual: $currentBranch" -ForegroundColor Yellow

if ($currentBranch -ne "main" -and $currentBranch -ne "master") {
    Write-Host "Renombrando branch a 'main'..." -ForegroundColor Yellow
    git branch -M main
    Write-Host "✓ Branch renombrado a 'main'" -ForegroundColor Green
}

# Push inicial
Write-Host ""
Write-Host "¿Deseas hacer push a GitHub? (S/N)" -ForegroundColor Yellow
$pushResponse = Read-Host

if ($pushResponse -eq "S" -or $pushResponse -eq "s") {
    Write-Host "Haciendo push a GitHub..." -ForegroundColor Yellow
    git push -u origin main
    Write-Host "✓ Push completado" -ForegroundColor Green
}

# Crear tag
Write-Host ""
Write-Host "¿Deseas crear el tag v$Version? (S/N)" -ForegroundColor Yellow
$tagResponse = Read-Host

if ($tagResponse -eq "S" -or $tagResponse -eq "s") {
    Write-Host "Creando tag v$Version..." -ForegroundColor Yellow
    git tag -a "v$Version" -m "Release v$Version - Primer Release Estable"
    Write-Host "✓ Tag creado" -ForegroundColor Green
    
    Write-Host ""
    Write-Host "¿Deseas hacer push del tag? (S/N)" -ForegroundColor Yellow
    $pushTagResponse = Read-Host
    
    if ($pushTagResponse -eq "S" -or $pushTagResponse -eq "s") {
        git push origin "v$Version"
        Write-Host "✓ Tag pusheado" -ForegroundColor Green
    }
}

# Resumen
Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Resumen" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "✓ Repositorio preparado" -ForegroundColor Green
Write-Host "✓ Versión: $Version" -ForegroundColor Green
Write-Host ""
Write-Host "Próximos pasos:" -ForegroundColor Yellow
Write-Host "1. Ve a https://github.com/$GitHubUsername/$RepositoryName" -ForegroundColor White
Write-Host "2. Click en 'Releases' → 'Create a new release'" -ForegroundColor White
Write-Host "3. Selecciona el tag 'v$Version'" -ForegroundColor White
Write-Host "4. Usa el contenido de RELEASE_NOTES_v1.0.0.md como descripción" -ForegroundColor White
Write-Host "5. Publica el release" -ForegroundColor White
Write-Host ""
Write-Host "¡Listo para publicar!" -ForegroundColor Green
