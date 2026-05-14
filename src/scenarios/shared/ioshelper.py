"""iOS deploy and startup measurement helpers (simulator + physical device).

This module is the single source of truth for "F5-style" install + launch
measurement on iOS. Two device kinds, two slightly different toolchains:

  ┌──────────┬──────────────────────────┬──────────────────────────────┐
  │          │ install                  │ cold-startup measurement     │
  ├──────────┼──────────────────────────┼──────────────────────────────┤
  │ device   │ mlaunch --installdev     │ mlaunch --launchdev          │
  │          │ (handles devicectl tunnel│ (returns PID immediately,    │
  │          │  + devicectl signing     │  watchdog log captures the   │
  │          │  metadata)               │  startup phases)             │
  ├──────────┼──────────────────────────┼──────────────────────────────┤
  │ simulator│ mlaunch --installsim     │ xcrun simctl launch          │
  │          │ (matches what the IDE    │ (NOT mlaunch --launchsim,    │
  │          │  does during F5)         │  see Note 1 below)           │
  └──────────┴──────────────────────────┴──────────────────────────────┘

Per .NET iOS team guidance (Rolf Bjarne Kvinge), mlaunch is the canonical
tool the IDEs use during F5 — it handles ad-hoc signing, devicectl tunnel
setup, and stdout tunnelling. We use it wherever the IDE would.

Note 1 — why simctl for simulator launch instead of mlaunch --launchsim:
   mlaunch --launchsim blocks until the launched app exits (it tunnels app
   stdout/stderr) and during testing on Apple Silicon Helix machines the
   simulator transitioned from Booted → Shutdown silently during the call,
   producing 180-second timeouts with no diagnostic output. simctl launch
   is what mlaunch invokes internally for the actual launch step, so the
   wall-clock measurement is equivalent — we just skip mlaunch's stdout
   tunnel layer (we don't need it for measurement). See
   ``measure_cold_startup`` for the implementation and the post-launch
   stabilization check that confirms the launched PID survives.

Note 2 — physical-device install requires code signing:
   ``mlaunch --installdev`` ultimately calls ``xcrun devicectl device
   install app``, which refuses to install an unsigned bundle (errors with
   MIInstallerErrorDomain code 13, "No code signature found"). The .proj
   builds with ``EnableCodeSigning=false`` to keep the build deterministic
   on Helix, then ``sign_app_for_device`` re-signs the bundle using the
   ``embedded.mobileprovision`` and ``sign`` tool that Helix machine prep
   is expected to provide. If those artifacts are missing the install
   will fail; the orchestrator (setup_helix.py) is responsible for
   detecting that case.
"""

import glob
import json
import os
import re
import subprocess
import tempfile
import time
from datetime import datetime
from logging import getLogger

from performance.common import RunCommand


class iOSHelper:
    """Unified helper for iOS simulator and physical device operations.

    Callers use the same API (setup_device, install_app, measure_cold_startup,
    cleanup) regardless of device type. See module docstring for the
    install/launch tooling matrix and the rationale behind it.
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
                # mlaunch uses the hardware UDID (e.g. 00008020-001965D83C43002E),
                # NOT the CoreDevice identifier (a UUID like 5AE7F3E5-...).
                # Fall back to the CoreDevice identifier if hw UDID is missing.
                hw_udid = device.get('hardwareProperties', {}).get('udid', '')
                device_udid = hw_udid or device.get('identifier', '')
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
        """Return the UDID of the simulator we should target.

        Resolution order:
          1. ``$HELIX_WORKITEM_ROOT/sim_udid.txt`` — written by setup_helix.py
             after creating + booting our own per-workitem device. This pins
             the UDID end-to-end so we never accidentally target a stale or
             foreign device that happens to be booted on the machine.
          2. ``simctl list devices booted -j`` — fallback for local runs (no
             Helix) where setup_helix.py was not invoked.

        mlaunch requires a real UDID (--device :v2:udid=<uuid>); it does not
        understand simctl's "booted" shortcut.
        """
        # Prefer the pinned UDID written by setup_helix.py
        workitem_root = os.environ.get('HELIX_WORKITEM_ROOT')
        if workitem_root:
            pinned = os.path.join(workitem_root, 'sim_udid.txt')
            if os.path.isfile(pinned):
                try:
                    with open(pinned, 'r', encoding='utf-8') as f:
                        udid = f.read().strip()
                    if udid:
                        getLogger().info("Using pinned simulator UDID from %s: %s",
                                         pinned, udid)
                        return udid
                except OSError as e:
                    getLogger().warning("Could not read %s: %s", pinned, e)

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

    @staticmethod
    def delete_simulator(udid):
        """Shutdown and delete a simulator by UDID. Best-effort, never raises.

        Used by post.py to clean up the per-workitem simulator created in
        setup_helix.py so devices don't accumulate across runs on the same
        Helix machine.
        """
        if not udid:
            return
        for cmd in (['xcrun', 'simctl', 'shutdown', udid],
                    ['xcrun', 'simctl', 'delete', udid]):
            try:
                subprocess.run(cmd, capture_output=True, text=True, timeout=60)
            except Exception as e:
                getLogger().warning("Cleanup '%s' failed: %s", ' '.join(cmd), e)

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
            # Uninstall any stale app so first-deploy timing isn't affected
            getLogger().info("Uninstalling any existing app (%s) from physical device: %s", bundle_id, device_id)
            mlaunch = self._resolve_mlaunch()
            self._run_quiet([mlaunch, '--uninstalldevbundleid', bundle_id,
                             '--devname', device_id])
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

        No-op for simulator builds or local runs where MSBuild handles signing
        automatically (EnableCodeSigning is not disabled).
        """
        if not self.is_physical_device:
            return

        from performance.common import runninginlab
        if not runninginlab():
            getLogger().info("Skipping post-build signing (local run — MSBuild handles signing)")
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

        # Find the sign tool — it's pre-installed on Helix Mac machines
        # but may not be on PATH in HelixWorkItem (vs XHarness) runners.
        # Fast path: try direct resolution first, then fall back to running
        # through a login shell (which is how Device Startup's XHarness
        # CustomCommands find it — the login shell has the full PATH).
        sign_cmd = shutil.which('sign')
        if sign_cmd:
            getLogger().info("Found sign tool on PATH: %s", sign_cmd)
            RunCommand([sign_cmd, app_name], verbose=True).run(working_directory=app_dir)
            return

        # Check known Helix machine locations
        for candidate in ['/usr/local/bin/sign',
                          os.path.join(os.environ.get('HELIX_SCRIPT_ROOT', ''), 'sign')]:
            if os.path.isfile(candidate) and os.access(candidate, os.X_OK):
                getLogger().info("Found sign tool at known path: %s", candidate)
                RunCommand([candidate, app_name], verbose=True).run(working_directory=app_dir)
                return

        # Last resort: run through a login shell to get the full PATH.
        # This mirrors how Device Startup's XHarness CustomCommands execute
        # 'sign' — the XHarness runner's shell has the right PATH.
        getLogger().info("sign tool not found on PATH or known locations; "
                         "trying login shell (bash -lc)")
        shell_result = subprocess.run(
            ['bash', '-lc', f'cd "{app_dir}" && sign "{app_name}"'],
            stdout=subprocess.PIPE, stderr=subprocess.STDOUT, text=True,
        )
        if shell_result.stdout:
            getLogger().info("sign output:\n%s", shell_result.stdout)
        if shell_result.returncode != 0:
            # The 'sign' tool only exists on Helix CI machines. For local
            # device runs, MSBuild/Xcode already sign the app with the
            # developer's identity during 'dotnet build', so re-signing is
            # unnecessary.  Warn instead of crashing so local measurement
            # scripts (which set PERFLAB_INLAB=1 for reporting) can proceed.
            getLogger().warning(
                "'sign' tool not found — skipping re-signing. "
                "App should already be signed by MSBuild for local device deployment. "
                "(Tried: shutil.which('sign'), /usr/local/bin/sign, "
                "HELIX_SCRIPT_ROOT/sign, bash -lc 'sign'. "
                "Login shell exit code: %d)",
                shell_result.returncode,
            )
            return
        getLogger().info("Signed %s via login shell successfully", app_name)

    # ── Unified Operations ───────────────────────────────────────────

    def install_app(self, app_bundle_path):
        """Install the app bundle and return wall-clock install time in ms.

        Per .NET iOS team guidance (Rolf), mlaunch is the canonical way to
        deploy on both simulator and physical devices — it matches what the
        IDEs do during F5, handles ad-hoc signing for personal-team device
        deploys, and avoids `xcrun devicectl`'s strict CoreSign requirement
        which fails for unsigned/ad-hoc Debug builds (MIInstallerErrorDomain
        error 13 / "No code signature found").

        Device:    mlaunch --installdev <app> --devname <UDID>
        Simulator: mlaunch --installsim <app> --device :v2:udid=<UDID>
        """
        start = time.time()
        mlaunch = self._resolve_mlaunch()

        if self.is_physical_device:
            cmd = [mlaunch, '--installdev', app_bundle_path,
                   '--devname', self.device_id]
        else:
            cmd = [mlaunch, '--installsim', app_bundle_path,
                   '--device', f':v2:udid={self.device_id}']
        RunCommand(cmd, verbose=True).run()

        elapsed_ms = (time.time() - start) * 1000
        getLogger().info("Install completed in %.1f ms", elapsed_ms)
        return elapsed_ms

    def measure_cold_startup(self, bundle_id):
        """Measure app cold startup time in ms (int).

          - Simulator: ``xcrun simctl launch`` returns the launched PID
            immediately. We measure wall-clock from invocation to PID
            return (the same as what mlaunch internally reports), then
            verify the process is still alive after a short stabilization
            window so we don't report success for a crashed launch.
          - Device:    mlaunch --launchdev (returns immediately with PID)

        Note on the simulator path: an earlier version used
        ``mlaunch --launchsim`` to better mirror the IDE F5 experience,
        but on Apple Silicon Helix queues the simulator went from Booted
        to Shutdown during/after that call (with no diagnostic output
        from mlaunch). ``simctl launch`` is what mlaunch invokes
        internally for the actual launch step, so the measurement is
        equivalent for our purposes.
        """

        if self.is_physical_device:
            return self._measure_device_startup_via_watchdog(bundle_id)

        # ── Simulator ────────────────────────────────────────────────
        # Sanity-check the simulator state before we start the timer so
        # we fail fast with a clear error if the simulator has shut down
        # (e.g. due to a previous workitem cleanup or system pressure).
        self._assert_simulator_booted()

        # Verify the app is actually installed before timing the launch
        # — otherwise an install-registration failure would be reported
        # as a launch failure.
        try:
            container = subprocess.run(
                ['xcrun', 'simctl', 'get_app_container',
                 self.device_id, bundle_id, 'app'],
                capture_output=True, text=True, timeout=15,
            )
            if container.returncode != 0:
                raise RuntimeError(
                    f"App {bundle_id} is not installed in simulator "
                    f"{self.device_id} (simctl get_app_container exit "
                    f"{container.returncode}): {(container.stderr or '').strip()}"
                )
            getLogger().info("App container: %s", (container.stdout or '').strip())
        except subprocess.TimeoutExpired:
            raise RuntimeError(
                f"simctl get_app_container timed out — simulator "
                f"{self.device_id} not responding")

        # Terminate any running instance for a true cold start
        self._run_quiet(['xcrun', 'simctl', 'terminate', self.device_id, bundle_id])
        time.sleep(0.5)

        # `simctl launch` returns immediately with the launched PID.
        # `--terminate-running-process` makes the launch reliably cold
        # even if termination above didn't take effect.
        cmd = ['xcrun', 'simctl', 'launch', '--terminate-running-process',
               self.device_id, bundle_id]
        getLogger().info("$ %s", ' '.join(cmd))
        start = time.time()
        result = subprocess.run(cmd, capture_output=True, text=True, timeout=180)
        elapsed_ms = int((time.time() - start) * 1000)

        if result.returncode != 0:
            # Dump diagnostics so we can tell whether the simulator died
            # or the app failed to launch.
            self._dump_simulator_diagnostics(bundle_id)
            raise subprocess.CalledProcessError(
                result.returncode, cmd, result.stdout, result.stderr)

        # `simctl launch` prints "<bundle_id>: <pid>" on success.
        pid = None
        for token in (result.stdout or '').split():
            if token.isdigit():
                pid = int(token)
                break
        if pid is None:
            getLogger().warning(
                "Could not parse PID from simctl launch output: %r",
                (result.stdout or '').strip())
        else:
            getLogger().info("Launched %s with PID %d", bundle_id, pid)
            # Stabilization check: the app should still be alive 2s
            # later. If not, the launch was a crash, not a real start.
            #
            # iOS Simulator apps run as real macOS processes (sandboxed
            # but in the host's process table — the PID returned by
            # `simctl launch` IS the host PID), so we use host `ps -p`
            # for the check. The simulator's userland does NOT include
            # /bin/ps, so `simctl spawn <UDID> ps` would fail with
            # ENOENT regardless of whether the app is alive.
            time.sleep(2.0)
            check = subprocess.run(
                ['ps', '-p', str(pid), '-o', 'pid='],
                capture_output=True, text=True, timeout=15,
            )
            if check.returncode != 0 or str(pid) not in (check.stdout or ''):
                self._dump_simulator_diagnostics(bundle_id)
                raise RuntimeError(
                    f"App {bundle_id} (PID {pid}) crashed within 2s of "
                    f"launch; host ps -p exit "
                    f"{check.returncode}, output: "
                    f"{(check.stdout or '').strip()!r}")

        getLogger().info("Cold startup: %d ms", elapsed_ms)
        return elapsed_ms

    def _assert_simulator_booted(self):
        """Raise RuntimeError if ``self.device_id`` is not in the Booted state."""
        try:
            result = subprocess.run(
                ['xcrun', 'simctl', 'list', 'devices', '-j'],
                capture_output=True, text=True, timeout=15,
            )
        except subprocess.TimeoutExpired:
            raise RuntimeError("simctl list devices timed out")
        try:
            data = json.loads(result.stdout or '{}')
        except Exception:
            data = {}
        for runtime_devices in (data.get('devices') or {}).values():
            for dev in runtime_devices:
                if dev.get('udid') == self.device_id:
                    state = dev.get('state', '?')
                    if state != 'Booted':
                        raise RuntimeError(
                            f"Simulator {self.device_id} is in state "
                            f"{state!r}, expected 'Booted'. "
                            f"It must have been shut down between "
                            f"create_and_boot_simulator and the measurement.")
                    return
        raise RuntimeError(
            f"Simulator {self.device_id} not found in `simctl list devices`.")

    def _dump_simulator_diagnostics(self, bundle_id):
        """Best-effort diagnostics dump for a failed simulator launch."""
        for cmd in (
            ['xcrun', 'simctl', 'list', 'devices', self.device_id],
            ['xcrun', 'simctl', 'listapps', self.device_id],
            # iOS sim has no /bin/ps; query the host process table for
            # any descendants of the simulator hosting the bundle id.
            ['ps', '-A', '-o', 'pid,ppid,command'],
        ):
            try:
                r = subprocess.run(cmd, capture_output=True, text=True, timeout=15)
                getLogger().error(
                    "$ %s\n--- exit %d ---\n%s\n--- stderr ---\n%s",
                    ' '.join(cmd), r.returncode,
                    (r.stdout or '')[-3000:], (r.stderr or '')[-1500:])
            except Exception as e:
                getLogger().error("Diagnostic %s failed: %s", ' '.join(cmd), e)

    def _measure_device_startup_via_watchdog(self, bundle_id):
        """Measure physical device cold startup using SpringBoard watchdog events.

        During every iOS app launch, SpringBoard emits four watchdog log events:
          1. "Now monitoring resource allowance of 20.00s" — OS starts loading the process
          2. "Stopped monitoring" — app reached main()
          3. "Now monitoring resource allowance of N.NNs" — OS waits for first frame
          4. "Stopped monitoring" — first frame drawn

        Time to Main = event2.timestamp - event1.timestamp
        Time to First Draw = event4.timestamp - event3.timestamp
        Total startup = Time to Main + Time to First Draw

        Requires sudo for `log collect --device`.
        """
        # Give device a moment to settle before launch
        time.sleep(1)

        # Record timestamp before launch for log collection window
        start_ts = time.strftime('%Y-%m-%d %H:%M:%S%z')

        # Launch the app via mlaunch --launchdev (Rolf's guidance — same
        # tool the IDEs use during F5; consistent with --installdev above).
        # mlaunch --launchdev BLOCKS until the app exits (it tunnels stdout/
        # stderr from the device), so run it in a subprocess and terminate
        # after we've collected enough log data. Killing mlaunch closes the
        # tunnel but the app stays running on the device long enough for
        # SpringBoard to emit the watchdog events we need.
        mlaunch = self._resolve_mlaunch()
        launch_cmd = [mlaunch, '--launchdev', self.app_bundle_path,
                      '--devname', self.device_id]
        getLogger().info("$ %s", ' '.join(launch_cmd))
        launch_proc = subprocess.Popen(
            launch_cmd, stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)
        try:
            # Wait for the app to fully start before collecting logs.
            time.sleep(5)
        finally:
            launch_proc.terminate()
            try:
                launch_proc.wait(timeout=5)
            except subprocess.TimeoutExpired:
                launch_proc.kill()
                launch_proc.wait()

        # Collect device logs covering the launch window.
        # Both rm -rf calls use sudo because `sudo log collect` writes the
        # logarchive owned by root; an unprivileged rm cannot recurse into it
        # and `log collect` refuses to overwrite an existing --output path
        # (exit 74), so a stale archive from iteration N would block iteration
        # N+1 from collecting any logs.
        logarchive = os.path.join(tempfile.gettempdir(), 'ioshelper_startup.logarchive')
        self._run_quiet(['sudo', 'rm', '-rf', logarchive])
        collect_cmd = ['sudo', 'log', 'collect', '--device',
                       '--start', start_ts, '--output', logarchive]
        RunCommand(collect_cmd, verbose=True).run()

        # Parse SpringBoard watchdog events for this bundle ID
        show_cmd = ['log', 'show',
                    '--predicate', '(process == "SpringBoard") && (category == "Watchdog")',
                    '--info', '--style', 'ndjson', logarchive]
        show = RunCommand(show_cmd, verbose=True)
        show.run()

        events = []
        for line in show.stdout.splitlines():
            try:
                data = json.loads(line)
                msg = data.get('eventMessage', '')
                if bundle_id not in msg:
                    continue
                if 'Now monitoring resource allowance' in msg or 'Stopped monitoring' in msg:
                    events.append(data)
            except (json.JSONDecodeError, KeyError):
                continue

        if len(events) < 4:
            getLogger().warning("Expected 4 watchdog events, got %d — falling back to wall-clock", len(events))
            # Couldn't parse watchdog events; return -1 to signal invalid measurement
            return -1

        # Sort by timestamp — log show output is not guaranteed chronological
        events.sort(key=lambda evt: evt['timestamp'])

        # Validate expected sequence: monitor → stop → monitor → stop
        expected = ['Now monitoring', 'Stopped monitoring', 'Now monitoring', 'Stopped monitoring']
        for i, keyword in enumerate(expected):
            if keyword not in events[i].get('eventMessage', ''):
                getLogger().warning("Unexpected watchdog event sequence at index %d: %s", i, events[i].get('eventMessage', ''))
                return -1

        # Parse timestamps: "2026-04-13 20:36:19.836430+0200"
        def parse_ts(evt):
            return datetime.strptime(evt['timestamp'], '%Y-%m-%d %H:%M:%S.%f%z')

        t_main_start = parse_ts(events[0])
        t_main_end = parse_ts(events[1])
        t_draw_start = parse_ts(events[2])
        t_draw_end = parse_ts(events[3])

        time_to_main_ms = int((t_main_end - t_main_start).total_seconds() * 1000)
        time_to_draw_ms = int((t_draw_end - t_draw_start).total_seconds() * 1000)
        total_ms = time_to_main_ms + time_to_draw_ms

        getLogger().info("Cold startup: %d ms (Time to Main: %d ms, Time to First Draw: %d ms)",
                         total_ms, time_to_main_ms, time_to_draw_ms)

        # Clean up logarchive (sudo: log collect ran as root, so the tree is root-owned)
        self._run_quiet(['sudo', 'rm', '-rf', logarchive])

        return total_ms

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

    def find_app_bundle(self, build_output_dir, app_name, configuration='Debug', is_physical=False):
        """Find the .app bundle in the build output directory.

        Searches for: bin/<config>/net*/<rid>/<app>.app
        Returns the absolute path. Raises FileNotFoundError if not found.
        """
        rid_patterns = ['ios-arm64'] if is_physical else ['iossimulator-*', 'ios-arm64']
        for rid_pattern in rid_patterns:
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
