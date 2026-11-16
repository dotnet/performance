"""Bounded, opt-in machine-wide collect-linux diagnostics on dedicated Linux workers."""

import errno
import os
import platform
import shutil
import signal
import subprocess
import threading
import time
from pathlib import Path


def validate_collector():
    if platform.system() != "Linux":
        raise ValueError("--collect-linux requires Linux.")
    version = tuple(int(part) for part in platform.release().split("-")[0].split(".")[:2])
    if version < (6, 4):
        raise RuntimeError("collect-linux requires kernel 6.4+ with CONFIG_USER_EVENTS=y.")
    tool = os.environ.get("PERFLAB_DOTNET_TRACE")
    if not tool:
        payload = os.environ.get("HELIX_CORRELATION_PAYLOAD")
        staged = Path(payload) / "dotnet-trace" / "dotnet-trace" if payload else None
        tool = str(staged) if staged and staged.is_file() else shutil.which("dotnet-trace")
    if not tool or not Path(tool).is_file():
        raise FileNotFoundError("Set PERFLAB_DOTNET_TRACE to a dotnet-trace supporting collect-linux.")
    tool = str(Path(tool).resolve())
    command = [tool]
    if os.geteuid() != 0:
        command = ["sudo", "-n", "--", "env",
                   f"DOTNET_ROOT={os.environ.get('DOTNET_ROOT', '')}", tool]
    subprocess.run(command + ["collect-linux", "--help"], check=True,
                   stdout=subprocess.DEVNULL, timeout=30)
    return command


def collect_linux(collector, build, trace_directory, env, timeout, *, recording_timeout):
    import pty

    trace = (Path(trace_directory) / "diagnostic.nettrace").resolve()
    log_path = Path(trace_directory) / "collect-linux.log"
    if trace.exists():
        raise FileExistsError(trace)
    # A terminal makes collect-linux emit its recording-ready progress message.
    # Redirected output suppresses that message; a startup sleep would race short builds.
    master, slave = pty.openpty()
    ready = threading.Event()
    reader_errors = []
    process = None
    reader = None
    days, seconds = divmod(recording_timeout + 60, 86400)
    hours, seconds = divmod(seconds, 3600)
    minutes, seconds = divmod(seconds, 60)
    duration = f"{days:02}:{hours:02}:{minutes:02}:{seconds:02}"
    with log_path.open("wb") as log:
        def read_output():
            tail = b""
            try:
                while True:
                    try:
                        data = os.read(master, 8192)
                    except OSError as error:
                        if error.errno == errno.EIO:
                            break
                        raise
                    if not data:
                        break
                    log.write(data)
                    log.flush()
                    tail = (tail + data)[-8192:]
                    if b"recording trace" in tail.lower():
                        ready.set()
            except OSError as error:
                reader_errors.append(error)

        try:
            process = subprocess.Popen(
                collector + ["collect-linux", "--profile", "dotnet-common,cpu-sampling",
                             "--duration", duration,
                             "--output", str(trace)],
                stdin=slave, stdout=slave, stderr=slave, env=env)
            os.close(slave)
            slave = None
            reader = threading.Thread(target=read_output, daemon=True)
            reader.start()
            deadline = time.monotonic() + 60
            while not ready.wait(0.1):
                if process.poll() is not None:
                    raise RuntimeError(f"collect-linux exited before recording; see {log_path}")
                if reader_errors or time.monotonic() >= deadline:
                    raise RuntimeError(f"collect-linux did not become ready; see {log_path}")
            build()
            if process.poll() is not None:
                raise RuntimeError(f"collect-linux stopped before the diagnostic build completed; see {log_path}")
        finally:
            try:
                if process is not None:
                    if process.poll() is None:
                        process.send_signal(signal.SIGINT)
                    try:
                        process.wait(timeout=timeout)
                    except subprocess.TimeoutExpired:
                        process.kill()
                        process.wait()
                        raise RuntimeError(f"collect-linux did not finalize within {timeout} seconds; see {log_path}")
            finally:
                if slave is not None:
                    os.close(slave)
                if reader is not None:
                    reader.join(timeout=10)
                os.close(master)
        if reader_errors:
            raise RuntimeError(f"Unable to read collect-linux output; see {log_path}") from reader_errors[0]
        if process.returncode != 0:
            raise subprocess.CalledProcessError(process.returncode, process.args)
        if not trace.is_file() or trace.stat().st_size == 0:
            raise RuntimeError(f"collect-linux produced no trace; see {log_path}")
