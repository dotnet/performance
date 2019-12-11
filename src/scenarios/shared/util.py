'''
Utility routines
'''
import sys
import os
import platform
from os import environ
from shared import const
from performance.constants import UPLOAD_TOKEN_VAR

def helixpayload():
    '''
    Returns the helix payload. Will be None outside of helix.
    '''
    return environ.get('HELIX_CORRELATION_PAYLOAD')

def helixuploaddir():
    '''
    Gets the directory to upload files
    '''
    return environ.get('HELIX_WORKITEM_UPLOAD_ROOT')

def extension():
    'gets platform specific extension'
    return '.exe' if sys.platform == 'win32' else ''

def builtexe(exename: str):
    'gets binary path'
    return os.path.join(const.BINDIR, '%s%s' % (exename, extension()))

def publishedexe(exename: str):
    'gets binary path for published exe'
    return os.path.join(const.PUBDIR, '%s%s' % (exename, extension()))

def uploadtokenpresent():
    return environ.get(UPLOAD_TOKEN_VAR) is not None

def runninginlab():
    return environ.get('PERFLAB_INLAB') is not None

def getruntimeidentifier():
    rid = 'win-' if sys.platform == 'win32' else 'linux-'
    rid += 'x64' if platform.machine().endswith('64') else 'x86'
    return rid