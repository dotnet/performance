$scripts = Join-Path $PSScriptRoot '..\..\scripts\' -Resolve
$env:PYTHONPATH="$scripts;$PSScriptRoot"