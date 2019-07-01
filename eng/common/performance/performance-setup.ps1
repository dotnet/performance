Param(
    [string] $SourceDirectory=$env:BUILD_SOURCESDIRECTORY,
    [string] $CoreRootDirectory,
    [string] $Architecture="x64",
    [string] $Framework="netcoreapp3.0",
    [string] $CompilationMode="Tiered",
    [string] $Repository=$env:BUILD_REPOSITORY_NAME,
    [string] $Branch=$env:BUILD_SOURCEBRANCH,
    [string] $CommitSha=$env:BUILD_SOURCEVERSION,
    [string] $BuildNumber=$env:BUILD_BUILDNUMBER,
    [string] $RunCategories="coreclr corefx",
    [string] $Csproj="src\benchmarks\micro\MicroBenchmarks.csproj",
    [string] $Kind="micro",
    [switch] $Internal,
    [string] $Configurations="CompilationMode=$CompilationMode"
)

$RunFromPerformanceRepo = ($Repository -eq "dotnet/performance")
$UseCoreRun = ($CoreRootDirectory -ne [string]::Empty)

$PayloadDirectory = (Join-Path $SourceDirectory "Payload")
$PerformanceDirectory = (Join-Path $PayloadDirectory "performance")
$WorkItemDirectory = (Join-Path $SourceDirectory "workitem")
$Creator = ""

if ($Internal) {
    $Queue = "Windows.10.Amd64.ClientRS1.Perf"
    $PerfLabArguments = "--upload-to-perflab-container"
    $ExtraBenchmarkDotNetArguments = ""
    $Creator = ""
    $HelixSourcePrefix = "official"
}
else {
    if ($Framework.StartsWith("netcoreapp")) {
        $Queue = "Windows.10.Amd64.ClientRS4.Open"
    }
    else {
        $Queue = "Windows.10.Amd64.ClientRS4.DevEx.15.8.Open"
    }
    $ExtraBenchmarkDotNetArguments = "--iterationCount 1 --warmupCount 0 --invocationCount 1 --unrollFactor 1 --strategy ColdStart --stopOnFirstError true"
    $Creator = $env:BUILD_DEFINITIONNAME
    $PerfLabArguments = ""
    $HelixSourcePrefix = "pr"
}

$CommonSetupArguments="--frameworks $Framework --queue $Queue --build-number $BuildNumber --build-configs $Configurations"

if ($RunFromPerformanceRepo) {
    $SetupArguments = "--perf-hash $CommitSha $CommonSetupArguments"
    
    robocopy $SourceDirectory $PerformanceDirectory /E /XD $PayloadDirectory $SourceDirectory\artifacts $SourceDirectory\.git
}
else {
    $SetupArguments = "--repository https://github.com/$Repository --branch $Branch --get-perf-hash --commit-sha $CommitSha $CommonSetupArguments"
    
    git clone --branch master --depth 1 --quiet https://github.com/dotnet/performance $PerformanceDirectory
}

if ($UseCoreRun) {
    $NewCoreRoot = (Join-Path $PayloadDirectory "Core_Root")
    Move-Item -Path $CoreRootDirectory -Destination $NewCoreRoot
}

$DocsDir = (Join-Path $PerformanceDirectory "docs")
robocopy $DocsDir $WorkItemDirectory

# Set variables that we will need to have in future steps
$ci = $true

. "$PSScriptRoot\..\pipeline-logging-functions.ps1"

# Directories
Write-PipelineSetVariable -Name 'PayloadDirectory' -Value "$PayloadDirectory" -IsSingleJobVariable
Write-PipelineSetVariable -Name 'PerformanceDirectory' -Value "$PerformanceDirectory" -IsSingleJobVariable
Write-PipelineSetVariable -Name 'WorkItemDirectory' -Value "$WorkItemDirectory" -IsSingleJobVariable

# Script Arguments
Write-PipelineSetVariable -Name 'Python' -Value "py -3"
Write-PipelineSetVariable -Name 'ExtraBenchmarkDotNetArguments' -Value "$ExtraBenchmarkDotNetArguments" -IsSingleJobVariable
Write-PipelineSetVariable -Name 'SetupArguments' -Value "$SetupArguments" -IsSingleJobVariable
Write-PipelineSetVariable -Name 'PerfLabArguments' -Value "$PerfLabArguments" -IsSingleJobVariable
Write-PipelineSetVariable -Name 'BDNCategories' -Value "$RunCategories" -IsSingleJobVariable
Write-PipelineSetVariable -Name 'TargetCsproj' -Value "$Csproj" -IsSingleJobVariable
Write-PipelineSetVariable -Name 'Kind' -Value "$Kind" -IsSingleJobVariable
Write-PipelineSetVariable -Name 'Architecture' -Value "$Architecture" -IsSingleJobVariable
Write-PipelineSetVariable -Name 'UseCoreRun' -Value "$UseCoreRun" -IsSingleJobVariable
Write-PipelineSetVariable -Name 'RunFromPerfRepo' -Value "$RunFromPerformanceRepo" -IsSingleJobVariable

# Helix Arguments
Write-PipelineSetVariable -Name 'Creator' -Value "$Creator" -IsSingleJobVariable
Write-PipelineSetVariable -Name 'Queue' -Value "$Queue" -IsSingleJobVariable
Write-PipelineSetVariable -Name 'HelixSourcePrefix' -Value "$HelixSourcePrefix" -IsSingleJobVariable
Write-PipelineSetVariable -Name 'BuildConfig' -Value "$Architecture.$Kind.$Framework" -IsSingleJobVariable

# Write-Host "##vso[task.setvariable variable=UseCoreRun]$UseCoreRun"
# Write-Host "##vso[task.setvariable variable=PayloadDirectory]$PayloadDirectory"
# Write-Host "##vso[task.setvariable variable=PerformanceDirectory]$PerformanceDirectory"
# Write-Host "##vso[task.setvariable variable=WorkItemDirectory]$WorkItemDirectory"
# Write-Host "##vso[task.setvariable variable=Queue]$Queue"
# Write-Host "##vso[task.setvariable variable=SetupArguments]$SetupArguments"
# Write-Host "##vso[task.setvariable variable=Python]py -3"
# Write-Host "##vso[task.setvariable variable=ExtraBenchmarkDotNetArguments]$ExtraBenchmarkDotNetArguments"
# Write-Host "##vso[task.setvariable variable=BDNCategories]$RunCategories"
# Write-Host "##vso[task.setvariable variable=TargetCsproj]$Csproj"
# Write-Host "##vso[task.setvariable variable=RunFromPerfRepo]$RunFromPerformanceRepo"
# Write-Host "##vso[task.setvariable variable=Creator]$Creator"
# Write-Host "##vso[task.setvariable variable=PerfLabArguments]$PerfLabArguments"
# Write-Host "##vso[task.setvariable variable=Architecture]$Architecture"
# Write-Host "##vso[task.setvariable variable=HelixSourcePrefix]$HelixSourcePrefix"
# Write-Host "##vso[task.setvariable variable=Kind]$Kind"
# Write-Host "##vso[task.setvariable variable=_BuildConfig]$Architecture.$Kind.$Framework"

exit 0