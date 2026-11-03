"""Execute the enabled PerfStar operations without timing their setup or diagnostics."""

import json
import ctypes
import os
from pathlib import Path
import shutil
import subprocess
import sys
import time
import uuid
import xml.etree.ElementTree as ET

from shared import const
from msbuild_catalog import ASSET_DEFINITIONS

RESTORE = ["/t:restore", "/p:WarningsNotAsErrors=NU3018%3BNU3027%3BNU3042"]


def publish_selection(app):
    orchard = app / "OrchardCore"
    projects = sorted([
        *orchard.joinpath("src/OrchardCore.Modules").rglob("*.csproj"),
        *orchard.joinpath("src/OrchardCore.Themes").rglob("*.csproj"),
    ], key=lambda path: path.as_posix())
    if not projects:
        raise ValueError("Orchard module/theme projects are missing.")
    solution = ET.Element("Solution")
    for path in projects:
        ET.SubElement(solution, "Project", Path=path.relative_to(orchard).as_posix())
    path = orchard / "OrchardCore.PublishPerf.slnx"
    ET.ElementTree(solution).write(path, encoding="utf-8", xml_declaration=True)
    props = ET.parse(orchard / "src/OrchardCore.Build/TargetFrameworks.props")
    framework = next((element.text for element in props.iter()
                      if element.tag.split("}")[-1] == "DefaultTargetFramework"), None)
    if not framework or not framework.strip():
        raise ValueError("Orchard DefaultTargetFramework is missing.")
    return path, framework.strip()


def execute_scenario(scenario, dotnet, iterations, timeout, diagnostics=False,
                     linux_trace=False, evaluation_collection="off", legacy=False):
    if not 1 <= iterations <= 2_147_483_647 or timeout < 1:
        raise ValueError("Iterations must be positive and fit Int32; timeout must be positive.")
    kind = scenario["kind"]
    lighthouse = kind.endswith("-evaluation")
    if lighthouse and evaluation_collection != "current":
        raise ValueError("Evaluation lighthouses require evaluation collection.")
    collector = None
    if linux_trace:
        from shared.collect_linux import validate_collector
        collector = validate_collector()
    app = Path(const.APPDIR).resolve()
    asset = ASSET_DEFINITIONS[scenario["asset"]]
    project = app / asset["project"]
    verb = "build"
    extra = []
    if kind == "publish-evaluation":
        project, framework = publish_selection(app)
        extra = ["-f", framework]
        verb = "publish"
    elif kind == "pack-evaluation":
        verb = "pack"
    elif kind == "project-query-evaluation":
        project = app / "OrchardCore/src/OrchardCore.Modules/OrchardCore.Admin/OrchardCore.Admin.csproj"
        verb = "msbuild"
    if not project.is_file():
        raise FileNotFoundError(project)
    env = os.environ.copy()
    for name in ("MSBUILDDISABLENODEREUSE", "MSBUILDUSESERVER", "UseSharedCompilation",
                 "MSBUILDDEBUGCOMM", "MSBUILDDEBUGPATH"):
        env.pop(name, None)
    env.update(DOTNET_ROOT=str(Path(dotnet).resolve().parent),
               DOTNET_HOST_PATH=str(Path(dotnet).resolve()), DOTNET_MULTILEVEL_LOOKUP="0",
               DOTNET_CLI_TELEMETRY_OPTOUT="1", DOTNET_SKIP_FIRST_TIME_EXPERIENCE="1",
               DOTNET_CLI_ENABLEAOT="0", NUGET_PACKAGES=str(Path("packages").resolve()),
               NUGET_HTTP_CACHE_PATH=str(Path("http-cache").resolve()))
    env["PATH"] = str(Path(dotnet).resolve().parent) + os.pathsep + env.get("PATH", "")
    if lighthouse:
        env.update(MSBUILDDISABLENODEREUSE="0", MSBUILDUSESERVER="1")
    mode = "/mt" if scenario["mt"] else "/mt:false"
    base = [str(dotnet), verb, str(project), *extra, "/m", mode, *asset["arguments"]]
    if kind == "rebuild-nr-false":
        base.append("/nr:false")
    if lighthouse:
        base.extend(["-nologo", "-v:q"])
    traces = Path(const.TRACEDIR)
    traces.mkdir(parents=True, exist_ok=True)
    measurements = traces / "build-wall-clock.json"
    if measurements.exists():
        raise FileExistsError(f"Use fresh measurement output: {measurements}")
    stopped = False
    sessions = []
    samples = []
    variants = {"get-property": [], "get-item": []} if kind == "project-query-evaluation" else {}
    hook = None
    frequency = 1_000_000_000
    if os.name == "nt":
        counter_frequency = ctypes.c_longlong()
        if not ctypes.windll.kernel32.QueryPerformanceFrequency(ctypes.byref(counter_frequency)):
            raise OSError("QueryPerformanceFrequency failed.")
        frequency = counter_frequency.value
    if evaluation_collection == "current":
        tools = os.environ.get("PERFLAB_EVALUATION_HOOK")
        payload = os.environ.get("HELIX_CORRELATION_PAYLOAD")
        hook = Path(tools) if tools else Path(payload or "artifacts") / "startup/PerfStar.EvaluationMetrics.StartupHook.dll"
        if not hook.is_file():
            raise FileNotFoundError(f"Evaluation startup hook not found: {hook}")

    def execute(command, label, environment=None):
        nonlocal stopped
        print("Running:", subprocess.list2cmdline(command), flush=True)
        stopped = False
        with (traces / (label + ".log")).open("w", encoding="utf-8") as log:
            start = time.perf_counter_ns()
            try:
                subprocess.run(command, env=environment or env, stdout=log,
                               stderr=subprocess.STDOUT, check=True, timeout=timeout, cwd=const.APPDIR)
            except (subprocess.CalledProcessError, subprocess.TimeoutExpired):
                print(f"Command failed; see {traces / (label + '.log')}", flush=True)
                raise
            return start, time.perf_counter_ns()

    def shutdown(label):
        nonlocal stopped
        if not stopped:
            execute([str(dotnet), "build-server", "shutdown"], label)
            stopped = True

    def edit_sources():
        if not asset["edits"]:
            raise ValueError("Incremental scenario has no declared source edits.")
        for relative in asset["edits"]:
            with (app / relative).open("a", encoding="utf-8") as source:
                source.write("\n// Hello world\n")

    def remove_obj():
        for path in sorted(app.rglob("obj"), key=lambda entry: len(entry.parts), reverse=True):
            if path.is_dir():
                shutil.rmtree(path)

    def round_run(index, diagnostic=False):
        shutdown(f"shutdown-{index}")
        environment = env.copy()
        token = uuid.uuid4().hex if hook and not diagnostic else None
        if token:
            metrics_directory = traces.resolve() / ".perfstar-evaluation-metrics"
            metrics_directory.mkdir(exist_ok=True)
            environment["PERFSTAR_EVALUATION_METRICS_DIR"] = str(metrics_directory)
            environment["PERFSTAR_EVALUATION_METRICS_SESSION"] = token
            existing = environment.get("DOTNET_STARTUP_HOOKS")
            hooks = existing.split(os.pathsep) if existing else []
            hook_path = str(hook.resolve())
            if os.path.normcase(hook_path) not in [os.path.normcase(path) for path in hooks]:
                hooks.append(hook_path)
            environment["DOTNET_STARTUP_HOOKS"] = os.pathsep.join(hooks)
        priming = None
        if kind == "warm-inc-build":
            edit_sources()
            priming = execute(base, f"prime-{index}", environment)
            edit_sources()
        elif kind == "warm-restore":
            priming = execute(base + RESTORE, f"prime-{index}", environment)
        elif kind in ("restore", "cold-restore"):
            remove_obj()
        order = ["get-property", "get-item"]
        if index % 2:
            order.reverse()
        operations = order if variants else [""]
        result = {}
        pending = []
        for variant in operations:
            execution_env = environment
            execution_token = token
            if variant and token:
                execution_token = uuid.uuid4().hex
                execution_env = environment | {"PERFSTAR_EVALUATION_METRICS_SESSION": execution_token}
            arguments = []
            if variant:
                arguments = ["-getProperty:TargetFramework" if variant == "get-property" else "-getItem:ProjectReference"]
            elif kind in ("rebuild", "rebuild-nr-false"):
                arguments = ["/t:rebuild"]
            elif kind in ("restore", "cold-restore", "warm-restore"):
                arguments = RESTORE
            label = f"{'diagnostic' if diagnostic else 'measured'}-{index}" + (f"-{variant}" if variant else "")
            if diagnostic and legacy and not variant:
                label = "diagnostic"
            if diagnostic:
                arguments += [f"/bl:{(traces / (label + '.binlog')).resolve()}"]
            start, end = execute(base + arguments, label, execution_env)
            result[variant] = (end - start) / 1_000_000
            if execution_token:
                request = {
                    "directory": ".", "session": execution_token,
                    "variant": variant, "iteration": index, "require_server": lighthouse,
                    "clock_frequency": frequency,
                }
                if kind in ("warm-inc-build", "warm-restore"):
                    request["window"] = {"start": start, "end": end}
                    request["priming_window"] = {"start": priming[0], "end": priming[1]}
                pending.append(request)
            shutdown(f"shutdown-after-{index}" + (f"-{variant}" if variant else ""))
        return result, pending

    try:
        if lighthouse and verb != "msbuild":
            execute([str(dotnet), "restore", str(project), "--nologo", "-v:q", "/m"], "restore")
            execute([str(dotnet), "build", str(project), "-c", "Release", "--no-restore",
                     *extra, "--nologo", "-v:q", "/m"], "prebuild-release")
            execute(base, "warmup")
        elif kind == "project-query-evaluation":
            for name, argument in (("get-property", "-getProperty:TargetFramework"),
                                   ("get-item", "-getItem:ProjectReference")):
                execute(base + [argument], "warmup-" + name)
        else:
            execute([str(dotnet), "build", str(project), *RESTORE, "/m", mode], "restore")
            if kind in ("rebuild", "rebuild-nr-false", "warm-inc-build"):
                if kind != "warm-inc-build":
                    execute(base, "warmup")
        recollections = 0
        for index in range(iterations):
            while True:
                try:
                    values, pending = round_run(index)
                    break
                except subprocess.CalledProcessError:
                    if sys.platform != "linux" or not scenario["mt"] or recollections >= 5:
                        raise
                    recollections += 1
                    print(f"Re-collecting failed Linux MT round ({recollections}/5).", flush=True)
                    shutdown(f"recollection-{recollections}")
            samples.append(values["get-property"] if variants else values[""])
            for name in variants:
                variants[name].append(values[name])
            sessions.extend(pending)
        os.environ["PERFLAB_DATA_MSBuildRecollectedAttempts"] = str(recollections)
        if legacy:
            output = samples
        else:
            output = {
                "samples": samples,
                "counters": [{"name": name + "-time", "samples": values} for name, values in variants.items()],
                "sessions": sessions,
                "recollected_attempts": recollections,
            }
        measurements.write_text(json.dumps(output), encoding="utf-8")
        if diagnostics or linux_trace:
            shutdown("diagnostic-shutdown")
            if linux_trace:
                from shared.collect_linux import collect_linux
                collect_linux(collector, lambda: round_run(0, diagnostic=True), traces, env, timeout)
            else:
                round_run(0, diagnostic=True)
        return samples
    finally:
        shutdown("final-shutdown")
