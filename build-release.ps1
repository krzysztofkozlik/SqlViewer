#Requires -Version 5.1
<#
.SYNOPSIS
    Builds a release package for SqlViewer (backend + frontend combined).

.PARAMETER SelfContained
    Bundle the .NET 9 runtime into the executable.
    Produces a larger file (~150 MB) but the target machine needs no .NET installation.
    Without this flag (default) the exe is ~15 MB but requires .NET 9 runtime on the machine.

.EXAMPLE
    .\build-release.ps1                  # framework-dependent (smaller, .NET 9 required)
    .\build-release.ps1 -SelfContained   # self-contained  (larger, no .NET required)
#>
param(
    [switch]$SelfContained
)

$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot

function Step([string]$msg) { Write-Host "`n==> $msg" -ForegroundColor Cyan }
function Ok([string]$msg)   { Write-Host "    $msg"   -ForegroundColor Green }
function Warn([string]$msg) { Write-Host "    $msg"   -ForegroundColor Yellow }

# ---------------------------------------------------------------------------
Step "Building Angular frontend (production)..."
Push-Location "$root\src\SqlViewer.Web"
try   { npx ng build --configuration production }
finally { Pop-Location }

# ---------------------------------------------------------------------------
Step "Copying Angular output to wwwroot..."

# The Angular application builder outputs to dist\SqlViewer.Web\browser\
$angularOut = "$root\src\SqlViewer.Web\dist\SqlViewer.Web\browser"
if (-not (Test-Path $angularOut)) {
    # Fallback: older layout without browser\ subdirectory
    $angularOut = "$root\src\SqlViewer.Web\dist\SqlViewer.Web"
}

$wwwroot = "$root\src\SqlViewer.Api\wwwroot"
if (Test-Path $wwwroot) { Remove-Item $wwwroot -Recurse -Force }
Copy-Item $angularOut -Destination $wwwroot -Recurse -Force
Ok "Copied from: $angularOut"

# ---------------------------------------------------------------------------
Step "Publishing .NET backend..."

$releaseDir = "$root\release"
if (Test-Path $releaseDir) { Remove-Item $releaseDir -Recurse -Force }

$sc = if ($SelfContained) { 'true' } else { 'false' }

dotnet publish "$root\src\SqlViewer.Api\SqlViewer.Api.csproj" `
    --configuration Release `
    --runtime win-x64 `
    --self-contained $sc `
    -p:PublishSingleFile=true `
    -p:DebugType=none `
    --output $releaseDir `
    --source https://api.nuget.org/v3/index.json

# Remove files that are not needed at runtime.
# appsettings.Development.json  — dev-only logging overrides
# *.staticwebassets.endpoints.json — build artifact for MapStaticAssets(); we use UseStaticFiles()
Remove-Item "$releaseDir\appsettings.Development.json"       -Force -ErrorAction SilentlyContinue
Remove-Item "$releaseDir\*.staticwebassets.endpoints.json"   -Force -ErrorAction SilentlyContinue

# ---------------------------------------------------------------------------
Step "Creating release zip..."
$zip = "$root\release.zip"
if (Test-Path $zip) { Remove-Item $zip -Force }
Compress-Archive -Path "$releaseDir\*" -DestinationPath $zip
Ok "Archive: $zip"

# ---------------------------------------------------------------------------
Write-Host ""
Write-Host "Build complete." -ForegroundColor Green
Write-Host ""
Warn "Before distributing / running:"
Warn "  1. Edit release\appsettings.json  (set MonitoringConnectionString, MonitoredLogin, MonitoredDatabase)"
Warn "  2. Ensure C:\temp\ exists on the target machine (XEvent file directory)"
if (-not $SelfContained) {
    Warn "  3. .NET 9 runtime must be installed: https://dot.net/9 (use -SelfContained to bundle it)"
}
Write-Host ""
Ok "Run:  .\release\SqlViewer.Api.exe"
Ok "Open: http://localhost:5050"
