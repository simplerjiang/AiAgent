param(
    [string]$Configuration = "Release",
    [string]$RuntimeIdentifier = "win-x64",
    [string]$OutputDir = ".\artifacts\windows-package",
    [bool]$SelfContained = $true
)

$ErrorActionPreference = "Stop"

$root = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$packageRoot = Join-Path $root $OutputDir
$backendOutput = Join-Path $packageRoot "Backend"
$financialWorkerOutput = Join-Path $packageRoot "FinancialWorker"

if (Test-Path $packageRoot) {
    Remove-Item $packageRoot -Recurse -Force
}

New-Item -ItemType Directory -Force -Path $packageRoot | Out-Null

Write-Host "Building frontend..."
$frontendDir = Join-Path $root "frontend"
# Use cmd /c so that Vite chunk-size warnings on stderr do not trigger
# PowerShell's $ErrorActionPreference = "Stop" for NativeCommandError.
cmd /c "npm --prefix `"$frontendDir`" run build 2>&1"
if ($LASTEXITCODE -ne 0) {
    throw "Frontend build failed with exit code $LASTEXITCODE"
}

Write-Host "Publishing backend..."
dotnet publish (Join-Path $root "backend/SimplerJiangAiAgent.Api/SimplerJiangAiAgent.Api.csproj") `
    -c $Configuration `
    -r $RuntimeIdentifier `
    --self-contained $SelfContained `
    -o $backendOutput

Write-Host "Copying frontend dist into backend package..."
$backendFrontendDir = Join-Path $backendOutput "frontend\dist"
New-Item -ItemType Directory -Force -Path $backendFrontendDir | Out-Null
Copy-Item (Join-Path $root "frontend/dist/*") $backendFrontendDir -Recurse -Force

Write-Host "Seeding default LLM settings without local secrets..."
$backendAppDataDir = Join-Path $backendOutput "App_Data"
New-Item -ItemType Directory -Force -Path $backendAppDataDir | Out-Null
Copy-Item (Join-Path $root "backend/SimplerJiangAiAgent.Api/App_Data/llm-settings.json") $backendAppDataDir -Force
Remove-Item (Join-Path $backendAppDataDir "llm-settings.local.json") -Force -ErrorAction SilentlyContinue

Write-Host "Publishing financial worker..."
dotnet publish (Join-Path $root "backend/SimplerJiangAiAgent.FinancialWorker/SimplerJiangAiAgent.FinancialWorker.csproj") `
    -c $Configuration `
    -r $RuntimeIdentifier `
    --self-contained $SelfContained `
    -o $financialWorkerOutput

Write-Host "Publishing desktop host..."
dotnet publish (Join-Path $root "desktop/SimplerJiangAiAgent.Desktop/SimplerJiangAiAgent.Desktop.csproj") `
    -c $Configuration `
    -r $RuntimeIdentifier `
    --self-contained $SelfContained `
    -o $packageRoot

Write-Host "Package ready: $packageRoot"
Write-Host "Main executable: $(Join-Path $packageRoot 'SimplerJiangAiAgent.Desktop.exe')"