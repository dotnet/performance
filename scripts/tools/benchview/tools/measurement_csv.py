from os import path

import csv

def read_test_data(fileName: str, has_header: bool, metric: str, unit: str, better: str, **other_args: dict) -> tuple:
    if not path.isfile(fileName):
        raise ValueError('Input csv file "{0}" does not exist.'.format(fileName))
    else:
        nColumns = None
        skip = has_header
        metric_info = { 'metric': metric, 'unit': unit, 'better': better }
        with open(fileName) as csvfile:
            reader = csv.reader(csvfile)
            for row in reader:
                if skip:
                    skip = False
                    continue
                nColumns = len(row)
                if nColumns == 0:
                    raise TypeError('Row does not have columns.')
                if nColumns == 1:
                    raise TypeError('Row does not have iteration result.')
                yield (row[:nColumns - 1],row[-1],metric_info)
