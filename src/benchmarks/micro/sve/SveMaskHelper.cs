// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Numerics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics.Arm;

namespace SveBenchmarks
{
    // Compatibility shim for CreateWhileLessThanMask API rename.
    // The runtime renamed these methods from bit-width suffixes (e.g. CreateWhileLessThanMask8Bit)
    // to type-name suffixes (e.g. CreateWhileLessThanMaskByte). Because the SDK ref assemblies and
    // the corerun may be from different builds, we detect which names exist at runtime via
    // reflection and cache delegates. The one-time reflection cost is negligible for benchmarks.
    internal static class SveMaskHelper
    {
        private static readonly Type[] s_intInt = new[] { typeof(int), typeof(int) };
        private static readonly Type[] s_longLong = new[] { typeof(long), typeof(long) };

        private static readonly Func<int, int, Vector<byte>> s_maskByte = InitMaskByte();
        private static readonly Func<int, int, Vector<ushort>> s_maskUInt16 = InitMaskUInt16();
        private static readonly Func<int, int, Vector<short>> s_maskInt16 = InitMaskInt16();
        private static readonly Func<int, int, Vector<uint>> s_maskUInt32 = InitMaskUInt32();
        private static readonly Func<int, int, Vector<int>> s_maskInt32 = InitMaskInt32();
        private static readonly Func<int, int, Vector<float>> s_maskSingle = InitMaskSingle();
        private static readonly Func<int, int, Vector<ulong>> s_maskUInt64 = InitMaskUInt64();
        private static readonly Func<long, long, Vector<uint>> s_maskUInt32Long = InitMaskUInt32Long();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static Vector<byte> CreateWhileLessThanMaskByte(int left, int right) => s_maskByte(left, right);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static Vector<ushort> CreateWhileLessThanMaskUInt16(int left, int right) => s_maskUInt16(left, right);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static Vector<short> CreateWhileLessThanMaskInt16(int left, int right) => s_maskInt16(left, right);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static Vector<uint> CreateWhileLessThanMaskUInt32(int left, int right) => s_maskUInt32(left, right);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static Vector<int> CreateWhileLessThanMaskInt32(int left, int right) => s_maskInt32(left, right);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static Vector<float> CreateWhileLessThanMaskSingle(int left, int right) => s_maskSingle(left, right);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static Vector<ulong> CreateWhileLessThanMaskUInt64(int left, int right) => s_maskUInt64(left, right);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static Vector<uint> CreateWhileLessThanMaskUInt32(long left, long right) => s_maskUInt32Long(left, right);

        // Helpers to resolve method by new name first, then old name.
        private static MethodInfo Resolve(string newName, string oldName, Type[] paramTypes)
        {
            return typeof(Sve).GetMethod(newName, BindingFlags.Public | BindingFlags.Static, null, paramTypes, null)
                ?? typeof(Sve).GetMethod(oldName, BindingFlags.Public | BindingFlags.Static, null, paramTypes, null)
                ?? throw new PlatformNotSupportedException($"Neither '{newName}' nor '{oldName}' found on Sve.");
        }

        private static TDelegate Bind<TDelegate>(string newName, string oldName, Type[] paramTypes) where TDelegate : Delegate
            => (TDelegate)Delegate.CreateDelegate(typeof(TDelegate), Resolve(newName, oldName, paramTypes));

        // Direct delegate binding — return type matches between old and new names.
        private static Func<int, int, Vector<byte>> InitMaskByte()
            => Bind<Func<int, int, Vector<byte>>>("CreateWhileLessThanMaskByte", "CreateWhileLessThanMask8Bit", s_intInt);

        private static Func<int, int, Vector<ushort>> InitMaskUInt16()
            => Bind<Func<int, int, Vector<ushort>>>("CreateWhileLessThanMaskUInt16", "CreateWhileLessThanMask16Bit", s_intInt);

        private static Func<int, int, Vector<uint>> InitMaskUInt32()
            => Bind<Func<int, int, Vector<uint>>>("CreateWhileLessThanMaskUInt32", "CreateWhileLessThanMask32Bit", s_intInt);

        private static Func<int, int, Vector<ulong>> InitMaskUInt64()
            => Bind<Func<int, int, Vector<ulong>>>("CreateWhileLessThanMaskUInt64", "CreateWhileLessThanMask64Bit", s_intInt);

        private static Func<long, long, Vector<uint>> InitMaskUInt32Long()
            => Bind<Func<long, long, Vector<uint>>>("CreateWhileLessThanMaskUInt32", "CreateWhileLessThanMask32Bit", s_longLong);

        // Cast-wrapping delegates — old name returns unsigned type, but caller needs signed/float reinterpret.
        private static Func<int, int, Vector<short>> InitMaskInt16()
        {
            var m = typeof(Sve).GetMethod("CreateWhileLessThanMaskInt16", BindingFlags.Public | BindingFlags.Static, null, s_intInt, null);
            if (m != null) return (Func<int, int, Vector<short>>)Delegate.CreateDelegate(typeof(Func<int, int, Vector<short>>), m);
            var old = Bind<Func<int, int, Vector<ushort>>>("CreateWhileLessThanMaskUInt16", "CreateWhileLessThanMask16Bit", s_intInt);
            return (l, r) => { var v = old(l, r); return Unsafe.As<Vector<ushort>, Vector<short>>(ref v); };
        }

        private static Func<int, int, Vector<int>> InitMaskInt32()
        {
            var m = typeof(Sve).GetMethod("CreateWhileLessThanMaskInt32", BindingFlags.Public | BindingFlags.Static, null, s_intInt, null);
            if (m != null) return (Func<int, int, Vector<int>>)Delegate.CreateDelegate(typeof(Func<int, int, Vector<int>>), m);
            var old = Bind<Func<int, int, Vector<uint>>>("CreateWhileLessThanMaskUInt32", "CreateWhileLessThanMask32Bit", s_intInt);
            return (l, r) => { var v = old(l, r); return Unsafe.As<Vector<uint>, Vector<int>>(ref v); };
        }

        private static Func<int, int, Vector<float>> InitMaskSingle()
        {
            var m = typeof(Sve).GetMethod("CreateWhileLessThanMaskSingle", BindingFlags.Public | BindingFlags.Static, null, s_intInt, null);
            if (m != null) return (Func<int, int, Vector<float>>)Delegate.CreateDelegate(typeof(Func<int, int, Vector<float>>), m);
            var old = Bind<Func<int, int, Vector<uint>>>("CreateWhileLessThanMaskUInt32", "CreateWhileLessThanMask32Bit", s_intInt);
            return (l, r) => { var v = old(l, r); return Unsafe.As<Vector<uint>, Vector<float>>(ref v); };
        }
    }
}
