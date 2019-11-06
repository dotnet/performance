from shared.runner import TestTraits, Runner

SCENARIO_NAME = '.NET Core 2.0 Console Template'
EXE_NAME = 'NetCoreApp'

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
