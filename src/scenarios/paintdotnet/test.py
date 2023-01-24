import os
import winreg
from datetime import datetime, timezone
from shared.runner import TestTraits, Runner
from shared import const
from performance.logger import setup_loggers, getLogger

EXENAME = 'paintdotnet'

def main():
    setup_loggers(True)

    set_environment()
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

def set_environment():
    os.environ['DOTNET_ROLL_FORWARD'] = 'LatestMajor'
    os.environ['DOTNET_ROLL_FORWARD_TO_PRERELEASE'] = '1'
    getLogger().info("Environment variables set.")

def set_registry():
    value = (datetime.now(timezone.utc) - datetime(1, 1, 1, tzinfo=timezone.utc)).total_seconds() * 10000000
    with winreg.CreateKey(winreg.HKEY_CURRENT_USER, r'Software\paint.net') as key:
        winreg.SetValueEx(key, 'Updates/LastCheckTimeUtc', None, winreg.REG_SZ, str(int(value)))
    getLogger().info("Fixed up Updates/LastCheckTimeUtc.")


if __name__ == "__main__":
    main()
