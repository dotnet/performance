'''
constant values for strings
'''
import os
import shared

STARTUP = 'startup'
SDK = 'sdk'
IL = 'il'
R2R = 'r2r'

SCENARIO_NAMES = {STARTUP: 'Startup',
                  SDK: 'SDK'}

BINDIR = 'bin'
PUBDIR = 'pub'
APPDIR = 'app'
TRACEDIR = 'traces'
SRCDIR = 'src' # used for checked in source.

CLEAN_BUILD = 'clean_build'
BUILD_NO_CHANGE = 'build_no_change'

DOTNET = 'dotnet'

ITERATION_SETUP_FILE = os.path.join(os.path.dirname(shared.__file__), 'sdk_iteration_setup.py')

STARTUP_PROCESSTIME = "ProcessTime"

