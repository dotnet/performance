import json
import os
import shutil
import subprocess
from pathlib import Path

import pytest


POWERSHELL = shutil.which("powershell") or shutil.which("pwsh")
pytestmark = pytest.mark.skipif(POWERSHELL is None, reason="PowerShell is required")
PROBE = Path(__file__).resolve().parents[2] / "eng/performance/Check-MSBuildSourceAccess.ps1"
TOKEN = "test-job-token-not-for-output"
LATEST = "1" * 40


def invoke_probe(tmp_path, scenario, checkout=False):
    harness = tmp_path / "probe-test.ps1"
    result = tmp_path / "result.json"
    harness.write_text(
        r"""
param([string]$Probe, [string]$Result, [string]$Scenario, [string]$CheckoutDirectory)
$ErrorActionPreference = 'Stop'
$global:calls = [System.Collections.Generic.List[object]]::new()
function Invoke-RestMethod {
    param($Uri, $Headers, $TimeoutSec)
    if ($Headers.Authorization -ne "Bearer $env:SYSTEM_ACCESSTOKEN") {
        throw 'The identity request did not authenticate with the job token.'
    }
    return @{ authenticatedUser = @{
        providerDisplayName = 'internal Build Service (dnceng)'
        id = 'job-principal-id'
    } }
}
function git {
    $arguments = @($args)
    $global:calls.Add(@{
        arguments = $arguments
        authenticated = ($env:GIT_CONFIG_VALUE_0 -eq "AUTHORIZATION: bearer $env:SYSTEM_ACCESSTOKEN")
    })
    $global:LASTEXITCODE = 0
    if ($arguments -contains 'ls-remote') {
        if ($Scenario -eq 'denied') {
            $global:LASTEXITCODE = 128
        } elseif ($Scenario -eq 'invalid-ref') {
            Write-Output "bad-sha`trefs/heads/main"
        } else {
            Write-Output ("1" * 40 + "`trefs/heads/main")
        }
    } elseif ($Scenario -eq 'checkout-failed' -and $arguments -contains 'checkout') {
        $global:LASTEXITCODE = 128
    }
}
$env:GIT_CONFIG_COUNT = '7'
$env:GIT_CONFIG_KEY_0 = 'original-key'
$env:GIT_CONFIG_VALUE_0 = 'original-value'
$env:GIT_TERMINAL_PROMPT = 'original-prompt'
$env:GCM_INTERACTIVE = 'original-interactive'
$exitCode = 0
try {
    if ($CheckoutDirectory) {
        & $Probe -SourceDirectory $CheckoutDirectory
    } else {
        & $Probe
    }
} catch {
    Write-Output $_.Exception.Message
    $exitCode = 1
}
@{
    calls = @($global:calls.ToArray())
    restored = @($env:GIT_CONFIG_COUNT, $env:GIT_CONFIG_KEY_0, $env:GIT_CONFIG_VALUE_0,
        $env:GIT_TERMINAL_PROMPT, $env:GCM_INTERACTIVE)
} | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath $Result
exit $exitCode
""",
        encoding="utf-8",
    )
    env = os.environ | {
        "SYSTEM_ACCESSTOKEN": TOKEN,
        "ADO_COLLECTION_URI": "https://dev.azure.com/dnceng/",
    }
    command = [POWERSHELL, "-NoProfile", "-NonInteractive", "-File", str(harness),
               "-Probe", str(PROBE), "-Result", str(result), "-Scenario", scenario]
    if checkout:
        command += ["-CheckoutDirectory", str(tmp_path / "source")]
    process = subprocess.run(command, env=env, capture_output=True, text=True, timeout=30)
    data = json.loads(result.read_text(encoding="utf-8-sig"))
    assert TOKEN not in process.stdout + process.stderr + result.read_text(encoding="utf-8-sig")
    assert data["restored"] == [
        "7", "original-key", "original-value", "original-prompt", "original-interactive"]
    assert all(call["authenticated"] for call in data["calls"])
    return process, data


def test_access_only_prints_identity_and_reads_ref_without_checkout(tmp_path):
    process, data = invoke_probe(tmp_path, "allowed")
    assert process.returncode == 0, process.stdout + process.stderr
    assert "internal Build Service (dnceng)" in process.stdout
    assert "job-principal-id" in process.stdout and LATEST in process.stdout
    assert len(data["calls"]) == 1
    assert data["calls"][0]["arguments"][:3] == ["-c", "credential.helper=", "ls-remote"]


@pytest.mark.parametrize("scenario,message", [
    ("denied", "cannot read the MSBuild mirror"),
    ("invalid-ref", "exactly one valid main ref"),
])
def test_source_probe_failures_are_explicit_after_identity_output(tmp_path, scenario, message):
    process, _ = invoke_probe(tmp_path, scenario)
    assert process.returncode != 0
    assert "internal Build Service (dnceng)" in process.stdout
    assert message in process.stdout


def test_checkout_uses_resolved_sha_not_moving_main(tmp_path):
    process, data = invoke_probe(tmp_path, "allowed", checkout=True)
    assert process.returncode == 0, process.stdout + process.stderr
    commands = [call["arguments"] for call in data["calls"]]
    assert any("fetch" in command for command in commands)
    checkout = next(command for command in commands if "checkout" in command)
    assert checkout[-2:] == ["--detach", LATEST]


def test_checkout_failure_does_not_report_success(tmp_path):
    process, _ = invoke_probe(tmp_path, "checkout-failed", checkout=True)
    assert process.returncode != 0
    assert "MSBuild mirror checkout failed" in process.stdout
    assert "Checked out MSBuild mirror SHA" not in process.stdout
