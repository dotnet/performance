'''
Utility routines
'''
import sys
import os
import platform
from os import environ, path
from shared import const
from performance.common import iswin, extension

def helixworkitempayload():
    '''
    Returns the helix workitem payload. Will be None outside of helix.
    '''
    return environ.get('HELIX_WORKITEM_PAYLOAD')

def helixcorrelationpayload():
    '''
    Returns the helix correlation payload. Will be None outside of helix.
    '''
    return environ.get('HELIX_CORRELATION_PAYLOAD')

def helixuploaddir():
    '''
    Gets the directory to upload files
    '''
    return environ.get('HELIX_WORKITEM_UPLOAD_ROOT')

def builtexe(exename: str):
    'gets binary path'
    return os.path.join(const.BINDIR, '%s%s' % (exename, extension()))

def appfolder(projname: str, projext: str):
    'gets path for calling dotnet run'
    return os.path.join(const.APPDIR, '%s%s' % (projname, projext))

def publishedexe(exename: str):
    'gets binary path for published exe'
    return os.path.join(const.PUBDIR, '%s%s' % (exename, extension()))

def publisheddll(exename: str):
    'gets binary path for published dll'
    return os.path.join(const.PUBDIR, '%s%s' % (exename, ".dll"))

def getruntimeidentifier():
    rid = None
    if iswin():
        rid = 'win-'
    elif sys.platform == 'linux' or sys.platform == 'linux2':
        rid = 'linux-'
    elif sys.platform == 'darwin':
        rid = 'osx-'
    else:
        raise Exception('Platform %s not supported.' % sys.platform)

    if platform.machine() in ('aarch64', 'arm64') or os.environ.get('PERFLAB_BUILDARCH') == 'arm64':
        rid += 'arm64'
    elif platform.machine() == 's390x':
        rid += 's390x'
    elif platform.machine().endswith('64'):
        rid += 'x64'
    elif platform.machine().endswith('86'):
        rid += 'x86'
    else:
        raise Exception('Machine %s not supported.' % platform.machine())

    return rid

# https://stackoverflow.com/a/42580137
def is_venv(): 
    return (hasattr(sys, 'real_prefix') or
            (hasattr(sys, 'base_prefix') and sys.base_prefix != sys.prefix))

def pythoncommand():
    if is_venv():
        return 'python'
    elif iswin():
        return 'py -3'
    else:
        return 'python3'

def xharnesscommand():
    xharnesspath = os.environ.get('XHARNESS_CLI_PATH')
    if xharnesspath is None or not os.path.exists(xharnesspath):
        return ['xharness']
    return ['dotnet','exec',xharnesspath]

def xharness_adb():
    return xharnesscommand() + ['android', 'adb', '--']
