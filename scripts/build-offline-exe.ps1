param(
    [string]$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path,
    [string]$FrontendRoot = "",
    [string]$OutputDir = ""
)

$ErrorActionPreference = "Stop"

if ([string]::IsNullOrWhiteSpace($FrontendRoot)) {
    $FrontendRoot = (Resolve-Path (Join-Path $RepoRoot "..\\ERPv2 Angular")).Path
}

if ([string]::IsNullOrWhiteSpace($OutputDir)) {
    $OutputDir = Join-Path $RepoRoot "artifacts\\offline"
}

$apiProject = Join-Path $RepoRoot "src\\ERP.API\\ERP.API.csproj"
$apiRoot = Split-Path $apiProject -Parent
$apiAppSettings = Join-Path $apiRoot "appsettings.json"
$offlineAppSettings = Join-Path $apiRoot "appsettings.Offline.json"
$appSettingsBackup = Join-Path $apiRoot "appsettings.online.backup.tmp"

$wwwRoot = Join-Path $apiRoot "wwwroot"
$downloadsDir = Join-Path $wwwRoot "downloads"
$frontendDist = Join-Path $FrontendRoot "dist\\ERPv2\\browser"

$publishDir = Join-Path $OutputDir "publish"
$offlineExe = Join-Path $OutputDir "ERPv2-Offline.exe"
$offlineZip = Join-Path $OutputDir "ERPv2-Offline-Package.zip"
$downloadTarget = Join-Path $downloadsDir "ERPv2-Offline.exe"
$downloadZipTarget = Join-Path $downloadsDir "ERPv2-Offline-Package.zip"

Write-Host "1) Frontend build baslatiliyor..."
Push-Location $FrontendRoot
npm run build
Pop-Location

if (!(Test-Path $frontendDist)) {
    throw "Angular build cikti klasoru bulunamadi: $frontendDist"
}

Write-Host "2) API wwwroot guncelleniyor..."
if (Test-Path $wwwRoot) {
    Get-ChildItem -Path $wwwRoot -Force | Remove-Item -Recurse -Force
}
New-Item -ItemType Directory -Path $wwwRoot -Force | Out-Null
Copy-Item -Path (Join-Path $frontendDist "*") -Destination $wwwRoot -Recurse -Force

Write-Host "3) Offline konfig ile publish aliniyor..."
Copy-Item -Path $apiAppSettings -Destination $appSettingsBackup -Force
Copy-Item -Path $offlineAppSettings -Destination $apiAppSettings -Force

try {
    if (Test-Path $publishDir) {
        Remove-Item -LiteralPath $publishDir -Recurse -Force
    }
    New-Item -ItemType Directory -Path $publishDir -Force | Out-Null

    dotnet publish $apiProject `
        -c Release `
        -r win-x64 `
        --self-contained true `
        /p:PublishSingleFile=true `
        /p:IncludeNativeLibrariesForSelfExtract=true `
        /p:IncludeAllContentForSelfExtract=true `
        /p:PublishTrimmed=false `
        -o $publishDir
}
finally {
    if (Test-Path $appSettingsBackup) {
        Move-Item -Path $appSettingsBackup -Destination $apiAppSettings -Force
    }
}

$publishedExe = Join-Path $publishDir "ERP.API.exe"
if (!(Test-Path $publishedExe)) {
    throw "Publish sonrasi exe bulunamadi: $publishedExe"
}

Write-Host "4) Offline exe ve paket dosyalari olusturuluyor..."
New-Item -ItemType Directory -Path $OutputDir -Force | Out-Null
Copy-Item -Path $publishedExe -Destination $offlineExe -Force

if (Test-Path $offlineZip) {
    Remove-Item -LiteralPath $offlineZip -Force
}
Compress-Archive -Path (Join-Path $publishDir "*") -DestinationPath $offlineZip -Force

Write-Host "5) Web download dizinine kopyalaniyor..."
New-Item -ItemType Directory -Path $downloadsDir -Force | Out-Null
Copy-Item -Path $offlineExe -Destination $downloadTarget -Force
Copy-Item -Path $offlineZip -Destination $downloadZipTarget -Force

Write-Host ""
Write-Host "Hazirlandi:"
Write-Host " - EXE      : $offlineExe"
Write-Host " - ZIP      : $offlineZip"
Write-Host " - Web Link : /downloads/ERPv2-Offline-Package.zip"
