import os
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
        """
        self.bundle_id = bundle_id
        self.device_id = device_id
        self.app_bundle_path = app_bundle_path

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

    def terminate_app(self, bundle_id):
        """Terminate the app on the simulator (ignore errors)."""
        getLogger().info("Terminating app: %s", bundle_id)
        try:
            RunCommand(['xcrun', 'simctl', 'terminate', self.device_id, bundle_id], verbose=True).run()
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
