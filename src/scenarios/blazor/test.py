'''
Static Blazor Wasm
'''
from shared.runner import TestTraits, Runner

SCENARIONAME = 'Static Blazor Wasm'
EXENAME = 'blazor'

if __name__ == "__main__":
    traits = TestTraits(exename=EXENAME, 
                        guiapp='false',
                        )
    Runner(traits).run()
