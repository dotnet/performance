#!/usr/bin/env python3

from argparse import ArgumentParser, ArgumentTypeError
from os import path
from sys import exit
from traceback import format_exc

from benchview.console import write
from benchview.format.JSONFormat import Metric
from benchview.format.JSONFormat import Result
from benchview.format.JSONFormat import Test
from benchview.format.JSONReaders import read_tests_from_json
from benchview.format.helpers import is_valid_name
from benchview.format.helpers import write_object_as_json
from benchview.utils.common import is_number
from benchview.utils.common import get_script_directory
from benchview.utils.common import is_supported_version
from benchview.utils.common import to_number

# each of these must have a read_test_data(...) function:
import benchview.tools.measurement_bdn as bdn_impl
import benchview.tools.measurement_csv as csv_impl
import benchview.tools.measurement_rps as rps_impl
import benchview.tools.measurement_tao as tao_impl
import benchview.tools.measurement_xamarin_benchmarker as benchmarker_impl
import benchview.tools.measurement_xunit as xunit_impl
import benchview.tools.measurement_xunitscenario as xunitscenario_impl

MAX_NUMBER_VALUES = 128


def get_argument_parser() -> dict:
    top_parser = ArgumentParser(
        description='Generates a JSON serialized BenchView Test object.',
        allow_abbrev=False
    )

    subparsers = top_parser.add_subparsers(
        title='input formats',
        description='Specifies the input data format. For details, see `<format> -h` (where <format> is any of the valid formats).',
        help='valid formats',
        dest='input format'
    )
    subparsers.required = True

    bdn_parser = subparsers.add_parser('bdn')
    bdn_parser.set_defaults(impl=bdn_impl)
    csv_parser = subparsers.add_parser('csv')
    csv_parser.set_defaults(impl=csv_impl)
    rps_parser = subparsers.add_parser('rps')
    rps_parser.set_defaults(impl=rps_impl)
    tao_parser = subparsers.add_parser('tao')
    tao_parser.set_defaults(impl=tao_impl)
    xamarin_benchmarker_parser = subparsers.add_parser('xamarin_benchmarker')
    xamarin_benchmarker_parser.set_defaults(impl=benchmarker_impl)
    xunit_parser = subparsers.add_parser('xunit')
    xunit_parser.set_defaults(impl=xunit_impl)
    xunitscenario_parser = subparsers.add_parser('xunitscenario')
    xunitscenario_parser.set_defaults(impl=xunitscenario_impl)

    all_parsers = [
        bdn_parser,
        csv_parser,
        rps_parser,
        tao_parser,
        xamarin_benchmarker_parser,
        xunit_parser,
        xunitscenario_parser
    ]

    [parser.add_argument(
        'infiles',
        metavar='<input data file>',
        help='One or more files contaning the test data.',
        nargs='+'
    ) for parser in all_parsers]

    csv_parser.add_argument(
        '-m',
        '--metric',
        help='Defines (or overrides) the metric being measured in the specified data file (e.g. Code size, Execution time, time).',
        required=True
    )

    csv_parser.add_argument(
        '-u',
        '--unit',
        help='Defines (or overrides) the unit of the measurements in the specified data file (e.g. bytes, seconds, hours).',
        required=True
    )

    [parser.add_argument(
        '--better',
        help='Defines (or overrides) whether it is better for the value of a measurement to ascend or descend.',
        required=True,
        choices=['asc', 'desc']
    ) for parser in [csv_parser, xunitscenario_parser, xunit_parser, rps_parser, tao_parser]]

    def valid_xunitscenario_separator(value: str):
        if len(value) != 1:
            raise ArgumentTypeError('Invalid separator specified {}'.format(value))
        return value

    [parser.add_argument(
        '--namespace-separator',
        help='Defines the character separator used to split namespaces.',
        required=False,
        type=valid_xunitscenario_separator,
        default=None
    ) for parser in [xunitscenario_parser]]

    [parser.add_argument(
        '-o',
        '--outfile',
        metavar='<Output json file name>',
        help='The file path to write to (if not specified it defaults to tests.json).',
        required=False,
        default='measurement.json'
    ) for parser in all_parsers]

    [parser.add_argument(
        '--append',
        help='Flag indicating whether the new data is to be appended to the output file.',
        action='store_true',
        required=False,
        default=False
    ) for parser in all_parsers]

    csv_parser.add_argument(
        '--has-header',
        help='Flag indicating if csv data files have a header (this will ignore the first line of each csv file).',
        action='store_true',
        required=False,
        default=False
    )

    [parser.add_argument(
        '--drop-first-value',
        help='Discards the first value for each test (useful if the warm-up run is included in the data).',
        action='store_true',
        required=False,
        default=False
    ) for parser in all_parsers if parser is not bdn_parser]  # BenchmarkDotNet already removes the warmup iteration.

    [parser.add_argument(
        '-c',
        '--counter',
        help='Name of a counter to extract from the xml file.',
        required=False,
        action='append'
    ) for parser in [rps_parser, tao_parser]]

    return vars(top_parser.parse_args())


def make_test_mapping(test_node_mapping: dict, test_names: list, tests: list):
    for test in tests:
        if not is_valid_name(test.name):
            raise TypeError('Test name cannot be null, empty, or white space.')
        test_names.append(test.name)
        make_test_mapping(test_node_mapping, test_names, test.tests)
        key = make_key(test_names)
        test_node_mapping[key] = test
        test_names.pop()


def get_test(test_names: list, tests: list) -> 'Test':
    node = None
    test_list = tests

    for test_name in test_names:
        test = None
        for curr_test in test_list:
            if curr_test.name == test_name:
                test = curr_test
                test_list = test.tests
                break
        if test is None:
            test = Test(test_name)
            test_list.append(test)
            test_list = test.tests
        node = test
    return node


class MetricCreator(object):
    def __init__(self):
        self._cache = {}

    def __call__(self, metric_info: dict) -> 'Metric':
        better = metric_info['better']
        metric = metric_info['metric']
        unit = metric_info['unit']
        key = make_key([metric, unit, better])
        if key not in self._cache:
            # Convert the 'better' flag to the boolean use in the database.
            greater_the_better = better.casefold() == 'asc'.casefold()
            self._cache[key] = Metric(
                metric,
                unit,
                greater_the_better)
        return self._cache[key]


get_metric = MetricCreator()


def validate_test_data(test_names: list, value: str, metric_info: list):
    for test_name in test_names:
        if not is_valid_name(test_name):
            raise TypeError(
                'Test name cannot be null, empty, or white space: "tests: {0}, value: {1}, metric_info: {2}".'.format(
                    test_names, value, metric_info))
    if not is_number(value):
        raise TypeError(
            'Test value is not a number: "tests: {0}, value: {1}, metric_info: {2}".'.format(
                test_names, value, metric_info))
    for prop in ['metric', 'unit', 'better']:
        if prop not in metric_info:
            raise TypeError(
                'Test metric must contain a "{0}" field'.format(prop))
    if metric_info['better'] not in ['asc', 'desc']:
        raise TypeError(
            'Metric "better" must be either "asc" or "desc": "{0}"'.format(
                metric_info['better']))


def make_key(name_list: list) -> tuple:
    return tuple(name_list)


def insert_test_values(test: 'Test', value: float,
                       metric: 'Metric', drop_first_value: bool):
    if not is_number(value):
        raise TypeError('Test value must be a number (float).')
    result = None
    for r in test.results:
        if r.metric == metric:
            result = r
            break
    if result is None:
        result = Result(metric)
        test.results.append(result)
        if drop_first_value:
            return
    result.values.append(value)
    if len(result.values) > MAX_NUMBER_VALUES:
        raise OverflowError(
            'Number of values for {0} exceeded {1}'.format(
                test.name, MAX_NUMBER_VALUES))


def merge_tests(tests: list, dataFileName: str, args: dict):
    test_node_mapping = {}
    make_test_mapping(test_node_mapping, [], tests)
    # Consume test data using read_test_data(), which is imported from the module that implements parsing
    # for the currently selected input format. The module we want is stored as args['impl']. This gets its
    # value from two places--at first where we import modules:
    #
    #     # each of these must have a read_test_data(...) function:
    #     import benchview.tools.measurement_csv as csv_impl
    #     import benchview.tools.measurement_xunit as xunit_impl
    #
    # and then later in get_argument_parser():
    #
    #     csv_parser.set_defaults(impl = csv_impl)
    #     xunit_parser.set_defaults(impl = xunit_impl)
    #
    testDataGenerator = args['impl'].read_test_data(dataFileName, **args)
    for test_names, value, metric_info in testDataGenerator:
        validate_test_data(test_names, value, metric_info)
        metric = get_metric(metric_info)
        value = to_number(value)
        key = make_key(test_names)
        test = None

        if key in test_node_mapping:
            test = test_node_mapping[key]
        else:
            test = get_test(test_names, tests)
            test_node_mapping[key] = test

        drop_value = 'drop_first_value' in args and args['drop_first_value']
        insert_test_values(test, value, metric, drop_value)


def main() -> int:
    try:
        if not is_supported_version():
            write.error("You need to use Python 3.5 or newer.")
            return 1

        args = get_argument_parser()

        # Read from output file if the data is to be appended.
        measurementJson = args['outfile']
        tests = []
        if args['append'] and path.isfile(measurementJson):
            tests = read_tests_from_json(measurementJson)

        for infile in args['infiles']:
            merge_tests(tests, infile, args)

        if not len(tests) > 0:
            raise ValueError(r'There are no tests in the input files.')

        scriptDirectory = get_script_directory(__file__)
        schemaFileName = path.join(
            scriptDirectory,
            '..',
            'schemas',
            'measurement.json')
        write_object_as_json(measurementJson, tests, schemaFileName)
    except Exception as ex:
        write.error('Unexpected exception caught: {0}'.format(str(ex)))
        write.error(format_exc())
        return 1

    return 0


if __name__ == "__main__":
    exit(main())
