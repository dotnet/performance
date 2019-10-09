# Syntax for running commands

Commands all have the syntax `py . command-name [args...]`.
For a list of all commands, run `py . help`.

All commands can be called like `py . diff --help` (or `py . help diff`) to see all arguments.

Each command can have up to one “name optional” argument, which should come immediately after the command name. (Other than that, the ordering of arguments doesn't matter.) This argument actually has a name but it is optional to provide it. All other arguments should be prefixed by their name which should have two dashes.

If an argument is a list (or tuple), the values should be separated by spaces. E.g., `py . diff a.etl b.etl --run-metrics HeapSizeBeforeMB_Mean HeapSizeAfterMB_Mean` . Here `a.etl` and `b.etl` are the values for the name-optional argument (`paths`), and  `HeapSizeBeforeMB_Mean` and `HeapSizeAfterMB_Mean` are the values for the argument run-metrics .



## Boolean arguments

A boolean argument can be specified like `--arg true` or `--arg false`.

For convenience, `--arg` is shorthand for `--arg true` and all boolean arguments are optional and default to false. So if the argument exists it is true, else it is false.


## Argsfiles

For convenience, you could store some arguments in a file.
For an equivalent of the command:

```sh
py . diff bench/suite/low_memory_container.yaml --vary coreclr
```

You could create a file `bench/diff_low_memory_container.yaml` (file can be anywhere) containing:

```yaml
paths:
- bench/suite/low_memory_container.yaml
vary: coreclr
```

(use `py . diff --help` to see that the nameless argument is named "paths")

And then run:

```sh
py . diff --argsfile bench/diff_low_memory_container.yaml
```


## Where arguments

Many commands have arguments ending in `where` which take the following syntax:

    py . analyze-single foo.etl --gc-where Generation=2 PauseDurationMSec>100

In this case the argument filters it so we only print GCs that are gen 2 and took over 100ms.
The value of `where` consists of a number of space-separated filters.
Each filter is of the form “name operator value”, e.g.,
Generation is the name, = is the operator, 2 is the value.

The operators are the usual: =, !=, <, >, <=, >=.
The value may be a number or (unquoted) string.

You can also put any number of `or` in between the clauses, as in:

    py . analyze-single foo.etl --gc-where Generation=2 PauseDurationMSec>100 or Generation=0 PauseDurationMSec<10

Which would print GCs that are either long gen2 or short gen0.
Any more complicated filters should be written manually in the code.
