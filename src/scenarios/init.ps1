[CmdletBinding(PositionalBinding=$false)]
Param(
    [switch] $Help,
    [string] $Channel,
    [string] $DotnetDirectory
)

function Print-Usage(){
    Write-Host "This script sets up PYTHONPATH and determines which dotnet to use."
    Write-Host "Choose ONE of the following commands:"
    Write-Host ".\init.ps1                                                                                     # sets up PYTHONPATH only; uses default dotnet in PATH" 
    Write-Host ".\init.ps1 -DotnetDirectory <custom dotnet root directory; ex: 'C:\Program Files\dotnet\'>     # sets up PYTHONPATH; uses the specified dotnet"
    Write-Host ".\init.ps1 -Channel <channel to download new dotnet; ex: 'master'>                             # sets up PYTHONPATH; downloads dotnet from the specified channel or branch to <repo root>\tools\ and uses it\n For a list of channels, check <repo root>\scripts\channel_map.py"     
    Exit 1
}

function Setup-Env($directory){
    $env:Path="$directory;$env:Path"
    $env:DOTNET_ROOT=$directory
    $env:DOTNET_CLI_TELEMETRY_OPTOUT='1'
    $env:DOTNET_MULTILEVEL_LOOKUP='0'
    $env:UseSharedCompilation='false'
    Write-Host "Current dotnet directory: $env:DOTNET_ROOT"
    Write-Host "If more than one version exist in this directory, usually the latest runtime and sdk will be used."
}

function Download-Dotnet($channel){
    Write-Host "Downloading dotnet from channel $channel"
    $dotnetScript= Join-Path "$scripts" 'dotnet.py' -Resolve
    py -3 $dotnetScript install --channels $channel -v
    If (!$?){
        Write-Host "Dotnet installation failed."
        Exit 1
    }
}

# Add scripts and current directory to PYTHONPATH
$scripts = Join-Path $PSScriptRoot '..\..\scripts' -Resolve
$env:PYTHONPATH="$scripts;$PSScriptRoot"
Write-Host PYTHONPATH=$env:PYTHONPATH

# Parse arguments
If ($Help) {
    Print-Usage
}
ElseIf (($Channel -ne "") -and ($DotnetDirectory -ne "")) {
    Print-Usage
}
ElseIf ($DotnetDirectory -ne ""){
    Setup-Env -directory $DotnetDirectory
}
ElseIf ($Channel -ne "") {
    Download-Dotnet -channel $Channel
    $DotnetDirectory = Join-Path $PSScriptRoot '..\..\tools\dotnet\x64'
    Setup-Env -directory $DotnetDirectory
}
Else {
    Write-Host "Existing dotnet directory in PATH will be used."
}
