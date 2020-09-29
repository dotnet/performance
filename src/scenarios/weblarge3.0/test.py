import os
from shared.runner import TestTraits, Runner
from shared import const

EXENAME = 'weblarge30'

def main():
    traits = TestTraits(exename=EXENAME,
                        guiapp='false',
                        workingdir=os.path.join(const.APPDIR, 'mvc'),
                        timeout= f'{const.MINUTE*30}',   # increase timeout for the large project
                        )
    runner = Runner(traits)
    runner.run()


if __name__ == "__main__":
    main()
