[CmdletBinding(PositionalBinding=$false)]
Param(
    [string] $Channel,
    [string] $DotnetDirectory
)

function Print-Usage(){
    Write-Host "Invalid argument. Usage:"
    Write-Host ".\init.ps1"
    Write-Host ".\init.ps1 -DotnetDirectory <custom dotnet directory>"
    Write-Host ".\init.ps1 -Channel <channel to download new dotnet>"
    Exit 1
}

# Parse arguments
If (($Channel -ne "") -and ($DotnetDirectory -ne "")) {
    Print-Usage
}

If ($DotnetDirectory -eq "" ){
    $DotnetDirectory = Join-Path $PSScriptRoot '..\..\tools\dotnet\x64'
}

# Add scripts and current directory to PYTHONPATH
$scripts = Join-Path $PSScriptRoot '..\..\scripts' -Resolve
$env:PYTHONPATH="$scripts;$PSScriptRoot"

If (($Channel -ne "")){
    # Download dotnet from the specified channel
    Write-Host "Downloading dotnet from channel $channel"
    $dotnetScript= Join-Path "$scripts" 'dotnet.py' -Resolve
    python $dotnetScript install --channels $channel -v
    If (!$?){
        Write-Host "Dotnet installation failed."
        Exit 1
    }
}

$env:Path="$DotnetDirectory;$env:Path"
$env:DOTNET_ROOT=$DotnetDirectory
$env:DOTNET_CLI_TELEMETRY_OPTOUT='1'
$env:DOTNET_MULTILEVEL_LOOKUP='0'
$env:UseSharedCompilation='false'



