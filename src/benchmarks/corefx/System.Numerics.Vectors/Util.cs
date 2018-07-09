// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.CompilerServices;

namespace System.Numerics.Tests
{
    internal static class Util
    {
        private static Random s_random = new Random();

        internal static T[] GenerateRandomValues<T>(int numValues, int min = 1, int max = 100) where T : struct
        {
            T[] values = new T[numValues];
            for (int g = 0; g < numValues; g++)
            {
                values[g] = GenerateSingleValue<T>(min, max);
            }

            return values;
        }

        private static T GenerateSingleValue<T>(int min = 1, int max = 100) where T : struct
        {
            var randomRange = s_random.Next(min, max);
            T value = Unsafe.As<int, T>(ref randomRange);
            return value;
        }
    }
}
