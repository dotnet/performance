---
name: maui-android-innerloop
description: Guide for running MAUI Android Inner Loop deploy measurements in the dotnet/performance repo. Use this when asked to measure, benchmark, or compare MAUI Android first deploy and incremental deploy times across runtime configurations (Mono+Interpreter, CoreCLR+JIT, etc.).
---

# MAUI Android Inner Loop Measurement

Measures first deploy and incremental deploy times for a .NET MAUI Android app using MSBuild binary logs. Located in `src/scenarios/mauiandroidinnerloop/` within the dotnet/performance repo.

## Prerequisites

Before running measurements, verify:

1. **Android device** connected via USB: `adb devices` should list a device.
2. **.NET SDK** installed at `tools/dotnet/arm64/`. Bootstrap with:
   ```bash
   cd src/scenarios && . ./init.sh -channel main
   ```
3. **maui-android workload** installed (NOT full `maui` — iOS packages fail without the iOS workload):
   ```bash
   export DOTNET_ROOT="$(pwd)/tools/dotnet/arm64"
   export PATH="$DOTNET_ROOT:$PATH"
   dotnet workload install maui-android \
     --from-rollback-file src/scenarios/mauiandroidinnerloop/rollback_maui.json \
     --skip-sign-check
   ```
4. **Startup tool** (binlog parser) built:
   ```bash
   PERFLAB_TARGET_FRAMEWORKS=net11.0 dotnet publish \
     src/tools/ScenarioMeasurement/Startup/Startup.csproj \
     -c Release -o artifacts/startup --ignore-failed-sources /p:NuGetAudit=false
   ```

## Environment Setup (every new shell)

```bash
cd src/scenarios && . ./init.sh -dotnetdir <REPO_ROOT>/tools/dotnet/arm64
cd mauiandroidinnerloop
```

## Create the App Template

```bash
python3 pre.py publish -f net11.0-android --has-workload
```

### CRITICAL: Fix csproj after pre.py

`dotnet new maui` targets all platforms. Since only maui-android workload is installed, you MUST edit `app/MauiAndroidInnerLoop.csproj` and remove the iOS/MacCatalyst/Windows TargetFrameworks conditions, leaving only:

```xml
<TargetFrameworks>net11.0-android</TargetFrameworks>
```

Remove these two lines that follow the android TargetFrameworks line:
```xml
<TargetFrameworks Condition="!$([MSBuild]::IsOSPlatform('linux'))">$(TargetFrameworks);net11.0-ios;net11.0-maccatalyst</TargetFrameworks>
<TargetFrameworks Condition="$([MSBuild]::IsOSPlatform('windows'))">$(TargetFrameworks);net11.0-windows10.0.19041.0</TargetFrameworks>
```

## MSBuild Args per Configuration

| Configuration     | MSBuild Properties                                                                    |
|-------------------|---------------------------------------------------------------------------------------|
| Mono+Interpreter  | `/p:UseMonoRuntime=true`                                                              |
| CoreCLR+JIT       | `/p:UseMonoRuntime=false /p:PublishReadyToRun=false /p:PublishReadyToRunComposite=false` |

## Running a Measurement

```bash
# Clean from any prior run
rm -rf app/bin app/obj traces

# Mono+Interpreter
python3 test.py androidinnerloop \
  --csproj-path app/MauiAndroidInnerLoop.csproj \
  --edit-src src/MainPage.xaml.cs \
  --edit-dest app/MainPage.xaml.cs \
  -f net11.0-android -c Debug \
  --msbuild-args "/p:UseMonoRuntime=true"

# CoreCLR+JIT
python3 test.py androidinnerloop \
  --csproj-path app/MauiAndroidInnerLoop.csproj \
  --edit-src src/MainPage.xaml.cs \
  --edit-dest app/MainPage.xaml.cs \
  -f net11.0-android -c Debug \
  --msbuild-args "/p:UseMonoRuntime=false;/p:PublishReadyToRun=false;/p:PublishReadyToRunComposite=false"
```

## What test.py androidinnerloop Does

1. **First deploy:** `dotnet build <csproj> -t:Install -c Debug -f net11.0-android <msbuild-args> /p:UseSharedCompilation=true -bl:traces/first-deploy.binlog`
2. **File edit:** Copies modified `MainPage.xaml.cs` to simulate an incremental code edit
3. **Incremental deploy:** `dotnet build <csproj> -t:Install ... -bl:traces/incremental-deploy.binlog`
4. **Parse binlogs:** Extracts per-task timings using the Startup tool

## Clean Between Configurations

```bash
python3 post.py   # Uninstalls APK, shuts down build servers, removes app/traces/etc.
python3 pre.py publish -f net11.0-android --has-workload
# Fix csproj again! (remove iOS/MacCatalyst/Windows targets)
```

## Persisting Binlogs

Binlogs are in `traces/` and get cleaned by `post.py`. To keep them:

```bash
mkdir -p binlogs
cp traces/first-deploy.binlog binlogs/<config>-first-deploy.binlog
cp traces/incremental-deploy.binlog binlogs/<config>-incremental-deploy.binlog
```

## Key Facts & Gotchas

- **UseSharedCompilation=true** is set by `runner.py` for this scenario, overriding the repo default of `false`. This mirrors a real dev workflow where the Roslyn compiler server stays warm. `post.py` runs `dotnet build-server shutdown` to clean up between runs.
- **FastDev** (Fast Deployment) is ON by default in Debug (`EmbedAssembliesIntoApk=false`). Do NOT set `EmbedAssembliesIntoApk=true` — it disables FastDev and makes deploys 10x slower.
- **Startup tool** targets `net8.0` by default and needs .NET 8 runtime. Build with `PERFLAB_TARGET_FRAMEWORKS=net11.0` to retarget if only .NET 11 is available.
- **Dead NuGet feeds** (`darc-pub-dotnet-android-*`) break Startup tool builds. Use `--ignore-failed-sources /p:NuGetAudit=false`.
- **macOS Spotlight** can race with builds causing random errors (XARDF7024, MAUIR0001, CS2012). Fix: `sudo mdutil -i off <worktree_path>`.
- **The repo sets `UseSharedCompilation=false`** in `src/Directory.Build.props` and `src/scenarios/init.sh`. The runner.py override via `/p:UseSharedCompilation=true` on the command line takes precedence.
