'''
C# Console app
'''
from shared.runner import TestTraits, Runner

EXENAME = 'appblazorwasmci'

if __name__ == "__main__":
    traits = TestTraits(exename=EXENAME, 
                        guiapp='false',
                        skipmeasurementiteration='false',
                        warmup='false',
                        iterations='1'
                        )
    Runner(traits).run()
