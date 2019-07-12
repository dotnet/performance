from benchview.format.JSONFormat import Build
from benchview.format.JSONFormat import Machine
from benchview.format.JSONFormat import MachineData
from benchview.format.JSONFormat import OperatingSystem
from benchview.format.JSONFormat import Submission
from benchview.format.JSONFormat import Test

from os import path

import json

def as_build(dct: dict):
    obj = object.__new__(Build)
    obj.__dict__.update(dct)
    return obj

def as_machinedata(dct: dict):
    condition = 'name' in dct and 'architecture' in dct and 'manufacturer' in dct and 'cores' in dct and 'threads' in dct and 'physicalMemory' in dct
    if condition:
        machine = Machine(
            dct['name'],
            dct['architecture'],
            dct['manufacturer'],
            dct['cores'],
            dct['threads'],
            dct['physicalMemory'])
        return machine

    condition = 'name' in dct and 'version' in dct and 'edition' in dct and 'architecture' in dct
    if condition:
        os = OperatingSystem(
            dct['name'],
            dct['version'],
            dct['edition'],
            dct['architecture'])
        return os

    condition = 'machine' in dct and 'os' in dct
    if condition:
        return MachineData(dct['machine'], dct['os'])

    raise TypeError('Unrecognized object')

def as_submission(dct: dict):
    obj = object.__new__(Submission)
    obj.__dict__.update(dct)
    return obj

def as_test(dct: dict):
    obj = object.__new__(Test)
    obj.__dict__.update(dct)
    return obj

def read_build(filename: str) -> 'Build':
    if not path.isfile(filename):
        raise ValueError('Specified build json file "{0}" does not exist.'.format(filename))
    with open(filename) as jsonfile:
        jsonbuild = json.load(jsonfile)
        return json.loads(json.JSONEncoder().encode(jsonbuild), object_hook = as_build)

def read_machinedata(filename: str) -> 'MachineData':
    if not path.isfile(filename):
        raise ValueError('Specified machine-data json file "{0}" does not exist.'.format(filename))
    with open(filename) as jsonfile:
        jsonmachinedata = json.load(jsonfile)
        return json.loads(json.JSONEncoder().encode(jsonmachinedata), object_hook = as_machinedata)

def read_submission_metadata(filename: str) -> 'Submission':
    if not path.isfile(filename):
        raise ValueError('Specified submission-metadata json file "{0}" does not exist.'.format(filename))
    with open(filename) as jsonfile:
        jsonsubmission = json.load(jsonfile)
        return json.loads(json.JSONEncoder().encode(jsonsubmission), object_hook = as_submission)

def read_tests_from_json(filename: str) -> list:
    if not path.isfile(filename):
        raise ValueError('Specified measurement json file "{0}" does not exist.'.format(filename))
    else:
        with open(filename) as jsonfile:
            jsontestlist = json.load(jsonfile)
            return [json.loads(json.JSONEncoder().encode(jsontest), object_hook = as_test) for jsontest in jsontestlist]
