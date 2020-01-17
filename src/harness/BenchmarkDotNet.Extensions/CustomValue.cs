// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BenchmarkDotNet.Extensions
{
    public struct CustomValue : IEquatable<CustomValue>, IComparable<CustomValue>
    {
        private readonly int _value;

        public CustomValue(int value)
        {
            _value = value;
        }

        public bool Equals(CustomValue other)
        {
            return _value == other._value;
        }
        
        public int CompareTo(CustomValue other)
        {
            return _value - other._value;
        }

        public override int GetHashCode()
        {
            return _value;
        }
    }
}