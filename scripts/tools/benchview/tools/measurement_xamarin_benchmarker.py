from benchview.format.helpers import is_valid_name
from os import path

import json


def read_test_data(fileName: str, **kwargs: dict) -> tuple:
    class RunSet(object):
        pass

    def __as_runset(dct: dict) -> object:
        obj = object.__new__(RunSet)
        obj.__dict__.update(dct)
        return obj

    def __load_xamarin_runset(fileName: str) -> object:
        with open(fileName) as jsonfile:
            jsonRunSet = json.load(jsonfile)
            return json.loads(json.JSONEncoder().encode(
                jsonRunSet), object_hook=__as_runset)

    def __get_xamarin_metric_info(metricName: str) -> tuple:
        if metricName == 'aot-time':
            return 'AOT Elapsed Time', 'ms', 'desc'
        if metricName.startswith('jit-'):
            phaseName = metricName[len('jit-'):]
            return 'JIT {}'.format(phaseName), 'ms', 'desc'
        if metricName == 'time':
            return 'Elapsed Time', 'ms', 'desc'

        if metricName == 'branch-mispred':
            return 'Branch mispredictions', 'rate', 'desc'
        if metricName == 'cache-miss':
            return 'Cache miss', 'rate', 'desc'
        if metricName == 'memory-integral':
            return 'Memory usage', 'MB * Giga Instructions', 'desc'

        if metricName == 'code-size':
            return 'Code size', 'bytes', 'desc'
        if metricName == 'instructions':
            return 'Instruction', 'count', 'desc'

        # FIXME: Ignoring the 3 metrics below while we decide how to handle
        #   them in db and site.
        if metricName == 'cachegrind':
            return None, None, None  # '(double[])Value'
        if metricName == 'pause-starts':
            return None, None, None  # '(double[])Value'
        if metricName == 'pause-times':
            return None, None, None  # '(double[])Value'

        raise ValueError('Unknown metric type.')

    if not path.isfile(fileName):
        raise ValueError(
            'Input xamarin benchmarker JSON file "{0}" does not exist.'.format(
                fileName))
    else:
        runSet = __load_xamarin_runset(fileName)
        for run in runSet.Runs:
            for metric in run.RunMetrics:
                metricName, unit, better = __get_xamarin_metric_info(
                    metric.MetricName)
                if metricName is None:
                    continue
                metric_info = {
                    'metric': metricName,
                    'unit': unit,
                    'better': better}
                commandLine = ' '.join(run.Benchmark.CommandLine)
                testName = run.Benchmark.TestDirectory.split('/')
                testName.append(commandLine)
                yield (testName, metric.ApiValue, metric_info)
