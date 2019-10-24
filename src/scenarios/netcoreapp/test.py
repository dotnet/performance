import os
from shared.runner import TestTraits, Runner
from shared import const

SCENARIO_NAME = '.NET Core 2.0 Console Template'
EXE_NAME = 'NetCoreApp'

def main():
    traits = TestTraits(scenarioname=SCENARIO_NAME,
                        exename=EXE_NAME,
                        guiapp='false',  # string passed through to tool
                        sdk=True,
                        startup=True,
                        iterations='10'
                        )
    runner = Runner(traits)
    runner.parseargs()
    runner.run()


if __name__ == "__main__":
    main()
