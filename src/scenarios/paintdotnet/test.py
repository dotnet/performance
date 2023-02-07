import os, subprocess
import winreg
from datetime import datetime, timezone
from shared.runner import TestTraits, Runner
from shared import const
from performance.logger import setup_loggers, getLogger
from os.path import join
from shared.versionmanager import get_version_from_dll_powershell

EXENAME = 'paintdotnet'

def main():
    setup_loggers(True)

    pdn_version = get_pdn_version()
    set_environment(pdn_version)
    set_registry()
    traits = TestTraits(exename=EXENAME,
                        guiapp='true',
                        startupmetric='PDN', 
                        timeout= f'{const.MINUTE*1}',
                        measurementdelay='10',
                        runwithoutexit='false',
                        processwillexit="false",
                        runwithdotnet='true',
                        )
    runner = Runner(traits)
    runner.run()

def set_environment(pdn_version):
    os.environ['DOTNET_ROLL_FORWARD'] = 'LatestMajor'
    os.environ['DOTNET_ROLL_FORWARD_TO_PRERELEASE'] = '1'
    os.environ['PDN_VERSION'] = pdn_version
    getLogger().info("Environment variables set.")

def set_registry():
    value = (datetime.now(timezone.utc) - datetime(1, 1, 1, tzinfo=timezone.utc)).total_seconds() * 10000000
    with winreg.CreateKey(winreg.HKEY_CURRENT_USER, r'Software\paint.net') as key:
        winreg.SetValueEx(key, 'Updates/LastCheckTimeUtc', None, winreg.REG_SZ, str(int(value)))
    getLogger().info("Fixed up Updates/LastCheckTimeUtc.")

def get_pdn_version():
    file = join(os.environ['HELIX_WORKITEM_ROOT'], const.PUBDIR, EXENAME) + '.dll'
    version = get_version_from_dll_powershell(file)
    getLogger().info(f"PDN version is {version}.")
    return version

if __name__ == "__main__":
    main()
