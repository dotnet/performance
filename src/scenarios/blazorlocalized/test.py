'''
Localized Blazor Wasm Template
'''
import os
from shared.runner import TestTraits, Runner
from shared.const import APPDIR

EXENAME = 'blazorlocalized'

if __name__ == "__main__":
    traits = TestTraits(exename=EXENAME,
                        guiapp='false'
                        )
    Runner(traits).run()
