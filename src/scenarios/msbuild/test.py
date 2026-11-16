"""Run a catalog-selected MSBuild scenario."""

from shared.runner import Runner, TestTraits

if __name__ == "__main__":
    Runner(TestTraits(exename="msbuild", guiapp="false")).run()
