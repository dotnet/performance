Param(
    $Channel = 'master' # default is master
)

$scripts = Join-Path $PSScriptRoot '..\..\scripts' -Resolve
$env:PYTHONPATH="$scripts;$PSScriptRoot"
$dotnetScript= Join-Path "$scripts" 'dotnet.py' -Resolve
$dotnetDirectory = Join-Path $PSScriptRoot '..\..\tools\dotnet\x64'

Write-Host "Downloading dotnet from channel $Channel"
python $dotnetScript install --channels $Channel -v

if (!$?){
    Write-Host "Dotnet installation failed."
    Exit 1
}

$env:DOTNET_CLI_TELEMETRY_OPTOUT='1'
$env:DOTNET_MULTILEVEL_LOOKUP='0'
$env:UseSharedCompilation='false'
$env:DOTNET_ROOT=$dotnetDirectory
$env:Path="$dotnetDirectory;$env:Path"


