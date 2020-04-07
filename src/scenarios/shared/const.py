'''
constant values for strings
'''
import os
import shared

STARTUP = 'startup'
SDK = 'sdk'
IL = 'il'
R2R = 'r2r'
CROSSGEN = 'crossgen'

SCENARIO_NAMES = {STARTUP: 'Startup',
                  SDK: 'SDK',
                  CROSSGEN: 'Crossgen'}

BINDIR = 'bin'
PUBDIR = 'pub'
APPDIR = 'app'
TRACEDIR = 'traces'
SRCDIR = 'src' # used for checked in source.
TMPDIR = 'tmp'

CLEAN_BUILD = 'clean_build'
BUILD_NO_CHANGE = 'build_no_change'
NEW_CONSOLE = 'new_console'

DOTNET = 'dotnet'

ITERATION_SETUP_FILE = os.path.join(os.path.dirname(shared.__file__), 'sdk_iteration_setup.py')

STARTUP_PROCESSTIME = "ProcessTime"

MINUTE = 60
