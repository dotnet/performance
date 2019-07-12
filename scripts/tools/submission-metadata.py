#!/usr/bin/env python3

from argparse import ArgumentParser
from datetime import datetime
from datetime import timezone
from os import path
from sys import exit
from traceback import format_exc

from benchview.console import write
from benchview.format.helpers import get_timestamp_format
from benchview.format.helpers import write_object_as_json
from benchview.format.JSONFormat import Submission
from benchview.utils.common import get_script_directory
from benchview.utils.common import is_supported_version

# This is an external module, so we attempt to exit gracefully if module if not present.
try:
    from cuid import cuid
except ImportError as ex:
    write.error(str(ex))
    exit(1)

def get_argument_parser() -> dict:
    parser = ArgumentParser(
        description='Generates a JSON serialized BenchView Run object.',
        allow_abbrev=False
    )

    parser.add_argument(
        '--name',
        help = 'Submission name.',
        required = True
    )

    parser.add_argument(
        '--user-email',
        help = 'User email.',
        required = False,
        default = 'dotnet-bot@microsoft.com'
    )

    parser.add_argument(
        '--description',
        help = 'Submission description.',
        required = False,
        default = None
    )

    parser.add_argument(
        '--timestamp',
        help = 'Submission timestamp (date-time from RFC 3339, Section 5.6. "%%Y-%%m-%%dT%%H:%%M:%%SZ").',
        required = False
    )

    parser.add_argument(
        '--cuid',
        help = 'Collision-resistant id assigned to this submission (if not specified, it will be generated).',
        required = False
    )

    parser.add_argument(
        '-o',
        '--outfile',
        metavar = '<Output json file name>',
        help = 'The file path to write to (If not specfied, defaults to "submission-metadata.json").',
        required = False,
        default = 'submission-metadata.json'
    )

    return vars(parser.parse_args())

def main() -> int:
    try:
        if not is_supported_version():
            write.error("You need to use Python 3.5 or newer.")
            return 1

        args = get_argument_parser()

        name = args['name']
        description = args['description']
        submission_cuid = args['cuid'] if 'cuid' in args and not args['cuid'] is None else cuid()
        submission_timestamp = args['timestamp'] if 'timestamp' in args and not args['timestamp'] is None else datetime.now(timezone.utc).strftime(get_timestamp_format())
        if args['user_email'] != 'dotnet-bot@microsoft.com':
            write.warning("User email must be dotnet-bot@microsoft.com, changing the value")
        user_email = 'dotnet-bot@microsoft.com'

        submission = Submission(name, description, user_email, submission_cuid, submission_timestamp)

        scriptDirectory = get_script_directory(__file__)
        schemaFileName = path.join(scriptDirectory, '..', 'schemas', 'submission-metadata.json')
        write_object_as_json(args['outfile'], submission, schemaFileName)
    except Exception as ex:
        write.error('Unexpected exception caught: {0}'.format(str(ex)))
        write.error(format_exc())
        return 1

    return 0

if __name__ == "__main__":
    exit(main())
