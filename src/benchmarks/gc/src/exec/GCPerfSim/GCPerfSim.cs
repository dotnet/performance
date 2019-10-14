// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

/*
 * This is a simulator for managed memory behaviors to test the GC performance. It has the following aspects:

1) It provides general things you'd want to do when testing perf, eg, # of threads, running time, allocated bytes.

2) It includes different building blocks to simulate different relevant behaviors for GC, eg, survival rates,
   flat or pointer-rich object graphs, different SOH/LOH ratio.
   
The idea is when we see a relevant behavior we want this simulator to be able to generate that behavior but we
also want to have a dial (ie, how intense that behavior is) and mix it with other behaviors.

Each run writes out a log named pid-output.txt that includes some general info such as parameters used and execution time.



Right now it's fairly simple - during initialization it allocates an array that holds onto byte 
arrays that are placed on SOH and LOH based on the alloc ratio given; and during steady state it 
allocates byte arrays and survives based on survival intervals given.

It has the following characteristics:

It has a very flat object graph and very simple lifetime. It's a simple test that's good for things that
mostly just care about # of bytes allocated/promoted like BGC scheduling.

It allows you to specify the following commandline args. Notes on commandline args -

1) they are NOT case sensitive;

2) always specify a number as the value for an arg. For boolean values use 1 for true and 0 for false.

3) if you want to still specify an arg but want to use the default (Why? Maybe you want to run this with a lot of different
configurations and it's easier to compare if you have all the configs specified), just specify something that can't be parsed
as an integer. For example you could specify "defArgName". That way you could replace all defArgName with a value if you needed.

-testKind/-tk: g_testKind
    Either "time" (default) or "highsurvival"
    For "highsurvival", -totalMins should be set, -sohSurvInterval and -lohSurvInterval should not

-threadCount/-tc: g_threadCount
allocating thread count (usually I use half of the # of CPUs on the machine, this is just to reduce the OS scheduler effect 
so we can test the GC effect better)

-lohAllocRatio/-lohar: g_lohAllocRatio
LOH alloc ratio (this controls the bytes we allocate on LOH out of all allocations we do)
It's in in per thousands (not percents! even though in the output it says %). So if it's 5, that means 
5‰ of the allocations will be on LOH.

-lohAllocInterval:
    One out of this many objects will be large. Can't use combined with -lohAllocRatio.

-totalLiveGB/-tlgb: g_totalLiveBytesGB
this is the total live data size in GB

-totalAllocGB/-tagb: g_totalAllocBytesGB
this is the total allocated size in GB, instead of accepting an arg like # of iterations where you don't really know what 
an iteration does, we use the allocated bytes to indicate how much work the threads do.

-totalMins/-tm: g_totalMinutesToRun
time to run in minutes (for things that need long term effects like scheduling you want to run for 
a while, eg, a few hours to see how stable it is)

Note that if neither -totalAllocMB nor -totalMins is specified, it will run for the default for -totalMins.
If both are specified, we take whichever one that's met first. 

-sohSizeRange/-sohsr: g_sohAllocLow, g_sohAllocHigh
eg: -sohSizeRange 100-4000 will set g_sohAllocLow and g_sohAllocHigh to 100 and 4000
we allocate SOH that's randomly chosen between this range.

-lohSizeRange/-lohsr: g_lohAllocLow, g_lohAllocHigh
we allocate LOH that's randomly chosen between this range.
    And if it is used, the calculation of lohAllocInterval is wrong.

-sohSurvInterval/-sohsi: g_sohSurvInterval
meaning every Nth SOH object allocated will survive. This is something we will consider changing to survival rate
later. When the allocated objects are of similiar sizes the surv rate is 1/g_sohSurvInterval but we may not want them
to all be similiar sizes.

-lohSurvInterval/-lohsi: g_lohSurvInterval
meaning every Nth LOH object allocated will survive. 

Note that -sohSurvInterval/-lohSurvInterval are only applicable for steady state, during initialization everything
survives.

-sohPinningInterval/-sohpi: g_sohPinningInterval
meaning every Nth SOH object survived will be pinned. 

-lohPinningInterval/-lohpi: g_lohPinningInterval
meaning every Nth LOH object survived will be pinned. 

-allocType/-at: g_allocType
What kind of objects are we allocating? Current supported types: 
0 means SimpleItem - a byte array (implemented by the Item class)
1 means ReferenceItem - contains refs and can form linked list (implemented by the ReferenceItemWithSize class)

-handleTest - NOT IMPLEMENTED other than pinned handles. Should write some interesting cases for weak handles.

-lohPauseMeasure/-lohpm: g_lohPauseMeasure
measure the time it takes to do a LOH allocation. When turned on the top 10 longest pauses will be included in the log file.
TODO: The longest pauses are interesting but we should also include all pauses by pause buckets.

-endException/-ee: g_endException
induces an exception at the end so you can do some post mortem debugging.

The default for these args are specified in "Default parameters".

---

Other things worth noting:

1) There's also a lohPauseMeasure var that you could make into a config - for now I just always measure pauses for LOH 
   allocs (since it was used for measuring LOH alloc pause wrt BGC sweep).

2) At the end of the test I do an EmptyWorkingSet - the reason for this is when I run the test in a loop if I don't 
   empty the WS for an iteration it's common to observe that it heavily affects the beginning of the next iteration.


For perf runs I usually run with GC internal logging on which means I'd want to get the last (in memory) part of the log 
at the end. So this provides a config that make it convenient for that purpose by intentionally inducing an exception at 
the end so you can get the last part of the logging by setting this

   for your post mortem debugging in the registry: 
   under HKLM\Software\Microsoft\Windows NT\CurrentVersion\AeDebug 
   you can add a reg value called Debugger of REG_SZ type and it should be
   "d:\debuggers\amd64\windbg" -p %ld -e %ld -c ".jdinfo 0x%p; !DumpGCLog c:\temp;q"
   (obviously make sure the directory for windbg is correct for the machine you run the test on and
   replace c:\temp with whatever directory that you want to put the last part of the log in)

This is by default off and can be turned on via the -endException/-ee config.

**********************************************************************************************************************

When working on this please KEEP THE FOLLOWING IN MIND:

1) Do not use anything fancier than needed. In other words, use very simple code in this project. I am not against 
   using cool/rich lang/framework features normally but for this purpose we want something that's as easy to reason about 
   as possible, eg, using things like linq is forbidden because you don't know how much allocations/survivals it 
   does and even if you do understand perfectly (which is doubtful) it might change and it's a burden for other 
   people who need to work on this. 
   
2) Clearly document your intentions for each building block you write. This is important for others because a building
   block is supposed to be easy to use so in general another person shouldn't need to understand how the code works
   in order to use it. For example, if a building block is supposed to allocate X bytes with Y links (where the user
   specifies X and Y) it should say that in the comments.

3) Only add new files if there's a good reason to. Do NOT create tons of little files.
*/

#define PRINT_ITER_INFO

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Reflection;

static class Util
{
    public static void AlwaysAssert(bool condition, string? message = null)
    {
        if (!condition)
            throw new Exception(message);
    }

    private const ulong BYTES_IN_KB = 1024;
    private const ulong BYTES_IN_MB = 1024 * BYTES_IN_KB;
    private const ulong BYTES_IN_GB = 1024 * BYTES_IN_MB;

    public static double BytesToGB(ulong bytes) =>
        ((double)bytes) / BYTES_IN_GB;

    public static ulong GBToBytes(double GB) =>
        (ulong) Math.Round(GB * BYTES_IN_GB);

    public static double BytesToMB(ulong bytes) =>
        ((double) bytes) / BYTES_IN_MB;

    public static ulong MBToBytes(double MB) =>
        (ulong) Math.Round(MB * BYTES_IN_MB);

    public static ulong Mean(ulong a, ulong b) =>
        (a + b) / 2;

    public static bool AboutEquals(double a, double b)
    {
        if (b == 0)
            return a == 0;
        else
            return Math.Abs((a / b) - 1.0) < 0.05;
    }

    public static void AssertAboutEqual(ulong a, ulong b)
    {
        if (!AboutEquals((double)a, (double)b))
        {
            throw new Exception($"Values are not close: {a}, {b}");
        }
    }

    public static T NonNull<T>(T? value) where T : class
    {
        if (value == null)
        {
            throw new NullReferenceException();
        }
        else
        {
            return value;
        }
    }
    
    public static T NonNull<T>(T? value) where T : struct
    {
        if (value == null)
        {
            throw new NullReferenceException();
        }
        else
        {
            return value.Value;
        }
    }

    unsafe static uint GetPointerSize() => (uint) sizeof(IntPtr);

    public static readonly uint POINTER_SIZE = GetPointerSize();
    public static ulong ArraySize(ITypeWithPayload?[] a) =>
        (((ulong)a.Length) + 3) * POINTER_SIZE;

    public static bool isNth(uint interval, ulong count) =>
        interval != 0 && (count % interval) == 0;
}

sealed class Rand
{
    /* Generate Random numbers
     */
    private uint x = 0;

    private uint GetRand()
    {
        unchecked
        {
            x = (314159269 * x + 278281) & 0x7FFFFFFF;
        }
        return x;
    }

    // obtain random number in the range 0 .. r-1
    public uint GetRand(uint r) =>
        (uint)(((ulong)GetRand() * r) >> 31);

    public uint GetRand(uint low, uint high) =>
        low + GetRand(high - low);

    public uint GetRand(SizeRange range) =>
        GetRand(range.low, range.high);

    public double GetFloat() =>
        (double)GetRand() / (double)0x7FFFFFFF;
};

interface ITypeWithPayload
{
    byte[] GetPayload();
    ulong TotalSize { get; }
    void Free();
}

enum TestKind
{
    time,
    highsurvival,
}

enum ItemType
{
    SimpleItem = 0,
    ReferenceItem = 1
};

enum ItemState
{
    // there isn't a handle associated with this object.
    NoHandle = 0,
    Pinned = 1,
    Strong = 2,
    WeakShort = 3,
    WeakLong = 4
};

class Item : ITypeWithPayload
{
#if DEBUG
    public static long NumConstructed = 0;
    public static long NumFreed = 0;
#endif

    public byte[]? payload; // Only null if this item has been freed
    public ItemState state;
    public GCHandle h;

    // 3 for the byte[] overhead, 1 for state, 1 for handle
    static readonly uint Overhead = (3 + 2) * Util.POINTER_SIZE;

    //TODO: isWeakLong never used
    public static Item New(uint size, bool isPinned, bool isFinalizable, bool isWeakLong=false)
    {
        if (isFinalizable)
        {
            throw new Exception("TODO");
        }
        return new Item(size, isPinned, isWeakLong);
    }

    private Item(uint size, bool isPinned, bool isWeakLong)
    {
#if DEBUG
        Interlocked.Increment(ref NumConstructed);
#endif

        uint baseSize = Overhead;
        if (size <= baseSize)
        {
            Console.WriteLine("allocating objects <= {0} is not supported for the Item class", size);
            throw new InvalidOperationException("Item class does not support allocating an object of this size");
        }
        uint payloadSize = size - baseSize;
        payload = new byte[payloadSize];

        // We only support these 3 states right now.
        state = (isPinned ? ItemState.Pinned : (isWeakLong ? ItemState.WeakLong : ItemState.NoHandle));

        // We can consider supporting multiple handles pointing to the same object. For now 
        // I am only doing at most one handle per item.
        if (isPinned || isWeakLong)
        {
            h = GCHandle.Alloc(payload, isPinned ? GCHandleType.Pinned : GCHandleType.WeakTrackResurrection);
        }

        Debug.Assert(TotalSize == size);
    }

    public ulong TotalSize => Overhead + (ulong)Util.NonNull(payload).Length;

    public void Free()
    {
#if DEBUG
        Interlocked.Increment(ref NumFreed);

        switch (state)
        {
            case ItemState.NoHandle:
                Util.AlwaysAssert(!h.IsAllocated);
                break;
            case ItemState.Pinned:
            case ItemState.WeakLong:
                Util.AlwaysAssert(h.IsAllocated);
                break;
            case ItemState.Strong:
            case ItemState.WeakShort:
            default:
                Util.AlwaysAssert(false); // these are unused
                break;
        }
#endif

        Debug.Assert((h.IsAllocated) == (state != ItemState.NoHandle)); // Note: this means the enum isn't useful..
        if (state != ItemState.NoHandle)
        {
            Debug.Assert(h.IsAllocated);
            //Console.WriteLine("freeing handle to byte[{0}]", payload.Length);
            h.Free();
        }

        Util.AlwaysAssert(!h.IsAllocated);

        payload = null;
    }

    public byte[] GetPayload() =>
        Util.NonNull(payload);
};

// This just contains a byte array to take up space
// and since it contains a ref it will exercise mark stack.
class SimpleRefPayLoad
{
#if DEBUG
    public static long NumPinned = 0;
    public static long NumUnpinned = 0;
#endif

    public byte[] payload;
    GCHandle handle;

    // Object header is 2 words.
    // pointer to 'payload' and handle are both 1 word.
    // 'payload' itself will have an overhead of 3 words.
    public static readonly uint Overhead = (2 + 1 + 1 + 3) * Util.POINTER_SIZE;

    public SimpleRefPayLoad(uint size, bool isPinned)
    {
        uint sizePayload = size - Overhead;
        payload = new byte[sizePayload];
        Debug.Assert(OwnSize == size);

        if (isPinned)
        {
            handle = GCHandle.Alloc(payload, GCHandleType.Pinned);
#if DEBUG
            Interlocked.Increment(ref NumPinned);
#endif
        }
    }

    public void Free()
    {
        if (handle.IsAllocated)
        {
            handle.Free();
#if DEBUG
            Interlocked.Increment(ref NumUnpinned);
#endif
        }
    }

    public ulong OwnSize => ((ulong)payload.Length) + Overhead;
}

enum ReferenceItemOperation
{
    NewWithExistingList = 0,
    NewWithNewList = 1,
    MultipleNew = 2,
    MaxOperation = 3
};

// ReferenceItem is structured this way so we can point to other
// ReferenceItemWithSize objets on decommand and record how much 
// memory it's holding alive.
abstract class ReferenceItemWithSize : ITypeWithPayload
{
#if DEBUG
    public static long NumConstructed = 0;
    public static long NumFreed = 0;
#endif

    // The size includes indirect children too.

    // The way we link these together is a linked list.
    // level indicates how many nodes are on the list pointed
    // to by next.
    // We could traverse the list to figure out sizeTotal and level,
    // but I'm keeping them so it's readily available. The list could
    // get very long.
    private SimpleRefPayLoad? payload; // null only if this has been freed
    private ReferenceItemWithSize? next; // Note: ReferenceItemWithSize owns its 'next' -- should be the only reference to that
    public ulong TotalSize { get; private set; }

    // 2 words for object header, then fields in order. Assuming no padding as fields are all large.
    // Node 'SimpleRefPayload' handles its own overhead internally.
    static readonly uint Overhead = 2 * Util.POINTER_SIZE + 2 * Util.POINTER_SIZE + sizeof(ulong);

    public static ReferenceItemWithSize New(uint size, bool isPinned, bool isFinalizable)
    {
        // Can't use conditional expression as these are two different classes
        if (isFinalizable)
        {
            return new ReferenceItemWithSizeFinalizable(size, isPinned);
        }
        else
        {
            return new ReferenceItemWithSizeNonFinalizable(size, isPinned);
        }

    }

    protected ReferenceItemWithSize(uint size, bool isPinned)
    {
        Debug.Assert(size >= Overhead + SimpleRefPayLoad.Overhead);
#if DEBUG
        Interlocked.Increment(ref NumConstructed);
#endif
        uint sizePayload = size - Overhead;
        payload = new SimpleRefPayLoad(sizePayload, isPinned: isPinned);
        Debug.Assert(OwnSize == size);
        TotalSize = size;
    }

    public void Free()
    {
#if DEBUG
        Interlocked.Increment(ref NumFreed);
#endif
        // Should not have already been freed, so payload should be non-null
        Util.NonNull(payload).Free();
        payload = null;
        next?.Free();
        next = null;
    }

    public byte[] GetPayload() => Util.NonNull(payload).payload;

    private ulong OwnSize => Util.NonNull(payload).OwnSize + Overhead;

    // NOTE: This adds the item on to the end of anything currently referenced.
    public void AddToEndOfList(ReferenceItemWithSize refItem)
    {
        if (next == null)
        {
            next = refItem;
        }
        else
        {
            next.AddToEndOfList(Util.NonNull(refItem));
        }
        TotalSize += refItem.TotalSize;

#if DEBUG
        ulong check = 0;
        for (ReferenceItemWithSize? r = this; r != null; r = r.next)
            check += r.OwnSize;
        Debug.Assert(check == TotalSize);
#endif
    }

    class ReferenceItemWithSizeFinalizable : ReferenceItemWithSize
    {
        public static long NumCreatedWithFinalizers = 0;
        public static long NumFinalized = 0;

        public ReferenceItemWithSizeFinalizable(uint size, bool isPinned)
            : base(size, isPinned)
        {
            Interlocked.Increment(ref NumCreatedWithFinalizers);
        }

        ~ReferenceItemWithSizeFinalizable()
        {
            Interlocked.Increment(ref NumFinalized);
        }
    }

    class ReferenceItemWithSizeNonFinalizable : ReferenceItemWithSize
    {
        public ReferenceItemWithSizeNonFinalizable(uint size, bool isPinned)
            : base(size, isPinned) { }
    }
}


readonly struct SizeRange
{
    public readonly uint low;
    public readonly uint high;

    public SizeRange(uint low, uint high)
    {
        this.low = low;
        this.high = high;
    }

    public ulong Mean => Util.Mean(low, high);

    public override string ToString() =>
        $"{low}-{high}";
}

readonly struct BucketSpec
{
    public readonly SizeRange sizeRange;
    public readonly uint survInterval;
    // Note: pinInterval and finalizableInterval only affect surviving objects
    public readonly uint pinInterval;
    public readonly uint finalizableInterval;
    // If we have buckets with weights of 2 and 1, we'll allocate 2 objects from the first bucket, then 1 from the next.
    public readonly uint weight;
    public BucketSpec(SizeRange sizeRange, uint survInterval, uint pinInterval, uint finalizableInterval, uint weight)
    {
        this.sizeRange = sizeRange;
        this.survInterval = survInterval;
        this.pinInterval = pinInterval;
        this.finalizableInterval = finalizableInterval;
        this.weight = weight;

        // Should avoid creating the bucket in this case, as our algorithm assumes it should use the bucket at least once
        Util.AlwaysAssert(weight != 0);

        if (this.pinInterval != 0 || this.finalizableInterval != 0)
        {
            Util.AlwaysAssert(this.survInterval != 0, "pinInterval and finalizableInterval only affect surviving objects, but nothing survives");
        }
    }

    public override string ToString() =>
        $"{sizeRange}; surv every {survInterval}; pin every {pinInterval}; weight {weight}";
}

readonly struct Phase
{
    public readonly TestKind testKind;
    public readonly ItemType allocType;
    public readonly ulong totalLiveBytes;
    public readonly ulong totalAllocBytes;
    public readonly double totalMinutesToRun;
    public readonly BucketSpec[] buckets;

    public Phase(
        TestKind testKind,
        ulong totalLiveBytes, ulong totalAllocBytes, double totalMinutesToRun,
        BucketSpec[] buckets,
        ItemType allocType)
    {
        Util.AlwaysAssert(totalAllocBytes != 0); // Must be set

        this.testKind = testKind;;
        this.totalLiveBytes = totalLiveBytes;
        this.totalAllocBytes = totalAllocBytes;
        this.totalMinutesToRun = totalMinutesToRun;
        this.buckets = buckets;
        this.allocType = allocType;
    }

    public void Validate()
    {
        Util.AlwaysAssert(totalAllocBytes != 0); // Must be set
    }

    public bool print => false;
    public bool lohPauseMeasure => false;

    public void Describe()
    {
        Console.WriteLine($"{testKind}, {allocType}, tlgb {Util.BytesToGB(totalLiveBytes)}, tagb {Util.BytesToGB(totalAllocBytes)}, totalMins {totalMinutesToRun}, buckets:");
        for (uint i = 0;  i < buckets.Length; i++)
        {
            Console.WriteLine("    {0}", buckets[i]);
        }
    }
}

readonly struct PerThreadArgs
{
    public readonly bool verifyLiveSize;
    public readonly uint printEveryNthIter;
    public readonly Phase[] phases;
    public PerThreadArgs(bool verifyLiveSize, uint printEveryNthIter, Phase[] phases)
    {
        this.verifyLiveSize = verifyLiveSize;
        this.printEveryNthIter = printEveryNthIter;
        this.phases = phases;
        for (uint i = 0; i < phases.Length; i++)
        {
            phases[i].Validate();
        }
    }
}

ref struct TextReader
{
    ReadOnlySpan<char> text;
    uint lineNumber;
    uint columnNumber;

    public TextReader(string fileName)
    {
        // TODO: dotnet was hanging on all accesses to invalid UNC paths, even just testing if it exists. So forbid those for now.
        if (fileName.StartsWith("//") || fileName.StartsWith("\\\\"))
            throw new Exception("TODO");
        this.text = File.ReadAllText(fileName);
        this.lineNumber = 1;
        this.columnNumber = 1;
    }

    public char Peek => text[0];

    public char Take()
    {
        char c = Peek;
        if (c == '\n')
        {
            lineNumber++;
            columnNumber = 1;
        }
        else
        {
            columnNumber++;
        }
        text = text.Slice(1);
        return c;
    }

    private ReadOnlySpan<char> TakeN(uint n)
    {
        ReadOnlySpan<char> span = text.Slice(0, (int)n);
        for (int i = 0; i < span.Length; i++)
            Debug.Assert(span[i] != '\n');
        text = text.Slice((int)n);
        columnNumber += n;
        return span;
    }

    public void Assert(bool condition, string message = "Parsing failed")
    {
        if (!condition)
            throw Fail(message);
    }

    public Exception Fail(string message = "Parsing failed") =>
        new Exception($"Parse error at {lineNumber}:{columnNumber} -- {message}");

    public void SkipBlankLines()
    {
        while (!Eof)
        {
            switch (Peek)
            {
                case '\r':
                case '\n':
                    Take();
                    break;
                case ';':
                case '#':
                    while (Take() != '\n') { }
                    break;
                default:
                    return;
            }
        }
    }

    public bool TryTake(char c)
    {
        if (Peek == c)
        {
            Take();
            return true;
        }
        else
        {
            return false;
        }
    }

    public bool Eof =>
        text.Length == 0;

    public void ShouldTake(char c)
    {
        if (Take() != c)
            Fail($"Expected to take '{c}'");
    }

    public void TakeSpace()
    {
        ShouldTake(' ');
    }

    public uint TakeUInt() =>
        (uint)TakeUlong();

    public ulong TakeUlong()
    {
        Assert(IsDigit(Peek), "Expected to parse a ulong");
        uint i = 1;
        for (;  i < text.Length && IsDigit(text[(int)i]); i++) {}
        return ulong.Parse(TakeN(i));
    }

    public double TakeDouble()
    {
        Assert(IsDigitOrDot(Peek), "Expected to parse a double");
        uint i = 1;
        for (; i < text.Length && IsDigitOrDot(text[(int)i]); i++) { }
        return double.Parse(TakeN(i));
    }

    public ReadOnlySpan<char> TakeWord()
    {
        Assert(IsLetter(Peek), "Expected to parse a word");
        uint i = 1;
        for (; i < text.Length && IsLetter(text[(int)i]); i++) { }
        return TakeN(i);
    }

    private static bool IsLetter(char c) =>
        ('a' <= c && c <= 'z') || ('A' <= c && c <= 'Z');
    private static bool IsDigit(char c) =>
        '0' <= c && c <= '9';
    private static bool IsDigitOrDot(char c) =>
        IsDigit(c) || c == '.';
}

class Args
{
    public readonly uint threadCount;
    public readonly PerThreadArgs perThreadArgs;
    public readonly bool endException;

    public Args(uint threadCount, in PerThreadArgs perThreadArgs, bool endException)
    {
        this.threadCount = threadCount;
        this.perThreadArgs = perThreadArgs;
        this.endException = endException;
    }

    public void Describe()
    {
        Console.WriteLine($"Running {threadCount} threads.");
        for (uint i = 0; i < perThreadArgs.phases.Length; i++)
            perThreadArgs.phases[i].Describe();
    }
}

class ArgsParser
{
    public static Args Parse(string[] args)
    {
        if (args[0] == "-file")
        {
            Util.AlwaysAssert(args.Length == 2, "`-file path` must be the only arguments if present");
            return ParseFromFile(args[1]);
        }
        else
        {
            return ParseFromCommandLine(args);
        }
    }

    private static uint EnumFromNames(string[] names, ReadOnlySpan<char> name)
    {
        for (uint i = 0; i < names.Length; i++)
        {
            if (Eq(names[i], name))
            {
                return i;
            }
        }
        throw new Exception($"Invalid enum member {name.ToString()}, accepted: {string.Join(',', names)}");
    }

    private static TestKind ParseTestKind(ReadOnlySpan<char> str) =>
        (TestKind)EnumFromNames(testKindNames, str);

    private static ItemType ParseItemType(ReadOnlySpan<char> str) =>
        (ItemType)EnumFromNames(itemTypeNames, str);

    private static void ParseRange(string str, out uint lo, out uint hi)
    {
        string[] parts = str.Split('-');
        Util.AlwaysAssert(parts.Length == 2);
        lo = ParseUInt32(parts[0]);
        hi = ParseUInt32(parts[1]);
    }

    private static readonly string[] testKindNames = new string[] { "time", "highSurvival" };
    private static readonly string[] itemTypeNames = new string[] { "simple", "reference" };

    private static uint ParseUInt32(string s)
    {
        bool success = s.StartsWith("0x")
            ? uint.TryParse(s.Substring("0x".Length), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out uint v)
            : uint.TryParse(s, out v);
        return success ? v : throw new Exception($"Failed to parse {s}");
    }

    private static double ParseDouble(string strDouble) => double.TryParse(strDouble, out double v) ? v : throw new Exception($"Failred to parse {strDouble}");

    private enum State { ParsePhase, ParseBucket, Eof };

    private static Args ParseFromFile(string fileName)
    {
        TextReader text = new TextReader(fileName);
        bool verifyLiveSize = false;
        uint printEveryNthIter = 0;
        uint threadCount = 1;
        while (true)
        {
            State? s = TryReadTag(ref text);
            if (s == State.Eof)
            {
                return new Args(threadCount: threadCount, perThreadArgs: new PerThreadArgs(verifyLiveSize: verifyLiveSize, printEveryNthIter: printEveryNthIter, ParsePhases(ref text, threadCount)), endException: false);
            }
            ReadOnlySpan<char> word = text.TakeWord();
            text.TakeSpace();
            if (Eq(word, "printEveryNthIter"))
            {
                printEveryNthIter = text.TakeUInt();
            }
            else if (Eq(word, "threadCount"))
            {
                threadCount = text.TakeUInt();
            }
            else if (Eq(word, "verifyLiveSize"))
            {
                verifyLiveSize = true;
            }
            else
            {
                throw text.Fail($"Unexpected argument '{word.ToString()}'");
            }
            text.SkipBlankLines();
        }
    }
    
    // Called after we see the first [phase]; ends at EOF
    static Phase[] ParsePhases(ref TextReader text, uint threadCount)
    {
        List<Phase> res = new List<Phase>();
        while (true)
        {
            (State s, Phase p) = ParsePhase(ref text, threadCount);
            res.Add(p);
            if (s != State.ParsePhase)
            {
                Util.AlwaysAssert(s == State.Eof);
                return res.ToArray();
            }
        }
    }

    static (State, Phase) ParsePhase(ref TextReader text, uint threadCount)
    {
        TestKind testKind = TestKind.time;
        ulong? totalLiveBytes = null;
        ulong? totalAllocBytes = null;
        double totalMinutesToRun = 0;
        ItemType allocType = ItemType.ReferenceItem;

        while (true)
        {
            State? s = TryReadTag(ref text);
            if (s != null)
            {
                text.Assert(totalLiveBytes.HasValue && totalAllocBytes.HasValue, "Must set 'totalLiveMB' and 'totalAllocMB'");
                Debug.Assert(totalLiveBytes.HasValue && totalAllocBytes.HasValue);
                text.Assert(s == State.ParseBucket, "phase must end with buckets");
                (State s2, BucketSpec[] buckets) = ParseBuckets(ref text);

                ulong livePerThread = Util.NonNull(totalLiveBytes) / threadCount;
                ulong allocPerThread = Util.NonNull(totalAllocBytes) / threadCount;
                Phase phase = new Phase(
                    testKind: testKind,
                    totalLiveBytes: livePerThread,
                    totalAllocBytes: allocPerThread,
                    totalMinutesToRun: totalMinutesToRun,
                    buckets: buckets,
                    allocType: allocType);
                return (s2, phase);
            }

            ReadOnlySpan<char> word = text.TakeWord();
            text.TakeSpace();
            switch (word[0])
            {
                case 'a':
                    if (Eq(word, "allocType"))
                    {
                        allocType = ParseItemType(text.TakeWord());
                    }
                    else
                    {
                        throw text.Fail($"Unexpected argument '{word.ToString()}'");
                    }
                    break;
                case 't':
                    if (Eq(word, "testKind"))
                    {
                        testKind = ParseTestKind(text.TakeWord());
                    }
                    else if (Eq(word, "threadCount"))
                    {
                        threadCount = text.TakeUInt();
                        text.Assert(threadCount != 0, "Cannot have 0 threads");
                    }
                    else if (Eq(word, "totalLiveMB"))
                    {
                        totalLiveBytes = Util.MBToBytes(text.TakeUlong());
                    }
                    else if (Eq(word, "totalAllocMB"))
                    {
                        totalAllocBytes = Util.MBToBytes(text.TakeUlong());
                    }
                    else
                    {
                        throw text.Fail($"Unexpected argument '{word.ToString()}'");
                    }
                    break;
                default:
                    throw text.Fail($"Unexpected argument '{word.ToString()}'");
            }
            text.SkipBlankLines();
        }
    }

    // Called after we see the first [bucket] in a phase; ends at the next [phase] or at EOF
    static (State, BucketSpec[]) ParseBuckets(ref TextReader text)
    {
        List<BucketSpec> res = new List<BucketSpec>();
        while (true)
        {
            (State s, BucketSpec b) = ParseBucket(ref text);
            res.Add(b);
            if (s != State.ParseBucket)
            {
                return (s, res.ToArray());
            }
        }
    }

    static (State, BucketSpec) ParseBucket(ref TextReader text)
    {
        uint lowSize = DEFAULT_SOH_ALLOC_LOW;
        uint highSize = DEFAULT_SOH_ALLOC_HIGH;
        uint survInterval = DEFAULT_SOH_SURV_INTERVAL;
        uint pinInterval = DEFAULT_PINNING_INTERVAL;
        uint finalizableInterval = DEFAULT_FINALIZABLE_INTERVAL;
        uint weight = 1;
        while (true)
        {

            State? s = TryReadTag(ref text);
            if (s != null)
            {
                return (s.Value, new BucketSpec(
                    sizeRange: new SizeRange(lowSize, highSize),
                    survInterval: survInterval,
                    pinInterval: pinInterval,
                    finalizableInterval: finalizableInterval,
                    weight: weight));
            }

            ReadOnlySpan<char> word = text.TakeWord();
            text.TakeSpace();
            switch (word[0])
            {
                case 'l':
                    text.Assert(Eq(word, "lowSize"));
                    lowSize = text.TakeUInt();
                    break;
                case 'h':
                    text.Assert(Eq(word, "highSize"));
                    highSize = text.TakeUInt();
                    break;
                case 's':
                    text.Assert(Eq(word, "survInterval"));
                    survInterval = text.TakeUInt();
                    break;
                case 'p':
                    text.Assert(Eq(word, "pinInterval"));
                    pinInterval = text.TakeUInt();
                    break;
                case 'w':
                    text.Assert(Eq(word, "weight"));
                    weight = text.TakeUInt();
                    break;
                default:
                    throw text.Fail();
            }
            text.SkipBlankLines();
        }
    }

    private static bool Eq(ReadOnlySpan<char> a, ReadOnlySpan<char> b)
    {
        // The '==' operator seems to test whether the spans refer to the same range of memory.
        // I can't find any builtin function for comparing actual equality, which seems wierd.
        if (a.Length != b.Length)
            return false;
        for (int i = 0; i < a.Length; i++)
            if (a[i] != b[i])
                return false;
        return true;
    }

    private static State? TryReadTag(ref TextReader text)
    {
        if (text.Eof)
        {
            return State.Eof;
        }
        else if (text.TryTake('['))
        {
            ReadOnlySpan<char> word = text.TakeWord();
            // https://github.com/dotnet/csharplang/issues/1881 -- can't switch on a span
            State res = Eq(word, "phase") ? State.ParsePhase : Eq(word, "bucket") ? State.ParseBucket : throw text.Fail($"Bad tag '{word.ToString()}'");
            text.ShouldTake(']');
            text.SkipBlankLines();
            return res;
        }
        else
        {
            return null;
        }
    }

    private const uint DEFAULT_SOH_ALLOC_LOW = 100;
    private const uint DEFAULT_SOH_ALLOC_HIGH = 4000;
    private const uint DEFAULT_LOH_ALLOC_LOW = 100 * 1024;
    private const uint DEFAULT_LOH_ALLOC_HIGH = 200 * 1024;
    private const uint DEFAULT_PINNING_INTERVAL = 100;
    private const uint DEFAULT_FINALIZABLE_INTERVAL = 0;
    private const uint DEFAULT_SOH_SURV_INTERVAL = 30;
    private const uint DEFAULT_LOH_SURV_INTERVAL = 5;

    private static Args ParseFromCommandLine(string[] args)
    {
        TestKind testKind = TestKind.time;
        uint threadCount = 4;
        uint? lohAllocRatioArg = null;
        uint? lohAllocIntervalArg = null;
        ulong? totalLiveBytes = null;
        ulong? totalAllocBytes = null;
        double totalMinutesToRun = 0.0;
        uint sohAllocLow = DEFAULT_SOH_ALLOC_LOW;
        uint sohAllocHigh = DEFAULT_SOH_ALLOC_HIGH;
        uint lohAllocLow = DEFAULT_LOH_ALLOC_LOW;
        uint lohAllocHigh = DEFAULT_LOH_ALLOC_HIGH;
        // default is we survive every 30th element for SOH...this is about 3%.
        uint sohSurvInterval = DEFAULT_SOH_SURV_INTERVAL;
        uint lohSurvInterval = DEFAULT_LOH_SURV_INTERVAL;
        uint sohPinInterval = DEFAULT_PINNING_INTERVAL;
        uint lohPinInterval = DEFAULT_PINNING_INTERVAL;
        uint sohFinalizableInterval = DEFAULT_FINALIZABLE_INTERVAL;
        uint lohFinalizableInterval = DEFAULT_FINALIZABLE_INTERVAL;
        ItemType allocType = ItemType.ReferenceItem;
        bool verifyLiveSize = false;
        uint printEveryNthIter = 0;
        bool endException = false;

        for (uint i = 0; i < args.Length; ++i)
        {
            switch (args[i])
            {
                case "-endException":
                    endException = true;
                    break;
                case "-testKind":
                case "-tk":
                    testKind = ParseTestKind(args[++i]);
                    break;
                case "-threadCount":
                case "-tc":
                    threadCount = ParseUInt32(args[++i]);
                    break;
                case "-lohAllocRatio":
                case "-lohar":
                    lohAllocRatioArg = ParseUInt32(args[++i]);
                    break;
                case "-lohAllocInterval":
                    lohAllocIntervalArg = ParseUInt32(args[++i]);
                    break;
                case "-totalLiveGB":
                case "-tlgb":
                    totalLiveBytes = Util.GBToBytes(ParseDouble(args[++i]));
                    break;
                case "-totalAllocGB":
                case "-tagb":
                    totalAllocBytes = Util.GBToBytes(ParseDouble(args[++i]));
                    break;
                case "-totalMins":
                case "-tm":
                    totalMinutesToRun = ParseDouble(args[++i]);
                    break;
                case "-sohSizeRange":
                case "-sohsr":
                    ParseRange(args[++i], out sohAllocLow, out sohAllocHigh);
                    break;
                case "-lohSizeRange":
                case "-lohsr":
                    ParseRange(args[++i], out lohAllocLow, out lohAllocHigh);
                    Util.AlwaysAssert(lohAllocLow >= 85000, "g_lohAllocLow is below the minimum large object size");
                    break;
                case "-sohSurvInterval":
                case "-sohsi":
                    sohSurvInterval = ParseUInt32(args[++i]);
                    break;
                case "-lohSurvInterval":
                case "-lohsi":
                    lohSurvInterval = ParseUInt32(args[++i]);
                    break;
                case "-sohPinningInterval":
                case "-sohpi":
                    sohPinInterval = ParseUInt32(args[++i]);
                    break;
                case "-sohFinalizableInterval":
                case "-sohfi":
                    sohFinalizableInterval = ParseUInt32(args[++i]);
                    break;
                case "-lohPinningInterval":
                case "-lohpi":
                    lohPinInterval = ParseUInt32(args[++i]);
                    break;
                case "-lohFinalizableInterval":
                case "-lohfi":
                    lohFinalizableInterval = ParseUInt32(args[++i]);
                    break;
                case "-allocType":
                case "-at":
                    allocType = ParseItemType(args[++i]);
                    break;
                case "-verifyLiveSize":
                    verifyLiveSize = true;
                    break;
                case "-printEveryNthIter":
                    printEveryNthIter = ParseUInt32(args[++i]);
                    break;
                default:
                    throw new Exception($"Unrecognized argument: {args[i]}");
            }
        }

        if (totalLiveBytes == 0 && (sohSurvInterval != 0 || lohSurvInterval != 0))
        {
            throw new Exception("Can't set -sohsi or -lohsi if -tlgb is 0");
        }

        if ((totalAllocBytes == null) && (totalMinutesToRun == 0))
        {
            totalMinutesToRun = 1;
        }

        ulong livePerThread = (totalLiveBytes ?? 0) / threadCount;
        ulong allocPerThread = (totalAllocBytes ?? 0) / threadCount;
        if (allocPerThread != 0) Console.WriteLine("allocating {0:n0} per thread", allocPerThread);

        (uint lohAllocInterval, uint lohAllocRatio) = GetLohAllocIntervalAndRatio(lohAllocIntervalArg, lohAllocRatioArg, sohAllocLow: sohAllocLow, sohAllocHigh: sohAllocHigh, lohAllocLow: lohAllocLow, lohAllocHigh: lohAllocHigh);

        BucketSpec lohBucket = new BucketSpec(
            sizeRange: new SizeRange(lohAllocLow, lohAllocHigh),
            survInterval: lohSurvInterval,
            pinInterval: lohPinInterval,
            finalizableInterval: lohFinalizableInterval,
            weight: 1);
        BucketSpec[] buckets;
        if (lohAllocInterval == 1)
        {
            buckets = new BucketSpec[] { lohBucket };
        }
        else
        {
            BucketSpec sohBucket = new BucketSpec(
                sizeRange: new SizeRange(sohAllocLow, sohAllocHigh),
                survInterval: sohSurvInterval,
                pinInterval: sohPinInterval,
                finalizableInterval: sohFinalizableInterval,
                weight: lohAllocInterval == 0 ? 1 : lohAllocInterval - 1);
            buckets = lohAllocInterval == 0 ? new BucketSpec[] { sohBucket } : new BucketSpec[] { sohBucket, lohBucket };
        }

        Phase onlyPhase = new Phase(
            testKind: testKind,
            totalLiveBytes: livePerThread,
            totalAllocBytes: allocPerThread,
            totalMinutesToRun: totalMinutesToRun,
            buckets: buckets,
            allocType: allocType);
        return new Args(
            threadCount: threadCount,
            perThreadArgs: new PerThreadArgs(verifyLiveSize: verifyLiveSize, printEveryNthIter: printEveryNthIter, phases: new Phase[] { onlyPhase }),
            endException: endException);
    }

    private static (uint interval, uint ratio) GetLohAllocIntervalAndRatio(uint? lohAllocInterval, uint? lohAllocRatio, uint sohAllocLow, uint sohAllocHigh, uint lohAllocLow, uint lohAllocHigh)
    {
        ulong meanSohObjSize = Util.Mean(sohAllocLow, sohAllocHigh);
        ulong meanLohObjSize = Util.Mean(lohAllocLow, lohAllocHigh);
        if (lohAllocInterval != null)
        {
            Util.AlwaysAssert(lohAllocRatio == null); // Can't set both
            uint interval = lohAllocInterval.Value;
            uint ratio = interval == 0 ? 0 : (uint)(1000 * meanLohObjSize / (meanLohObjSize + meanSohObjSize * (interval - 1)));
            return (interval, ratio);
        }
        else
        {
            uint ratio = lohAllocRatio ?? 5;
            uint interval = GetLohAllocInterval(ratio, sohObjSize: meanSohObjSize, lohObjSize: meanLohObjSize);
            return (interval, ratio);
        }
    }

    private static uint GetLohAllocInterval(uint lohAllocRatioOutOf1000, ulong sohObjSize, ulong lohObjSize)
    {
        if (lohAllocRatioOutOf1000 == 0)
        {
            return 0;
        }

        double lohAllocFraction = lohAllocRatioOutOf1000 / 1000.0;
        // We want lohAllocFraction to be the fraction of bytes that are on loh, meaning:
        // lohAllocFraction = lohObjSize / (lohObjSize + sohObjSize * (interval - 1));
        // ... math ...
        // interval = ((ls/lf) - ls + ss) / ss

        double interval = ((lohObjSize / lohAllocFraction) - lohObjSize + sohObjSize) / sohObjSize;
        Util.AlwaysAssert(interval >= 1.0);

        uint res = (uint)Math.Round(interval);

        double practicalAllocFraction = ((double)lohObjSize) / (lohObjSize + sohObjSize * (res - 1));
        if (!Util.AboutEquals(lohAllocFraction, practicalAllocFraction))
        {
            throw new Exception($"Expected to get {lohAllocFraction}, got {practicalAllocFraction}");
        }

        return res;
    }
}

readonly struct ObjectSpec
{
    public readonly uint Size;
    public readonly bool ShouldBePinned;
    public readonly bool ShouldBeFinalizable;
    public readonly bool ShouldSurvive;

    public ObjectSpec(uint size, bool shouldBePinned, bool shouldBeFinalizable, bool shouldSurvive)
    {
        Size = size;
        ShouldBeFinalizable = shouldBeFinalizable;
        ShouldBePinned = shouldBePinned;
        ShouldSurvive = shouldSurvive;
    }
}

class Bucket
{
    public readonly BucketSpec spec;
    public ulong count; // Used for pinInterval and survInterval
    public ulong allocatedBytesTotalSum;
    public ulong allocatedBytesAsOfLastPrint;
    public ulong survivedBytesSinceLastPrint;
    public ulong pinnedBytesSinceLastPrint;
    public ulong allocatedCountSinceLastPrint;
    public ulong survivedCountSinceLastPrint;

    public Bucket(BucketSpec spec)
    {
        this.spec = spec;
        count = 0;
        allocatedBytesTotalSum = 0;
        allocatedBytesAsOfLastPrint = 0;
        survivedBytesSinceLastPrint = 0;
        pinnedBytesSinceLastPrint = 0;
        allocatedCountSinceLastPrint = 0;
        survivedCountSinceLastPrint = 0;
    }

    public SizeRange sizeRange => spec.sizeRange;
    public uint survInterval => spec.survInterval;
    public uint pinInterval => spec.pinInterval;
    public uint finalizableInterval => spec.finalizableInterval;
    // If we have buckets with weights of 2 and 1, we'll allocate 2 objects from the first bucket, then 1 from the next.
    public uint weight => spec.weight;

    public ObjectSpec GetObjectSpec(Rand rand)
    {
        count++;

        uint size = rand.GetRand(sizeRange);
        bool shouldSurvive = Util.isNth(survInterval, count);
        bool shouldBePinned = shouldSurvive && Util.isNth(pinInterval, count / survInterval);
        bool shouldBeFinalizable = shouldSurvive && Util.isNth(finalizableInterval, count / survInterval);

        allocatedBytesTotalSum += size;
        allocatedCountSinceLastPrint++;
        if (shouldBePinned)
        {
            pinnedBytesSinceLastPrint += size;
        }
        if (shouldSurvive)
        {
            survivedBytesSinceLastPrint += size;
            survivedCountSinceLastPrint++;
        }

        return new ObjectSpec(size, shouldBePinned: shouldBePinned, shouldBeFinalizable: shouldBeFinalizable, shouldSurvive: shouldSurvive);
    }
}

struct BucketChooser
{
    public readonly Bucket[] buckets;
    /*mutable*/ uint bucketIndex;
    uint curLeftInBucket;

    public BucketChooser(BucketSpec[] bucketSpecs)
    {
        Util.AlwaysAssert(bucketSpecs.Length != 0);
        this.buckets = new Bucket[bucketSpecs.Length];
        for (uint i = 0;  i < bucketSpecs.Length; i++)
        {
            this.buckets[i] = new Bucket(bucketSpecs[i]);
        }
        this.bucketIndex = 0;
        this.curLeftInBucket = buckets[0].weight;
    }

    private Bucket GetNextBucket()
    {
        Bucket bucket = buckets[bucketIndex];
        Util.AlwaysAssert(curLeftInBucket != 0);
        curLeftInBucket--;
        if (curLeftInBucket == 0)
        {
            bucketIndex++;
            if (bucketIndex == buckets.Length)
            {
                bucketIndex = 0;
            }
            curLeftInBucket = buckets[bucketIndex].weight;
        }
        return bucket;
    }

    public ObjectSpec GetNextObjectSpec(Rand rand) =>
        GetNextBucket().GetObjectSpec(rand);

    public ulong AverageObjectSize()
    {
        // Average object size in each bucket, weighed by the bucket
        ulong totalAverage = 0;
        ulong totalWeight = 0;
        // https://github.com/dotnet/csharplang/issues/461
        for (uint i = 0; i < buckets.Length; i++)
        {
            Bucket bucket = buckets[i];
            totalAverage += bucket.sizeRange.Mean * bucket.weight;
            totalWeight += bucket.weight;
        }

        return totalAverage / totalWeight;
    }

    public ulong GetTotalAllocatedInAllBuckets()
    {
        ulong allocTotal = 0;
        for (uint i = 0; i < buckets.Length; i++)
        {
            allocTotal += buckets[i].allocatedBytesTotalSum;
        }
        return allocTotal;
    }
}

class ThreadLauncher
{
    readonly uint threadIndex;
    readonly PerThreadArgs perThreadArgs;
    public MemoryAlloc? alloc; // To be created by the thread

    public MemoryAlloc Alloc => Util.NonNull(alloc);

    public ThreadLauncher(uint threadIndex, in PerThreadArgs perThreadArgs)
    {
        this.threadIndex = threadIndex;
        this.perThreadArgs = perThreadArgs;
    }

    public void Run()
    {
        alloc = new MemoryAlloc(threadIndex, perThreadArgs);
        alloc.RunTest();
    }
}

// Encapsulating this to ensure items are freed
struct OldArr
{
    public readonly ITypeWithPayload?[] items;
    public ulong TotalLiveBytes { get; private set; }

    public OldArr(ulong numElements)
    {
        items = new ITypeWithPayload?[numElements];
        TotalLiveBytes = Util.ArraySize(items);
    }

    public ulong OwnSize => Util.ArraySize(items);

    public void Initialize(uint index, ITypeWithPayload item)
    {
        Debug.Assert(items[index] == null);
        items[index] = item;
        TotalLiveBytes += item.TotalSize;
    }

    public ITypeWithPayload? Peek(uint index) => items[index];

    public void Free(uint index)
    {
        ITypeWithPayload? item = items[index];
        items[index] = null;
        ulong size = item?.TotalSize ?? 0;
        item?.Free();
        TotalLiveBytes -= size;
    }

    public void Replace(uint index, ITypeWithPayload newItem)
    {
        Free(index);
        TotalLiveBytes += newItem.TotalSize;
        items[index] = newItem;
    }

    public ITypeWithPayload? TakeAndReduceTotalSizeButDoNotFree(uint index)
    {
        ITypeWithPayload? item = items[index];
        if (item != null)
        {
            items[index] = null;
            TotalLiveBytes -= item.TotalSize;
        }
        return item;
    }

    public uint Length => (uint) items.Length;

    public void FreeAll()
    {
        for (uint i = 0; i < items.Length; i++)
        {
            Free(i);
        }

        Debug.Assert(TotalLiveBytes == OwnSize);
    }

    public void VerifyLiveSize()
    {
        ulong liveSizeCalculated = OwnSize;
        for (uint i = 0; i < items.Length; i++)
            liveSizeCalculated += items[i]?.TotalSize ?? 0;
        Debug.Assert(liveSizeCalculated == TotalLiveBytes);
    }
}

class MemoryAlloc
{
    private readonly Rand rand;
    OldArr oldArr;
    // TODO We should consider adding another array for medium lifetime.
    private readonly uint threadIndex;
    private readonly PerThreadArgs args;
    private long totalAllocBytesLeft;
    //private readonly bool printIterInfo = false;
    private BucketChooser bucketChooser;

    // TODO: replace this with an array that records the 10 longest pauses 
    // and pause buckets.
    public List<double> lohAllocPauses = new List<double>(10);

    // TODO: should be mutable, change when we switch phases
    readonly Phase curPhase;

    public MemoryAlloc(uint _threadIndex, in PerThreadArgs args)
    {
        rand = new Rand();
        threadIndex = _threadIndex;

        this.args = args;
        this.curPhase = args.phases[0];
        this.totalAllocBytesLeft = (long) curPhase.totalAllocBytes;

        Util.AlwaysAssert(args.phases.Length == 1, "TODO: Phase switching");
        //printIterInfo = true;

        this.bucketChooser = new BucketChooser(curPhase.buckets);

        ulong numElements = curPhase.totalLiveBytes / bucketChooser.AverageObjectSize();
        oldArr = new OldArr(numElements);

        for (uint i = 0; i < numElements; i++)
        {
            (ITypeWithPayload item, ObjectSpec _) = MakeObjectAndTouchPage();
            oldArr.Initialize(i, item);
        }

        if (curPhase.totalLiveBytes == 0)
            Util.AlwaysAssert(oldArr.TotalLiveBytes < 100);
        else
            Util.AssertAboutEqual(oldArr.TotalLiveBytes, curPhase.totalLiveBytes);

        /*
        if (args.print)
        {
            Console.WriteLine("T{0}: allocated {1} ({2}MB) on SOH, {3} ({4}MB) on LOH",
                threadIndex,
                sohAllocatedElements, Util.BytesToMB(sohAllocatedBytes),
                lohAllocatedElements, Util.BytesToMB(lohAllocatedBytes));
        }
        */

        if (args.verifyLiveSize)
        {
            oldArr.VerifyLiveSize();
            if (!Util.AboutEquals(oldArr.TotalLiveBytes, curPhase.totalLiveBytes))
            {
                Console.WriteLine($"totalLiveBytes: {oldArr.TotalLiveBytes}, args.totalLiveBytes: {curPhase.totalLiveBytes}");
                throw new Exception("TODO");
            }
        }

        //GC.Collect();
        //Console.WriteLine("init done");
        //Console.ReadLine();

        if (curPhase.totalAllocBytes != 0)
        {
            Console.WriteLine("Thread {0} stopping phase after {1}MB", threadIndex, Util.BytesToMB(curPhase.totalAllocBytes));
        }
        else
        {
            Console.WriteLine("Stopping phase after {0} mins", curPhase.totalMinutesToRun);
        }
    }

    // This really doesn't belong in this class - this should be a building blocking that takes
    // a datastructure and modifies it based on configs, eg, it can modify arrays based on 
    // its element type.
    //
    // Note that this implementation will involve almost only old generation objects so it
    // doesn't affect ephemeral collection time. 
    // 
    // One way to use this is -
    // 
    // we move the first half of the array elements off the array and link them together onto a list.
    // then we discard the list and allocate the 1st half of the array again.
    // we move the second half of the array elements off the array and link them together onto a list.
    // then we discard the list and allocate the 2nd half of the array again.
    // repeat.
    // This means the live data size would be smaller when we remove the list completely and before we
    // allocate enough to replace what we removed.
    // 
    // TODO: ways to configure -
    // 
    // How and how much to convert the array onto a list/lists can be specified a config, eg we could 
    // pick every Nth to convert and make only short lists; or pick elements randomly, or
    // on a distribution, eg, the middle of the array is the most empty.
    // 
    // When do to this, eg do this periodically, or interleaved with replacing elements in the array.
    // 
    // Whether replace the array element with a new one when taking the old one off the array - this 
    // would be useful for temporarily increasing the live data size to see how the heap size changes.
    void MakeListFromContiguousItems(uint beginIndex, uint endIndex)
    {
        // Take off the end element and link it onto the previous element. Do this
        // till we get to the begin element.
        for (uint index = endIndex; index > beginIndex; index--)
        {
            ReferenceItemWithSize refItem = (ReferenceItemWithSize) Util.NonNull(oldArr.TakeAndReduceTotalSizeButDoNotFree(index));
            ReferenceItemWithSize refItemPrev = (ReferenceItemWithSize) Util.NonNull(oldArr.Peek(index - 1));
            refItemPrev.AddToEndOfList(refItem);

            // We are allocating some temp objects here just so that it will trigger GCs.
            // It's unnecesssary if other threads are already allocating objects.
            uint allocBytes = bucketChooser.GetNextObjectSpec(rand).Size;
            byte[] bTemp = new byte[allocBytes];
            TouchPage(bTemp);
        }

        // This GC will see the longest list.
        // GC.Collect();

        ReferenceItemWithSize? head = (ReferenceItemWithSize?)oldArr.Peek(beginIndex);
        Debug.Assert(head != null);
    }

    void TouchPage(byte[] b)
    {
        uint size = (uint)b.Length;
        const uint pageSize = 4096;
        uint numPages = size / pageSize;

        for (uint i = 0; i < numPages; i++)
        {
            b[i * pageSize] = (byte)(i % 256);
        }

        // Trying to increase the amount of work we do to see if that affects ideal thread count
        //for (uint i = 0; i < size; i++)
        //{
        //    b[i] = (byte)(i % 256);
        //}
    }

    void TouchPage(ITypeWithPayload item)
    {
        byte[] b = item.GetPayload();
        TouchPage(b);
    }

    public void RunTest()
    {
        switch (curPhase.testKind)
        {
            case TestKind.time:
                TimeTest();
                break;
            case TestKind.highsurvival:
                HighSurvivalTest();
                break;
            default:
                throw new InvalidOperationException();
        }
    }

    public void HighSurvivalTest()
    {
        if (curPhase.totalMinutesToRun == 0.0) throw new Exception("totalMinutesToRun must be set");

        // Note: threads already launched, so this is just the code for a single thread.
        // Initial memory for tlgb has already been allocated.

        Stopwatch stopwatch = new Stopwatch();
        stopwatch.Start();
        while (stopwatch.Elapsed.TotalMinutes < curPhase.totalMinutesToRun)
        {
            GC.Collect();
        }
        stopwatch.Stop();
    }

    private static void PrintIterInfoHeader(uint nBuckets)
    {
        Console.Write("{0,3} | {1,10} | {2,5} | {3,5} | {4,4} | {5,6} | {6,8}",
            "T",                     // 0
            "iter",                  // 1

            "gen0",                  // 2
            "gen1",                  // 3
            "gen2",                  // 4

            "ms",                    // 5
                                     // This size is what we get from GC.GetTotalMemory(false) so 
                                     // it's not the live data size. Change it to GC.GetTotalMemory(true)
                                     // if you want to make sure the live size is expected.
            "size(mb)"               // 6
        );
        for (uint b = 0; b < nBuckets; b++)
        {
            Console.Write(" | {0,14} | {1,14} | {2,14} | {3,5} | {4,10} | {5,10}",
                $"b{b} total",           // 0
                $"b{b} alloc",           // 1
                $"b{b} surv",            // 2
                "ratio",                 // 3
                $"b{b} pinned",          // 4
                "ratio"                  // 5
            );
        }
        Console.WriteLine();
    }

    // Keep this in sync with PrintIterInfoHeader
    private static void PrintIterInfoForBuckets(uint threadIndex, ulong n, long elapsedDiffMS, Bucket[] buckets)
    {
        Console.Write("{0,3} | {1,10:n0} | {2,5:d} | {3,5:d} | {4,4:d} | {5,6:n0} | {6,8:n0}",
            threadIndex,                                                                      // 0
            n,                                                                                // 1
            GC.CollectionCount(0),                                                            // 2
            GC.CollectionCount(1),                                                            // 3
            GC.CollectionCount(2),                                                            // 4
            elapsedDiffMS,                                                                    // 5
            Util.BytesToMB((ulong)GC.GetTotalMemory(false))                                   // 6
        );

        for (uint i = 0; i < buckets.Length; i++)
        {
            Bucket bucket = buckets[i];
            ulong allocatedBytesSinceLast = bucket.allocatedBytesTotalSum - bucket.allocatedBytesAsOfLastPrint;

            Console.Write(" | {0,14:n0} | {1,14:n0} | {2,14:n0} | {3,5:n0} | {4,10:n0} | {5,10:f2}",
                bucket.allocatedBytesTotalSum,
                allocatedBytesSinceLast,                                         // 0
                bucket.survivedBytesSinceLastPrint,                                              // 1
                GetPercent(bucket.survivedBytesSinceLastPrint, allocatedBytesSinceLast),         // 2
                bucket.pinnedBytesSinceLastPrint,                                                // 3
                // TODO: might be more pinned than survived ...
                GetPercent(bucket.pinnedBytesSinceLastPrint, bucket.survivedBytesSinceLastPrint) // 4
            );

            // Reset the numbers.
            bucket.allocatedBytesAsOfLastPrint = bucket.allocatedBytesTotalSum;
            bucket.survivedBytesSinceLastPrint = 0;
            bucket.pinnedBytesSinceLastPrint = 0;
            bucket.allocatedCountSinceLastPrint = 0;
            bucket.survivedCountSinceLastPrint = 0;
        }
        Console.WriteLine();
    }

    private static int GetPercent(ulong a, ulong b) => b == 0 ? 0 : (int)(a * 100.0 / b);

    public void TimeTest()
    {
        ulong n = 0;

        Stopwatch stopwatchGlobal = new Stopwatch();

        long elapsedLastMS = 0;

        if (threadIndex == 0 && args.printEveryNthIter != 0)
        {
            PrintIterInfoHeader((uint)bucketChooser.buckets.Length);
        }

        stopwatchGlobal.Reset();
        stopwatchGlobal.Start();

        while (true)
        {
            if (threadIndex == 0 && Util.isNth(args.printEveryNthIter, n))
            {
                long elapsedCurrentMS = stopwatchGlobal.ElapsedMilliseconds;
                long elapsedDiffMS = elapsedCurrentMS - elapsedLastMS;
                elapsedLastMS = elapsedCurrentMS;
                PrintIterInfoForBuckets(threadIndex, n, elapsedDiffMS, bucketChooser.buckets);
            }

            if (n % ((ulong)1 * 1024 * 1024) == 0)
            {
                if (curPhase.totalMinutesToRun != 0 && stopwatchGlobal.Elapsed.TotalMinutes >= curPhase.totalMinutesToRun)
                {
                    if (!GoToNextPhase()) break;
                }
            }

            if (totalAllocBytesLeft <= 0)
            {
                //if (args.print) Console.WriteLine("T{0}: SOH/LOH alocated {1:n0}/{2:n0} >= {3:n0}", threadIndex, sohAllocatedBytesTotalSum, lohAllocatedBytesTotalSum, totalAllocBytesLeft);
                if (!GoToNextPhase()) break;
            }

            MakeObjectAndMaybeSurvive(); // modifies totalAllocBytesLeft

            n++;
        }

        Finish();
    }

    void Finish()
    {
        oldArr.FreeAll();
    }

    // Returns false if no next phase
    bool GoToNextPhase()
    {
#if DEBUG
        Util.AssertAboutEqual(bucketChooser.GetTotalAllocatedInAllBuckets(), curPhase.totalAllocBytes);
#endif
        Util.AlwaysAssert(this.args.phases.Length == 1); // If more, we should be switching phases
        return false;
    }

    (ITypeWithPayload, ObjectSpec spec) JustMakeObject()
    {
        ObjectSpec spec = bucketChooser.GetNextObjectSpec(rand);
        totalAllocBytesLeft -= spec.Size;
        switch (curPhase.allocType)
        {
            case ItemType.SimpleItem:
                return (Item.New(spec.Size, isPinned: spec.ShouldBePinned, isFinalizable: spec.ShouldBeFinalizable), spec);
            case ItemType.ReferenceItem:
                return (ReferenceItemWithSize.New(spec.Size, isPinned: spec.ShouldBePinned, isFinalizable: spec.ShouldBeFinalizable), spec);
            default:
                throw new NotImplementedException();
        }
    }

    (ITypeWithPayload, ObjectSpec) MakeObjectAndTouchPage()
    {
        (ITypeWithPayload item, ObjectSpec spec) = JustMakeObject();
        TouchPage(item);
        return (item, spec);
    }

    // Allocates object and survives it. Returns the object size.
    void MakeObjectAndMaybeSurvive()
    {
        //if (isLarge && args.lohPauseMeasure)
        //{
        //    stopwatch.Reset();
        //    stopwatch.Start();
        //}

        // TODO: We should have a sequence number that just grows (since we only allocate sequentially on 
        // the same thread anyway). This way we can use this number to indicate the ages of items. 
        // If an item with a very current seq number points to an item with small seq number we can conclude
        // that we have young gen object pointing to a very old object. This can help us recognize things
        // like object locality, eg if demotion has demoted very old objects next to young objects.
        (ITypeWithPayload item, ObjectSpec spec) = MakeObjectAndTouchPage();

        //if (isLarge && args.lohPauseMeasure)
        //{
        //    stopwatch.Stop();
        //    lohAllocPauses.Add(stopwatch.Elapsed.TotalMilliseconds);
        //}

        //Thread.Sleep(1);

        if (spec.ShouldSurvive)
        {
            DoSurvive(item, spec);
        }
        else
        {
            item.Free();
        }
    }

    private void DoSurvive(ITypeWithPayload item, in ObjectSpec spec)
    {
        // TODO: How to survive shouldn't belong here; it should
        // belong to the building block that churns the array.
        switch (curPhase.allocType)
        {
            case ItemType.SimpleItem:
                oldArr.Replace(rand.GetRand(oldArr.Length), item);
                break;
            case ItemType.ReferenceItem:
                // For ref items we want to create some variation, ie, we want
                // to create different graphs. But we also want to keep our live 
                // data size the same. So we do a few different operations -
                // If the live data size is the same as what we set, we randomly
                // choose an action which can be one of the following -
                // 
                // 1) create a new item, take a few items off the array and link them onto 
                // the new item.
                //
                // 2) create a new item and a few extra ones and link them onto the new item.
                // note this may not have much affect in ephemeral GCs 'cause it's very likely 
                // they all get promoted to gen2.
                //
                // 3) replace a bunch of entries with newly created items.
                // 
                // If the live data size is > what's set, we randomly choose a non null entry 
                // and set it to null.

                if (oldArr.TotalLiveBytes < curPhase.totalLiveBytes)
                {
                    MixItUp((ReferenceItemWithSize)item, spec);
                }
                else
                {
                    // Choosing not to actually survive this item!
                    item.Free();
                    while (oldArr.TotalLiveBytes >= curPhase.totalLiveBytes)
                    {
                        oldArr.Free(rand.GetRand(oldArr.Length));
                    }
                }
                break;
            default:
                throw new InvalidOperationException();
        }

        if (args.verifyLiveSize)
        {
            oldArr.VerifyLiveSize();
        }
    }

    void MixItUp(ReferenceItemWithSize refItem, in ObjectSpec spec)
    {
        // 5 is just a random number I picked that's big enough to exercise the mark stack reasonably.
        // MakeListFromContiguousItems is another way to make a list.
        uint numItemsToModify = rand.GetRand(5);
        //Console.WriteLine("\nlive is supposed to be {0:n0}, current {1:n0}, new item s {2:n0} -> OP {3}",
        //    totalLiveBytes, totalLiveBytesCurrent, refItem.sizeTotal,
        //    ((totalLiveBytesCurrent < totalLiveBytes) ? "INC" : "DEC"));
        
        ReferenceItemOperation operation = (ReferenceItemOperation)rand.GetRand((uint)ReferenceItemOperation.MaxOperation);
        switch (operation)
        {
            case ReferenceItemOperation.NewWithExistingList:
            {
                ReferenceItemWithSize? listHead = null;
                for (uint itemModifyIndex = 0; itemModifyIndex < numItemsToModify; itemModifyIndex++)
                {
                    uint randomIndex = rand.GetRand(oldArr.Length);
                    ReferenceItemWithSize? randomItem = (ReferenceItemWithSize?)oldArr.TakeAndReduceTotalSizeButDoNotFree(randomIndex);
                    if (randomItem != null)
                    {
                        if (listHead != null)
                        {
                            randomItem.AddToEndOfList(listHead);
                        }
                        listHead = randomItem;
                    }
                }
                if (listHead != null)
                {
                    refItem.AddToEndOfList(listHead);
                }
                break;
            }

            case ReferenceItemOperation.NewWithNewList:
            {
                ReferenceItemWithSize? listHead = null;
                for (int itemModifyIndex = 0; itemModifyIndex < numItemsToModify; itemModifyIndex++)
                {
                    // TODO: should this additional object be pinned too?
                    ReferenceItemWithSize randomItem = ReferenceItemWithSize.New(spec.Size, isPinned: spec.ShouldBePinned, isFinalizable: spec.ShouldBeFinalizable);
                    if (listHead != null)
                    {
                        randomItem.AddToEndOfList(listHead);
                    }
                    listHead = randomItem;
                }
                if (listHead != null)
                {
                    refItem.AddToEndOfList(listHead);
                }
                break;
            }

            case ReferenceItemOperation.MultipleNew:
                for (int itemModifyIndex = 0; itemModifyIndex < numItemsToModify; itemModifyIndex++)
                {
                    // This doesn't have to be allocBytes, could be randomly generated and based on
                    // the LOH alloc interval.
                    // For large objects creating a few new ones could be quite significant.
                    ITypeWithPayload randomItemNew = ReferenceItemWithSize.New(spec.Size, isPinned: spec.ShouldBePinned, isFinalizable: spec.ShouldBeFinalizable);
                    oldArr.Replace(rand.GetRand(oldArr.Length), randomItemNew);
                }
                break;

            default:
                throw new InvalidOperationException();
        }

        // Now survive the item we allocated.
        oldArr.Replace(rand.GetRand(oldArr.Length), refItem);

        //Console.WriteLine("final ELE#{0}, s - {1:n0} replaced by {2:n0}",
        //    randomIndexToSurv, sizeToReplace, refItem.sizeTotal);
        ////refItem.Print();
        //Console.WriteLine("{0:n0} - {1} + {2} = {3:n0} op {4}, heap {5:n0}",
        //    totalLiveBytesCurrentSaved,
        //    sizeToReplace, refItem.sizeTotal, 
        //    totalLiveBytesCurrent,
        //    (ReferenceItemOperation)operationIndex, GC.GetTotalMemory(false));
    }

    void PrintPauses(StreamWriter sw)
    {
        if (curPhase.lohPauseMeasure)
        {
            sw.WriteLine("T{0} {1:n0} entries in pause, top entries(ms)", threadIndex, lohAllocPauses.Count);
            sw.Flush();

            int numLOHAllocPauses = lohAllocPauses.Count;
            if (numLOHAllocPauses >= 0)
            {
                lohAllocPauses.Sort();
                //lohAllocPauses.OrderByDescending(a => a);

                sw.WriteLine("===============STATS for thread {0}=================", threadIndex);

                int startIndex = ((numLOHAllocPauses < 10) ? 0 : (numLOHAllocPauses - 10));
                for (int i = startIndex; i < numLOHAllocPauses; i++)
                {
                    sw.WriteLine(lohAllocPauses[i]);
                }

                sw.WriteLine("===============END STATS for thread {0}=================", threadIndex);
            }
        }
    }

#if TODO
    [DllImport("psapi.dll")]
    public static extern bool EmptyWorkingSet(IntPtr hProcess);
#endif

    static void DoTest(in Args args, int currentPid)
    {
        // TODO: we probably need to synchronoze writes to this somehow
        string logFileName = currentPid + "-output.txt";
        StreamWriter sw = new StreamWriter(logFileName);
        sw.WriteLine("Started running");

        long tStart = Environment.TickCount;
        if (args.threadCount > 1)
        {
            ThreadLauncher[] threadLaunchers = new ThreadLauncher[args.threadCount];
            Thread[] threads = new Thread[args.threadCount];

            for (uint i = 0; i < args.threadCount; i++)
            {
                threadLaunchers[i] = new ThreadLauncher(i, args.perThreadArgs);
                ThreadStart ts = new ThreadStart(threadLaunchers[i].Run);
                threads[i] = new Thread(ts);
            }

            for (uint i = 0; i < threads.Length; i++)
                threads[i].Start();
            for (uint i = 0; i < threads.Length; i++)
                threads[i].Join();
            for (uint i = 0; i < threadLaunchers.Length; i++)
                threadLaunchers[i].Alloc.PrintPauses(sw);
        }
        else
        {
            // Easier to debug without launching a separate thread
            ThreadLauncher t = new ThreadLauncher(0, args.perThreadArgs);
            t.Run();
            t.Alloc.PrintPauses(sw);
        }
        long tEnd = Environment.TickCount;

#if DEBUG
        Debug.Assert(Item.NumConstructed == Item.NumFreed);
        Debug.Assert(ReferenceItemWithSize.NumConstructed == ReferenceItemWithSize.NumFreed);
        Debug.Assert(SimpleRefPayLoad.NumPinned == SimpleRefPayLoad.NumUnpinned);
#endif

        sw.WriteLine("Took {0}ms", tEnd - tStart);
        sw.Flush();

        //sw.WriteLine("after init: heap size {0}, press any key to continue", GC.GetTotalMemory(false));
        //Console.ReadLine();

        sw.Flush();
        sw.Close();
    }

    public static int Main(string[] argsStrs)
    {
        Console.WriteLine($"Running 64-bit? {Environment.Is64BitProcess}");

        int currentPid = Process.GetCurrentProcess().Id;
        Console.WriteLine("PID: {0}", currentPid);

        Stopwatch stopwatch = new Stopwatch();
        stopwatch.Start();

        Args args;
        try
        {
            args = ArgsParser.Parse(argsStrs);
            args.Describe();
            DoTest(args, currentPid);
        }
        catch (Exception e)
        {
            Console.Error.WriteLine(e.Message);
            Console.Error.WriteLine(e.StackTrace);
            return 1;
        }

        if (args.endException)
        {
            GC.Collect(2, GCCollectionMode.Forced, true);
#if TODO
            EmptyWorkingSet(Process.GetCurrentProcess().Handle);
#endif
            //Debugger.Break();
            throw new System.ArgumentException("Just an opportunity for debugging", "test");
        }

        stopwatch.Stop();
        PrintResult(secondsTaken: stopwatch.Elapsed.TotalSeconds);
        
        return 0;
    }

    private static void PrintResult(double secondsTaken)
    {
        // GCPerfSim may print additional info before this.
        // dotnet-gc-infra will slice the stdout to after "=== STATS ===" and parse as yaml.
        // See `class GCPerfSimResult` in `bench_file.py`, and `_parse_gcperfsim_result` in `run_single_test.py`.
        Console.WriteLine("=== STATS ===");
        Console.WriteLine($"seconds_taken: {secondsTaken}");

        Console.Write($"collection_counts: [");
        for (int gen = 0; gen <= 2; gen++)
        {
            if (gen != 0) Console.Write(", ");
            Console.Write($"{GC.CollectionCount(gen)}");
        }
        Console.WriteLine("]");

        Console.WriteLine($"final_total_memory_bytes: {GC.GetTotalMemory(forceFullCollection: false)}");

        // Use reflection to detect GC.GetGCMemoryInfo because it doesn't exist in dotnet core 2.0 or in .NET framework.
        var getGCMemoryInfo = typeof(GC).GetMethod("GetGCMemoryInfo");
        if (getGCMemoryInfo != null)
        {
            object info = Util.NonNull(getGCMemoryInfo.Invoke(null, parameters: null));
            long heapSizeBytes = GetProperty<long>(info, "HeapSizeBytes");
            long fragmentedBytes = GetProperty<long>(info, "FragmentedBytes");
            Console.WriteLine($"final_heap_size_bytes: {heapSizeBytes}");
            Console.WriteLine($"final_fragmentation_bytes: {fragmentedBytes}");
        }
    }

    private static T GetProperty<T>(object instance, string name)
    {
        PropertyInfo property = Util.NonNull(instance.GetType().GetProperty(name));
        return (T) Util.NonNull(Util.NonNull(property.GetGetMethod()).Invoke(instance, parameters: null));
    }
}
