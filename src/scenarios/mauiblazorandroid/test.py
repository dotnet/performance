'''
C# Console app
'''
import os
from shared.runner import TestTraits, Runner

EXENAME = 'MauiBlazorAndroidDefault'
MAUIVERSIONFILE = 'MAUI_VERSION.txt'

if __name__ == "__main__":
    try:
        with open(f'pub/{MAUIVERSIONFILE}', 'r') as f:
            maui_version = f.read()
            if("sha" not in maui_version or "azdo" not in maui_version):
                raise ValueError(f"MAUI_VERSION does not contain sha and azdo indicating failure to retrieve or set the value. MAUI_VERSION: {maui_version}")
            else:
                print(f"Found MAUI_VERSION {maui_version}")
                os.environ["MAUI_VERSION"] = maui_version
            
    except Exception as e:
        print("Failed to read MAUI_VERSION.txt")

    traits = TestTraits(exename=EXENAME, 
                        guiapp='false',
                        )
    Runner(traits).run()
