using System;
using System.Runtime.InteropServices;

namespace ScenarioMeasurement
{

    public class Util
    {
        // [DllImport("User32.Dll")]
        // private static extern long SetCursorPos(int x, int y);


        public static void Init()
        {
            // SetCursorPos(0, 0);
        }

        public static void TakeScreenshot()
        {

        }

        public static bool IsWindows()
        {
            return Environment.OSVersion.Platform == PlatformID.Win32NT;
        }
        
    }
}
