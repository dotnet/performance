'''
MAUI Desktop BenchmarkDotNet benchmarks.

Delegates to the shared Runner infrastructure which dispatches to
BDNDesktopHelper for the full lifecycle: clone dotnet/maui, build
dependencies, patch for PerfLabExporter, run BDN suites, collect results.

Usage: test.py bdndesktop --framework net11.0 --suite all
'''
from shared.runner import TestTraits, Runner

EXENAME = 'MauiDesktopBDNBenchmarks'

if __name__ == '__main__':
    traits = TestTraits(exename=EXENAME, guiapp='false')
    Runner(traits).run()
