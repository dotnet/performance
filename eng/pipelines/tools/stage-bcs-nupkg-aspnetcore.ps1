<#
.SYNOPSIS
    Stages the per-RID Microsoft.AspNetCore.App runtime-pack nupkg as the Build
    Cache Service (BCS) artifact for the perf-build pipeline.

.DESCRIPTION
    The single, canonical "find the runtime-pack nupkg and stage it under the BCS
    artifact name" recipe used by every job in aspnetcore-perf-build-jobs.yml
    (Windows x64/x86/arm64, Linux x64, Linux arm64). Each build job invokes it as
    an explicit `pwsh` step with the RID(s) it produced.

    The BCS stores the runtime-pack nupkg VERBATIM (no extract/repack). A nupkg is
    a zip on every OS, so dotnet/crank's BuildCacheClient extracts it with the same
    ZipFile path on Windows and Linux; the nupkg's own `runtimes/{rid}/...` layout
    is the load-bearing contract consumed there. Storing the raw nupkg preserves the
    full shipped artifact (crank filters at consume time) rather than a lossy
    projection that cannot be re-derived for historical bisection.

    For each RID it:
      1. Locates artifacts/packages/Release/Shipping/Microsoft.AspNetCore.App.Runtime.{rid}.*.nupkg
         (excluding the *.symbols.nupkg), asserting exactly one match.
      2. Copies it to $StagingRoot/{artifactName}/{artifactName}.nupkg, renaming to
         the fixed, version-independent BCS artifact name crank resolves from a sha.

.PARAMETER Rids
    One or more .NET RIDs to stage (e.g. win-x64, win-x86, win-arm64, linux-x64,
    linux-arm64). os/arch and the BCS naming are derived from each RID.

.PARAMETER ShippingDir
    Directory containing the runtime-pack nupkgs. Defaults to
    $(Build.SourcesDirectory)/artifacts/packages/Release/Shipping so the pipeline
    can call this with no path arguments; override it for local runs.

.PARAMETER StagingRoot
    Directory under which the per-config artifact staging folders are created.
    Defaults to $(Build.ArtifactStagingDirectory)/bcs.

.EXAMPLE
    # In the pipeline (ShippingDir/StagingRoot resolved from BUILD_* env vars):
    ./stage-bcs-nupkg-aspnetcore.ps1 -Rids win-x64,win-x86,win-arm64

.EXAMPLE
    # Local run -- the BUILD_* env vars are not set off-agent, so pass the
    # directories explicitly:
    ./stage-bcs-nupkg-aspnetcore.ps1 -Rids linux-x64 `
        -ShippingDir ./artifacts/packages/Release/Shipping `
        -StagingRoot ./artifacts/bcs
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string[]]$Rids,

    # Default to the BUILD_* agent paths in the pipeline; fall back to the current
    # directory off-agent (those env vars are unset locally, and Join-Path $null
    # throws during parameter binding), keeping the documented local-override flow.
    [string]$ShippingDir = (Join-Path ($env:BUILD_SOURCESDIRECTORY ? $env:BUILD_SOURCESDIRECTORY : (Get-Location).Path) 'artifacts/packages/Release/Shipping'),

    [string]$StagingRoot = (Join-Path ($env:BUILD_ARTIFACTSTAGINGDIRECTORY ? $env:BUILD_ARTIFACTSTAGINGDIRECTORY : (Get-Location).Path) 'bcs')
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

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

    Write-Host ''
    Write-Host "=== Staging $configKey (rid=$rid) ==="

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
        # the Shipping dir; pick-first would silently stage the lexicographically
        # smallest (likely wrong) version, so fail loudly instead.
        $names = ($nupkgMatches | ForEach-Object { $_.Name }) -join ', '
        throw "Expected exactly one runtime pack nupkg matching '$pattern' under '$ShippingDir', found $($nupkgMatches.Count): $names."
    }
    $nupkg = $nupkgMatches[0]
    Write-Host "Found nupkg: $($nupkg.FullName)"

    # Stage the nupkg verbatim under the fixed, version-independent BCS artifact
    # name. The real nupkg filename embeds the build version (e.g.
    # ...win-x64.10.0.0-dev.nupkg), which crank cannot predict from a commit sha;
    # crank resolves the blob as .../{configKey}/{artifactFile} with a fixed
    # artifactFile, so we rename to the predictable name here.
    $stageDir = Join-Path $StagingRoot $artifactName
    if (Test-Path $stageDir) {
        Remove-Item -LiteralPath $stageDir -Recurse -Force
    }
    New-Item -ItemType Directory -Force -Path $stageDir | Out-Null

    $stagedPath = Join-Path $stageDir "$artifactName.nupkg"
    Copy-Item -LiteralPath $nupkg.FullName -Destination $stagedPath -Force

    $size = (Get-Item $stagedPath).Length
    Write-Host "Staged nupkg: $stagedPath ($size bytes)"
}
