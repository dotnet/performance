import os
from shared.runner import TestTraits, Runner
from shared import const

SCENARIONAME = 'Web Large 2.0'
EXENAME = 'weblarge20'

def main():
    traits = TestTraits(exename=EXENAME,
                        guiapp='false',
                        workingdir=os.path.join(const.APPDIR, 'mvc'),
                        timeout= f'{const.MINUTE*30}',  # increase timeout for the large project
                        sdk=True,
                        )
    runner = Runner(traits)
    runner.run()


if __name__ == "__main__":
    main()
