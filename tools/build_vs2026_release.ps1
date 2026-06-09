param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
$solution = Join-Path $root "JCLib.VisualStudio.sln"
$vswhere = Join-Path ${env:ProgramFiles(x86)} "Microsoft Visual Studio\Installer\vswhere.exe"

if (-not (Test-Path $vswhere)) {
    throw "vswhere.exe est introuvable. Installe Visual Studio 2026 et la charge de travail Visual Studio extension development."
}

$msbuild = & $vswhere -latest -products * -requires Microsoft.Component.MSBuild -find "MSBuild\**\Bin\MSBuild.exe" | Select-Object -First 1
if ([string]::IsNullOrWhiteSpace($msbuild) -or -not (Test-Path $msbuild)) {
    throw "MSBuild est introuvable dans l'installation Visual Studio détectée."
}

Write-Host "MSBuild : $msbuild"
Write-Host "Solution : $solution"
& $msbuild $solution /restore /m "/p:Configuration=$Configuration" "/p:Platform=Any CPU"
if ($LASTEXITCODE -ne 0) {
    throw "La compilation MSBuild a échoué avec le code $LASTEXITCODE."
}

$projectOutput = Join-Path $root "src\JCLib.VisualStudio\bin\$Configuration\net472"
$vsix = Get-ChildItem -Path $projectOutput -Filter "*.vsix" -Recurse | Select-Object -First 1
if ($null -eq $vsix) {
    throw "La compilation a réussi mais aucun fichier VSIX n'a été trouvé sous $projectOutput."
}

Write-Host "VSIX généré : $($vsix.FullName)"
Write-Host "Installe ce VSIX puis exécute docs\MANUAL_TEST_CHECKLIST_1.3.11.md dans l'instance expérimentale Visual Studio 2026."
