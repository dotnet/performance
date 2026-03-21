@echo off
setlocal enabledelayedexpansion

set "LOGFILE=%HELIX_WORKITEM_UPLOAD_ROOT%\output.log"
set "FRAMEWORK=%~1"
set "MSBUILD_ARGS=%~2"
set "SCENARIO_NAME=%~3"

echo === DIAGNOSTICS === >> "%LOGFILE%" 2>&1
echo DOTNET_ROOT=!DOTNET_ROOT! >> "%LOGFILE%" 2>&1
echo ANDROID_HOME=!ANDROID_HOME! >> "%LOGFILE%" 2>&1
echo ANDROID_SDK_ROOT=!ANDROID_SDK_ROOT! >> "%LOGFILE%" 2>&1
echo NUGET_PACKAGES=!NUGET_PACKAGES! >> "%LOGFILE%" 2>&1
echo PYTHONPATH=!PYTHONPATH! >> "%LOGFILE%" 2>&1
where adb >> "%LOGFILE%" 2>&1
where dotnet >> "%LOGFILE%" 2>&1
where python >> "%LOGFILE%" 2>&1
dotnet --version >> "%LOGFILE%" 2>&1
echo. >> "%LOGFILE%" 2>&1

REM ci_setup.py installs the .NET 11 SDK into %HELIX_CORRELATION_PAYLOAD%\dotnet
REM but DOTNET_ROOT points to %HELIX_CORRELATION_PAYLOAD%\dotnet-cli (has .NET 8).
REM Override DOTNET_ROOT to use the correct SDK for building.
set "DOTNET_ROOT=%HELIX_CORRELATION_PAYLOAD%\dotnet"
set "PATH=%DOTNET_ROOT%;%PATH%"

echo === SDK after DOTNET_ROOT override === >> "%LOGFILE%" 2>&1
echo DOTNET_ROOT=!DOTNET_ROOT! >> "%LOGFILE%" 2>&1
%DOTNET_ROOT%\dotnet --version >> "%LOGFILE%" 2>&1
echo. >> "%LOGFILE%" 2>&1

REM === Set up ANDROID_HOME from XHarness bundled ADB ===
REM The Helix queue (Windows.11.Amd64.Pixel.Perf) has Pixel devices but
REM ANDROID_HOME is not set and ADB is not on PATH.  XHarness ships a
REM bundled ADB, so we create a minimal fake Android SDK directory and
REM point ANDROID_HOME at it.  dotnet build -t:Install then finds ADB.
echo === Setting up Android SDK from XHarness === >> "%LOGFILE%" 2>&1
for /d %%d in (!HELIX_CORRELATION_PAYLOAD!\microsoft.dotnet.xharness.cli\*) do set "XHARNESS_DIR=%%d"
set "ADB_SRC=!XHARNESS_DIR!\runtimes\any\native\adb\windows"
set "ANDROID_HOME=%HELIX_WORKITEM_ROOT%\android-sdk"
mkdir "!ANDROID_HOME!\platform-tools" >> "%LOGFILE%" 2>&1
copy /Y "!ADB_SRC!\*" "!ANDROID_HOME!\platform-tools\" >> "%LOGFILE%" 2>&1
set "PATH=!ANDROID_HOME!\platform-tools;!PATH!"
echo XHARNESS_DIR=!XHARNESS_DIR! >> "%LOGFILE%" 2>&1
echo ADB_SRC=!ADB_SRC! >> "%LOGFILE%" 2>&1
echo ANDROID_HOME=!ANDROID_HOME! >> "%LOGFILE%" 2>&1
where adb >> "%LOGFILE%" 2>&1
echo. >> "%LOGFILE%" 2>&1

REM === Set up JAVA_HOME for Android builds ===
REM dotnet workload install maui-android does NOT install Java/OpenJDK.
REM dotnet build -t:Install fails with XA5300 if Java is missing.
REM Search common JDK locations on Windows Helix machines.
echo === Java SDK Setup === >> "%LOGFILE%" 2>&1

REM Check common JDK locations on Windows
for /d %%d in ("!ProgramW6432!\Microsoft\jdk-*") do set "JAVA_HOME=%%~d"
if not defined JAVA_HOME for /d %%d in ("!ProgramFiles!\Microsoft\jdk-*") do set "JAVA_HOME=%%~d"
if not defined JAVA_HOME for /d %%d in ("!ProgramW6432!\Android\openjdk\jdk-*") do set "JAVA_HOME=%%~d"
if not defined JAVA_HOME for /d %%d in ("!ProgramFiles!\Java\jdk-*") do set "JAVA_HOME=%%~d"
if not defined JAVA_HOME for /d %%d in ("!ProgramFiles!\Eclipse Adoptium\jdk-*") do set "JAVA_HOME=%%~d"

if not defined JAVA_HOME (
    echo Java not found in common paths, searching... >> "%LOGFILE%" 2>&1
    for /f "delims=" %%f in ('where java 2^>nul') do (
        set "JAVA_EXE=%%f"
    )
    if defined JAVA_EXE (
        REM Extract JAVA_HOME from java.exe path (remove \bin\java.exe)
        for %%p in ("!JAVA_EXE!\..\..\") do set "JAVA_HOME=%%~fp"
    )
)

if not defined JAVA_HOME (
    echo Java not found in common paths. Installing via Chocolatey... >> "%LOGFILE%" 2>&1
    choco install microsoft-openjdk-17 -y --no-progress >> "%LOGFILE%" 2>&1
    REM After choco install, JDK is typically at %ProgramFiles%\Microsoft\jdk-17.*
    for /d %%d in ("!ProgramW6432!\Microsoft\jdk-17*") do set "JAVA_HOME=%%~d"
    if not defined JAVA_HOME for /d %%d in ("!ProgramFiles!\Microsoft\jdk-17*") do set "JAVA_HOME=%%~d"
    echo Chocolatey JDK install complete. JAVA_HOME=!JAVA_HOME! >> "%LOGFILE%" 2>&1
)

if not defined JAVA_HOME (
    echo ERROR: Java SDK still not found after Chocolatey install >> "%LOGFILE%" 2>&1
    dir "!ProgramW6432!\Microsoft\" >> "%LOGFILE%" 2>&1
    dir "!ProgramFiles!\Microsoft\" >> "%LOGFILE%" 2>&1
    choco list --local-only >> "%LOGFILE%" 2>&1
)

echo JAVA_HOME=!JAVA_HOME! >> "%LOGFILE%" 2>&1
if defined JAVA_HOME (
    set "PATH=!JAVA_HOME!\bin;!PATH!"
    dir "!JAVA_HOME!\bin\java.exe" >> "%LOGFILE%" 2>&1
)
echo. >> "%LOGFILE%" 2>&1

REM Helix machines cannot reach NuGet certificate revocation servers (NU3018).
REM Use the local CRL cache instead of contacting the server online.
set "NUGET_CERT_REVOCATION_MODE=offline"

REM CI packages are signed with internal certs (CN=VS Bld Lab) not in the
REM Helix machine trust store. Disable signature verification entirely.
set "DOTNET_NUGET_SIGNATURE_VERIFICATION=false"

REM Patch NuGet.config to accept CI-signed packages (NU3018 on Helix)
powershell -Command "$f='app\NuGet.config'; [xml]$x=Get-Content $f; if(-not $x.configuration.config){$c=$x.CreateElement('config');[void]$x.configuration.AppendChild($c)}; $a=$x.CreateElement('add');$a.SetAttribute('key','signatureValidationMode');$a.SetAttribute('value','accept');[void]$x.configuration.config.AppendChild($a);$x.Save($f)" >> "!LOGFILE!" 2>&1

echo === STEP 1: Workload Install === >> "%LOGFILE%" 2>&1
echo [%DATE% %TIME%] Starting workload install >> "%LOGFILE%" 2>&1
%DOTNET_ROOT%\dotnet workload install maui --from-rollback-file %HELIX_WORKITEM_ROOT%\rollback_maui.json --configfile %HELIX_WORKITEM_ROOT%\app\NuGet.config >> "%LOGFILE%" 2>&1
if errorlevel 1 (
    echo [%DATE% %TIME%] STEP 1 FAILED with errorlevel !errorlevel! >> "%LOGFILE%" 2>&1
    type "%LOGFILE%"
    exit /b 1
)
echo [%DATE% %TIME%] Workload install succeeded >> "%LOGFILE%" 2>&1

echo === STEP 2: Restore === >> "%LOGFILE%" 2>&1
echo [%DATE% %TIME%] Starting restore >> "%LOGFILE%" 2>&1
%DOTNET_ROOT%\dotnet restore %HELIX_WORKITEM_ROOT%\app\MauiAndroidInnerLoop.csproj --configfile %HELIX_WORKITEM_ROOT%\app\NuGet.config /p:AllowMissingPrunePackageData=true >> "%LOGFILE%" 2>&1
if errorlevel 1 (
    echo [%DATE% %TIME%] STEP 2 FAILED with errorlevel !errorlevel! >> "%LOGFILE%" 2>&1
    type "%LOGFILE%"
    exit /b 2
)
echo [%DATE% %TIME%] Restore succeeded >> "%LOGFILE%" 2>&1

echo === STEP 3: Test === >> "%LOGFILE%" 2>&1
echo [%DATE% %TIME%] Starting test.py >> "%LOGFILE%" 2>&1
echo Command: python test.py androidinnerloop --csproj-path app\MauiAndroidInnerLoop.csproj --edit-src src\MainPage.xaml.cs --edit-dest app\MainPage.xaml.cs -f %FRAMEWORK% -c Debug --msbuild-args "%MSBUILD_ARGS%" --scenario-name "%SCENARIO_NAME%" >> "%LOGFILE%" 2>&1
python test.py androidinnerloop --csproj-path app\MauiAndroidInnerLoop.csproj --edit-src src\MainPage.xaml.cs --edit-dest app\MainPage.xaml.cs -f %FRAMEWORK% -c Debug --msbuild-args "%MSBUILD_ARGS%" --scenario-name "%SCENARIO_NAME%" >> "%LOGFILE%" 2>&1
if errorlevel 1 (
    echo [%DATE% %TIME%] STEP 3 FAILED with errorlevel !errorlevel! >> "%LOGFILE%" 2>&1
    type "%LOGFILE%"
    exit /b 3
)

echo === ALL STEPS SUCCEEDED === >> "%LOGFILE%" 2>&1
echo [%DATE% %TIME%] Complete >> "%LOGFILE%" 2>&1
type "%LOGFILE%"
exit /b 0
