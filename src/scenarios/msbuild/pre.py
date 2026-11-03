import argparse
from msbuild_catalog import SCENARIOS
from shared.msbuild import prepare

parser = argparse.ArgumentParser()
parser.add_argument("--scenario-id", required=True, choices=SCENARIOS)
args = parser.parse_args()
prepare(SCENARIOS[args.scenario_id]["asset"])
