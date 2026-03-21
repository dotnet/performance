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
