'''
Empty Blazor Wasm Template
'''
import os
from shared.runner import TestTraits, Runner
from shared.const import APPDIR

EXENAME = 'emptyblazorwasmtemplate'

if __name__ == "__main__":
    net5_linker_dump = os.path.join(APPDIR, 'obj', 'Release', 'net5.0', 'browser-wasm', 'linked', 'linker-dependencies.xml.gz')
    netstandard21_linker_dump = os.path.join(APPDIR, 'obj', 'Release', 'netstandard2.1', 'blazor', 'linker', 'linker-dependencies.xml.gz')
    if os.path.exists(net5_linker_dump):
        artifact = net5_linker_dump
    elif os.path.exists(netstandard21_linker_dump):
        artifact = netstandard21_linker_dump

    traits = TestTraits(exename=EXENAME, 
                        guiapp='false',
                        artifact=artifact
                        )
    Runner(traits).run()
