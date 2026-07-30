#!/usr/bin/env python3
'''
Machine health checks for Helix performance machines.

Some prerequisite-install failures are not transient (a bad run) but indicate a broken host. The
canonical example is the apt/dpkg database getting stuck in an interrupted state, which surfaces as:

    E: dpkg was interrupted, you must manually run 'sudo dpkg --configure -a' to correct the problem.

When that state is detected this module:
  1. Takes the machine out of Helix rotation by creating the ``offline`` sentinel file in
     ``$HELIX_CONFIG_ROOT`` (Helix stops sending work to a machine that has this file).
  2. Writes a semaphore blob named after the machine to the ``offline-machines`` container in the
     ``pvscmdupload`` storage account so the perf team is notified of the occurrence.

The semaphore blob is only cleared when the machine next uploads results successfully (see
``clear_offline_semaphore`` / ``benchmarks_ci.py``), which guarantees a machine must actually be
working to have its semaphore removed.

The blob write is best effort: it first tries the azure libraries that are already importable, then
falls back to creating a throwaway virtual environment and installing the dependencies there. If
both approaches fail the machine is still taken offline and a message is printed to the console.
'''

import argparse
import json
import os
import shutil
import socket
import subprocess
import sys
import tempfile
from datetime import datetime, timezone
from logging import getLogger, basicConfig, INFO

# Allow importing sibling modules (``upload``, ``performance.*``) regardless of the PYTHONPATH that
# happens to be configured when this runs (during prereq failure the PYTHONPATH is not yet set up).
SCRIPT_DIR = os.path.dirname(os.path.abspath(__file__))
if SCRIPT_DIR not in sys.path:
    sys.path.insert(0, SCRIPT_DIR)

# Azure dependency versions used when bootstrapping a throwaway venv. These must cover everything
# imported (transitively) by ``upload`` - which pulls in azure.storage.blob, azure.storage.queue and
# azure.identity - so that ``from upload import get_credential`` succeeds inside the fallback venv.
# Kept in sync with the versions installed for result uploads in run_performance_job.py.
_AZURE_PACKAGES = [
    "azure.storage.blob==12.13.0",
    "azure.storage.queue==12.4.0",
    "azure.identity==1.16.1",
    "cryptography==46.0.3",
]

_DPKG_BROKEN_SIGNATURES = (
    "dpkg was interrupted",
    "dpkg --configure -a",
)


def get_machine_name() -> str:
    '''Best-effort stable identifier for the current machine, used as the semaphore blob name.'''
    for env_var in ("HELIX_MACHINENAME", "COMPUTERNAME"):
        value = os.getenv(env_var)
        if value:
            return value
    try:
        name = socket.gethostname()
    except Exception:  # pragma: no cover - extremely unlikely
        name = ""
    return name or "unknown-machine"


def is_dpkg_broken() -> bool:
    '''Return True if apt/dpkg is stuck in the interrupted state that requires 'dpkg --configure -a'.

    Detection mirrors exactly what triggers the failure: any apt-get invocation on such a machine
    prints the "dpkg was interrupted" error. ``apt-get check`` is a read-only diagnostic, so it is a
    safe way to probe for the condition. On non-apt systems (Azure Linux, macOS) apt-get is absent
    and this returns False.
    '''
    if not sys.platform.startswith("linux"):
        return False
        result = subprocess.run(
            ["sudo", "-n", "apt-get", "check"],
            stdout=subprocess.PIPE,
            stderr=subprocess.STDOUT,
            text=True,
            timeout=120,
        )
    except FileNotFoundError:
        # apt-get not present (e.g. Azure Linux) - not an apt/dpkg machine.
        return False
    except Exception as ex:  # pragma: no cover - defensive
        getLogger().warning("Unable to run 'apt-get check' to probe dpkg state: %s", ex)
        return False

    output = result.stdout or ""
    getLogger().info("'apt-get check' output:\n%s", output.strip())
    return any(signature in output for signature in _DPKG_BROKEN_SIGNATURES)


def create_offline_sentinel(reason: str) -> bool:
    '''Create the Helix ``offline`` sentinel so the machine stops receiving work.

    The file is created in ``$HELIX_CONFIG_ROOT``. A direct write is attempted first, falling back to
    ``sudo tee`` since that directory is typically root-owned.
    '''
    config_root = os.getenv("HELIX_CONFIG_ROOT")
    if not config_root:
        getLogger().error("HELIX_CONFIG_ROOT is not set; cannot create the Helix offline sentinel.")
        return False

    offline_path = os.path.join(config_root, "offline")
    content = reason.rstrip("\n") + "\n"

    try:
        with open(offline_path, "w", encoding="utf8") as sentinel:
            sentinel.write(content)
        getLogger().info("Created Helix offline sentinel at %s", offline_path)
        return True
    except OSError as ex:
        getLogger().info("Direct write to %s failed (%s); retrying with sudo.", offline_path, ex)

    try:
        subprocess.run(
            ["sudo", "tee", offline_path],
            input=content,
            text=True,
            stdout=subprocess.DEVNULL,
            check=True,
            timeout=30,
        )
        getLogger().info("Created Helix offline sentinel at %s (via sudo).", offline_path)
        return True
    except Exception as ex:
        getLogger().error("Failed to create Helix offline sentinel at %s: %s", offline_path, ex)
        return False


def _semaphore_blob_body(machine_name: str, reason: str) -> str:
    return json.dumps(
        {
            "machine": machine_name,
            "reason": reason,
            "timestamp_utc": datetime.now(timezone.utc).isoformat(),
            "helix_correlation_id": os.getenv("HELIX_CORRELATION_ID"),
            "helix_workitem_id": os.getenv("HELIX_WORKITEM_ID"),
            "perflab_queue": os.getenv("PERFLAB_QUEUE"),
        },
        indent=2,
    )


def _get_offline_blob_client(machine_name: str):
    '''Build a BlobClient for this machine's semaphore, ensuring the container exists.'''
    from upload import get_credential
    from performance.constants import UPLOAD_STORAGE_URI, OFFLINE_MACHINES_CONTAINER
    from azure.storage.blob import BlobServiceClient
    from azure.core.exceptions import ResourceExistsError

    credential = get_credential()
    service_client = BlobServiceClient(account_url=UPLOAD_STORAGE_URI.format("blob"), credential=credential)
    container_client = service_client.get_container_client(OFFLINE_MACHINES_CONTAINER)
    try:
        container_client.create_container()
        getLogger().info("Created '%s' container.", OFFLINE_MACHINES_CONTAINER)
    except ResourceExistsError:
        pass
    return container_client.get_blob_client(machine_name)


def _write_semaphore_with_current_python(machine_name: str, reason: str) -> bool:
    '''Write the semaphore blob using whatever azure libraries are already importable.'''
    from azure.storage.blob import ContentSettings

    blob_client = _get_offline_blob_client(machine_name)
    blob_client.upload_blob(
        _semaphore_blob_body(machine_name, reason),
        blob_type="BlockBlob",
        overwrite=True,
        content_settings=ContentSettings(content_type="application/json"),
    )
    getLogger().info("Wrote offline semaphore blob '%s'.", machine_name)
    return True


def _write_semaphore_with_new_venv(machine_name: str, reason: str) -> bool:
    '''Fallback: create a throwaway venv, install the azure dependencies, and retry the write.'''
    import venv

    venv_dir = tempfile.mkdtemp(prefix="machine_health_venv_")
    try:
        getLogger().info("Creating temporary virtual environment at %s to install azure dependencies.", venv_dir)
        venv.create(venv_dir, with_pip=True)
        python_exe = os.path.join(venv_dir, "bin", "python")
        if not os.path.exists(python_exe):  # Windows layout (not expected here, but be safe)
            python_exe = os.path.join(venv_dir, "Scripts", "python.exe")

        subprocess.run([python_exe, "-m", "pip", "install", "-q", "-U", "pip"], check=True, timeout=300)
        subprocess.run([python_exe, "-m", "pip", "install", "-q", *_AZURE_PACKAGES], check=True, timeout=600)

        # Re-run this module inside the venv to do just the upload, now that azure is available.
        result = subprocess.run(
            [
                python_exe,
                os.path.abspath(__file__),
                "write-semaphore",
                "--machine", machine_name,
                "--reason", reason,
            ],
            env=dict(os.environ),
            timeout=300,
        )
        return result.returncode == 0
    except Exception as ex:
        getLogger().warning("Failed to write offline semaphore via a fresh venv: %s", ex)
        return False
    finally:
        shutil.rmtree(venv_dir, ignore_errors=True)


def notify_offline(machine_name: str, reason: str) -> bool:
    '''Write the semaphore blob, trying existing libraries first then a throwaway venv.'''
    try:
        return _write_semaphore_with_current_python(machine_name, reason)
    except ImportError as ex:
        getLogger().info("Azure libraries not importable with the current interpreter (%s).", ex)
    except Exception as ex:
        getLogger().warning("Writing offline semaphore with existing libraries failed: %s", ex)

    getLogger().info("Retrying offline semaphore write using a freshly created virtual environment.")
    return _write_semaphore_with_new_venv(machine_name, reason)


def check_and_mark_offline() -> int:
    '''Detect a corrupted dpkg state and, if found, take the machine offline and notify.'''
    if not is_dpkg_broken():
        getLogger().info("apt/dpkg is not in a corrupted state; no machine health action required.")
        return 0

    machine_name = get_machine_name()
    reason = (
        f"apt/dpkg is in a corrupted (interrupted) state on {machine_name}. Prerequisite "
        f"installation cannot proceed. The machine has been taken out of Helix rotation. Recover it "
        f"by running 'sudo dpkg --configure -a' and removing $HELIX_CONFIG_ROOT/offline."
    )
    getLogger().warning(reason)

    # Taking the machine offline is the critical safety action and always runs first.
    create_offline_sentinel(reason)

    # Notification (semaphore blob) is best effort.
    if notify_offline(machine_name, reason):
        getLogger().info("Offline semaphore recorded for %s.", machine_name)
    else:
        print(
            f"** Error: Failed to write the offline semaphore for {machine_name} using both the "
            f"existing libraries and a freshly created virtual environment. The machine has still "
            f"been taken offline via the $HELIX_CONFIG_ROOT/offline sentinel. **"
        )

    # Never change the work item's exit code here: the prerequisite-failure path already exits 1.
    return 0


def clear_offline_semaphore() -> int:
    '''Delete this machine's offline semaphore blob, if present.

    Called after a successful results upload: reaching this point means the machine is working, so
    its semaphore should be cleared. Missing blobs/containers are treated as success.
    '''
    machine_name = get_machine_name()
    try:
        from azure.core.exceptions import ResourceNotFoundError

        blob_client = _get_offline_blob_client(machine_name)
        try:
            blob_client.delete_blob()
            getLogger().info("Cleared offline semaphore for %s.", machine_name)
        except ResourceNotFoundError:
            getLogger().info("No offline semaphore present for %s; nothing to clear.", machine_name)
        return 0
    except Exception as ex:
        # Clearing the semaphore must never fail an otherwise successful run.
        getLogger().warning("Failed to clear offline semaphore for %s: %s", machine_name, ex)
        return 0


def main(argv=None) -> int:
    basicConfig(level=INFO, format="%(message)s")
    parser = argparse.ArgumentParser(description=__doc__)
    subparsers = parser.add_subparsers(dest="command", required=True)

    subparsers.add_parser("check", help="Detect a corrupted dpkg state; if found, take the machine offline and notify.")
    subparsers.add_parser("clear", help="Clear this machine's offline semaphore after a successful run.")

    write_parser = subparsers.add_parser("write-semaphore", help=argparse.SUPPRESS)
    write_parser.add_argument("--machine", required=True)
    write_parser.add_argument("--reason", required=True)

    args = parser.parse_args(argv)

    if args.command == "check":
        return check_and_mark_offline()
    if args.command == "clear":
        return clear_offline_semaphore()
    if args.command == "write-semaphore":
        return 0 if _write_semaphore_with_current_python(args.machine, args.reason) else 1
    return 1


if __name__ == "__main__":
    sys.exit(main())
