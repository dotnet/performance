from datetime import datetime
from glob import glob
from os import path
from pathlib import Path

import json

from benchview.console import write
from benchview.utils.common import is_null_or_whitespace
from benchview.utils.common import is_number

# External modules: Attempt to exit gracefully if module if not present.
try:
    from jsonschema import Draft4Validator
    from jsonschema import FormatChecker
    from jsonschema import RefResolver
except ImportError as ex:
    write.error(str(ex))
    exit(1)


def write_object_as_json(fileName: str, obj: object, schemaFileName: str):
    class JsonFormatSerializer(json.JSONEncoder):

        def default(self, obj):
            if hasattr(obj, '__dict__'):
                return obj.__dict__
            return json.JSONEncoder.default(self, obj)

    def __to_json_string(obj: object) -> str:
        return json.dumps(obj, cls=JsonFormatSerializer, sort_keys=True)

    if obj is None:
        raise ValueError('Attempting to write None as serialized json.')
    with open(fileName, mode='w') as jsonfile:
        jsonfile.write(__to_json_string(obj))

    def __load(fileName: str) -> object:
        with open(fileName) as jsonfile:
            return json.load(jsonfile)

    def __get_schemas_local_cache_store(dirName: str, baseSchemaFileName: str) -> dict:
        store = {}
        schemaRealPath = path.realpath(baseSchemaFileName)
        pathName = path.join(dirName, '*.json')
        for schemaJsonFile in glob(pathName):
            if schemaJsonFile == schemaRealPath:
                continue # Skipping the base schema file which was already loaded
            uri = r'https://benchview/schemas/{}'.format(path.basename(schemaJsonFile))
            store[uri] = __load(schemaJsonFile)
        return store

    def __get_resolver(schemaFileName: str, schema) -> 'RefResolver':
        dirName = path.dirname(path.realpath(schemaFileName))
        baseUri = Path(dirName).as_uri() + '/'
        store = __get_schemas_local_cache_store(dirName, schemaFileName)
        return RefResolver(baseUri, schema, store=store)

    def __validate(fileName: str, schemaFileName: str):
        instance = __load(fileName)
        schema = __load(schemaFileName)
        refResolver = __get_resolver(schemaFileName, schema)
        validator = Draft4Validator(schema, resolver=refResolver, format_checker=FormatChecker())
        validator.validate(instance)

    __validate(fileName, schemaFileName)
    write.info('Serialized object written to: "{}"'.format(fileName))


def get_timestamp_format() -> str:
    return '%Y-%m-%dT%H:%M:%SZ'


def is_valid_name(name: str) -> bool:
    return isinstance(name, str) and not is_null_or_whitespace(name)


def is_valid_description(description: str) -> bool:
    return description is None or (isinstance(description, str) and not is_null_or_whitespace(description))


def is_valid_datetime(dt: str) -> bool:
    try:
        datetime.strptime(dt, get_timestamp_format())
        return True
    except ValueError:
        return False
