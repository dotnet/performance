import sys
from pathlib import Path


REPOSITORY_ROOT = Path(__file__).parents[2]
sys.path.insert(0, str(REPOSITORY_ROOT))
sys.path.insert(0, str(REPOSITORY_ROOT / "scripts"))

from src.scenarios.mauidesktopbenchmarks.pre import get_maui_branch
from src.scenarios.mauidesktopbenchmarks.test import get_bdn_arguments, parse_bdn_arguments


def test_framework_maps_to_maui_branch():
    assert get_maui_branch("net11.0-windows10.0.19041.0") == "net11.0"


def test_public_bdn_arguments_drop_all_category_values():
    configured = parse_bdn_arguments(
        "--anyCategories Libraries Runtime ThirdParty "
        "--iterationCount 1 --warmupCount 0"
    )

    assert get_bdn_arguments(
        "graphics",
        configured,
        ["--filter", "*ParseBlack*"],
    ) == [
        "--iterationCount",
        "1",
        "--warmupCount",
        "0",
        "--filter",
        "*ParseBlack*",
    ]


def test_core_defaults_include_filter_and_log_exclusion():
    assert get_bdn_arguments("core", [], []) == [
        "--filter",
        "*",
        "--exclusion-filter",
        "*MauiLoggerWithLoggerMinLevelErrorBenchmarker*",
    ]
