// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using BenchmarkDotNet.Attributes;
using MicroBenchmarks;

namespace System.Tests
{
    [BenchmarkCategory(Categories.CoreFX)]
    public class Perf_Enum
    {
        #region Enums
        [Flags]
        public enum Colors
        {
            Red = 0x1,
            Orange = 0x2,
            Yellow = 0x4,
            Green = 0x8,
            Blue = 0x10
        }

        public enum ByteEnum : byte
        {
            Min = byte.MinValue,
            One = 1,
            Two = 2,
            Max = byte.MaxValue
        }

        public enum SByteEnum : sbyte
        {
            Min = sbyte.MinValue,
            One = 1,
            Two = 2,
            Max = sbyte.MaxValue
        }

        public enum UInt16Enum : ushort
        {
            Min = ushort.MinValue,
            One = 1,
            Two = 2,
            Max = ushort.MaxValue
        }

        public enum Int16Enum : short
        {
            Min = short.MinValue,
            One = 1,
            Two = 2,
            Max = short.MaxValue
        }

        public enum UInt32Enum : uint
        {
            Min = uint.MinValue,
            One = 1,
            Two = 2,
            Max = uint.MaxValue
        }

        public enum Int32Enum : int
        {
            Min = int.MinValue,
            One = 1,
            Two = 2,
            Max = int.MaxValue
        }

        public enum UInt64Enum : ulong
        {
            Min = ulong.MinValue,
            One = 1,
            Two = 2,
            Max = ulong.MaxValue
        }

        public enum Int64Enum : long
        {
            Min = long.MinValue,
            One = 1,
            Two = 2,
            Max = long.MaxValue
        }

        public enum BigContiguousEnum
        {
            A,
            B,
            C,
            D,
            E,
            F,
            G,
            H,
            I,
            J,
            K,
            L,
            M,
            N,
            O,
            P,
            Q,
            R,
            S,
            T,
            U,
            V,
            W,
            X,
            Y,
            Z
        }

        public enum BigNonContiguousEnum
        {
            A = 0,
            B = 2,
            C = 4,
            D = 6,
            E = 8,
            F = 10,
            G = 12,
            H = 14,
            I = 16,
            J = 18,
            K = 20,
            L = 22,
            M = 24,
            N = 26,
            O = 28,
            P = 30,
            Q = 32,
            R = 34,
            S = 36,
            T = 38,
            U = 40,
            V = 42,
            W = 44,
            X = 46,
            Y = 48,
            Z = 50
        }
        #endregion

        [Benchmark]
        //[Arguments(Colors.Green | Colors.Red, Colors.Green)] // BenchmarkDotNet Issue #1020
        //public bool HasFlagTrue(Enum value, Enum flag) => value.HasFlag(flag);
        public bool HasFlagTrue() => (Colors.Green | Colors.Red).HasFlag(Colors.Green);

        [Benchmark]
        //[Arguments(Colors.Green | Colors.Red, Colors.Blue)] // BenchmarkDotNet Issue #1020
        //public bool HasFlagFalse(Enum value, Enum flag) => value.HasFlag(flag);
        public bool HasFlagFalse() => (Colors.Green | Colors.Red).HasFlag(Colors.Blue);

        #region IsDefined
        [Benchmark]
        [Arguments(typeof(BigContiguousEnum), BigContiguousEnum.G)]
        [Arguments(typeof(BigNonContiguousEnum), BigNonContiguousEnum.G)]
        [Arguments(typeof(Int32Enum), Int32Enum.Max)]
        public bool IsDefinedEnumTrue(Type enumType, object value) => Enum.IsDefined(enumType, value);

        [Benchmark]
        [Arguments(typeof(BigContiguousEnum), (int)BigContiguousEnum.G)]
        [Arguments(typeof(BigNonContiguousEnum), (int)BigNonContiguousEnum.G)]
        [Arguments(typeof(Int32Enum), (int)Int32Enum.Max)]
        public bool IsDefinedUnderlyingTrue(Type enumType, object value) => Enum.IsDefined(enumType, value);

        [Benchmark]
        [Arguments(typeof(BigContiguousEnum), "G")]
        [Arguments(typeof(BigNonContiguousEnum), "G")]
        [Arguments(typeof(Int32Enum), "Max")]
        public bool IsDefinedStringTrue(Type enumType, object value) => Enum.IsDefined(enumType, value);

        [Benchmark]
        //[Arguments(typeof(BigContiguousEnum), (BigContiguousEnum)(-1))] // BenchmarkDotNet Issue #1020
        //[Arguments(typeof(BigNonContiguousEnum), (BigNonContiguousEnum)1)]
        //[Arguments(typeof(Int32Enum), (Int32Enum)3)]
        //public bool IsDefinedEnumFalse(Type enumType, object value) => Enum.IsDefined(enumType, value);
        [Arguments(typeof(BigContiguousEnum))]
        [Arguments(typeof(BigNonContiguousEnum))]
        [Arguments(typeof(Int32Enum))]
        public bool IsDefinedEnumFalse(Type enumType) => Enum.IsDefined(enumType, (BigContiguousEnum)(-1));

        [Benchmark]
        [Arguments(typeof(BigContiguousEnum), 26)]
        [Arguments(typeof(BigNonContiguousEnum), 1)]
        [Arguments(typeof(Int32Enum), 3)]
        public bool IsDefinedUnderlyingFalse(Type enumType, object value) => Enum.IsDefined(enumType, value);

        [Benchmark]
        [Arguments(typeof(BigContiguousEnum), "GG")]
        [Arguments(typeof(BigNonContiguousEnum), "GG")]
        [Arguments(typeof(Int32Enum), "Three")]
        public bool IsDefinedStringFalse(Type enumType, object value) => Enum.IsDefined(enumType, value);
        #endregion

        [Benchmark]
        [Arguments(typeof(Int32Enum))]
        public Type GetUnderlyingType(Type enumType) => Enum.GetUnderlyingType(enumType);

        [Benchmark]
        [Arguments(typeof(BigContiguousEnum))]
        [Arguments(typeof(Int32Enum))]
        public Array GetValues(Type enumType) => Enum.GetValues(enumType);

        [Benchmark]
        [Arguments(typeof(BigContiguousEnum))]
        [Arguments(typeof(Int32Enum))]
        public string[] GetNames(Type enumType) => Enum.GetNames(enumType);

        #region GetName
        [Benchmark]
        [Arguments(typeof(BigContiguousEnum), BigContiguousEnum.D)]
        public string GetNameEnumDefined(Type enumType, object value) => Enum.GetName(enumType, value);

        [Benchmark]
        [Arguments(typeof(BigContiguousEnum), 3)]
        public string GetNameUnderlyingDefined(Type enumType, object value) => Enum.GetName(enumType, value);

        [Benchmark]
        //[Arguments(typeof(BigContiguousEnum), (BigContiguousEnum)(-1))] // BenchmarkDotNet Issue #1020
        //public string GetNameEnumUndefined(Type enumType, object value) => Enum.GetName(enumType, value);
        [Arguments(typeof(BigContiguousEnum))]
        public string GetNameEnumUndefined(Type enumType) => Enum.GetName(enumType, (BigContiguousEnum)(-1));

        [Benchmark]
        [Arguments(typeof(BigContiguousEnum), -1)]
        public string GetNameUnderlyingUndefined(Type enumType, object value) => Enum.GetName(enumType, value);
        #endregion

        #region ToObject
        [Benchmark]
        [Arguments(typeof(BigContiguousEnum), 7)]
        public object ToObject(Type enumType, object value) => Enum.ToObject(enumType, value);

        [Benchmark]
        [Arguments(typeof(BigContiguousEnum), 7)]
        public object ToObjectInt32(Type enumType, int value) => Enum.ToObject(enumType, value);
        #endregion

        #region Equals
        [Benchmark]
        [Arguments(BigContiguousEnum.G, BigContiguousEnum.G)]
        public bool EqualsTrue(Enum value, object obj) => value.Equals(obj);

        [Benchmark]
        [Arguments(BigContiguousEnum.G, BigContiguousEnum.H)]
        public bool EqualsFalse(Enum value, object obj) => value.Equals(obj);

        [Benchmark]
        [Arguments(BigContiguousEnum.G)]
        public bool EqualsNullFalse(Enum value) => value.Equals(null);

        [Benchmark]
        [Arguments(BigContiguousEnum.G, (int)BigContiguousEnum.G)]
        public bool EqualsUnderlyingFalse(Enum value, object obj) => value.Equals(obj);
        #endregion

        [Benchmark]
        [Arguments(BigContiguousEnum.N)]
        public int GetHashCode(Enum value) => value.GetHashCode();

        #region ToString
        [Benchmark]
        [Arguments(BigContiguousEnum.Z)]
        public string ToStringDefined(Enum value) => value.ToString();

        [Benchmark]
        //[Arguments((BigContiguousEnum)(-1))] // BenchmarkDotNet Issue #1020
        //public string ToStringUndefined(Enum value) => value.ToString();
        public string ToStringUndefined() => ((BigContiguousEnum)(-1)).ToString();

        [Benchmark]
        //[Arguments(Colors.Blue | Colors.Green | Colors.Orange | Colors.Red | Colors.Yellow)] // BenchmarkDotNet Issue #1020
        //public string ToStringFlags(Enum value) => value.ToString();
        public string ToStringFlags() => (Colors.Blue | Colors.Green | Colors.Orange | Colors.Red | Colors.Yellow).ToString();

        [Benchmark]
        [Arguments(BigContiguousEnum.L, "D")]
        [Arguments(BigContiguousEnum.L, "X")]
        [Arguments(BigContiguousEnum.L, "G")]
        [Arguments(BigContiguousEnum.L, "F")]
        public string ToStringFormat(Enum value, string format) => value.ToString(format);
        #endregion

        [Benchmark]
        [Arguments(BigContiguousEnum.C, BigContiguousEnum.E)]
        public int CompareToEnum(Enum value, object target) => value.CompareTo(target);

        [Benchmark]
        [Arguments(BigContiguousEnum.C)]
        public int CompareToNull(Enum value) => value.CompareTo(null);

        [Benchmark]
        [Arguments(Int32Enum.One)]
        public TypeCode GetTypeCode(Enum value) => value.GetTypeCode();

        #region Parsing
        [Benchmark]
        [Arguments(typeof(Colors), "Red, Orange, Yellow, Green, Blue")]
        public object ParseFlagsNonGeneric(Type enumType, string text) => Enum.Parse(enumType, text);

        [Benchmark]
        [Arguments("Red, Orange, Yellow, Green, Blue")]
        public Colors ParseFlagsGeneric(string text) => Enum.Parse<Colors>(text);

        [Benchmark]
        [Arguments(typeof(ByteEnum), "Two")]
        [Arguments(typeof(SByteEnum), "Two")]
        [Arguments(typeof(UInt16Enum), "Two")]
        [Arguments(typeof(Int16Enum), "Two")]
        [Arguments(typeof(UInt32Enum), "Two")]
        [Arguments(typeof(Int32Enum), "Two")]
        [Arguments(typeof(UInt64Enum), "Two")]
        [Arguments(typeof(Int64Enum), "Two")]
        [Arguments(typeof(BigContiguousEnum), "Z")]
        public object ParseNameNonGeneric(Type enumType, string value) => Enum.Parse(enumType, value);

        [Benchmark]
        [Arguments(typeof(ByteEnum))]
        [Arguments(typeof(SByteEnum))]
        [Arguments(typeof(UInt16Enum))]
        [Arguments(typeof(Int16Enum))]
        [Arguments(typeof(UInt32Enum))]
        [Arguments(typeof(Int32Enum))]
        [Arguments(typeof(UInt64Enum))]
        [Arguments(typeof(Int64Enum))]
        public object ParseValueNonGeneric(Type enumType) => Enum.Parse(enumType, "2");

        [Benchmark]
        public ByteEnum ParseNameGenericByte() => Enum.Parse<ByteEnum>("Two");

        [Benchmark]
        public SByteEnum ParseNameGenericSByte() => Enum.Parse<SByteEnum>("Two");

        [Benchmark]
        public UInt16Enum ParseNameGenericUInt16() => Enum.Parse<UInt16Enum>("Two");

        [Benchmark]
        public Int16Enum ParseNameGenericInt16() => Enum.Parse<Int16Enum>("Two");

        [Benchmark]
        public UInt32Enum ParseNameGenericUInt32() => Enum.Parse<UInt32Enum>("Two");

        [Benchmark]
        public Int32Enum ParseNameGenericInt32() => Enum.Parse<Int32Enum>("Two");

        [Benchmark]
        public UInt64Enum ParseNameGenericUInt64() => Enum.Parse<UInt64Enum>("Two");

        [Benchmark]
        public Int64Enum ParseNameGenericInt64() => Enum.Parse<Int64Enum>("Two");

        [Benchmark]
        public BigContiguousEnum ParseNameGenericBigContiguous() => Enum.Parse<BigContiguousEnum>("Z");

        [Benchmark]
        public ByteEnum ParseValueGenericByte() => Enum.Parse<ByteEnum>("2");

        [Benchmark]
        public SByteEnum ParseValueGenericSByte() => Enum.Parse<SByteEnum>("2");

        [Benchmark]
        public UInt16Enum ParseValueGenericUInt16() => Enum.Parse<UInt16Enum>("2");

        [Benchmark]
        public Int16Enum ParseValueGenericInt16() => Enum.Parse<Int16Enum>("2");

        [Benchmark]
        public UInt32Enum ParseValueGenericUInt32() => Enum.Parse<UInt32Enum>("2");

        [Benchmark]
        public Int32Enum ParseValueGenericInt32() => Enum.Parse<Int32Enum>("2");

        [Benchmark]
        public UInt64Enum ParseValueGenericUInt64() => Enum.Parse<UInt64Enum>("2");

        [Benchmark]
        public Int64Enum ParseValueGenericInt64() => Enum.Parse<Int64Enum>("2");

        [Benchmark]
        public bool TryParseOverflowGeneric() => Enum.TryParse<DayOfWeek>("9223372036854775807", out _); // long.MaxValue

        [Benchmark]
        public bool TryParseMissingGeneric() => Enum.TryParse<DayOfWeek>("Three", out _);

        [Benchmark]
        public bool TryParseOverflowNonGeneric() => Enum.TryParse(typeof(DayOfWeek), "9223372036854775807", out _); // long.MaxValue

        [Benchmark]
        public bool TryParseMissingNonGeneric() => Enum.TryParse(typeof(DayOfWeek), "Three", out _);
        #endregion
    }
}
