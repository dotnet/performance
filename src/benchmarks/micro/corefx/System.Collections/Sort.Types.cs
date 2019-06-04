﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Collections
{
    public readonly struct IntStruct : IComparable<IntStruct>
    {
        private readonly int _value;

        public IntStruct(int value) => _value = value;

        public int CompareTo(IntStruct other) => _value.CompareTo(other._value);
    }

    public class IntClass : IComparable<IntClass>
    {
        private readonly int _value;

        public IntClass(int value) => _value = value;

        public int CompareTo(IntClass other) => _value.CompareTo(other._value);
    }

    public readonly struct BigStruct : IComparable<BigStruct>
    {
        private readonly long _long;
        private readonly int _int0;
        private readonly int _int1;
        private readonly short _short0;
        private readonly short _short1;
        private readonly short _short2;
        private readonly short _short3;
        private readonly double _double; 

        public BigStruct(int value)
        {
            _long = value;
            _int0 = value;
            _int1 = value;
            _short0 = (short)value;
            _short1 = (short)value;
            _short2 = (short)value;
            _short3 = (short)value;
            _double = value;
        }

        public int CompareTo(BigStruct other) => _int1.CompareTo(other._int1);
    }
}
