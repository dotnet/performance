@echo off
setlocal enabledelayedexpansion

set "LOGFILE=!HELIX_WORKITEM_UPLOAD_ROOT!\output.log"
set "FRAMEWORK=%~1"
set "MSBUILD_ARGS=%~2"
set "SCENARIO_NAME=%~3"

REM Collect remaining arguments (ScenarioArgs from the proj file, e.g. --upload-to-perflab-container)
set "EXTRA_ARGS="
shift
shift
shift
:parse_extra_args
if "%~1"=="" goto :done_extra_args
if defined EXTRA_ARGS (
    set "EXTRA_ARGS=!EXTRA_ARGS! %~1"
) else (
    set "EXTRA_ARGS=%~1"
)
shift
goto :parse_extra_args
:done_extra_args

echo === DIAGNOSTICS ===
echo === DIAGNOSTICS === >> "!LOGFILE!" 2>&1
echo DOTNET_ROOT=!DOTNET_ROOT!
echo DOTNET_ROOT=!DOTNET_ROOT! >> "!LOGFILE!" 2>&1
echo ANDROID_HOME=!ANDROID_HOME!
echo ANDROID_HOME=!ANDROID_HOME! >> "!LOGFILE!" 2>&1
echo ANDROID_SDK_ROOT=!ANDROID_SDK_ROOT! >> "!LOGFILE!" 2>&1
echo NUGET_PACKAGES=!NUGET_PACKAGES! >> "!LOGFILE!" 2>&1
echo PYTHONPATH=!PYTHONPATH! >> "!LOGFILE!" 2>&1
where adb >> "!LOGFILE!" 2>&1
where dotnet >> "!LOGFILE!" 2>&1
where python >> "!LOGFILE!" 2>&1
dotnet --version >> "!LOGFILE!" 2>&1
echo. >> "!LOGFILE!" 2>&1

REM ci_setup.py installs the .NET 11 SDK into !HELIX_CORRELATION_PAYLOAD!\dotnet
REM but DOTNET_ROOT points to !HELIX_CORRELATION_PAYLOAD!\dotnet-cli (has .NET 8).
REM Override DOTNET_ROOT to use the correct SDK for building.
set "DOTNET_ROOT=!HELIX_CORRELATION_PAYLOAD!\dotnet"
set "PATH=!DOTNET_ROOT!;!PATH!"

echo === SDK after DOTNET_ROOT override === >> "!LOGFILE!" 2>&1
echo DOTNET_ROOT=!DOTNET_ROOT! >> "!LOGFILE!" 2>&1
!DOTNET_ROOT!\dotnet --version >> "!LOGFILE!" 2>&1
echo. >> "!LOGFILE!" 2>&1

REM === Set up ANDROID_HOME from XHarness bundled ADB ===
REM The Helix queue (Windows.11.Amd64.Pixel.Perf) has Pixel devices but
REM ANDROID_HOME is not set and ADB is not on PATH.  XHarness ships a
REM bundled ADB, so we create a minimal fake Android SDK directory and
REM point ANDROID_HOME at it.  dotnet build -t:Install then finds ADB.
echo === Setting up Android SDK from XHarness === >> "!LOGFILE!" 2>&1
for /d %%d in (!HELIX_CORRELATION_PAYLOAD!\microsoft.dotnet.xharness.cli\*) do set "XHARNESS_DIR=%%d"
set "ADB_SRC=!XHARNESS_DIR!\runtimes\any\native\adb\windows"
set "ANDROID_HOME=!HELIX_WORKITEM_ROOT!\android-sdk"
mkdir "!ANDROID_HOME!\platform-tools" >> "!LOGFILE!" 2>&1
copy /Y "!ADB_SRC!\*" "!ANDROID_HOME!\platform-tools\" >> "!LOGFILE!" 2>&1
set "PATH=!ANDROID_HOME!\platform-tools;!PATH!"
echo XHARNESS_DIR=!XHARNESS_DIR! >> "!LOGFILE!" 2>&1
echo ADB_SRC=!ADB_SRC! >> "!LOGFILE!" 2>&1
echo ANDROID_HOME=!ANDROID_HOME! >> "!LOGFILE!" 2>&1
where adb >> "!LOGFILE!" 2>&1
echo. >> "!LOGFILE!" 2>&1

REM === Set up JAVA_HOME for Android builds ===
REM dotnet workload install maui-android does NOT install Java/OpenJDK.
REM dotnet build -t:Install fails with XA5300 if Java is missing.
REM Search common JDK locations on Windows Helix machines.
echo === Java SDK Setup === >> "!LOGFILE!" 2>&1

REM Check common JDK locations on Windows
for /d %%d in ("!ProgramW6432!\Microsoft\jdk-*") do set "JAVA_HOME=%%~d"
if not defined JAVA_HOME for /d %%d in ("!ProgramFiles!\Microsoft\jdk-*") do set "JAVA_HOME=%%~d"
if not defined JAVA_HOME for /d %%d in ("!ProgramW6432!\Android\openjdk\jdk-*") do set "JAVA_HOME=%%~d"
if not defined JAVA_HOME for /d %%d in ("!ProgramFiles!\Java\jdk-*") do set "JAVA_HOME=%%~d"
if not defined JAVA_HOME for /d %%d in ("!ProgramFiles!\Eclipse Adoptium\jdk-*") do set "JAVA_HOME=%%~d"

if not defined JAVA_HOME (
    echo Java not found in common paths, searching... >> "!LOGFILE!" 2>&1
    for /f "delims=" %%f in ('where java 2^>nul') do (
        set "JAVA_EXE=%%f"
    )
    if defined JAVA_EXE (
        REM Extract JAVA_HOME from java.exe path ^(remove \bin\java.exe^)
        for %%p in ("!JAVA_EXE!\..\..\") do set "JAVA_HOME=%%~fp"
    )
)

if not defined JAVA_HOME (
    echo Java not found in common paths. Downloading Microsoft OpenJDK 17... >> "!LOGFILE!" 2>&1
    set "JDK_ZIP=!HELIX_WORKITEM_ROOT!\openjdk17.zip"
    set "JDK_EXTRACT=!HELIX_WORKITEM_ROOT!\jdk"
    echo [!DATE! !TIME!] Starting OpenJDK download >> "!LOGFILE!" 2>&1
    curl.exe -L -o "!JDK_ZIP!" "https://aka.ms/download-jdk/microsoft-jdk-17.0.13-windows-x64.zip" >> "!LOGFILE!" 2>&1
    if errorlevel 1 (
        echo ERROR: Failed to download OpenJDK >> "!LOGFILE!" 2>&1
    ) else (
        echo [!DATE! !TIME!] Download complete. Extracting... >> "!LOGFILE!" 2>&1
        powershell -Command "Expand-Archive -Path '!JDK_ZIP!' -DestinationPath '!JDK_EXTRACT!' -Force" >> "!LOGFILE!" 2>&1
        echo [!DATE! !TIME!] Extraction complete >> "!LOGFILE!" 2>&1
        REM The ZIP extracts to a subdirectory like jdk-17.0.13+11
        for /d %%d in ("!JDK_EXTRACT!\jdk-*") do set "JAVA_HOME=%%~d"
        echo Downloaded JDK JAVA_HOME=!JAVA_HOME! >> "!LOGFILE!" 2>&1
    )
)

if not defined JAVA_HOME (
    echo ERROR: Java SDK still not found after download attempt >> "!LOGFILE!" 2>&1
    dir "!ProgramW6432!\Microsoft\" >> "!LOGFILE!" 2>&1
    dir "!ProgramFiles!\Microsoft\" >> "!LOGFILE!" 2>&1
    if exist "!JDK_EXTRACT!" dir /s "!JDK_EXTRACT!" >> "!LOGFILE!" 2>&1
)

echo JAVA_HOME=!JAVA_HOME! >> "!LOGFILE!" 2>&1
if defined JAVA_HOME (
    set "PATH=!JAVA_HOME!\bin;!PATH!"
    dir "!JAVA_HOME!\bin\java.exe" >> "!LOGFILE!" 2>&1
)
echo. >> "!LOGFILE!" 2>&1

echo === STEP 1: Workload Install ===
echo === STEP 1: Workload Install === >> "!LOGFILE!" 2>&1
echo [!DATE! !TIME!] Starting workload install >> "!LOGFILE!" 2>&1
!DOTNET_ROOT!\dotnet workload install maui-android --from-rollback-file !HELIX_WORKITEM_ROOT!\rollback_maui.json --configfile !HELIX_WORKITEM_ROOT!\app\NuGet.config >> "!LOGFILE!" 2>&1
if errorlevel 1 (
    echo [!DATE! !TIME!] STEP 1 FAILED with errorlevel !errorlevel!
    echo [!DATE! !TIME!] STEP 1 FAILED with errorlevel !errorlevel! >> "!LOGFILE!" 2>&1
    type "!LOGFILE!"
    exit /b 1
)
echo [!DATE! !TIME!] Workload install succeeded >> "!LOGFILE!" 2>&1

REM dotnet workload restore reads the .csproj and installs any implicit workload
REM dependencies the SDK requires (e.g. ios workload pulled in by MAUI SDK even
REM when only targeting Android).
echo [!DATE! !TIME!] Running dotnet workload restore >> "!LOGFILE!" 2>&1
echo dotnet workload restore !HELIX_WORKITEM_ROOT!\app\MauiAndroidInnerLoop.csproj --configfile !HELIX_WORKITEM_ROOT!\app\NuGet.config >> "!LOGFILE!" 2>&1
!DOTNET_ROOT!\dotnet workload restore !HELIX_WORKITEM_ROOT!\app\MauiAndroidInnerLoop.csproj --configfile !HELIX_WORKITEM_ROOT!\app\NuGet.config >> "!LOGFILE!" 2>&1
if errorlevel 1 (
    echo [!DATE! !TIME!] WARNING: dotnet workload restore returned errorlevel !errorlevel! ^(non-fatal^) >> "!LOGFILE!" 2>&1
) else (
    echo [!DATE! !TIME!] Workload restore succeeded >> "!LOGFILE!" 2>&1
)
echo. >> "!LOGFILE!" 2>&1

REM === Set up Android Build-Tools (aapt2, zipalign) ===
REM dotnet build for Android requires aapt2.exe and zipalign.exe from
REM Android SDK Build-Tools.  MSBuild searches ANDROID_HOME\build-tools\
REM <version>\aapt2.exe.  Download the complete package from Google — the
REM workload pack only has aapt2, not zipalign.
echo === Android Build-Tools Setup === >> "!LOGFILE!" 2>&1
set "BUILD_TOOLS_DIR=!ANDROID_HOME!\build-tools\35.0.0"

echo [!DATE! !TIME!] Downloading Android SDK Build-Tools from Google... >> "!LOGFILE!" 2>&1
set "BT_ZIP=!HELIX_WORKITEM_ROOT!\build-tools.zip"
set "BT_EXTRACT=!HELIX_WORKITEM_ROOT!\build-tools-extract"
curl.exe -L -o "!BT_ZIP!" "https://dl.google.com/android/repository/build-tools_r35_windows.zip" >> "!LOGFILE!" 2>&1
if errorlevel 1 (
    echo ERROR: Failed to download Build-Tools. Build will likely fail with XA5205. >> "!LOGFILE!" 2>&1
) else (
    echo [!DATE! !TIME!] Download complete. Extracting... >> "!LOGFILE!" 2>&1
    powershell -Command "Expand-Archive -Path '!BT_ZIP!' -DestinationPath '!BT_EXTRACT!' -Force" >> "!LOGFILE!" 2>&1
    echo [!DATE! !TIME!] Extraction complete >> "!LOGFILE!" 2>&1
    REM Find the top-level directory inside the ZIP ^(e.g. android-15/^)
    mkdir "!BUILD_TOOLS_DIR!" >> "!LOGFILE!" 2>&1
    for /d %%d in ("!BT_EXTRACT!\*") do (
        echo Moving contents from %%d to !BUILD_TOOLS_DIR! >> "!LOGFILE!" 2>&1
        xcopy /Y /E /Q "%%d\*" "!BUILD_TOOLS_DIR!\" >> "!LOGFILE!" 2>&1
    )
)

if exist "!BUILD_TOOLS_DIR!\aapt2.exe" (
    echo aapt2.exe found at !BUILD_TOOLS_DIR!\aapt2.exe >> "!LOGFILE!" 2>&1
) else (
    echo WARNING: aapt2.exe NOT found. Build will likely fail with XA5205. >> "!LOGFILE!" 2>&1
)
if exist "!BUILD_TOOLS_DIR!\zipalign.exe" (
    echo zipalign.exe found at !BUILD_TOOLS_DIR!\zipalign.exe >> "!LOGFILE!" 2>&1
) else (
    echo WARNING: zipalign.exe NOT found. Build may fail. >> "!LOGFILE!" 2>&1
)
if exist "!BUILD_TOOLS_DIR!" dir "!BUILD_TOOLS_DIR!" >> "!LOGFILE!" 2>&1
echo. >> "!LOGFILE!" 2>&1

REM === Set up android.jar (platforms) ===
REM android.jar is a Google Android SDK Platform artifact — it is NOT bundled
REM in any .NET MAUI workload pack.  The CI Android SDK (36.99.0-ci.main.0)
REM requires API level 36.1.  Download the platform ZIP from Google and place
REM android.jar at ANDROID_HOME\platforms\android-36.1\android.jar where
REM MSBuild expects it.
echo === Android Platforms (android.jar) Setup === >> "!LOGFILE!" 2>&1
set "PLATFORM_DIR=!ANDROID_HOME!\platforms\android-36.1"

echo [!DATE! !TIME!] Downloading Android SDK Platform from Google... >> "!LOGFILE!" 2>&1
set "PLAT_ZIP=!HELIX_WORKITEM_ROOT!\platform.zip"
set "PLAT_EXTRACT=!HELIX_WORKITEM_ROOT!\platform-extract"
curl.exe -L -o "!PLAT_ZIP!" "https://dl.google.com/android/repository/platform-36.1_r01.zip" >> "!LOGFILE!" 2>&1
if errorlevel 1 (
    echo ERROR: Failed to download Android SDK Platform. Build will likely fail. >> "!LOGFILE!" 2>&1
) else (
    echo [!DATE! !TIME!] Download complete. Extracting... >> "!LOGFILE!" 2>&1
    powershell -Command "Expand-Archive -Path '!PLAT_ZIP!' -DestinationPath '!PLAT_EXTRACT!' -Force" >> "!LOGFILE!" 2>&1
    echo [!DATE! !TIME!] Extraction complete >> "!LOGFILE!" 2>&1
    REM The ZIP contains a top-level directory ^(e.g. android-16/^) with android.jar inside
    mkdir "!PLATFORM_DIR!" >> "!LOGFILE!" 2>&1
    for /d %%d in ("!PLAT_EXTRACT!\*") do (
        echo Moving contents from %%d to !PLATFORM_DIR! >> "!LOGFILE!" 2>&1
        xcopy /Y /E /Q "%%d\*" "!PLATFORM_DIR!\" >> "!LOGFILE!" 2>&1
    )
)

if exist "!PLATFORM_DIR!\android.jar" (
    echo android.jar found at !PLATFORM_DIR!\android.jar >> "!LOGFILE!" 2>&1
) else (
    echo WARNING: android.jar NOT found at !PLATFORM_DIR!\android.jar. Build will likely fail. >> "!LOGFILE!" 2>&1
)
if exist "!PLATFORM_DIR!" dir "!PLATFORM_DIR!" >> "!LOGFILE!" 2>&1
echo. >> "!LOGFILE!" 2>&1

REM === ADB Device Setup ===
REM Start the ADB server and verify the Pixel device is visible.
REM dotnet build -t:Install calls ADB directly (unlike XHarness which manages
REM its own ADB server).  We must ensure the server is running and the device
REM is authorized before the test step.
echo === ADB DEVICE SETUP === >> "!LOGFILE!" 2>&1

REM Log environment for debugging connectivity issues
echo [!DATE! !TIME!] ADB diagnostics starting >> "!LOGFILE!" 2>&1
echo ANDROID_HOME=!ANDROID_HOME! >> "!LOGFILE!" 2>&1
echo PATH=!PATH! >> "!LOGFILE!" 2>&1

REM Log ADB binary location and version
echo --- ADB binary info --- >> "!LOGFILE!" 2>&1
where adb >> "!LOGFILE!" 2>&1
if errorlevel 1 echo CRITICAL: adb not found on PATH >> "!LOGFILE!" 2>&1
adb version >> "!LOGFILE!" 2>&1
if errorlevel 1 echo CRITICAL: adb version failed >> "!LOGFILE!" 2>&1

REM Kill any existing ADB server to start fresh
echo [!DATE! !TIME!] Killing existing ADB server... >> "!LOGFILE!" 2>&1
adb kill-server >> "!LOGFILE!" 2>&1
if errorlevel 1 echo WARNING: adb kill-server failed (may not have been running) >> "!LOGFILE!" 2>&1

REM Start fresh ADB server
echo [!DATE! !TIME!] Starting fresh ADB server... >> "!LOGFILE!" 2>&1
adb start-server >> "!LOGFILE!" 2>&1
if errorlevel 1 echo WARNING: adb start-server failed >> "!LOGFILE!" 2>&1

REM First device listing (verbose)
echo [!DATE! !TIME!] Initial device listing: >> "!LOGFILE!" 2>&1
adb devices -l >> "!LOGFILE!" 2>&1

REM Check USB devices for Android hardware
echo --- USB Android devices (wmic) --- >> "!LOGFILE!" 2>&1
wmic path Win32_PnPEntity where "Name like '%%Android%%'" get Name,DeviceID >> "!LOGFILE!" 2>&1
if errorlevel 1 echo WARNING: wmic query for Android USB devices failed >> "!LOGFILE!" 2>&1

REM Count devices (skip header line "List of devices attached" and empty lines)
set "DEVICE_COUNT=0"
for /f "skip=1 tokens=1" %%a in ('adb devices 2^>nul') do (
    if not "%%a"=="" set /a DEVICE_COUNT+=1
)
echo Device count: !DEVICE_COUNT! >> "!LOGFILE!" 2>&1

if !DEVICE_COUNT! EQU 0 (
    echo *** CRITICAL: NO DEVICES DETECTED *** >> "!LOGFILE!" 2>&1
    echo This indicates a hardware/driver issue, not a software one. >> "!LOGFILE!" 2>&1
    echo Possible causes: >> "!LOGFILE!" 2>&1
    echo   - No Android device physically connected >> "!LOGFILE!" 2>&1
    echo   - USB driver not installed >> "!LOGFILE!" 2>&1
    echo   - Device not authorized for USB debugging >> "!LOGFILE!" 2>&1
    echo   - ADB binary incompatible with device >> "!LOGFILE!" 2>&1

    REM Try using the full ADB path explicitly
    echo [!DATE! !TIME!] Retrying with full ADB path: !ANDROID_HOME!\platform-tools\adb.exe >> "!LOGFILE!" 2>&1
    "!ANDROID_HOME!\platform-tools\adb.exe" devices -l >> "!LOGFILE!" 2>&1
    if errorlevel 1 echo WARNING: Full-path ADB devices failed >> "!LOGFILE!" 2>&1

    REM Wait for device with 30-second timeout
    echo [!DATE! !TIME!] Waiting for device ^(timeout 30s^)... >> "!LOGFILE!" 2>&1
    start /b cmd /c "ping -n 31 127.0.0.1 >nul & taskkill /f /im adb.exe >nul 2>&1" >nul 2>&1
    adb wait-for-device >> "!LOGFILE!" 2>&1
    if errorlevel 1 echo WARNING: adb wait-for-device timed out or failed >> "!LOGFILE!" 2>&1
) else (
    echo Devices detected. Proceeding. >> "!LOGFILE!" 2>&1
)

REM Final device listing after all diagnostics
echo [!DATE! !TIME!] Final device listing: >> "!LOGFILE!" 2>&1
adb devices -l >> "!LOGFILE!" 2>&1

REM Re-count devices after wait
set "FINAL_COUNT=0"
for /f "skip=1 tokens=1" %%a in ('adb devices 2^>nul') do (
    if not "%%a"=="" set /a FINAL_COUNT+=1
)
echo Final device count: !FINAL_COUNT! >> "!LOGFILE!" 2>&1
if !FINAL_COUNT! EQU 0 (
    echo *** CRITICAL: STILL NO DEVICES AFTER ALL DIAGNOSTICS *** >> "!LOGFILE!" 2>&1
    echo The build -t:Install step WILL FAIL with XA0010. >> "!LOGFILE!" 2>&1
)
echo. >> "!LOGFILE!" 2>&1

echo === STEP 2: Restore ===
echo === STEP 2: Restore === >> "!LOGFILE!" 2>&1
echo [!DATE! !TIME!] Starting restore >> "!LOGFILE!" 2>&1
!DOTNET_ROOT!\dotnet restore !HELIX_WORKITEM_ROOT!\app\MauiAndroidInnerLoop.csproj --configfile !HELIX_WORKITEM_ROOT!\app\NuGet.config /p:TargetFrameworks=!FRAMEWORK! !MSBUILD_ARGS! >> "!LOGFILE!" 2>&1
if errorlevel 1 (
    echo [!DATE! !TIME!] STEP 2 FAILED with errorlevel !errorlevel!
    echo [!DATE! !TIME!] STEP 2 FAILED with errorlevel !errorlevel! >> "!LOGFILE!" 2>&1
    type "!LOGFILE!"
    exit /b 2
)
echo [!DATE! !TIME!] Restore succeeded >> "!LOGFILE!" 2>&1

echo === STEP 3: Test ===
echo === STEP 3: Test === >> "!LOGFILE!" 2>&1
echo [!DATE! !TIME!] Starting test.py >> "!LOGFILE!" 2>&1

REM Pass MSBuild args via environment variable to avoid batch parsing issues
REM with special characters (=, /) inside delayed expansion on the command line.
REM runner.py reads PERFLAB_MSBUILD_ARGS as fallback when --msbuild-args is empty.
set "PERFLAB_MSBUILD_ARGS=!MSBUILD_ARGS!"
echo PERFLAB_MSBUILD_ARGS=!PERFLAB_MSBUILD_ARGS! >> "!LOGFILE!" 2>&1

python test.py androidinnerloop ^
    --csproj-path app\MauiAndroidInnerLoop.csproj ^
    --edit-src src\MainPage.xaml.cs ^
    --edit-dest app\MainPage.xaml.cs ^
    --package-name com.companyname.mauiandroidinnerloop ^
    -f !FRAMEWORK! -c Debug ^
    --scenario-name "!SCENARIO_NAME!" ^
    !EXTRA_ARGS! >> "!LOGFILE!" 2>&1
if errorlevel 1 (
    echo [!DATE! !TIME!] STEP 3 FAILED with errorlevel !errorlevel!
    echo [!DATE! !TIME!] STEP 3 FAILED with errorlevel !errorlevel! >> "!LOGFILE!" 2>&1
    type "!LOGFILE!"
    exit /b 3
)

echo === ALL STEPS SUCCEEDED ===
echo === ALL STEPS SUCCEEDED === >> "!LOGFILE!" 2>&1
echo [!DATE! !TIME!] Complete >> "!LOGFILE!" 2>&1
type "!LOGFILE!"
exit /b 0
