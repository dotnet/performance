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
TMPDIR = 'tmp'

DOTNET = 'dotnet'
PYTHON = 'python'

BUILD_CLEAN = 'Clean Build'
BUILD_NO_CHANGES = 'Build No Changes'
ITERATION_SETUP_FILE = os.path.join(os.path.dirname(shared.__file__), 'sdk_iteration_setup.py')

STARTUP_PROCESSTIME = "ProcessTime"

