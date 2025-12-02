# MAUI Workload Management

## Overview

The MAUI scenarios in this repository automatically download and use workload dependencies directly from the upstream [dotnet/maui](https://github.com/dotnet/maui) repository. **No MAUI dependencies need to be maintained in the performance repository's Version.Details.xml.**

## How It Works

### Automatic Dependency Resolution

MAUI workload installations automatically download dependency information from upstream:

**`install_latest_maui()`** - Uses feed-based discovery:
1. Queries NuGet feeds for latest published MAUI workload packages
2. Filters and selects highest versions
3. Generates rollback file dynamically
4. Installs workload with rollback

**`install_versioned_maui()`** - Uses MAUI's Version.Details.xml:
1. Downloads MAUI's `Version.Details.xml` from target branch (e.g., `net10.0`)
2. Extracts workload versions (Android, iOS, MacCatalyst, macOS, tvOS, MAUI Controls)
3. Generates rollback file with exact versions from MAUI upstream
4. Installs workload with rollback

### What Gets Downloaded

The following MAUI workload dependencies are resolved from upstream:

- `Microsoft.Android.Sdk.*`
- `Microsoft.iOS.Sdk.*`
- `Microsoft.MacCatalyst.Sdk.*`
- `Microsoft.macOS.Sdk.*`
- `Microsoft.tvOS.Sdk.*`
- `Microsoft.Maui.Controls`
- `Microsoft.NETCore.App.Ref`
- `Microsoft.NET.Sdk`
- `Microsoft.NET.Workload.Emscripten.*`

## Manual Version Inspection

You can inspect what versions would be used with the standalone script:

```bash
# Check versions for .NET 10.0 (default)
python scripts/sync_maui_versions.py --framework net10.0

# Check versions for specific framework
python scripts/sync_maui_versions.py --framework net9.0
```

**Note:** This script still syncs to Version.Details.xml for informational/auditing purposes, but MAUI workload installation no longer depends on it.

## No Version.Details.xml Maintenance Required

### What You Can Remove

MAUI workload dependencies can be **completely removed** from `eng/Version.Details.xml`:

```xml
<!-- These are NO LONGER NEEDED and can be removed: -->
<Dependency Name="Microsoft.Maui.Controls" Version="...">
<Dependency Name="Microsoft.Android.Sdk.Windows" Version="...">
<Dependency Name="Microsoft.iOS.Sdk.net10.0_18.5" Version="...">
<Dependency Name="Microsoft.MacCatalyst.Sdk.net10.0_18.5" Version="...">
<Dependency Name="Microsoft.macOS.Sdk.net10.0_15.5" Version="...">
<Dependency Name="Microsoft.tvOS.Sdk.net10.0_18.5" Version="...">
<!-- etc. -->
```

The `Mapping_*` comments are also no longer needed:

```xml
<!-- This comment block is NO LONGER NEEDED: -->
<!--
  Mapping_Microsoft.Maui.Controls:default
  Mapping_Microsoft.Android.Sdk:default
  Mapping_Microsoft.iOS.Sdk:default
  ...
-->
```

### What To Keep

Keep all non-MAUI dependencies in Version.Details.xml:

- `Microsoft.DotNet.Arcade.Sdk`
- `Microsoft.DotNet.Helix.Sdk`
- `Microsoft.DotNet.XHarness.CLI`
- Other non-MAUI dependencies

## Benefits

✅ **Zero MAUI maintenance** - No Version.Details.xml entries to update  
✅ **Always current** - Downloads latest from MAUI upstream every run  
✅ **Version-aware** - Automatically uses correct branch (net9.0, net10.0, etc.)  
✅ **Simpler Version.Details.xml** - Only contains performance-repo-specific dependencies  
✅ **No merge conflicts** - MAUI version updates don't require repo changes  

## Troubleshooting

### Download Failures

If downloading MAUI's Version.Details.xml fails:

```
[ERROR] Failed to download MAUI Version.Details.xml: <error>
```

**Cause:** Network issue or MAUI branch doesn't exist  
**Solution:** Check internet connection and verify framework version is valid (e.g., net10.0 branch exists)

### Network Issues

MAUI workload installation requires internet access to download dependency information from github.com. Offline builds are not supported for MAUI scenarios.

## Related Files

- **`src/scenarios/shared/mauisharedpython.py`** - Contains `sync_maui_version_details()` function
- **`scripts/sync_maui_versions.py`** - Standalone sync script
- **`eng/Version.Details.xml`** - Repository version dependencies

## See Also

- [MAUI Scenarios Documentation](../docs/basic-scenarios.md)
- [dotnet/maui Version.Details.xml](https://github.com/dotnet/maui/blob/main/eng/Version.Details.xml)
- [.NET Arcade Dependency Management](https://github.com/dotnet/arcade/blob/main/Documentation/DependencyFlowOnboarding.md)
