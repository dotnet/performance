[CmdletBinding()]
param(
    [string]$SourceDirectory,
    [string]$Commit = 'main'
)

$ErrorActionPreference = 'Stop'
$PSNativeCommandUseErrorActionPreference = $false
$repository = 'https://dev.azure.com/devdiv/DevDiv/_git/DotNet-msbuild-Trusted'

if ([string]::IsNullOrWhiteSpace($env:MSBUILD_SOURCE_ACCESS_TOKEN)) {
    throw 'Map a DevDiv-authorized federated token to MSBUILD_SOURCE_ACCESS_TOKEN.'
}
if ($Commit -ne 'main' -and $Commit -cnotmatch '^[0-9a-fA-F]{40}$') {
    throw 'Select main or a full MSBuild commit SHA.'
}

$connection = Invoke-RestMethod `
    -Uri 'https://dev.azure.com/devdiv/_apis/connectionData' `
    -Headers @{ Authorization = "Bearer $env:MSBUILD_SOURCE_ACCESS_TOKEN" } `
    -TimeoutSec 60
$identity = $connection.authenticatedUser
if ([string]::IsNullOrWhiteSpace($identity.providerDisplayName) -or
    [string]::IsNullOrWhiteSpace($identity.id)) {
    throw 'DevDiv did not return the authenticated source identity.'
}
Write-Host "DevDiv source identity: $($identity.providerDisplayName)"
Write-Host "DevDiv source identity ID: $($identity.id)"
Write-Host "Checking source read access with the federated token: $repository"

$variables = @('GIT_CONFIG_COUNT', 'GIT_CONFIG_KEY_0', 'GIT_CONFIG_VALUE_0',
    'GIT_TERMINAL_PROMPT', 'GCM_INTERACTIVE')
$previous = @{}
foreach ($name in $variables) {
    $previous[$name] = [Environment]::GetEnvironmentVariable($name, 'Process')
}
try {
    $env:GIT_CONFIG_COUNT = '1'
    $env:GIT_CONFIG_KEY_0 = "http.$repository.extraheader"
    $env:GIT_CONFIG_VALUE_0 = "AUTHORIZATION: bearer $env:MSBUILD_SOURCE_ACCESS_TOKEN"
    $env:GIT_TERMINAL_PROMPT = '0'
    $env:GCM_INTERACTIVE = 'Never'

    $refs = @(git -c credential.helper= ls-remote --exit-code $repository refs/heads/main)
    if ($LASTEXITCODE -ne 0) {
        throw 'The federated source credential cannot read the MSBuild mirror main ref. Check the preceding Git error for authorization or connectivity failures; local developer access does not establish pipeline access.'
    }
    if ($refs.Count -ne 1 -or $refs[0] -cnotmatch '^([0-9a-f]{40})\s+refs/heads/main$') {
        throw 'The MSBuild mirror did not return exactly one valid main ref.'
    }
    $latest = $Matches[1]
    Write-Host "MSBuild mirror main SHA: $latest"

    if (-not [string]::IsNullOrWhiteSpace($SourceDirectory)) {
        if (Test-Path -LiteralPath $SourceDirectory) {
            throw 'Use a fresh MSBuild checkout directory.'
        }
        git init $SourceDirectory
        if ($LASTEXITCODE -ne 0) { throw 'MSBuild git init failed.' }
        git -C $SourceDirectory remote add origin $repository
        if ($LASTEXITCODE -ne 0) { throw 'MSBuild git remote failed.' }
        git -C $SourceDirectory fetch --filter=blob:none origin main
        if ($LASTEXITCODE -ne 0) { throw 'MSBuild mirror fetch failed.' }
        $selected = if ($Commit -eq 'main') { $latest } else { $Commit }
        git -C $SourceDirectory merge-base --is-ancestor $selected origin/main
        if ($LASTEXITCODE -ne 0) { throw 'The selected commit is not on the MSBuild mirror main branch.' }
        git -C $SourceDirectory checkout --detach $selected
        if ($LASTEXITCODE -ne 0) { throw 'MSBuild mirror checkout failed.' }
        Write-Host "Checked out MSBuild mirror SHA: $selected"
    }
}
finally {
    foreach ($name in $variables) {
        [Environment]::SetEnvironmentVariable($name, $previous[$name], 'Process')
    }
}
