using System.Runtime.InteropServices;


namespace ScenarioMeasurement;

partial class Util
{
    [DllImport("User32.Dll")]
    private static extern long SetCursorPos(int x, int y);

    static partial void SetCursorPosition(int x, int y)
    {
        SetCursorPos(x, y);
    }
}