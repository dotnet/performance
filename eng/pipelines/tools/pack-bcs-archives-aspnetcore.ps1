<#
.SYNOPSIS
    Packs per-RID Microsoft.AspNetCore.App runtime packs into Build Cache
    Service (BCS) archives for the perf-build pipeline.

.DESCRIPTION
    This is the single, canonical "find the runtime-pack nupkg and zip it into
    the BCS archive layout" recipe used by every job in
    aspnetcore-perf-build-jobs.yml (Windows x64/x86/arm64, Linux x64, Linux
    arm64). Each build job invokes it as an explicit `pwsh` step (pwsh, NOT
    Windows PowerShell 5.1, whose Compress-Archive writes backslash separators
    that corrupt the archive for crank) with the RID(s) it produced and the
    archive format that matches its platform convention (.zip on Windows,
    .tar.gz on Linux).

    For each RID it:
      1. Locates artifacts/packages/Release/Shipping/Microsoft.AspNetCore.App.Runtime.{rid}.*.nupkg
         (excluding the *.symbols.nupkg).
      2. Extracts it into the LOWERCASE archive root
         microsoft.aspnetcore.app.runtime.{rid}/Release/ -- this directory name
         is a load-bearing contract for the future dotnet/crank aspnetcore
         overlay path, which mirrors BuildCacheClient's
         FindDirectory(extractDir, "microsoft.aspnetcore.app.runtime.{rid}").
      3. Validates the managed Microsoft.AspNetCore.*.dll assemblies landed under
         runtimes/{rid}/lib and strips any stray *.pdb / *.dbg files.
      4. Compresses the lowercase root directory into the per-config archive.
      5. Asserts the archive's single root entry matches the contract.

.PARAMETER Rids
    One or more .NET RIDs to pack (e.g. win-x64, win-x86, win-arm64, linux-x64,
    linux-arm64). os/arch and the BCS naming are derived from each RID.

.PARAMETER Format
    Archive format: 'zip' (Windows convention) or 'targz' (Linux convention).

.PARAMETER ShippingDir
    Directory containing the runtime-pack nupkgs. Defaults to
    $(Build.SourcesDirectory)/artifacts/packages/Release/Shipping so the
    pipeline can call this with no path arguments; override it for local runs.

.PARAMETER StagingRoot
    Directory under which the per-config archive staging folders are created.
    Defaults to $(Build.ArtifactStagingDirectory)/bcs.

.EXAMPLE
    # In the pipeline (ShippingDir/StagingRoot resolved from BUILD_* env vars):
    ./pack-bcs-archives-aspnetcore.ps1 -Rids win-x64,win-x86,win-arm64 -Format zip

.EXAMPLE
    # Local run -- the BUILD_* env vars are not set off-agent, so pass the
    # directories explicitly:
    ./pack-bcs-archives-aspnetcore.ps1 -Rids linux-x64 -Format targz `
        -ShippingDir ./artifacts/packages/Release/Shipping `
        -StagingRoot ./artifacts/bcs
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string[]]$Rids,

    [Parameter(Mandatory = $true)]
    [ValidateSet('zip', 'targz')]
    [string]$Format,

    # Default to the BUILD_* agent paths in the pipeline; fall back to the current
    # directory off-agent (those env vars are unset locally, and Join-Path $null
    # throws during parameter binding), keeping the documented local-override flow.
    [string]$ShippingDir = (Join-Path ($env:BUILD_SOURCESDIRECTORY ? $env:BUILD_SOURCESDIRECTORY : (Get-Location).Path) 'artifacts/packages/Release/Shipping'),

    [string]$StagingRoot = (Join-Path ($env:BUILD_ARTIFACTSTAGINGDIRECTORY ? $env:BUILD_ARTIFACTSTAGINGDIRECTORY : (Get-Location).Path) 'bcs')
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
Add-Type -AssemblyName System.IO.Compression.FileSystem

function Get-RidParts {
    param([string]$Rid)

    $dash = $Rid.IndexOf('-')
    if ($dash -lt 1) {
        throw "RID '$Rid' is not in the expected '<os>-<arch>' form."
    }

    $osToken = $Rid.Substring(0, $dash)
    $arch = $Rid.Substring($dash + 1)
    $os = switch ($osToken) {
        'win'   { 'windows' }
        'linux' { 'linux' }
        'osx'   { 'osx' }
        default { throw "Unsupported RID os token '$osToken' in RID '$Rid'." }
    }

    return [pscustomobject]@{ Os = $os; Arch = $arch }
}

if (-not (Test-Path $ShippingDir)) {
    throw "Shipping directory '$ShippingDir' does not exist."
}

New-Item -ItemType Directory -Force -Path $StagingRoot | Out-Null

foreach ($rid in $Rids) {
    $parts = Get-RidParts -Rid $rid
    $os = $parts.Os
    $arch = $parts.Arch
    $configKey = "aspnetcore_${arch}_${os}"
    $artifactName = "BuildArtifacts_${os}_${arch}_Release_aspnetcore"
    $ext = if ($Format -eq 'zip') { 'zip' } else { 'tar.gz' }
    $archiveFile = "${artifactName}.${ext}"

    Write-Host ''
    Write-Host "=== Packing $configKey (rid=$rid, format=$Format) ==="

    $pattern = "Microsoft.AspNetCore.App.Runtime.$rid.*.nupkg"
    $nupkgMatches = @(Get-ChildItem -Path $ShippingDir -Filter $pattern -File -ErrorAction SilentlyContinue |
        Where-Object { $_.Name -notmatch '\.symbols\.nupkg$' } |
        Sort-Object Name)
    if ($nupkgMatches.Count -eq 0) {
        throw "Could not find runtime pack nupkg matching '$pattern' under '$ShippingDir'."
    }
    if ($nupkgMatches.Count -gt 1) {
        # A clean from-source build produces exactly one non-symbols runtime pack
        # per RID. More than one means a stale/duplicate version is lingering in
        # the Shipping dir; pick-first would silently pack the lexicographically
        # smallest (likely wrong) version, so fail loudly instead.
        $names = ($nupkgMatches | ForEach-Object { $_.Name }) -join ', '
        throw "Expected exactly one runtime pack nupkg matching '$pattern' under '$ShippingDir', found $($nupkgMatches.Count): $names."
    }
    $nupkg = $nupkgMatches[0]
    Write-Host "Found nupkg: $($nupkg.FullName)"

    # microsoft.aspnetcore.app.runtime.{rid}/Release/  <-- archive root (lowercase!)
    $stageDir = Join-Path $StagingRoot $artifactName
    $payloadDir = Join-Path $stageDir "microsoft.aspnetcore.app.runtime.$rid"
    $releaseDir = Join-Path $payloadDir 'Release'
    if (Test-Path $payloadDir) {
        Remove-Item -LiteralPath $payloadDir -Recurse -Force
    }
    New-Item -ItemType Directory -Force -Path $releaseDir | Out-Null

    # Extract the nupkg (it's a zip) into Release/. Using the .NET API keeps the
    # extraction identical on Windows and Linux agents (no unzip/Expand-Archive split).
    [System.IO.Compression.ZipFile]::ExtractToDirectory($nupkg.FullName, $releaseDir)

    $runtimesDir = Join-Path $releaseDir "runtimes/$rid"
    if (-not (Test-Path $runtimesDir)) {
        throw "Extracted runtime pack is missing expected directory '$runtimesDir'."
    }

    # Sanity-check that managed assemblies landed where we expect.
    $libMatches = @(Get-ChildItem -Path (Join-Path $runtimesDir 'lib') -Recurse -Filter 'Microsoft.AspNetCore.*.dll' -ErrorAction SilentlyContinue)
    if ($libMatches.Count -eq 0) {
        throw "Extracted runtime pack for $rid is missing managed Microsoft.AspNetCore.*.dll under 'runtimes/$rid/lib'."
    }
    Write-Host "Validated managed lib dir contains $($libMatches.Count) Microsoft.AspNetCore.*.dll(s)."

    # Defensive: strip debug-symbol files that occasionally tag along.
    Get-ChildItem -Path $runtimesDir -Recurse -Include '*.pdb', '*.dbg' -ErrorAction SilentlyContinue |
        ForEach-Object { Remove-Item -LiteralPath $_.FullName -Force }

    $archivePath = Join-Path $stageDir $archiveFile
    if (Test-Path $archivePath) {
        Remove-Item -LiteralPath $archivePath -Force
    }

    $expectedRoot = "microsoft.aspnetcore.app.runtime.$rid/"

    if ($Format -eq 'zip') {
        # Zip the lowercase root directory so the archive contents start with
        # `microsoft.aspnetcore.app.runtime.{rid}/...`.
        Compress-Archive -Path $payloadDir -DestinationPath $archivePath -CompressionLevel Optimal -Force

        # Assert the archive root matches the load-bearing contract.
        $zip = [System.IO.Compression.ZipFile]::OpenRead($archivePath)
        try {
            $rootEntries = @($zip.Entries | ForEach-Object { ($_.FullName -split '/')[0] } | Sort-Object -Unique)
            if ($rootEntries.Count -ne 1 -or "$($rootEntries[0])/" -ne $expectedRoot) {
                throw "Archive '$archivePath' has root entries [$($rootEntries -join ', ')] but expected exactly '$expectedRoot' (load-bearing contract for future crank PR)."
            }
        }
        finally {
            $zip.Dispose()
        }
    }
    else {
        # Tar the lowercase root directory (so the archive contents start with
        # `microsoft.aspnetcore.app.runtime.{rid}/...`).
        & tar -czf $archivePath -C $stageDir "microsoft.aspnetcore.app.runtime.$rid"
        if ($LASTEXITCODE -ne 0) {
            throw "tar of '$payloadDir' failed (exit $LASTEXITCODE)."
        }

        # Assert the archive root matches the load-bearing contract.
        $firstEntry = (& tar -tzf $archivePath | Select-Object -First 1)
        if (-not $firstEntry.StartsWith($expectedRoot)) {
            throw "Archive '$archivePath' first entry '$firstEntry' does not start with '$expectedRoot' (load-bearing contract for future crank PR)."
        }
    }

    # Drop the working payload directory so only the archive ends up in the published pipeline artifact.
    Remove-Item -LiteralPath $payloadDir -Recurse -Force

    $size = (Get-Item $archivePath).Length
    Write-Host "Produced archive: $archivePath ($size bytes)"
}
