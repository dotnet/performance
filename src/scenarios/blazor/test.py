'''
Empty Blazor Wasm Template
'''
from shared.runner import TestTraits, Runner

SCENARIONAME = 'Empty Blazor Wasm Template'
EXENAME = 'emptyblazorwasmtemplate'

if __name__ == "__main__":
    traits = TestTraits(exename=EXENAME, 
                        guiapp='false',
                        )
    Runner(traits).run()
