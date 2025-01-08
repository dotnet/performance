import re
import time
from performance.common import RunCommand
from logging import exception, getLogger
from shared import const
from shared.util import xharnesscommand

class AndroidHelper:
    def __init__(self):
        self.activityname = None
        self.adbpath = None
        self.xadb = []
        self.packagename = None
        self.startappcommand = None
        self.stopappcommand = None
        self.screenwasoff = False
        self.startwindowanimationscale = None
        self.starttransitionanimationscale = None
        self.startanimatordurationscale = None
        self.startscreenofftimeout = None

    def setup_device(self, packagename: str, packagepath: str, animationsdisabled: bool, forcewaitstart: bool = True):
        runSplitRegex = r":\s(.+)" 
        self.screenwasoff = False
        self.packagename = packagename
        # cmdline = xharnesscommand() + ['android', 'state', '--adb']
        # adb = RunCommand(cmdline, verbose=True)
        # adb.run()
        self.xadb = xharnesscommand() + ['android', 'adb', '--']

        # Try calling xharness with stdout=None and stderr=None to hopefully bypass the hang
        getLogger().info("Clearing xharness stdout and stderr to avoid hang")
        cmdline = self.xadb + [
            'shell',
            'echo', 'Hello World'
        ]
        RunCommand(cmdline, verbose=False).run_without_out_err()
        getLogger().info("Running echo command to clear stdout and stderr")

        # Do not remove, XHarness install seems to fail without an adb command called before the xharness command
        getLogger().info("Preparing ADB")
        #self.adbpath = adb.stdout.strip()
        cmdline = self.xadb + [
            'shell',
            'wm',
            'size'
        ]
        RunCommand(cmdline, verbose=True).run()

        # Get animation values
        getLogger().info("Getting Values we will need set specifically")
        cmdline = self.xadb + [
            'shell', 'settings', 'get', 'global', 'window_animation_scale'
        ]
        window_animation_scale_cmd = RunCommand(cmdline, verbose=True)
        window_animation_scale_cmd.run()
        self.startwindowanimationscale = window_animation_scale_cmd.stdout.strip()
        cmdline = self.xadb + [
            'shell', 'settings', 'get', 'global', 'transition_animation_scale'
        ]
        transition_animation_scale_cmd = RunCommand(cmdline, verbose=True)
        transition_animation_scale_cmd.run()
        self.starttransitionanimationscale = transition_animation_scale_cmd.stdout.strip()
        cmdline = self.xadb + [
            'shell', 'settings', 'get', 'global', 'animator_duration_scale'
        ]
        animator_duration_scale_cmd = RunCommand(cmdline, verbose=True)
        animator_duration_scale_cmd.run()
        self.startanimatordurationscale = animator_duration_scale_cmd.stdout.strip()
        cmdline = self.xadb + [
            'shell', 'settings', 'get', 'system', 'screen_off_timeout'
        ]
        screen_off_timeout_cmd = RunCommand(cmdline, verbose=True)
        screen_off_timeout_cmd.run()
        self.startscreenofftimeout = screen_off_timeout_cmd.stdout.strip()
        getLogger().info(f"Retrieved values window {self.startwindowanimationscale}, transition {self.starttransitionanimationscale}, animator {self.startanimatordurationscale}, screen timeout {self.startscreenofftimeout}")

        # Make sure animations are set to 1 or disabled
        getLogger().info("Setting needed values")
        if animationsdisabled:
            animationValue = 0
        else:
            animationValue = 1
        minimumTimeoutValue = 2 * 60 * 1000 # milliseconds
        cmdline = self.xadb + [
            'shell', 'settings', 'put', 'global', 'window_animation_scale', str(animationValue)
        ]
        RunCommand(cmdline, verbose=True).run()
        cmdline = self.xadb + [
            'shell', 'settings', 'put', 'global', 'transition_animation_scale', str(animationValue)
        ]
        RunCommand(cmdline, verbose=True).run()
        cmdline = self.xadb + [
            'shell', 'settings', 'put', 'global', 'animator_duration_scale', str(animationValue)
        ]
        RunCommand(cmdline, verbose=True).run()
        cmdline = self.xadb + [
            'shell', 'settings', 'put', 'system', 'screen_off_timeout', str(minimumTimeoutValue)
        ]
        if minimumTimeoutValue > int(screen_off_timeout_cmd.stdout.strip()):
            getLogger().info("Screen off value is lower than minimum time, setting to higher time")
            RunCommand(cmdline, verbose=True).run()

        # Check for success
        getLogger().info("Getting animation values to verify it worked")
        cmdline = self.xadb + [
            'shell', 'settings', 'get', 'global', 'window_animation_scale'
        ]
        windowSetValue = RunCommand(cmdline, verbose=True)
        windowSetValue.run()
        cmdline = self.xadb + [
            'shell', 'settings', 'get', 'global', 'transition_animation_scale'
        ]
        transitionSetValue = RunCommand(cmdline, verbose=True)
        transitionSetValue.run()
        cmdline = self.xadb + [
            'shell', 'settings', 'get', 'global', 'animator_duration_scale'
        ]
        animatorSetValue = RunCommand(cmdline, verbose=True)
        animatorSetValue.run()
        if int(windowSetValue.stdout.strip()) != animationValue or int(transitionSetValue.stdout.strip()) != animationValue or int(animatorSetValue.stdout.strip()) != animationValue:
            # Setting the values didn't work, error out
            getLogger().exception(f"Failed to set animation values to {animationValue}.")
            raise Exception(f"Failed to set animation values to {animationValue}.")
        else:
            getLogger().info(f"Animation values successfully set to {animationValue}.")

        self.stopappcommand = self.xadb + [
            'shell',
            'am',
            'force-stop',
            self.packagename
        ]

        installCmd = xharnesscommand() + [
            'android',
            'install',
            '--app', packagepath,
            '--package-name',
            self.packagename,
            '-o',
            const.TRACEDIR,
            '-v'
        ]
        RunCommand(installCmd, verbose=True).run()

        getLogger().info("Completed install, running shell.")
        cmdline = self.xadb + [
            'shell',
            f'cmd package resolve-activity --brief {self.packagename} | tail -n 1'
        ]
        getActivity = RunCommand(cmdline, verbose=True)
        getActivity.run()
        getLogger().info(f"Target Activity {getActivity.stdout}")

        # More setup stuff
        checkScreenOnCmd = self.xadb + [
            'shell',
            f'dumpsys input_method | grep mInteractive'
        ]
        checkScreenOn = RunCommand(checkScreenOnCmd, verbose=True)
        checkScreenOn.run()

        keyInputCmd = self.xadb + [
            'shell',
            'input',
            'keyevent'
        ]

        if "mInteractive=false" in checkScreenOn.stdout: 
            # Turn on the screen to make interactive and see if it worked
            getLogger().info("Screen was off, turning on.")
            self.screenwasoff = True
            RunCommand(keyInputCmd + ['26'], verbose=True).run() # Press the power key
            RunCommand(keyInputCmd + ['82'], verbose=True).run() # Unlock the screen with menu key (only works if it is not a password lock)

            checkScreenOn = RunCommand(checkScreenOnCmd, verbose=True)
            checkScreenOn.run()
            if "mInteractive=false" in checkScreenOn.stdout:
                getLogger().exception("Failed to make screen interactive.")
                raise Exception("Failed to make screen interactive.")

        # Actual testing some run stuff
        getLogger().info("Test run to check if permissions are needed")
        self.activityname = getActivity.stdout.strip()

        # -W in the start command waits for the app to finish initial draw.
        self.startappcommand = self.xadb + [
            'shell',
            'am',
            'start-activity',
            '-W',
            '-n',
            self.activityname
        ]

        testRun = RunCommand(self.startappcommand, verbose=True)
        testRun.run()
        testRunStats = re.findall(runSplitRegex, testRun.stdout) # Split results saving value (List: Starting, Status, LaunchState, Activity, TotalTime, WaitTime) 
        getLogger().info(f"Test run activity: {testRunStats[3]}")
        time.sleep(10) # Add delay to ensure app is fully installed and give it some time to settle
        
        RunCommand(self.stopappcommand, verbose=True).run()
        if "com.google.android.permissioncontroller" in testRunStats[3]:
            # On perm screen, use the buttons to close it. it will stay away until the app is reinstalled
            RunCommand(keyInputCmd + ['22'], verbose=True).run() # Select next button
            time.sleep(1)
            RunCommand(keyInputCmd + ['22'], verbose=True).run() # Select next button
            time.sleep(1)
            RunCommand(keyInputCmd + ['66'], verbose=True).run() # Press enter to close main perm screen
            time.sleep(1)
            RunCommand(keyInputCmd + ['22'], verbose=True).run() # Select next button
            time.sleep(1)
            RunCommand(keyInputCmd + ['66'], verbose=True).run() # Press enter to close out of second screen
            time.sleep(1)

            # Check to make sure it worked
            testRun = RunCommand(self.startappcommand, verbose=True)
            testRun.run()
            testRunStats = re.findall(runSplitRegex, testRun.stdout) 
            getLogger().info(f"Test run activity: {testRunStats[3]}")
            RunCommand(self.stopappcommand, verbose=True).run() 
            
            if "com.google.android.permissioncontroller" in testRunStats[3]:
                getLogger().exception("Failed to get past permission screen, run locally to see if enough next button presses were used.")
                raise Exception("Failed to get past permission screen, run locally to see if enough next button presses were used.")
            
        self.startappcommand = self.xadb + [
            'shell',
            'am',
            'start-activity'
        ]
        if forcewaitstart:
            self.startappcommand.append('-W')

        self.startappcommand += [
            '-n',
            self.activityname
        ]

    def close_device(self):
        keyInputCmd = self.xadb + [
            'shell',
            'input',
            'keyevent'
        ]
                
        getLogger().info("Stopping App for uninstall")
        RunCommand(self.stopappcommand, verbose=True).run()
                
        getLogger().info("Uninstalling app")
        uninstallAppCmd = xharnesscommand() + [
            'android',
            'uninstall',
            '--package-name',
            self.packagename
        ]
        RunCommand(uninstallAppCmd, verbose=True).run()

        
        keyInputCmd = self.xadb + [
            'shell',
            'input',
            'keyevent'
        ]

        # Reset animation values 
        getLogger().info("Resetting animation values to pretest values")
        cmdline = self.xadb + [
            'shell', 'settings', 'put', 'global', 'window_animation_scale', self.startwindowanimationscale
        ]
        RunCommand(cmdline, verbose=True).run()
        cmdline = self.xadb + [
            'shell', 'settings', 'put', 'global', 'transition_animation_scale', self.starttransitionanimationscale
        ]
        RunCommand(cmdline, verbose=True).run()
        cmdline = self.xadb + [
            'shell', 'settings', 'put', 'global', 'animator_duration_scale', self.startanimatordurationscale
        ]
        RunCommand(cmdline, verbose=True).run()
        cmdline = self.xadb + [
            'shell', 'settings', 'put', 'system', 'screen_off_timeout', self.startscreenofftimeout
        ]
        RunCommand(cmdline, verbose=True).run()

        if self.screenwasoff:
            RunCommand(keyInputCmd + ['26'], verbose=True).run() # Turn the screen back off