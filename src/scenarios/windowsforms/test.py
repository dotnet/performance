import os
from shared.runner import TestTraits, Runner
from shared import const

SCENARIO_NAME = 'Windows Forms Template'
EXE_NAME = 'windowsforms'

def main():
    traits = TestTraits(scenarioname=SCENARIO_NAME,
                        exename=EXE_NAME,
                        guiapp='false', 
                        sdk=True,
                        )
    runner = Runner(traits)
    runner.run()


if __name__ == "__main__":
    main()
