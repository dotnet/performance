# Benchmarks

[Commit snapshot](https://github.com/PowerShell/PowerShell/commit/ec0eb220626c5d30e8feeb1e9519d028fe67e906)

This project contains a variety of performance tests for different pieces of the library.

At the moment, there are not many benchmarks, so it's recommended to just run everything.

Benchmarks:

[Parsing](Engine.Parser.cs): Currently benchmarks using statements.

[Scripting](Engine.ScriptBlock.cs): Benchmarks a few calls, including COM methods when run on Windows.
