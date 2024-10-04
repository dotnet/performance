# GC.Analysis.API

This repository contains all the code responsible for conducting GC, CPU and Threading analysis in .NET Interactive notebooks.

## Getting Started

1. Install all dependencies by:
   1. [Installing VSCode](https://code.visualstudio.com/Download) - install vscode using the installer; downloading a zip file of the exe will result in issues with the ``Install.cmd`` script.
   2. [Installing the .NET6 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/6.0)
   3. Ensure you have Jupyter (one of the following options):
      1. Install python using the Windows Store.
      2. Manually install Jupyter:
         1. ``pip`` is in your PATH:
            1. Manually installing python through the Windows Store.
      3. OR: Downloading Anaconda and using conda commands - this should also automatically install ``jupyter`` for you.
   4. Invoking the ``Install.cmd`` command.
2. Examine [GCAnalysisExamples.ipynb](GCAnalysisExamples.ipynb) for example usage for GC Analysis.

## Setting Up For CPU Analysis

The first time you make use of the TraceEvent dlls to conduct CPU Analysis, whether you are on a fresh machine or not or if you pulled in a newer version of the Trace Event library, you will encounter the following error:

```powershell
Exception: System.ApplicationException: Could not load native DLL C:\Users\<User>\.nuget\packages\microsoft.diagnostics.tracing.traceevent\<TraceEvent Version>\lib\native\amd64\msdia140.dll
   at NativeDlls.LoadNative(String simpleName)
   at Dia2Lib.DiaLoader.GetDiaSourceObject()
   at Microsoft.Diagnostics.Symbols.NativeSymbolModule..ctor(SymbolReader reader, String pdbFilePath, Action`1 loadData)
   at Microsoft.Diagnostics.Symbols.NativeSymbolModule..ctor(SymbolReader reader, String pdbFilePath)
   at Microsoft.Diagnostics.Symbols.SymbolReader.OpenSymbolFile(String pdbFilePath)
   at Microsoft.Diagnostics.Tracing.Etlx.TraceCodeAddresses.OpenPdbForModuleFile(SymbolReader symReader, TraceModuleFile moduleFile)
   at Microsoft.Diagnostics.Tracing.Etlx.TraceCodeAddresses.LookupSymbolsForModule(SymbolReader reader, TraceModuleFile moduleFile, IEnumerator`1 codeAddressIndexCursor, Boolean enumerateAll, Int32& totalAddressCount)
   at Microsoft.Diagnostics.Tracing.Etlx.TraceCodeAddresses.LookupSymbolsForModule(SymbolReader reader, TraceModuleFile file)
```

To fix this issue, you'll need to manually copy the contents of the native dlls from the build folder in your nuget packages to that of the lib folder. This step might entail you creating a new folder with the same name in the exception e.g. from the error message above, the folder to be created is \lib\native\amd64\ where you will copy the contents of the corresponding ``build\native\<architecture>``.
