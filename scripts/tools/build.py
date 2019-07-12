#!/usr/bin/env python3

from os import path
from traceback import format_exc

from benchview.console import write
from benchview.format.helpers import write_object_as_json
from benchview.format.JSONFormat import Build
from benchview.format.JSONFormat import get_valid_submission_types
from benchview.utils.common import get_script_directory
from benchview.utils.common import is_supported_version
from benchview.utils.argparse import set_default_subparser
from benchview.tools.build_git import git_get_branch, git_get_repository, git_get_sha1, git_get_timestamp, git_impl


def get_argument_parser() -> dict:
    import argparse
    argparse.ArgumentParser.set_default_subparser = set_default_subparser

    top_parser = argparse.ArgumentParser(
        description='Generates a JSON serialized BenchView Build object.',
        allow_abbrev=False
    )

    subparsers = top_parser.add_subparsers(
        title = 'environments',
        description = 'Specifies the environment to gather build information from. For details, see `<format> -h` (where <format> is any of the valid formats).',
        help = 'valid environments',
        dest = 'environment'
    )
    subparsers.required = True

    none_parser = subparsers.add_parser('none')
    git_parser = subparsers.add_parser('git')
    git_parser.set_defaults(impl = git_impl)
    all_parsers = [none_parser, git_parser]

    top_parser.set_default_subparser('none')

    git_parser.add_argument(
        '--branch',
        help = 'Product branch.',
        required = False,
        default = git_get_branch
    )

    none_parser.add_argument(
        '--branch',
        help = 'Product branch.',
        required = True
    )

    git_parser.add_argument(
        '--number',
        help = 'Product build number.',
        required = False,
        default = git_get_sha1
    )

    none_parser.add_argument(
        '--number',
        help = 'Product build number.',
        required = True
    )

    git_parser.add_argument(
        '--source-timestamp',
        help = 'Timestamp of the soruces used to generate this build (date-time from RFC 3339, Section 5.6. "%%Y-%%m-%%dT%%H:%%M:%%SZ").',
        required = False,
        default = git_get_timestamp
    )

    none_parser.add_argument(
        '--source-timestamp',
        help = 'Timestamp of the soruces used to generate this build (date-time from RFC 3339, Section 5.6. "%%Y-%%m-%%dT%%H:%%M:%%SZ").',
        required = True
    )

    git_parser.add_argument(
        '--repository',
        help = 'Repository URL.',
        required = False,
        default = git_get_repository
    )

    none_parser.add_argument(
        '--repository',
        help = 'Repository URL.',
        required = True
    )

    [ parser.add_argument(
        '--type',
        help = 'Build type.',
        required = True,
        choices = get_valid_submission_types(),
        type = str.casefold
    ) for parser in all_parsers ]

    [ parser.add_argument(
        '-o',
        '--outfile',
        metavar = '<Output json file name>',
        help = 'The file path to write to (If not specfied, defaults to "build.json").',
        required = False,
        default = 'build.json'
    ) for parser in all_parsers ]

    return vars(top_parser.parse_args())


def main() -> int:
    try:
        if not is_supported_version():
            write.error("You need to use Python 3.5 or newer.")
            return 1

        args = get_argument_parser()
        impl = args.get("impl")
        if impl is not None:
            impl(args)
        build = Build(args['repository'], args['branch'], args['number'], args['source_timestamp'], args['type'])

        scriptDirectory = get_script_directory(__file__)
        schemaFileName = path.join(scriptDirectory, '..', 'schemas', 'build.json')
        write_object_as_json(args['outfile'], build, schemaFileName)
    except Exception as ex:
        write.error('{0}: {1}'.format(type(ex), str(ex)))
        write.error(format_exc())
        return 1
    return 0


if __name__ == "__main__":
    exit(main())
