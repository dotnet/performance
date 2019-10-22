import os
from shared.runner import TestTraits, Runner
from shared import const

SCENARIO_NAME = 'NetCoreApp'
PROJECT_FILE = os.path.join(SCENARIO_NAME, SCENARIO_NAME+'.csproj')


def main():
    traits = TestTraits(scenarioname='NetCoreApp',
                        exename=const.DOTNET,
                        startupmetric='ProcessTime',
                        appargs='build %s' % PROJECT_FILE,
                        guiapp='false',  # string passed through to tool
                        sdk=True,
                        startup=True,
                        # iterationsetup='dotnet',
                        # setupargs='clean %s' % PROJECT_FILE
                        )
    runner = Runner(traits)
    runner.parseargs()
    runner.run()


if __name__ == "__main__":
    main()
