// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace ScenarioMeasurement
{
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
}
