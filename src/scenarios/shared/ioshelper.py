import glob
import json
import os
import re
import subprocess
import tempfile
import time
from logging import getLogger

from performance.common import RunCommand


class iOSHelper:
    """Unified helper for iOS simulator and physical device operations.

    Callers use the same API (setup_device, install_app, measure_cold_startup,
    cleanup) regardless of device type.

    Install and launch use mlaunch (the same tool Visual Studio uses for F5)
    to match the real developer inner-loop experience:
      - Simulator: mlaunch --installsim / --launchsim
      - Device:    mlaunch --installdev / --launchdev
    Device detection still uses devicectl; simulator management uses simctl.
    """

    _mlaunch_path = None  # resolved once, cached for the process

    def __init__(self):
        self.bundle_id = None
        self.device_id = None
        self.app_bundle_path = None
        self.is_physical_device = False

    # ── mlaunch Resolution ────────────────────────────────────────────

    @staticmethod
    def _resolve_mlaunch():
        """Resolve the mlaunch binary from the iOS SDK pack.

        Searches $DOTNET_ROOT/packs/Microsoft.iOS.Sdk.*/tools/bin/mlaunch,
        falling back to ~/.dotnet if DOTNET_ROOT is unset. Caches the result.
        """
        if iOSHelper._mlaunch_path is not None:
            return iOSHelper._mlaunch_path

        dotnet_root = os.environ.get('DOTNET_ROOT', os.path.expanduser('~/.dotnet'))
        pattern = os.path.join(dotnet_root, 'packs', 'Microsoft.iOS.Sdk.*', '*', 'tools', 'bin', 'mlaunch')
        matches = sorted(glob.glob(pattern))
        if not matches:
            raise FileNotFoundError(
                f"mlaunch not found. Searched: {pattern}\n"
                f"Ensure the iOS SDK workload is installed (dotnet workload install ios)."
            )
        # Use the last match (highest version when sorted lexicographically)
        mlaunch = matches[-1]
        getLogger().info("Resolved mlaunch: %s", mlaunch)
        iOSHelper._mlaunch_path = mlaunch
        return mlaunch

    # ── Device Detection ─────────────────────────────────────────────

    @staticmethod
    def detect_connected_device():
        """Detect a connected physical iOS device and return its UDID.

        Checks IOS_DEVICE_UDID env var first, then auto-detects via devicectl.
        Returns the UDID string, or None if no device is found.
        """
        udid = os.environ.get('IOS_DEVICE_UDID', '').strip()
        if udid:
            getLogger().info("Using IOS_DEVICE_UDID from environment: %s", udid)
            return udid

        getLogger().info("Auto-detecting connected iOS device...")
        return iOSHelper._detect_via_devicectl_json() or iOSHelper._detect_via_devicectl_text()

    @staticmethod
    def _detect_via_devicectl_json():
        """Try JSON-based device detection (Xcode 15+)."""
        fd, json_tmp = tempfile.mkstemp(suffix='.json', prefix='devicectl_')
        os.close(fd)
        try:
            result = subprocess.run(
                ['xcrun', 'devicectl', 'list', 'devices', '--json-output', json_tmp],
                capture_output=True, text=True, timeout=30
            )
            if result.returncode != 0:
                return None

            with open(json_tmp, 'r') as f:
                data = json.load(f)

            for device in data.get('result', {}).get('devices', []):
                conn = device.get('connectionProperties', {})
                transport = conn.get('transportType', '')
                device_udid = device.get('identifier', '')
                if transport in ('wired', 'localNetwork', 'wifi') and device_udid:
                    name = device.get('deviceProperties', {}).get('name', 'unknown')
                    getLogger().info("Found device: %s (UDID: %s, transport: %s)", name, device_udid, transport)
                    return device_udid
            return None
        except (subprocess.TimeoutExpired, json.JSONDecodeError, KeyError, OSError):
            return None
        finally:
            if os.path.exists(json_tmp):
                os.remove(json_tmp)

    @staticmethod
    def _detect_via_devicectl_text():
        """Fallback: parse device UDID from devicectl text output."""
        try:
            result = subprocess.run(
                ['xcrun', 'devicectl', 'list', 'devices'],
                capture_output=True, text=True, timeout=30
            )
            if result.returncode != 0:
                return None

            # Match CoreDevice UUID (8-4-4-4-12) or legacy Apple UDID formats
            uuid_re = re.compile(
                r'([0-9A-Fa-f]{8}-[0-9A-Fa-f]{4}-[0-9A-Fa-f]{4}-[0-9A-Fa-f]{4}-[0-9A-Fa-f]{12}'
                r'|[0-9A-Fa-f]{8}-[0-9A-Fa-f]{16}'
                r'|[0-9A-Fa-f]{25,40})'
            )
            for line in (result.stdout or '').splitlines():
                match = uuid_re.search(line)
                if match:
                    getLogger().info("Detected device UDID: %s", match.group(1))
                    return match.group(1)
            return None
        except Exception:
            return None

    # ── Simulator UDID Resolution ────────────────────────────────────

    @staticmethod
    def _resolve_booted_simulator_udid():
        """Return the UDID of the first booted simulator.

        mlaunch requires a real UDID (--device :v2:udid=<uuid>); it does
        not understand simctl's "booted" shortcut.  This method queries
        ``simctl list devices booted`` to find the actual UDID.
        """
        try:
            result = subprocess.run(
                ['xcrun', 'simctl', 'list', 'devices', 'booted', '-j'],
                capture_output=True, text=True, timeout=15
            )
            if result.returncode != 0:
                return None
            data = json.loads(result.stdout)
            for runtime_devices in data.get('devices', {}).values():
                for dev in runtime_devices:
                    if dev.get('state', '').lower() == 'booted':
                        return dev['udid']
            return None
        except Exception:
            return None

    # ── Unified Device Setup ─────────────────────────────────────────

    def setup_device(self, bundle_id, app_bundle_path, device_id='booted', is_physical=False):
        """Prepare a simulator or physical device for testing.

        For simulators: ensures the device is booted and uninstalls any
        existing app. For physical devices: stores the UDID for later use.
        Does NOT install the app — call install_app() separately so
        install timing can be captured independently.
        """
        self.bundle_id = bundle_id
        self.device_id = device_id
        self.app_bundle_path = app_bundle_path
        self.is_physical_device = is_physical

        if is_physical:
            getLogger().info("Physical device setup complete: %s", device_id)
            return

        # Simulator setup: boot if needed, uninstall stale app
        if device_id != 'booted':
            getLogger().info("Booting simulator: %s", device_id)
            result = subprocess.run(
                ['xcrun', 'simctl', 'boot', device_id],
                capture_output=True, text=True
            )
            if result.returncode != 0 and 'already booted' not in (result.stderr or '').lower():
                raise subprocess.CalledProcessError(result.returncode, result.args, result.stdout, result.stderr)

        # Resolve the actual UDID — mlaunch needs a real UDID, not "booted"
        if self.device_id == 'booted':
            resolved = self._resolve_booted_simulator_udid()
            if not resolved:
                raise RuntimeError(
                    "Could not resolve booted simulator UDID. "
                    "Ensure a simulator is booted (setup_helix.py should have done this).")
            getLogger().info("Resolved booted simulator UDID: %s", resolved)
            self.device_id = resolved

        self._run_quiet(['xcrun', 'simctl', 'uninstall', self.device_id, bundle_id])

    # ── Device Code Signing ──────────────────────────────────────────

    def sign_app_for_device(self, app_bundle_path):
        """Sign the .app bundle for physical device deployment.

        Mirrors the signing flow from maui_scenarios_ios.proj device startup:
          1. Copy embedded.mobileprovision into the .app bundle
          2. Run the Helix-provided 'sign' tool

        Both 'embedded.mobileprovision' and 'sign' are pre-installed on the
        Mac.iPhone.17.Perf Helix machines. The build must use
        EnableCodeSigning=false so MSBuild skips automatic signing.

        No-op for simulator builds.
        """
        if not self.is_physical_device:
            return

        import shutil
        provision_src = 'embedded.mobileprovision'
        provision_dst = os.path.join(app_bundle_path, 'embedded.mobileprovision')

        if not os.path.exists(provision_src):
            getLogger().warning(
                "embedded.mobileprovision not found in working directory. "
                "Device signing may fail if the Helix machine doesn't have it.")
        else:
            shutil.copy2(provision_src, provision_dst)
            getLogger().info("Copied provisioning profile into %s", app_bundle_path)

        app_name = os.path.basename(app_bundle_path)
        app_dir = os.path.dirname(os.path.abspath(app_bundle_path))
        getLogger().info("Signing %s for device deployment", app_name)
        RunCommand(['sign', app_name], verbose=True).run(working_directory=app_dir)

    # ── Unified Operations ───────────────────────────────────────────

    def install_app(self, app_bundle_path):
        """Install the app bundle and return wall-clock install time in ms.

        Device:    mlaunch --installdev
        Simulator: mlaunch --installsim
        """
        mlaunch = self._resolve_mlaunch()

        if self.is_physical_device:
            cmd = [mlaunch, '--installdev', app_bundle_path,
                   '--devname', self.device_id]
        else:
            cmd = [mlaunch, '--installsim', app_bundle_path,
                   '--device', f':v2:udid={self.device_id}']

        start = time.time()
        RunCommand(cmd, verbose=True).run()
        elapsed_ms = (time.time() - start) * 1000
        getLogger().info("Install completed in %.1f ms", elapsed_ms)
        return elapsed_ms

    def measure_cold_startup(self, bundle_id):
        """Measure app cold startup time in ms (int).

        Uses mlaunch to match the real F5 developer experience:
          - Simulator: mlaunch --launchsim (run as background process since
            it blocks until the app exits; we detect launch via simctl and
            then terminate the process)
          - Device:    mlaunch --launchdev (returns immediately with PID)

        Terminates any running instance first. For simulator this uses
        simctl terminate (mlaunch has no simulator terminate command).
        """
        mlaunch = self._resolve_mlaunch()

        if self.is_physical_device:
            cmd = [mlaunch, '--launchdev', self.app_bundle_path,
                   '--devname', self.device_id]
            start = time.time()
            RunCommand(cmd, verbose=True).run()
            elapsed_ms = int((time.time() - start) * 1000)
            getLogger().info("Cold startup: %d ms", elapsed_ms)
            return elapsed_ms

        # Simulator: --launchsim blocks until the app exits, so run it
        # in a subprocess and poll for the app to appear in the process list.
        self._run_quiet(['xcrun', 'simctl', 'terminate', self.device_id, bundle_id])
        time.sleep(0.5)
        cmd = [mlaunch, '--launchsim', self.app_bundle_path,
               '--device', f':v2:udid={self.device_id}']
        getLogger().info("$ %s", ' '.join(cmd))

        start = time.time()
        proc = subprocess.Popen(cmd, stdout=subprocess.PIPE, stderr=subprocess.PIPE)
        try:
            # Poll until the app is running in the simulator (max 120s)
            app_name = os.path.splitext(os.path.basename(self.app_bundle_path))[0]
            timeout = 120
            poll_interval = 0.5
            elapsed = 0.0
            while elapsed < timeout:
                # Check if mlaunch exited with an error
                ret = proc.poll()
                if ret is not None and ret != 0:
                    stdout = proc.stdout.read().decode() if proc.stdout else ''
                    stderr = proc.stderr.read().decode() if proc.stderr else ''
                    raise subprocess.CalledProcessError(ret, cmd, stdout, stderr)

                # Check if the app process is running via simctl
                check = subprocess.run(
                    ['xcrun', 'simctl', 'spawn', self.device_id, 'launchctl', 'list'],
                    capture_output=True, text=True, timeout=10
                )
                if bundle_id in (check.stdout or ''):
                    break
                time.sleep(poll_interval)
                elapsed = time.time() - start

            elapsed_ms = int((time.time() - start) * 1000)
            getLogger().info("Cold startup: %d ms", elapsed_ms)
            return elapsed_ms
        finally:
            proc.terminate()
            try:
                proc.wait(timeout=5)
            except subprocess.TimeoutExpired:
                proc.kill()
                proc.wait()

    def cleanup(self, skip_uninstall=False):
        """Clean up the device session (simulator or physical).

        Device uses mlaunch --uninstalldevbundleid. Simulator keeps simctl
        (mlaunch has no simulator terminate/uninstall commands).
        """
        if skip_uninstall:
            return
        if self.is_physical_device:
            mlaunch = self._resolve_mlaunch()
            self._run_quiet([mlaunch, '--uninstalldevbundleid', self.bundle_id,
                             '--devname', self.device_id])
        else:
            self._run_quiet(['xcrun', 'simctl', 'terminate', self.device_id, self.bundle_id])
            self._run_quiet(['xcrun', 'simctl', 'uninstall', self.device_id, self.bundle_id])

    # ── App Bundle Discovery ─────────────────────────────────────────

    def find_app_bundle(self, build_output_dir, app_name, configuration='Debug'):
        """Find the .app bundle in the build output directory.

        Searches for: bin/<config>/net*/<rid>/<app>.app
        Returns the absolute path. Raises FileNotFoundError if not found.
        """
        for rid_pattern in ['iossimulator-*', 'ios-arm64']:
            pattern = os.path.join(build_output_dir, 'bin', configuration, 'net*', rid_pattern, f'{app_name}.app')
            matches = glob.glob(pattern)
            if matches:
                if len(matches) > 1:
                    getLogger().warning("Multiple app bundles found: %s. Using first.", matches)
                app_path = os.path.abspath(matches[0])
                getLogger().info("Found app bundle: %s", app_path)
                return app_path

        raise FileNotFoundError(
            f"No .app bundle in {build_output_dir}/bin/{configuration}/net*/(iossimulator-*|ios-arm64)/{app_name}.app"
        )

    # ── Helpers ───────────────────────────────────────────────────────

    @staticmethod
    def _run_quiet(cmd):
        """Run a command, suppressing CalledProcessError (best-effort)."""
        try:
            RunCommand(cmd, verbose=True).run()
        except subprocess.CalledProcessError:
            pass
