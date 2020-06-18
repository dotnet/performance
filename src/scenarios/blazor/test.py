'''
Empty Blazor Wasm Template
'''
import os
from shared.runner import TestTraits, Runner
from shared.const import APPDIR

EXENAME = 'emptyblazorwasmtemplate'

if __name__ == "__main__":
    traits = TestTraits(exename=EXENAME, 
                        guiapp='false',
                        artifact=os.path.join(APPDIR, 'obj', 'Release', 'netstandard2.1', 'blazor', 'linker', 'linker-dependencies.xml.gz')
                        )
    Runner(traits).run()
