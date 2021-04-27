'''
Utility routines
'''
import sys
import os
import platform
from os import environ
from shared import const
from performance.common import iswin, extension
from performance.constants import UPLOAD_TOKEN_VAR

def helixworkitempayload():
    '''
    Returns the helix workitem payload. Will be None outside of helix.
    '''
    return environ.get('HELIX_WORKITEM_PAYLOAD')

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

def uploadtokenpresent():
    return environ.get(UPLOAD_TOKEN_VAR) is not None

def getruntimeidentifier():
    rid = None
    if iswin():
        rid = 'win-'
    elif sys.platform == 'linux' or sys.platform == 'linux2':
        rid = 'linux-'
    else:
        raise Exception('Platform %s not supported.' % sys.platform)

    if 'aarch64' in platform.machine() or os.environ.get('PERFLAB_BUILDARCH') == 'arm64':
        rid += 'arm64'
    elif platform.machine().endswith('64'):
        rid += 'x64'
    elif platform.machine().endswith('86'):
        rid += 'x86'
    else:
        raise Exception('Machine %s not supported.' % platform.machine())

    return rid

def pythoncommand():
    if iswin():
        return 'py'
    else:
        return 'python3'
