
# Android Startup Test

## Prereqs

- Ensure `python` is installed and available. Any currently supported `python` 3.* version should work. Downloads are available at https://www.python.org/downloads/.
- Ensure `dotnet` is installed and available with the `dotnet` command for easy xharness installation. Any supported .NET Core version should work. [Dotnet Download](https://dotnet.microsoft.com/en-us/download) or [Daily Dotnet Download](https://github.com/dotnet/sdk/blob/main/documentation/package-table.md).
- Ensure `xharness` is installed and available with the `xharness` command. The current version in use can be found in the `eng/performance/maui_scenarios_android.proj` file at line 7 (under the tag `MicrosoftDotNetXHarnessCLIVersion`), although any recent version should work. [XHarness Install Instructions](https://github.com/dotnet/xharness?tab=readme-ov-file#installation-and-usage).
- Have an Android app APK available for testing.
- Have an Android Device (with developer mode enabled) or emulator connected to computer, and viewable with `xharness android device` or `xharness android adb -- devices -l`.

## Steps

1. Initialize the environment (note the . for bash):

    ```sh
    cd src/scenarios
    . ./init.sh  # or `.\init.ps1` on Windows. Can specify custom dotnet install with -dotnetdir <dir>, but dotnet install should not impact Android Startup testing itself.
    ```

2. Navigate to the `helloandroid` scenario directory:

    ```sh
    cd helloandroid
    ```

3. Copy the APK into the `helloandroid` directory.
4. Run the test:

    ```sh
    python/(py -3 on Linux) test.py devicestartup --device-type android --package-path <path to apk (e.g. .)>/<apkname>.apk --package-name <apk package name> [--disable-animations] [--use-fully-drawn-time --fully-drawn-extra-delay <delay in sec> (**see below for note**)]
    ```

5. Read the output:

    During the running of the test you will see the loop of the activity being started to get the startup times.
    Once the testing is completed, you will see output similar to the following:

    ```txt
    [2025/01/29 11:15:44][INFO] Found Value (ms): 713
    [2025/01/29 11:15:44][INFO] Found Value (ms): 715
    [2025/01/29 11:15:44][INFO] Found Value (ms): 728
    [2025/01/29 11:15:44][INFO] Found Value (ms): 716
    [2025/01/29 11:15:44][INFO] Found Value (ms): 715
    [2025/01/29 11:15:44][INFO] Found Value (ms): 734
    [2025/01/29 11:15:44][INFO] Found Value (ms): 716
    [2025/01/29 11:15:44][INFO] Found Value (ms): 718
    [2025/01/29 11:15:44][INFO] Found Value (ms): 713
    [2025/01/29 11:15:44][INFO] Found Value (ms): 706
    [2025/01/29 11:15:44][INFO] Device Startup - Maui Android Default NoAnimation
    [2025/01/29 11:15:44][INFO] Metric          |Average        |Min            |Max
    [2025/01/29 11:15:44][INFO] ----------------|---------------|---------------|---------------
    [2025/01/29 11:15:44][INFO] Generic Startup |717.400 ms     |706.000 ms     |734.000 ms
    ```

    The Found Value's are the individual test run startup times with the overall stats at the bottom. The stats provided include the following startup stats: average, minimum, and maximum times.

## Notes

- Specific example command such as when using the runtime android example app: `python test.py devicestartup --device-type android --package-path HelloAndroid.apk --package-name net.dot.HelloAndroid`.
- Other example commands and additional logic can be found in the `maui_scenarios_android.proj` and `runner.py` files in the `performance` repository.
- If using `[--use-fully-drawn-time --fully-drawn-extra-delay <delay in sec>]` arguments, the Android app must have reportFullyDrawn() called on a ComponentActivity. Reference: https://developer.android.com/topic/performance/vitals/launch-time#retrieve-TTFD.
