'''
C# Console app
'''
from shared.runner import TestTraits, Runner

SCENARIONAME = 'Crossgen Throughput'
EXENAME = 'crossgen'

if __name__ == "__main__":
    traits = TestTraits(scenarioname=SCENARIONAME,
                        exename=EXENAME,
                        startupmetric='ProcessTime',
                        crossgen=True,
                        guiapp='false',
                        )
    Runner(traits).run()
