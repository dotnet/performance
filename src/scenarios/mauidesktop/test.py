import os
import subprocess
from shared.runner import TestTraits, Runner

EXENAME = 'MauiDesktopTesting'

def main():
    traits = TestTraits(exename=EXENAME,
                        guiapp='true',
                        startupmetric='WinUI',
                        timeout=30,
                        measurementdelay='6',
                        runwithoutexit='false',
                        processwillexit="false",
                        )
    runner = Runner(traits)
    runner.run()


if __name__ == "__main__":
    result = subprocess.run(['powershell', '-Command', r'Get-ChildItem .\pub\Microsoft.Maui.dll | Select-Object -ExpandProperty VersionInfo | Select-Object ProductVersion | Select-Object -ExpandProperty ProductVersion'], stdout=subprocess.PIPE, stderr=subprocess.STDOUT, shell=True)
    os.environ["MAUI_VERSION"] = result.stdout.decode('utf-8').strip()
    print(f'Env: MAUI_VERSION: {os.environ["MAUI_VERSION"]}')
    if("sha" not in os.environ["MAUI_VERSION"] and "azdo" not in os.environ["MAUI_VERSION"]):
        raise ValueError(f"MAUI_VERSION does not contain sha and azdo indicating failure to retrieve or set the value. MAUI_VERSION: {os.environ['MAUI_VERSION']}")
    main()
