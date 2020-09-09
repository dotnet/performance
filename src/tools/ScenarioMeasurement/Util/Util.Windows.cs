// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;

namespace ScenarioMeasurement
{
    partial class Util
    {
        [DllImport("User32.Dll")]
        private static extern long SetCursorPos(int x, int y);

        static partial void SetCursorPosition(int x, int y)
        {
            SetCursorPos(x, y);
        }
    }
}