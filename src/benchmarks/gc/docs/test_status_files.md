# Test Status Files

Every trace that wants to be analyzed by GC Benchmarking Infra must have an
associated _test status file_, which contains information about the process
over which we want to perform the analysis. Traces gotten through the Infra
and _GCPerfSim_ have these generated automatically so you don't need to worry
about those. For traces you captured elsewhere, you need to write them by hand.

## Fields

These are the fields a _test status file_ can have.

* `success`&#42;: Whether the test finished without errors.
* `trace_file_name`&#42;: Relative path to the trace file. It is highly recommended
that both files have the same name.
* `process_args`&#42;&#42;: The command-line arguments the process to analyze
received when it was run. It can be a regular expression. This is case-insensitive.
* `process_id`&#42;&#42;: The ID of the process to analyze. It can be retrieved
using `py . print-processes <trace_file_name>` if you don't know it.
* `process_name`&#42;&#42;: The name of the process to analyze. It can be a
regular expression. This is case-insensitive.
* `seconds_taken`: The amount of time it took the test to complete in seconds.
* `test`&#42;&#42;&#42;: Detailed information about a specific test.
* `stdout`&#42;&#42;&#42;: Whatever information was printed to the console while
the test was running.
* `gcperfsim_result`&#42;&#42;&#42;: Additional GC information about the test results.

&#42; This field is mandatory in a test status file.

&#42;&#42; Since _process\_id_ is the most specific parameter when searching for a
process, then _process\_name_ and _process\_args_ are redundant and therefore
should not be included. On the other hand, _process\_name_ and _process\_args_
are not mutually exclusive so you can have one or both in your status file.

&#42;&#42;&#42; These fields are exclusive to tests run with _GCPerfSim_.

It is worth to mention that if you specify wildcards in _process\_name_ and/or
_process\_args_ and more than one process matches, GC Infra will fail and then
will show you a list of all the matching processes so you can modify the test
status file and specify the one you intend to analyze.

## Examples for External Traces

Here are a few simple examples you can follow when writing your own test status
files for traces you captured elsewhere.

Let's assume the trace file name is `mytrace.etl` and the test status file is
called `mytrace.yaml`, and both are in the same directory. They could have
different names, but you've got to be careful in such scenario.

### With Process ID

In this example, we know the process ID or got it with `print-processes`.

```yml
success: true
trace_file_name: mytrace.etl
process_id: 7985
```

### With Process Name

In this example, we know the name of the process to analyze (most usual case
outside of _GCPerfSim_ traces).

```yml
success: true
trace_file_name: mytrace.etl
process_name: corerun
```

### With Process Args

In this example, we only know the command-line arguments the test process
received (this is highly unlikely and _process\_args_ is usually combined
with _process\_name_, as shown in the next examples).

```yml
success: true
trace_file_name: mytrace.etl
process_args: /path/to/dll
```

### With Wildcards

As mentioned earlier, _process\_name_ and _process\_args_ allow you to write
a regular expression instead to make it faster to write these test status files.
Here are some examples:

**We know the process name starts with _co_**

```yml
success: true
trace_file_name: mytrace.etl
process_name: co.*
```

**There might be more than one process starting with _co_, but only the one
we're interested in calls a dll named _collect1.dll_**

```yml
success: true
trace_file_name: mytrace.etl
process_name: co.*
process_args: .*collect1.dll
```
