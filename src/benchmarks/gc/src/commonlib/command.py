# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.
# See the LICENSE file in the project root for more information.

from codecs import getwriter
from dataclasses import dataclass, Field, fields, is_dataclass, MISSING
from difflib import get_close_matches
from enum import Enum
from inspect import getfullargspec
from pathlib import Path
import sys
from sys import argv as sys_argv
from typing import Any, Callable, cast, Dict, Mapping, Optional, Sequence, TextIO, Type, Union

from result import Err, Ok, Result

from .collection_util import find_only_or_none, is_empty, split_and_keep_splitter, zip_check
from .document import Align, Cell, Document, Row, Section, Table, print_document
from .option import optional_to_iter
from .parse_and_serialize import load_yaml, try_for_each_union_member, try_get_enum_from_str
from .type_utils import (
    argument,
    check_cast,
    construct_class_from_fields,
    get_field_argument_info,
    is_a,
    is_field_name_optional,
    match_type,
    NO_DEFAULT,
    non_optional_type,
    show_type_for_command,
    T,
    try_get_field_argument_info,
    unindent_doc,
    with_slots,
)
from .util import (
    did_you_mean,
    get_or_did_you_mean,
    make_absolute_path,
    os_is_windows,
    try_remove_str_start,
)


def _name_from_cmd_line_arg_name(name: str) -> str:
    return name.replace("-", "_")


def _to_cmd_line_arg_name(name: str) -> str:
    return name.replace("_", "-")


class CommandKind(Enum):
    run = 0
    analysis = 1
    suite = 2
    trace = 3
    infra = 4
    misc = 5


CommandFunction = Union[Callable[[], None], Callable[[Any], None]]


@with_slots
@dataclass(frozen=True)
class Command:
    kind: CommandKind
    fn: CommandFunction
    # Basic documentation, printed with `py . help` when we show all commands.
    doc: str
    # Additional details, printed with `py . command-name --help`.
    detail: Optional[str] = None
    hidden: bool = False
    # Tests with higher priority are listed later.
    priority: int = 0


CommandsMapping = Mapping[str, Command]


def run_command(commands: CommandsMapping) -> None:
    _avoid_ubuntu_hijack()
    _fix_stdout_bug()
    _, *cmd_and_args = sys_argv
    run_command_worker(commands, cmd_and_args)


def _avoid_ubuntu_hijack() -> None:
    # https://askubuntu.com/questions/309423/how-do-i-disable-apport-for-my-user-account
    if sys.excepthook != sys.__excepthook__ and sys.excepthook.__name__ == "apport_excepthook":
        sys.excepthook = sys.__excepthook__


def _fix_stdout_bug() -> None:
    # On Windows when stdout pipes to a file, Python will crash when unicode characters are used.
    # This can be fixed using a hack mentioned here:
    # https://stackoverflow.com/questions/14630288/unicodeencodeerror-charmap-codec-cant-encode-character-maps-to-undefined
    if os_is_windows() and not sys.stdout.isatty() and sys.stdout.encoding != "cp850":
        sys.stdout = cast(TextIO, getwriter("cp850")(sys.stdout.buffer, "strict"))


def _pre_parse_command_args(
    command_name: str,
    arg_strs: Sequence[str],
    name_optional_field_name: Optional[str],
    allowed_names: Sequence[str],
) -> Mapping[str, Sequence[str]]:
    """Preliminary parsing, separate arguments by their field"""
    nameless_args, groups = split_and_keep_splitter(arg_strs, _try_get_argument_name)
    res: Dict[str, Sequence[str]] = {}
    if not is_empty(nameless_args):
        assert (
            name_optional_field_name is not None
        ), f"Command '{command_name}' can not take an argument with no name."
        res[name_optional_field_name] = nameless_args

    for arg_name, arg_values in groups:
        assert arg_name in allowed_names, did_you_mean(
            allowed_names, arg_name, "argument", show_choice=_to_cmd_line_arg_name
        )
        assert arg_name not in res.keys(), f"'{arg_name}' appears twice"
        res[arg_name] = arg_values

    return res


def _try_get_argument_name(arg: str) -> Optional[str]:
    without_hyphens = try_remove_str_start(arg, "--")
    if without_hyphens is None:
        assert not arg.startswith("-"), f"{arg} starts with a single hyphen, need two"
        assert not arg.startswith("–"), (
            f"The character '–' looks like a hyphen but isn't.\n"
            + "A word processor may have collapsed two hyphens into this."
        )
        return None
    else:
        return _name_from_cmd_line_arg_name(without_hyphens)


@with_slots
@dataclass(frozen=True)
class HelpArgs:
    command_name: Optional[str] = argument(
        default=None, name_optional=True, doc="If set, only display help for this command."
    )
    hidden: bool = argument(default=False, doc="If true, display hidden commands / arguments too.")


def help_command(args: HelpArgs) -> None:
    if args.command_name is None:
        print_document(_document_for_help_all_commands(args.hidden))
    else:
        from ..all_commands import ALL_COMMANDS  # pylint:disable=import-outside-toplevel

        command = get_or_did_you_mean(ALL_COMMANDS, args.command_name, "command")
        _print_help_for_command(command, show_hidden_arguments=args.hidden)


HELP_COMMANDS: CommandsMapping = {
    "help": Command(kind=CommandKind.infra, fn=help_command, doc="Displays help for all commands.")
}


def _document_for_help_all_commands(show_hidden: bool) -> Document:
    from ..all_commands import ALL_COMMANDS  # pylint:disable=import-outside-toplevel

    def row_for_command(command_name: str, command: Command) -> Row:
        return (Cell(command_name), Cell(unindent_doc(command.doc), align=Align.left))

    def table_for_command_kind(kind: CommandKind) -> Optional[Table]:
        rows = rows = [
            row_for_command(command_name, command)
            for command_name, command in sorted(
                ALL_COMMANDS.items(), key=lambda pair: pair[1].priority
            )
            if command.kind == kind and not command.hidden or show_hidden
        ]
        if is_empty(rows):
            return None
        else:
            return Table(
                name=f"{kind.name} commands", text=unindent_doc(TEXT_FOR_KIND[kind]), rows=rows
            )

    tables = [
        table for kind in CommandKind for table in optional_to_iter(table_for_command_kind(kind))
    ]
    return Document(comment=_HELP_HEADER_COMMENT, sections=(Section(tables=tables),))


_HELP_HEADER_COMMENT = """
Read `README.md` first.
For help with an individual command, use `py . command-name --help`.
(You can also pass `--help --hidden` to see hidden arguments.)
""".strip()


TEXT_FOR_KIND: Mapping[CommandKind, str] = {
    CommandKind.run: """
    Commands for running tests.
    Usually you just need 'run' with no arguments.
    """,
    CommandKind.analysis: """
    Commands for analyzing test results (trace files).
    To compare a small number of configs, use 'diff'.
    To compare many, use 'chart-configs'.
    For detailed analysis of a single trace, use 'analyze-single' or 'chart-individual-gcs'.
    """,
    CommandKind.suite: """
    Commands for creating, running, and analyzing suites.
    A suite is a collection of benchfiles specified in a 'suite.yaml'.
    Suites can all be run and analyzed together all at once for convenience.
    This is useful if you want to test out many unrelated configs to search for regressions.
    """,
    CommandKind.trace: "Utilities for operating on trace files. Not normally needed.",
    CommandKind.infra: "Commands for managing the infra itself.",
    CommandKind.misc: "Miscellaneous",
}


def run_command_worker(commands: CommandsMapping, command_and_args: Sequence[str]) -> None:
    command_name, *arg_strs = command_and_args
    if command_name in commands:
        command = commands[command_name]
        if "--help" in arg_strs:
            _print_help_for_command(command, show_hidden_arguments="--hidden" in arg_strs)
        else:
            param_type = _get_command_parameter_type(command.fn)
            if param_type is None:
                assert is_empty(arg_strs), f"{command_name} takes no arguments"
                cast(Callable[[], None], command.fn)()
            else:
                if param_type == Sequence[str]:
                    cast(Callable[[Sequence[str]], None], command.fn)(arg_strs)
                else:
                    try:
                        args = parse_command_args(command_name, param_type, arg_strs)
                    except Exception:
                        _print_help_for_command(command, show_hidden_arguments=False)
                        raise
                    cast(Callable[[Any], None], command.fn)(args)
    else:
        near = get_close_matches(command_name, commands.keys())
        help_command(HelpArgs())
        start = f"No command is named '{command_name}'."
        if is_empty(near):
            raise Exception(start)
        elif len(near) == 1:
            raise Exception(f"{start} Did you mean '{near[0]}'?")
        else:
            raise Exception(f"{start} Did you mean one of: {near}?")


def check_command_parses(commands: CommandsMapping, cmd_and_argv: Sequence[str]) -> None:
    command_name, *arg_strs = cmd_and_argv
    command = get_or_did_you_mean(commands, command_name, name="command")
    param_type = _get_command_parameter_type(command.fn)
    if param_type is None:
        assert is_empty(arg_strs), f"{command_name} takes no arguments"
    elif param_type != Sequence[str]:
        try:
            parse_command_args(command_name, param_type, arg_strs)
        except Exception as e:
            _print_help_for_command(command, show_hidden_arguments=False)
            raise e


def _print_help_for_command(command: Command, show_hidden_arguments: bool) -> None:
    print_document(_document_for_help_for_command(command, show_hidden_arguments))


def _document_for_help_for_command(command: Command, show_hidden_arguments: bool) -> Document:

    param_type = _get_command_parameter_type(command.fn)

    if command.detail is None:
        doc = unindent_doc(command.doc)
    else:
        doc = unindent_doc(command.doc) + "\n\n" + unindent_doc(command.detail)

    table = (
        None
        if param_type is None
        else Table(
            headers=("arg name", "required?", "arg type", "description"),
            rows=[
                row
                for f in fields(param_type)
                for row in optional_to_iter(_command_help_row_for_field(f, show_hidden_arguments))
            ],
        )
    )
    return Document(sections=(Section(text=doc, tables=tuple(optional_to_iter(table))),))


# fld should be a Field[Any] but that fails at runtime
def _command_help_row_for_field(fld: Any, show_hidden_arguments: bool) -> Optional[Row]:
    info = get_field_argument_info(fld)
    if info.hidden and not show_hidden_arguments:
        return None
    else:
        name = f"--{_to_cmd_line_arg_name(fld.name)}"
        if info.name_optional:
            name += "\n(name optional)"
        return (
            Cell(name),
            Cell("Y" if fld.default is MISSING else None),
            # Not showing 'Optional[T]' because most args have defaults anyway
            Cell(show_type_for_command(non_optional_type(fld.type))),
            Cell(info.doc, align=Align.left),
        )


def validate_all_commands_are_documented() -> None:
    from ..all_commands import ALL_COMMANDS  # pylint:disable=import-outside-toplevel

    for command in ALL_COMMANDS.values():
        param_type = _get_command_parameter_type(command.fn)
        if param_type is not None and is_dataclass(param_type):
            for fld in fields(param_type):
                assert fld.default is NO_DEFAULT or is_a(
                    fld.default, fld.type
                ), f"{param_type.__name__}.{fld.name}: default value does not match type"
                if try_get_field_argument_info(fld) is None:
                    raise Exception(f"Field {param_type.__name__}#{fld.name} is not documented")


def _get_command_parameter_type(command: CommandFunction) -> Optional[Type[Any]]:
    spec = getfullargspec(command)
    args = spec.args
    if is_empty(args):
        return None
    else:
        assert len(args) == 1
        return cast(Type[Any], spec.annotations[args[0]])


def parse_command_args(command_name: str, cls: Type[T], argv: Sequence[str]) -> T:
    """
    Turns field names of a data class into command line argument names.
    Uses type annotations to determine what to parse.
    """
    assert is_dataclass(cls)

    # First arg may be 'file:foo', this is a yaml file containing args
    from_file: Optional[T]
    if not is_empty(argv) and argv[0] == "--argsfile":
        assert len(argv) >= 2, "--argsfile needs the file path"
        file = Path(argv[1])
        from_file = load_yaml(cls, file, all_optional=True)
        remaining_argv = argv[2:]
    else:
        from_file = None
        remaining_argv = argv

    flds = fields(cls)

    fields_from_file: Sequence[Field[Any]] = [] if from_file is None else [
        f for f in flds if getattr(from_file, f.name) is not None
    ]

    name_optional_field = find_only_or_none(
        is_field_name_optional, flds, lambda f: f.name, lambda: "is_field_name_optional"
    )
    fields_from_cmd = _pre_parse_command_args(
        command_name,
        remaining_argv,
        None if name_optional_field is None else name_optional_field.name,
        [f.name for f in flds if f not in fields_from_file],
    )

    # TODO: field: Field[Any]
    def get_value(field: Any) -> Any:
        if field in fields_from_file:
            return getattr(from_file, field.name)
        else:
            from_cmd = fields_from_cmd.get(field.name)
            if from_cmd is None:
                assert (
                    field.default is not NO_DEFAULT
                ), f"'{_to_cmd_line_arg_name(field.name)}' was not provided and has no default"
                return check_cast(field.type, field.default)
            else:
                return check_cast(
                    field.type,
                    _convert_cmd_line_arg(
                        field.type, from_cmd, f"--{_to_cmd_line_arg_name(field.name)}"
                    ),
                )

    return construct_class_from_fields(cls, [get_value(field) for field in flds])


def _convert_cmd_line_arg(cls: Type[Any], values: Sequence[str], desc: str) -> Any:
    # Don't care about optional -- None should be default, so only care about non-optional type
    cls = non_optional_type(cls)

    empty = len(values) == 0  # pylint:disable=len-as-condition

    def default_handler(t: Type[Any]) -> Any:
        assert not empty, f"Need a value for {desc}"
        assert (
            len(values) == 1
        ), f"{desc}: Not expecting a sequence, but got {len(values)} argument values"
        return _cmd_line_arg_from_str(t, values[0], desc)

    def handle_primitive(p: Type[Any]) -> Any:
        if p is bool:
            if len(values) == 1:
                v = values[0]
                res = _NAME_TO_BOOL.get(v, None)
                assert (
                    res is not None
                ), f"Boolean value must be one of: {tuple(_NAME_TO_BOOL.keys())}"
                return res
            else:
                assert empty, f"Too many values for {desc}"
                return True
        else:
            return default_handler(p)

    def handle_tuple(element_types: Sequence[Type[Any]]) -> Any:
        assert len(element_types) == len(
            values
        ), f"{desc}: Expected {len(element_types)} values, got {len(values)}"
        return tuple(
            _cmd_line_arg_from_str(et, v, desc) for et, v in zip_check(element_types, values)
        )

    return match_type(
        cls,
        default_handler=default_handler,
        handle_primitive=handle_primitive,
        handle_sequence=lambda seq_element_type: [
            _cmd_line_arg_from_str(seq_element_type, v, desc) for v in values
        ],
        handle_tuple=handle_tuple,
    )


_NAME_TO_BOOL: Mapping[str, bool] = {"true": True, "false": False}


def _try_get_cmd_line_arg_from_str(cls: Type[T], value: str) -> Result[str, T]:
    def handle_primitive(cls: Type[T]) -> Result[str, T]:
        if cls in (str, int, float):
            try:
                return Ok(cast(T, cast(Any, cls)(value)))
            except ValueError:
                return Err(f"Could not parse {cls} from {value}")
        elif cls is Path:
            return Ok(cast(T, make_absolute_path(Path(value))))
        else:
            return unhandled_type(cls)

    def unhandled_type(cls: Type[T]) -> Result[str, T]:
        return Err(f"Type {cls} cannot be parsed as a command-line argument")

    return match_type(
        cls,
        default_handler=unhandled_type,
        handle_primitive=handle_primitive,
        # Unreachable, we already split the union
        handle_union=lambda members: try_for_each_union_member(
            members, lambda t: _try_get_cmd_line_arg_from_str(t, value)
        ),
        handle_enum=lambda members: try_get_enum_from_str(cls, members, value),
    )


def _cmd_line_arg_from_str(cls: Type[T], value: str, desc: str) -> T:
    res = _try_get_cmd_line_arg_from_str(cls, value)
    return res.expect(f"For {desc}: {res.value}")
