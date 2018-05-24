@REM This is a template script to run .NET Performance benchmarks with CoreRun.exe
@if not defined _echo echo off


setlocal
  set "ERRORLEVEL="
  set "USAGE_DISPLAYED="
  set "PUBLISH_DIR="
  set "BENCHMARK_ASSEMBLY="

  set "ARCHITECTURE=x64"
  set "CONFIGURATION=Release"
  set "TARGET_FRAMEWORK=netcoreapp2.0"

  call :parse_command_line_arguments %* || exit /b 1
  if defined USAGE_DISPLAYED exit /b %ERRORLEVEL%
  cd "%PUBLISH_DIR%"
  call :patch_core_root                 || exit /b 1
  call :common_env                      || exit /b 1

  call :run_command %STABILITY_PREFIX% "%CORE_ROOT%\CoreRun.exe" PerformanceHarness.dll %BENCHMARK_ASSEMBLY% --perf:collect %COLLECTION_FLAGS%
endlocal& exit /b %ERRORLEVEL%


:common_env
  echo/   Common .NET environment variables set.
  set COMPlus_
  set DOTNET_
  set UseSharedCompilation
  set XUNIT_
  exit /b 0


:patch_core_root
rem ****************************************************************************
rem Copies latest managed binaries needed by the benchmarks, plus native
rem binaries used by TraceEvent.
rem ****************************************************************************
  REM Managed binaries
  for %%f in (Microsoft.CodeAnalysis Newtonsoft.Json) do (
    call :run_command xcopy.exe /VYRQKZ "%CD%\%%f*.dll" "%CORE_ROOT%\" || (
      call :print_error Failed to copy from: "%CD%\%%f*.dll", to: "%CORE_ROOT%\"
      exit /b 1
    )
  )

  REM Copy native libraries used by Microsoft.Diagnostics.Tracing.TraceEvent.dll
  for %%a in (amd64 x86) do (
    for %%f in (KernelTraceControl msdia140) do (
      call :run_command xcopy.exe /VYRQKZ "%CD%\%%a\%%f.dll" "%CORE_ROOT%\%%a\" || (
        call :print_error Failed to copy from: "%CD%\%%a\%%f.dll", to: "%CORE_ROOT%\%%a\"
        exit /b 1
      )
    )
  )
  exit /b %ERRORLEVEL%


:parse_command_line_arguments
rem ****************************************************************************
rem   Parses the script's command line arguments.
rem ****************************************************************************
  IF /I [%~1] == [--core-root] (
    set "CORE_ROOT=%~2"
    shift
    shift
    goto :parse_command_line_arguments
  )

  IF /I [%~1] == [--stability-prefix] (
    set "STABILITY_PREFIX=%~2"
    shift
    shift
    goto :parse_command_line_arguments
  )

  IF /I [%~1] == [--collection-flags] (
    set "COLLECTION_FLAGS=%~2"
    shift
    shift
    goto :parse_command_line_arguments
  )

  IF /I [%~1] == [--publish-dir] (
    set "PUBLISH_DIR=%~2"
    shift
    shift
    goto :parse_command_line_arguments
  )

  IF /I [%~1] == [--assembly] (
    set "BENCHMARK_ASSEMBLY=%~2"
    shift
    shift
    goto :parse_command_line_arguments
  )

  if /I [%~1] == [-?] (
    call :usage
    exit /b 0
  )
  if /I [%~1] == [-h] (
    call :usage
    exit /b 0
  )
  if /I [%~1] == [--help] (
    call :usage
    exit /b 0
  )

  if not defined CORE_ROOT (
    call :print_error CORE_ROOT was not defined.
    exit /b 1
  )

  if not defined PUBLISH_DIR (
    call :print_error --publish-dir was not specified.
    exit /b 1
  )
  if not exist "%PUBLISH_DIR%" (
    call :print_error Specified published directory: %PUBLISH_DIR%, does not exist.
    exit /b 1
  )

  if not defined BENCHMARK_ASSEMBLY (
    call :print_to_console --assembly DOTNET_ASSEMBLY_NAME was not specified, all benchmarks will be run.
  )

  if not defined COLLECTION_FLAGS (
    call :print_to_console COLLECTION_FLAGS was not defined. Defaulting to stopwatch
    set "COLLECTION_FLAGS=stopwatch"
  )

  exit /b %ERRORLEVEL%


:usage
rem ****************************************************************************
rem   Script's usage.
rem ****************************************************************************
  set "USAGE_DISPLAYED=1"
  echo/ %~nx0 [OPTIONS]
  echo/
  echo/ Options:
  echo/   --collection-flags COLLECTION_FLAGS
  echo/     xUnit-Performance Api valid performance collection flags: ^<default^+CacheMisses^+InstructionRetired^+BranchMispredictions^+gcapi^>
  echo/   --core-root CORE_ROOT
  echo/     CoreClr's CORE_ROOT directory (Binaries to be tested).
  echo/   --publish-dir PUBLISH_DIR
  echo/     Directory that contains the published .NET benchmarks.
  echo/   --stability-prefix STABILITY_PREFIX
  echo/     Command to prepend to the benchmark execution.
  echo/   --assembly DOTNET_ASSEMBLY_NAME
  echo/     .NET assembly to be tested.
  exit /b %ERRORLEVEL%


:run_command
rem ****************************************************************************
rem   Function wrapper used to send the command line being executed to the
rem   console screen, before the command is executed.
rem ****************************************************************************
  if "%~1" == "" (
    call :print_error No command was specified.
    exit /b 1
  )

  call :print_to_console $ %*
  call %*
  exit /b %ERRORLEVEL%


:print_error
rem ****************************************************************************
rem   Function wrapper that unifies how errors are output by the script.
rem   Functions output to the standard error.
rem ****************************************************************************
  call :print_to_console [ERROR] %*   1>&2
  exit /b %ERRORLEVEL%


:print_to_console
rem ****************************************************************************
rem   Sends text to the console screen. This can be useful to provide
rem   information on where the script is executing.
rem ****************************************************************************
  echo/
  echo/%USERNAME%@%COMPUTERNAME% "%CD%"
  echo/[%DATE%][%TIME:~0,-3%] %*
  exit /b %ERRORLEVEL%
