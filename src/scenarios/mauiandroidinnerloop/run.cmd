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
    echo Java not found in common paths. Downloading Microsoft OpenJDK 17... >> "%LOGFILE%" 2>&1
    set "JDK_ZIP=%HELIX_WORKITEM_ROOT%\openjdk17.zip"
    set "JDK_EXTRACT=%HELIX_WORKITEM_ROOT%\jdk"
    echo [%DATE% %TIME%] Starting OpenJDK download >> "%LOGFILE%" 2>&1
    curl.exe -L -o "!JDK_ZIP!" "https://aka.ms/download-jdk/microsoft-jdk-17.0.13-windows-x64.zip" >> "%LOGFILE%" 2>&1
    if errorlevel 1 (
        echo ERROR: Failed to download OpenJDK >> "%LOGFILE%" 2>&1
    ) else (
        echo [%DATE% %TIME%] Download complete. Extracting... >> "%LOGFILE%" 2>&1
        powershell -Command "Expand-Archive -Path '!JDK_ZIP!' -DestinationPath '!JDK_EXTRACT!' -Force" >> "%LOGFILE%" 2>&1
        echo [%DATE% %TIME%] Extraction complete >> "%LOGFILE%" 2>&1
        REM The ZIP extracts to a subdirectory like jdk-17.0.13+11
        for /d %%d in ("!JDK_EXTRACT!\jdk-*") do set "JAVA_HOME=%%~d"
        echo Downloaded JDK JAVA_HOME=!JAVA_HOME! >> "%LOGFILE%" 2>&1
    )
)

if not defined JAVA_HOME (
    echo ERROR: Java SDK still not found after download attempt >> "%LOGFILE%" 2>&1
    dir "!ProgramW6432!\Microsoft\" >> "%LOGFILE%" 2>&1
    dir "!ProgramFiles!\Microsoft\" >> "%LOGFILE%" 2>&1
    if exist "!JDK_EXTRACT!" dir /s "!JDK_EXTRACT!" >> "%LOGFILE%" 2>&1
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

REM === Set up Android Build-Tools (aapt2, zipalign) ===
REM dotnet build for Android requires aapt2.exe and zipalign.exe from
REM Android SDK Build-Tools.  MSBuild searches ANDROID_HOME\build-tools\
REM <version>\aapt2.exe.  Download the complete package from Google — the
REM workload pack only has aapt2, not zipalign.
echo === Android Build-Tools Setup === >> "%LOGFILE%" 2>&1
set "BUILD_TOOLS_DIR=!ANDROID_HOME!\build-tools\35.0.0"

echo [%DATE% %TIME%] Downloading Android SDK Build-Tools from Google... >> "%LOGFILE%" 2>&1
set "BT_ZIP=%HELIX_WORKITEM_ROOT%\build-tools.zip"
set "BT_EXTRACT=%HELIX_WORKITEM_ROOT%\build-tools-extract"
curl.exe -L -o "!BT_ZIP!" "https://dl.google.com/android/repository/build-tools_r35_windows.zip" >> "%LOGFILE%" 2>&1
if errorlevel 1 (
    echo ERROR: Failed to download Build-Tools. Build will likely fail with XA5205. >> "%LOGFILE%" 2>&1
) else (
    echo [%DATE% %TIME%] Download complete. Extracting... >> "%LOGFILE%" 2>&1
    powershell -Command "Expand-Archive -Path '!BT_ZIP!' -DestinationPath '!BT_EXTRACT!' -Force" >> "%LOGFILE%" 2>&1
    echo [%DATE% %TIME%] Extraction complete >> "%LOGFILE%" 2>&1
    REM Find the top-level directory inside the ZIP (e.g. android-15/)
    mkdir "!BUILD_TOOLS_DIR!" >> "%LOGFILE%" 2>&1
    for /d %%d in ("!BT_EXTRACT!\*") do (
        echo Moving contents from %%d to !BUILD_TOOLS_DIR! >> "%LOGFILE%" 2>&1
        xcopy /Y /E /Q "%%d\*" "!BUILD_TOOLS_DIR!\" >> "%LOGFILE%" 2>&1
    )
)

if exist "!BUILD_TOOLS_DIR!\aapt2.exe" (
    echo aapt2.exe found at !BUILD_TOOLS_DIR!\aapt2.exe >> "%LOGFILE%" 2>&1
) else (
    echo WARNING: aapt2.exe NOT found. Build will likely fail with XA5205. >> "%LOGFILE%" 2>&1
)
if exist "!BUILD_TOOLS_DIR!\zipalign.exe" (
    echo zipalign.exe found at !BUILD_TOOLS_DIR!\zipalign.exe >> "%LOGFILE%" 2>&1
) else (
    echo WARNING: zipalign.exe NOT found. Build may fail. >> "%LOGFILE%" 2>&1
)
if exist "!BUILD_TOOLS_DIR!" dir "!BUILD_TOOLS_DIR!" >> "%LOGFILE%" 2>&1
echo. >> "%LOGFILE%" 2>&1

REM === Set up android.jar (platforms) ===
REM The MAUI workload (version 36.99.0-ci.main.0) requires API level 36.1 but
REM android.jar is NOT installed via the standard Android SDK.  The workload
REM pack Microsoft.Android.Sdk.Windows bundles android.jar inside its payload.
REM MSBuild expects it at ANDROID_HOME\platforms\android-<api>\android.jar,
REM so find it in the workload pack and copy it into the expected location.
echo === Android Platforms (android.jar) Setup === >> "%LOGFILE%" 2>&1
set "ANDROID_PACK_DIR="
for /d %%d in (!HELIX_CORRELATION_PAYLOAD!\dotnet\packs\Microsoft.Android.Sdk.Windows\*) do set "ANDROID_PACK_DIR=%%d"

if defined ANDROID_PACK_DIR (
    echo Found workload pack: !ANDROID_PACK_DIR! >> "%LOGFILE%" 2>&1

    REM Search for android.jar inside the workload pack
    set "ANDROID_JAR_PATH="
    for /f "delims=" %%f in ('dir /s /b "!ANDROID_PACK_DIR!\android.jar" 2^>nul') do set "ANDROID_JAR_PATH=%%f"

    if defined ANDROID_JAR_PATH (
        echo Found android.jar at !ANDROID_JAR_PATH! >> "%LOGFILE%" 2>&1

        REM Extract the parent directory name to determine the API level
        REM e.g. ...\platforms\android-36.1\android.jar -> API_DIR_NAME=android-36.1
        for %%f in ("!ANDROID_JAR_PATH!") do set "ANDROID_JAR_DIR=%%~dpf"
        set "ANDROID_JAR_DIR=!ANDROID_JAR_DIR:~0,-1!"
        for %%p in ("!ANDROID_JAR_DIR!") do set "API_DIR_NAME=%%~nxp"
        echo API directory name: !API_DIR_NAME! >> "%LOGFILE%" 2>&1

        REM Create the expected directory structure and copy android.jar
        mkdir "!ANDROID_HOME!\platforms\!API_DIR_NAME!" >> "%LOGFILE%" 2>&1
        copy /Y "!ANDROID_JAR_PATH!" "!ANDROID_HOME!\platforms\!API_DIR_NAME!\" >> "%LOGFILE%" 2>&1
        echo Copied android.jar to !ANDROID_HOME!\platforms\!API_DIR_NAME!\ >> "%LOGFILE%" 2>&1
    ) else (
        echo WARNING: android.jar NOT found in workload pack >> "%LOGFILE%" 2>&1
        echo Listing workload pack contents for diagnostics... >> "%LOGFILE%" 2>&1
        dir /s "!ANDROID_PACK_DIR!" >> "%LOGFILE%" 2>&1
    )
) else (
    echo WARNING: Microsoft.Android.Sdk.Windows pack not found >> "%LOGFILE%" 2>&1
    echo Listing packs directory for diagnostics... >> "%LOGFILE%" 2>&1
    dir "!HELIX_CORRELATION_PAYLOAD!\dotnet\packs\" >> "%LOGFILE%" 2>&1
)
echo. >> "%LOGFILE%" 2>&1

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
