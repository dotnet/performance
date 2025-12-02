# MAUI Version Synchronization

## Overview

The MAUI scenarios in this repository automatically sync workload dependencies from the upstream [dotnet/maui](https://github.com/dotnet/maui) repository. This ensures builds always use the latest compatible MAUI versions without manual Version.Details.xml updates.

## Automatic Synchronization

### When It Runs

Version.Details.xml is automatically synchronized whenever MAUI workloads are installed:

- During `install_latest_maui()` - Uses feed-based discovery + syncs Version.Details.xml
- During `install_versioned_maui()` - Syncs before generating rollback files

### What Gets Synced

The following MAUI workload dependencies are kept up-to-date:

- `Microsoft.Android.Sdk.*`
- `Microsoft.iOS.Sdk.*`
- `Microsoft.MacCatalyst.Sdk.*`
- `Microsoft.macOS.Sdk.*`
- `Microsoft.tvOS.Sdk.*`
- `Microsoft.Maui.Controls`
- `Microsoft.NETCore.App.Ref`
- `Microsoft.NET.Sdk`
- `Microsoft.NET.Workload.Emscripten.*`

### How It Works

1. Downloads MAUI's `Version.Details.xml` from the appropriate branch (e.g., `net10.0`)
2. Extracts MAUI workload dependencies
3. Updates matching dependencies in `eng/Version.Details.xml`
4. Adds new dependencies if they don't exist
5. Preserves non-MAUI dependencies unchanged

## Manual Synchronization

You can manually sync MAUI versions using the standalone script:

```bash
# Sync for .NET 10.0 (default)
python scripts/sync_maui_versions.py

# Sync for specific framework version
python scripts/sync_maui_versions.py --framework net9.0
python scripts/sync_maui_versions.py --framework net11.0

# Verbose output
python scripts/sync_maui_versions.py --verbose
```

### When to Run Manually

- Before updating to a new .NET version
- To verify current versions match upstream
- When MAUI makes significant dependency updates
- As part of dependency update workflows

## Configuration

### Version Mappings

The `Mapping_*` comments in Version.Details.xml control band version resolution:

```xml
<!--
  Mapping_Microsoft.Maui.Controls:default
  Mapping_Microsoft.Android.Sdk:default
  Mapping_Microsoft.iOS.Sdk:default
  ...
-->
```

- `default` - Uses SDK version from `Microsoft.NET.Sdk`
- Specific version - Overrides with custom band version

### Disabling Auto-Sync

Auto-sync can be disabled by modifying the installation functions in `mauisharedpython.py`:

```python
# Comment out the sync call
# sync_maui_version_details(target_framework_wo_platform)
```

## Troubleshooting

### Sync Failures

If automatic sync fails, a warning is logged but the build continues:

```
[WARNING] Failed to sync MAUI Version.Details.xml: <error>. Continuing with existing versions.
```

This is non-fatal - the build uses existing Version.Details.xml entries.

### Version Conflicts

If you manually maintain specific MAUI versions:

1. Sync will overwrite them with upstream versions
2. Consider pinning to specific commits in your fork
3. Or disable auto-sync and manage manually

### Network Issues

Sync requires internet access to download MAUI's Version.Details.xml. Builds work offline using existing versions.

## Related Files

- **`src/scenarios/shared/mauisharedpython.py`** - Contains `sync_maui_version_details()` function
- **`scripts/sync_maui_versions.py`** - Standalone sync script
- **`eng/Version.Details.xml`** - Repository version dependencies

## See Also

- [MAUI Scenarios Documentation](../docs/basic-scenarios.md)
- [dotnet/maui Version.Details.xml](https://github.com/dotnet/maui/blob/main/eng/Version.Details.xml)
- [.NET Arcade Dependency Management](https://github.com/dotnet/arcade/blob/main/Documentation/DependencyFlowOnboarding.md)
