
# Android Startup Test

## Prereqs

- Ensure `dotnet` is installed and available with the `dotnet` command. [Dotnet Download](https://dotnet.microsoft.com/en-us/download) or [Daily Dotnet Download](https://github.com/dotnet/sdk/blob/main/documentation/package-table.md)
- Ensure `xharness` is installed and available with the `xharness` command. The current version in use can be found in the `eng/performance/maui_scenarios_android.proj` file at line 7 (under the tag `MicrosoftDotNetXHarnessCLIVersion`), although any recent version should work. [XHarness Install Instructions](https://github.com/dotnet/xharness?tab=readme-ov-file#installation-and-usage).
- Have an Android app APK available for testing.

## Steps

1. Initialize the environment (note the . for bash):

    ```sh
    cd src/scenarios
    . ./init.sh  # or .\init.ps1 on Windows
    cd ../
    ```

2. Navigate to the `helloandroid` scenario directory:

    ```sh
    cd ./scenarios/helloandroid
    ```

3. Copy the APK into the `helloandroid` directory.
4. Run the test:

    ```sh
    python test.py devicestartup --device-type android --package-path <path to apk (e.g. .)>/<apkname>.apk --package-name <apk package name> [--disable-animations]
    ```

## Notes

- Example commands and additional logic can be found in the `maui_scenarios_android.proj` and `runner.py` files in the `performance` repository.
