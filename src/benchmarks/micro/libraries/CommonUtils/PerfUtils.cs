// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Linq;

namespace System
{
    public static class PerfUtils
    {
        /// <summary>
        /// Helper method to create a string containing a number of 
        /// characters equal to the specified length
        /// </summary>
        public static string CreateString(int length)
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

        /// <summary>
        /// Helper method to create a string containing random alphanumeric
        /// characters equal to the specified length
        /// </summary>
        public static string CreateRandomString(int length, string alphabet = "abcdefghijklmnopqrstuvwxyz0123456789", int seed = 42)
        {
            Random random = new(seed);  // use the given seed, to make the benchmarks repeatable
            return new string(Enumerable.Repeat(alphabet, length).Select(s => s[random.Next(s.Length)]).ToArray());
        }
    }
}
