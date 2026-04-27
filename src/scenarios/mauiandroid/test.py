'''
Mobile Maui App
'''
import os
from shared.const import PUBDIR
from shared.runner import TestTraits, Runner
from shared.versionmanager import versions_read_json_file_save_env

EXENAME = 'MauiAndroidDefault'

if __name__ == "__main__":    
    versions_read_json_file_save_env(os.path.join(".", PUBDIR, "versions.json"))

    traits = TestTraits(exename=EXENAME, 
                        guiapp='false',
                        )
    Runner(traits).run()
