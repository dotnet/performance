import os
from shared.runner import TestTraits, Runner
from shared import const

EXENAME = 'wpf'

def main():
    traits = TestTraits(exename=EXENAME,
                        guiapp='false', 
                        )
    runner = Runner(traits)
    runner.run()


if __name__ == "__main__":
    main()
