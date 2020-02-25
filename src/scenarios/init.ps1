Param(
    [string] $InstallDotnetFromChannel,
    [string] $DotnetDirectory
)

# Add scripts and current directory to PYTHONPATH
$scripts = Join-Path $PSScriptRoot '..\..\scripts' -Resolve
$env:PYTHONPATH="$scripts;$PSScriptRoot"

If (($InstallDotnetFromChannel -ne "") -and ($DotnetDirectory -eq "")){
    # Download dotnet from the specified channel
    
    $installDirectory= Join-Path $PSScriptRoot 'dotnet'
    # Remove existing dotnet directory to make sure we only have one version of dotnet
    If (Test-Path $installDirectory){
        Write-Host "Removing $installDirectory ..."
        Remove-Item  $installDirectory -Recurse
    }

    Write-Host "Downloading dotnet from channel $InstallDotnetFromChannel"
    $dotnetScript= Join-Path "$scripts" 'dotnet.py' -Resolve
    python $dotnetScript install --channels $InstallDotnetFromChannel --install-dir $installDirectory

    If (!$?){
        Write-Host "Dotnet installation failed."
        Exit 1
    }
}

If ($DotnetDirectory -ne ""){
    $env:Path="$DotnetDirectory;$env:Path"
    $env:DOTNET_ROOT=$DotnetDirectory
}
Elseif ($installDirectory -ne ""){
    $env:Path="$installDirectory;$env:Path"
    $env:DOTNET_ROOT=$installDirectory
}

$env:DOTNET_CLI_TELEMETRY_OPTOUT='1'
$env:DOTNET_MULTILEVEL_LOOKUP='0'
$env:UseSharedCompilation='false'

dotnet --info


