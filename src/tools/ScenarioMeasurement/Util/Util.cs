using System;


namespace ScenarioMeasurement;


public partial class Util
{

    public static void Init()
    {
        SetCursorPosition(0, 0);
    }

    static partial void SetCursorPosition(int x, int y);

    public static void TakeScreenshot()
    {

    }

    public static bool IsWindows()
    {
        return Environment.OSVersion.Platform == PlatformID.Win32NT;
    }
    
}
