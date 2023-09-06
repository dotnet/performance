'''
.NET Android Default App
'''
from shared.const import PUBDIR
from shared.runner import TestTraits, Runner
from shared.versionmanager import versions_read_json_file_save_env

EXENAME = 'NetAndroidDefault'

if __name__ == "__main__":    
    versions_read_json_file_save_env(rf".\{PUBDIR}\versions.json")

    traits = TestTraits(exename=EXENAME, 
                        guiapp='false',
                        )
    Runner(traits).run()
