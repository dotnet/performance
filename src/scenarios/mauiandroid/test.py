'''
Mobile Maui App
'''
from shared.const import PUBDIR
from shared.runner import TestTraits, Runner
from shared.versionmanager import versionsreadjsonfilesaveenv

EXENAME = 'MauiAndroidDefault'

if __name__ == "__main__":    
    versionsreadjsonfilesaveenv(rf".\{PUBDIR}\versions.json")

    traits = TestTraits(exename=EXENAME, 
                        guiapp='false',
                        )
    Runner(traits).run()
