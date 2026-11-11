'''
Empty Blazor Wasm Template
'''
from shared.runner import TestTraits, Runner, get_linker_dump
from shared.const import APPDIR

EXENAME = 'blazorpizza'

if __name__ == "__main__":
    artifact = get_linker_dump(APPDIR)

    traits = TestTraits(exename=EXENAME, 
                        guiapp='false',
                        artifact=artifact
                        )
    Runner(traits).run()
