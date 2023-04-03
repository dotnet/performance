'''
Mobile Maui App
'''
import os
from shared.const import PUBDIR
from shared.runner import TestTraits, Runner
from shared.versionmanager import versions_read_json_file_save_env

EXENAME = 'BDNAndroidTest'

if __name__ == "__main__":
    if os.path.exists(rf".\{PUBDIR}\versions.json"):
        versions_read_json_file_save_env(rf".\{PUBDIR}\versions.json")

    traits = TestTraits(exename=EXENAME, 
                        guiapp='false',
                        )
    Runner(traits).run()
