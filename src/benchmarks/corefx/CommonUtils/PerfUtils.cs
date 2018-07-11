// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Runtime.CompilerServices;

namespace System
{
    public class PerfUtils
    {
        /// <summary>
        /// Helper method to create a string containing a number of 
        /// characters equal to the specified length
        /// </summary>
        public string CreateString(int length)
        {
            char[] str = new char[length];

            for (int i = 0; i < str.Length; i++)
            {
                // Add path separator so folders aren't too long.
                if (i % 20 == 0)
                {
                    str[i] = Path.DirectorySeparatorChar;
                }
                else
                {
                    str[i] = 'a';
                }
            }

            return new string(str);
        }
    }
}
