[cmdletbinding()]
param(
    [string] $InstallDir = "<auto>",
    [string] $Architecture = "x64",
    [string] $Runtime = "win7-x64",
    [string] $CreateStoreProjPath)

Set-StrictMode -Version Latest
$ErrorActionPreference="Stop"
$ProgressPreference="SilentlyContinue"

. "$(Split-Path $MyInvocation.MyCommand.Path -Parent)\AspNet-Shared.ps1"

# Create the installation directory and normalize to a fully qualified path
$InstallDir = New-InstallDirectory -Directory $InstallDir -Default ".store" -Clean -Create

# Blow away .temp - this will be used as a working directory by dotnet store, but it's
# bad at cleaning up after itself.
$temp = New-InstallDirectory -Directory "<auto>" -Default ".temp" -Clean

$Framework = $env:SCENARIOS_TARGET_FRAMEWORK_MONIKER
$FrameworkVersion = $env:SCENARIOS_FRAMEWORK_VERSION

Write-Host -ForegroundColor Green "Running dotnet store --manifest" $CreateStoreProjPath "-f" $Framework "-r" $Runtime "--framework-version" $FrameworkVersion "-w" $temp "-o" $InstallDir "--skip-symbols"
& "dotnet" "store", "--manifest", "$CreateStoreProjPath", "-f", "$Framework", "-r" "$Runtime" "--framework-version", "$FrameworkVersion", "-w", $temp, "-o", "$InstallDir", "--skip-symbols"
if ($LastExitCode -ne 0)
{
    throw "dotnet store failed."
}
$BinariesDirectory = $InstallDir
$Manifest = [System.IO.Path]::Combine($InstallDir, $Architecture, $Framework, 'artifact.xml')
Write-Host -ForegroundColor Green "Setting SCENARIOS_ASPNET_MANIFEST to $Manifest"

Write-Host -ForegroundColor Green "Setting DOTNET_SHARED_STORE to $BinariesDirectory"
$env:DOTNET_SHARED_STORE = $BinariesDirectory