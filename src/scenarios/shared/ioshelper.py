import json
import os
import re
import time
import glob
import subprocess
from performance.common import RunCommand
from logging import getLogger


class iOSHelper:
    def __init__(self):
        self.bundle_id = None
        self.device_id = None
        self.app_bundle_path = None
        self.is_physical_device = False

    @staticmethod
    def detect_connected_device():
        """Detect a connected physical iOS device and return its UDID.

        Checks IOS_DEVICE_UDID environment variable first. If not set,
        auto-detects using 'xcrun devicectl list devices' and returns the
        UDID of the first connected device.

        Returns the UDID string, or None if no device is found.
        """
        # Prefer explicit env var
        udid = os.environ.get('IOS_DEVICE_UDID', '').strip()
        if udid:
            getLogger().info("Using IOS_DEVICE_UDID from environment: %s", udid)
            return udid

        # Auto-detect via devicectl (Xcode 15+)
        getLogger().info("Auto-detecting connected iOS device via 'xcrun devicectl list devices'...")
        try:
            result = subprocess.run(
                ['xcrun', 'devicectl', 'list', 'devices', '--json-output', '/dev/stdout'],
                capture_output=True, text=True, timeout=30
            )
            if result.returncode != 0:
                getLogger().warning("devicectl list devices failed (exit %d): %s",
                                    result.returncode, result.stderr)
                return None

            data = json.loads(result.stdout)
            # devicectl JSON output has "result.devices" array with "identifier" (UDID)
            # and "connectionProperties.transportType" to filter for USB-connected devices
            devices = data.get('result', {}).get('devices', [])
            for device in devices:
                conn = device.get('connectionProperties', {})
                transport = conn.get('transportType', '')
                state = device.get('deviceProperties', {}).get('developerModeStatus', '')
                name = device.get('deviceProperties', {}).get('name', 'unknown')
                device_udid = device.get('identifier', '')

                # Only consider locally-connected (wired/WiFi) devices, not
                # paired watches or other peripherals
                if transport in ('wired', 'localNetwork', 'wifi') and device_udid:
                    getLogger().info("Found connected device: %s (UDID: %s, transport: %s, devMode: %s)",
                                    name, device_udid, transport, state)
                    return device_udid

            getLogger().warning("No connected iOS devices found in devicectl output")
            return None

        except subprocess.TimeoutExpired:
            getLogger().warning("devicectl list devices timed out")
            return None
        except (json.JSONDecodeError, KeyError) as e:
            getLogger().warning("Failed to parse devicectl JSON output: %s", e)
            # Fall back to text parsing of non-JSON output
            return iOSHelper._detect_device_fallback()

    @staticmethod
    def _detect_device_fallback():
        """Fallback device detection using text output from devicectl.

        Used when JSON parsing fails (e.g., older Xcode versions that don't
        support --json-output).
        """
        try:
            result = subprocess.run(
                ['xcrun', 'devicectl', 'list', 'devices'],
                capture_output=True, text=True, timeout=30
            )
            if result.returncode != 0:
                return None

            # Look for lines with a UUID pattern (device UDID)
            # Example line: "  PERFIOS-01  00008101-001A09223E08001E  ..."
            for line in (result.stdout or '').splitlines():
                match = re.search(r'([0-9A-Fa-f]{8}-[0-9A-Fa-f]{16}|[0-9A-Fa-f]{25,40})', line)
                if match:
                    udid = match.group(1)
                    getLogger().info("Fallback detection found device UDID: %s (from: %s)",
                                    udid, line.strip())
                    return udid
            return None
        except Exception:
            return None

    def setup_simulator(self, bundle_id, app_bundle_path, device_id='booted'):
        """Boot the iOS simulator and install the app bundle."""
        self.bundle_id = bundle_id
        self.device_id = device_id
        self.app_bundle_path = app_bundle_path

        if device_id != 'booted':
            getLogger().info("Booting iOS simulator: %s", device_id)
            result = subprocess.run(
                ['xcrun', 'simctl', 'boot', device_id],
                capture_output=True, text=True
            )
            if result.returncode != 0:
                if 'already booted' in result.stderr.lower():
                    getLogger().info("Simulator %s is already booted.", device_id)
                else:
                    raise subprocess.CalledProcessError(result.returncode, result.args, result.stdout, result.stderr)
        else:
            getLogger().info("Using already-booted simulator (device_id='booted')")

        # Install app
        getLogger().info("Installing app bundle: %s", app_bundle_path)
        RunCommand(['xcrun', 'simctl', 'install', device_id, app_bundle_path], verbose=True).run()
        getLogger().info("Completed install.")

    def setup_physical_device(self, bundle_id, app_bundle_path, device_id):
        """Set up a physical iOS device for testing.

        Installs the app bundle on the connected physical device using devicectl.
        Requires Xcode 15+ for the 'xcrun devicectl' toolchain.
        """
        self.bundle_id = bundle_id
        self.device_id = device_id
        self.app_bundle_path = app_bundle_path
        self.is_physical_device = True

        getLogger().info("Installing app bundle on physical device %s: %s", device_id, app_bundle_path)
        RunCommand(['xcrun', 'devicectl', 'device', 'install', 'app',
                     '--device', device_id, app_bundle_path], verbose=True).run()
        getLogger().info("Completed install on physical device.")

    def install_app(self, app_bundle_path):
        """Install the app bundle and return install time in milliseconds."""
        getLogger().info("Installing app bundle: %s", app_bundle_path)
        start = time.time()
        RunCommand(['xcrun', 'simctl', 'install', self.device_id, app_bundle_path], verbose=True).run()
        elapsed_ms = (time.time() - start) * 1000
        getLogger().info("Install completed in %.1f ms", elapsed_ms)
        return elapsed_ms

    def install_app_physical(self, app_bundle_path):
        """Install the app bundle on a physical device and return install time in milliseconds."""
        getLogger().info("Installing app bundle on physical device: %s", app_bundle_path)
        start = time.time()
        RunCommand(['xcrun', 'devicectl', 'device', 'install', 'app',
                     '--device', self.device_id, app_bundle_path], verbose=True).run()
        elapsed_ms = (time.time() - start) * 1000
        getLogger().info("Install completed in %.1f ms", elapsed_ms)
        return elapsed_ms

    def measure_cold_startup(self, bundle_id):
        """Measure app cold startup time in milliseconds.

        Terminates any running instance, waits briefly, then launches the app.
        Returns wall-clock time for the launch command in milliseconds as int.
        """
        # Terminate any running instance (ignore errors)
        getLogger().info("Terminating app for cold startup: %s", bundle_id)
        try:
            RunCommand(['xcrun', 'simctl', 'terminate', self.device_id, bundle_id], verbose=True).run()
        except subprocess.CalledProcessError:
            getLogger().debug("Terminate returned error (app may not be running), ignoring.")

        time.sleep(0.5)

        # Launch and measure wall-clock time
        getLogger().info("Launching app: %s", bundle_id)
        start = time.time()
        RunCommand(['xcrun', 'simctl', 'launch', self.device_id, bundle_id], verbose=True).run()
        elapsed_ms = (time.time() - start) * 1000
        getLogger().info("Cold startup time: %d ms", int(elapsed_ms))
        return int(elapsed_ms)

    def measure_cold_startup_physical(self, bundle_id):
        """Measure app cold startup time on a physical device in milliseconds.

        Terminates any running instance, waits briefly, then launches the app via devicectl.
        Returns wall-clock time for the launch command in milliseconds as int.
        """
        getLogger().info("Terminating app for cold startup on physical device: %s", bundle_id)
        try:
            RunCommand(['xcrun', 'devicectl', 'device', 'process', 'terminate',
                         '--device', self.device_id, '--bundle-id', bundle_id], verbose=True).run()
        except subprocess.CalledProcessError:
            getLogger().debug("Terminate returned error (app may not be running), ignoring.")

        time.sleep(0.5)

        getLogger().info("Launching app on physical device: %s", bundle_id)
        start = time.time()
        RunCommand(['xcrun', 'devicectl', 'device', 'process', 'launch',
                     '--device', self.device_id, bundle_id], verbose=True).run()
        elapsed_ms = (time.time() - start) * 1000
        getLogger().info("Cold startup time: %d ms", int(elapsed_ms))
        return int(elapsed_ms)

    def uninstall_app(self, bundle_id):
        """Uninstall the app from the simulator."""
        getLogger().info("Uninstalling app: %s", bundle_id)
        RunCommand(['xcrun', 'simctl', 'uninstall', self.device_id, bundle_id], verbose=True).run()

    def uninstall_app_physical(self, bundle_id):
        """Uninstall the app from a physical device."""
        getLogger().info("Uninstalling app from physical device: %s", bundle_id)
        try:
            RunCommand(['xcrun', 'devicectl', 'device', 'uninstall', 'app',
                         '--device', self.device_id, bundle_id], verbose=True).run()
        except subprocess.CalledProcessError:
            getLogger().debug("Uninstall returned error (app may not be installed), ignoring.")

    def terminate_app(self, bundle_id):
        """Terminate the app on the simulator (ignore errors)."""
        getLogger().info("Terminating app: %s", bundle_id)
        try:
            RunCommand(['xcrun', 'simctl', 'terminate', self.device_id, bundle_id], verbose=True).run()
        except subprocess.CalledProcessError:
            getLogger().debug("Terminate returned error (app may not be running), ignoring.")

    def terminate_app_physical(self, bundle_id):
        """Terminate the app on a physical device (ignore errors)."""
        getLogger().info("Terminating app on physical device: %s", bundle_id)
        try:
            RunCommand(['xcrun', 'devicectl', 'device', 'process', 'terminate',
                         '--device', self.device_id, '--bundle-id', bundle_id], verbose=True).run()
        except subprocess.CalledProcessError:
            getLogger().debug("Terminate returned error (app may not be running), ignoring.")

    def close_simulator(self, skip_uninstall=False):
        """Clean up the simulator session.

        Terminates and uninstalls the app unless skip_uninstall is True.
        Does NOT shutdown the simulator.
        """
        if not skip_uninstall:
            getLogger().info("Stopping app for uninstall")
            self.terminate_app(self.bundle_id)
            self.uninstall_app(self.bundle_id)

    def close_physical_device(self, skip_uninstall=False):
        """Clean up the physical device session.

        Terminates and uninstalls the app unless skip_uninstall is True.
        """
        if not skip_uninstall:
            getLogger().info("Stopping app for uninstall on physical device")
            self.terminate_app_physical(self.bundle_id)
            self.uninstall_app_physical(self.bundle_id)

    def cleanup(self, skip_uninstall=False):
        """Clean up the device session (simulator or physical).

        Dispatches to the appropriate cleanup method based on device type.
        """
        if self.is_physical_device:
            self.close_physical_device(skip_uninstall=skip_uninstall)
        else:
            self.close_simulator(skip_uninstall=skip_uninstall)

    def find_app_bundle(self, build_output_dir, app_name, configuration='Debug'):
        """Find the .app bundle in the build output directory.

        Searches for the typical path pattern:
            bin/<configuration>/net*/iossimulator-*/<AppName>.app
        Also searches for physical device builds:
            bin/<configuration>/net*/ios-arm64/<AppName>.app

        Returns the absolute path to the .app bundle.
        Raises FileNotFoundError if no bundle is found.
        """
        # Try simulator path first, then physical device path
        for rid_pattern in ['iossimulator-*', 'ios-arm64']:
            pattern = os.path.join(build_output_dir, 'bin', configuration, 'net*', rid_pattern, f'{app_name}.app')
            matches = glob.glob(pattern)
            if matches:
                if len(matches) > 1:
                    getLogger().warning("Found multiple app bundles matching pattern %s: %s. Using first match.", pattern, matches)
                app_path = os.path.abspath(matches[0])
                getLogger().info("Found app bundle: %s", app_path)
                return app_path

        raise FileNotFoundError(
            f"Could not find .app bundle in {build_output_dir}/bin/{configuration}/net*/(iossimulator-*|ios-arm64)/{app_name}.app"
        )
