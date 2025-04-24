'''
constant values for strings
'''
import os
import shared

STARTUP = 'startup'
SDK = 'sdk'
SOD = 'sod'
IL = 'il'
R2R = 'r2r'
CROSSGEN = 'crossgen'
CROSSGEN2 = 'crossgen2'
INNERLOOP = 'innerloop'
INNERLOOPMSBUILD = "innerloopmsbuild"
DOTNETWATCH = "dotnetwatch"
DEVICESTARTUP = "devicestartup"
DEVICEMEMORYCONSUMPTION = "devicememoryconsumption"
ANDROIDINSTRUMENTATION = "androidinstrumentation"
DEVICEPOWERCONSUMPTION = "devicepowerconsumption"
BUILDTIME = "buildtime"

SCENARIO_NAMES = {STARTUP: 'Startup',
                  SDK: 'SDK',
                  CROSSGEN: 'Crossgen',
                  CROSSGEN2: 'Crossgen2',
                  INNERLOOP: 'Innerloop',
                  INNERLOOPMSBUILD: 'InnerLoopMsBuild',
                  DOTNETWATCH: 'DotnetWatch',
                  BUILDTIME: 'BuildTime'}

BINDIR = 'bin'
PUBDIR = 'pub'
APPDIR = 'app'
TRACEDIR = 'traces'
SRCDIR = 'src' # used for checked in source.
TMPDIR = 'tmp'
ARTIFACTDIR = 'artifacts'
CROSSGENDIR = 'crossgen.out'
PYCACHE = '__pycache__'

CLEAN_BUILD = 'clean_build'
BUILD_NO_CHANGE = 'build_no_change'
NEW_CONSOLE = 'new_console'

CROSSGEN2_SINGLEFILE = 'Single'
CROSSGEN2_COMPOSITE = 'Composite'

DOTNET = 'dotnet'

ITERATION_SETUP_FILE = os.path.join(os.path.dirname(shared.__file__), 'sdk_iteration_setup.py')

STARTUP_PROCESSTIME = "ProcessTime"
STARTUP_CROSSGEN2 = "Crossgen2"
STARTUP_DEVICETIMETOMAIN = "DeviceTimeToMain"

MEMORYCONSUMPTION_ANDROID = "AndroidMemoryConsumption"

POWERCONSUMPTION_ANDROID = "AndroidPowerConsumption"

MINUTE = 60
