"""
Implements a parser for the BenchmarkDotNet output file.
"""
from json import JSONEncoder, load, loads
from os import path


# Predefined constant metrics output by BenchmarkDotNet.
DURATION = {
    'metric': 'Duration',
    'unit': 'msec',
    'better': 'desc'
}
DURATION_OF_SINGLE_INVOCATION = {
    'metric': 'Duration of single invocation',
    'unit': 'ns',
    'better': 'desc'
}


def read_test_data(fileName: str, **other_args: dict) -> tuple:
    """Reads test data output by BenchmarkDotNet."""
    class BenchmarkDotNet(object):
        """Helper class used to read BenchmarkDotNet json output file."""
        pass

    def __as_bdn(dct: dict) -> object:
        obj = object.__new__(BenchmarkDotNet)
        obj.__dict__.update(dct)
        return obj

    def __load_bdn_output_file(file_name: str) -> 'BenchmarkDotNet':
        if not file_name or not path.isfile(file_name):
            raise ValueError('"{0}" does not exist.'.format(file_name))
        with open(file_name, mode='r', encoding='utf8') as json_file:
            dct = load(json_file)
            return loads(JSONEncoder().encode(dct), object_hook=__as_bdn)

    benchmark_dotnet = __load_bdn_output_file(fileName)

    for b in benchmark_dotnet.Benchmarks:
        # We only care about the "Workload-Result"
        workload_result = [m for m in b.Measurements if m.IterationMode ==
                           'Workload' and m.IterationStage == 'Result']

        # Make sure that we store the iterations in the other they occurred.
        workload_result = sorted(workload_result, key=lambda x: (
            x.LaunchIndex, x.IterationIndex))

        # Create a test name equals to xunit.
        # Benchmark types might not be in a namespace.
        if b.Namespace:
            method_fullname = b.FullName.replace(
                '{}.{}.'.format(b.Namespace, b.Type), '', 1)
            # Namespaces are splitted on '.' for a detailed relationship
            test_names = b.Namespace.split('.') + [b.Type, method_fullname]
        else:
            method_fullname = b.FullName.replace(
                '{}.'.format(b.Type), '', 1)
            test_names = [b.Type, method_fullname]

        for m in workload_result:
            milliseconds = m.Nanoseconds / 1000000
            yield ((test_names, milliseconds, DURATION))

            value = m.Nanoseconds / m.Operations
            yield ((test_names, value, DURATION_OF_SINGLE_INVOCATION))

        # Get the captured metrics.
        for bdnmetric in b.Metrics:

            known_metrics = [
                "BranchMispredictions",
                "CacheMisses",
                "InstructionRetired",
            ]
            if bdnmetric.Descriptor.Id in known_metrics:
                bdnmetric.Descriptor.Unit = 'Count'

            # Not all Unit property fields are initialized.
            if bdnmetric.Descriptor.Unit:
                better = 'desc' \
                    if not bdnmetric.Descriptor.TheGreaterTheBetter \
                    else 'asc'
                metric = {
                    'metric': bdnmetric.Descriptor.Legend,
                    'unit': bdnmetric.Descriptor.Unit,
                    'better': better
                }
                value = bdnmetric.Value
                yield ((test_names, value, metric))
