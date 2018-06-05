// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
/*
** This program was translated to C# and adapted for xunit-performance.
** New variants of several tests were added to compare class versus
** struct and to compare jagged arrays vs multi-dimensional arrays.
*/

/*
** BYTEmark (tm)
** BYTE Magazine's Native Mode benchmarks
** Rick Grehan, BYTE Magazine
**
** Create:
** Revision: 3/95
**
** DISCLAIMER
** The source, executable, and documentation files that comprise
** the BYTEmark benchmarks are made available on an "as is" basis.
** This means that we at BYTE Magazine have made every reasonable
** effort to verify that the there are no errors in the source and
** executable code.  We cannot, however, guarantee that the programs
** are error-free.  Consequently, McGraw-HIll and BYTE Magazine make
** no claims in regard to the fitness of the source code, executable
** code, and documentation of the BYTEmark.
**
** Furthermore, BYTE Magazine, McGraw-Hill, and all employees
** of McGraw-Hill cannot be held responsible for any damages resulting
** from the use of this code or the results obtained from using
** this code.
*/

using System;
using System.IO;
using BenchmarkDotNet.Attributes;

#pragma warning disable CS0649, CS0169

internal class global
{
    public static long min_ticks;
    public static int min_secs;
    public static bool allstats;
    public static String ofile_name;    // Output file name
    public static StreamWriter ofile;   // Output file
    public static bool custrun;         // Custom run flag
    public static bool write_to_file;   // Write output to file
    public static int align;            // Memory alignment

    /*
    ** Following are global structures, one built for
    ** each of the tests.
    */
    public static SortStruct numsortstruct_jagged;    // For numeric sort
    public static SortStruct numsortstruct_rect;      // For numeric sort
    public static StringSort strsortstruct;           // For string sort
    public static BitOpStruct bitopstruct;            // For bitfield ops
    public static EmFloatStruct emfloatstruct_struct; // For emul. float. pt.
    public static EmFloatStruct emfloatstruct_class;  // For emul. float. pt.
    public static FourierStruct fourierstruct;        // For fourier test
    public static AssignStruct assignstruct_jagged;   // For assignment algs
    public static AssignStruct assignstruct_rect;     // For assignment algs
    public static IDEAStruct ideastruct;              // For IDEA encryption
    public static HuffStruct huffstruct;              // For Huffman compression
    public static NNetStruct nnetstruct_jagged;       // For Neural Net
    public static NNetStruct nnetstruct_rect;         // For Neural Net
    public static LUStruct lustruct;                  // For LU decomposition

    public const long TICKS_PER_SEC = 1000;
    public const long MINIMUM_TICKS = 60; // 60 msecs

    public const int MINIMUM_SECONDS = 1;

    public const int NUMNUMARRAYS = 1000;
    public const int NUMARRAYSIZE = 8111;
    public const int STRINGARRAYSIZE = 8111;
    // This is the upper limit of number of string arrays to sort in one
    // iteration. If we can sort more than this number of arrays in less
    // than MINIMUM_TICKS an exception is thrown.
    public const int NUMSTRARRAYS = 100;
    public const int HUFFARRAYSIZE = 5000;
    public const int MAXHUFFLOOPS = 50000;

    // Assignment constants
    public const int ASSIGNROWS = 101;
    public const int ASSIGNCOLS = 101;
    public const int MAXPOSLONG = 0x7FFFFFFF;

    // BitOps constants
#if LONG64
        public const int BITFARRAYSIZE = 16384;
#else
    public const int BITFARRAYSIZE = 32768;
#endif

    // IDEA constants
    public const int MAXIDEALOOPS = 5000;
    public const int IDEAARRAYSIZE = 4000;
    public const int IDEAKEYSIZE = 16;
    public const int IDEABLOCKSIZE = 8;
    public const int ROUNDS = 8;
    public const int KEYLEN = (6 * ROUNDS + 4);

    // LUComp constants
    public const int LUARRAYROWS = 101;
    public const int LUARRAYCOLS = 101;

    // EMFLOAT constants
    public const int CPUEMFLOATLOOPMAX = 50000;
    public const int EMFARRAYSIZE = 3000;

    // FOURIER constants
    public const int FOURIERARRAYSIZE = 100;
}
#pragma warning restore CS0649, CS0169
/*
** TYPEDEFS
*/

public abstract class HarnessTest
{
    public bool bRunTest = true;
    public double score;
    public int adjust;        /* Set adjust code */
    public int request_secs;  /* # of seconds requested */

    public abstract string Name();
    public abstract void ShowStats();
    public abstract double Run();
}

public abstract class SortStruct : HarnessTest
{
    public short numarrays = global.NUMNUMARRAYS;   /* # of arrays */
    public int arraysize = global.NUMARRAYSIZE;     /* # of elements in array */
    public override void ShowStats()
    {
        ByteMark.OutputString(
            string.Format("  Number of arrays: {0}", numarrays));
        ByteMark.OutputString(
            string.Format("  Array size: {0}", arraysize));
    }
}

public abstract class StringSortStruct : HarnessTest
{
    public short numarrays = global.NUMNUMARRAYS;   /* # of arrays */
    public int arraysize = global.STRINGARRAYSIZE;     /* # of elements in array */
    public override void ShowStats()
    {
        ByteMark.OutputString(
            string.Format("  Number of arrays: {0}", numarrays));
        ByteMark.OutputString(
            string.Format("  Array size: {0}", arraysize));
    }
}

public abstract class HuffStruct : HarnessTest
{
    public int arraysize = global.HUFFARRAYSIZE;
    public int loops = 0;
    public override void ShowStats()
    {
        ByteMark.OutputString(
            string.Format("  Array size: {0}", arraysize));
        ByteMark.OutputString(
            string.Format("  Number of loops: {0}", loops));
    }
}

public abstract class FourierStruct : HarnessTest
{
    public int arraysize = global.FOURIERARRAYSIZE;
    public override void ShowStats()
    {
        ByteMark.OutputString(
            string.Format("  Number of coefficients: {0}", arraysize));
    }
}

public abstract class AssignStruct : HarnessTest
{
    public short numarrays = global.NUMNUMARRAYS;   /* # of elements in array */
    public override void ShowStats()
    {
        ByteMark.OutputString(
            string.Format("  Number of arrays: {0}", numarrays));
    }
}

public abstract class BitOpStruct : HarnessTest
{
    public int bitoparraysize;                      /* Total # of bitfield ops */
    public int bitfieldarraysize = global.BITFARRAYSIZE; /* Bit field array size */
    public override void ShowStats()
    {
        ByteMark.OutputString(
            string.Format("  Operations array size: {0}", bitoparraysize));
        ByteMark.OutputString(
            string.Format("  Bitfield array size: {0}", bitfieldarraysize));
    }
}

public abstract class IDEAStruct : HarnessTest
{
    public int arraysize = global.IDEAARRAYSIZE;    /* Size of array */
    public int loops;                               /* # of times to convert */
    public override void ShowStats()
    {
        ByteMark.OutputString(
            string.Format("  Array size: {0}", arraysize));
        ByteMark.OutputString(
            string.Format("  Number of loops: {0}", loops));
    }
}

public abstract class LUStruct : HarnessTest
{
    public int numarrays;
    public override void ShowStats()
    {
        ByteMark.OutputString(
            string.Format("  Number of arrays: {0}", numarrays));
    }
}

public abstract class NNetStruct : HarnessTest
{
    public int loops;            /* # of times to learn */
    public double iterspersec;     /* Results */
    public override void ShowStats()
    {
        ByteMark.OutputString(
            string.Format("  Number of loops: {0}", loops));
    }
}

public abstract class EmFloatStruct : HarnessTest
{
    public int arraysize = global.EMFARRAYSIZE;     /* Size of array */
    public int loops;                               /* Loops per iterations */
    public override void ShowStats()
    {
        ByteMark.OutputString(
            string.Format("  Number of loops: {0}", loops));
        ByteMark.OutputString(
            string.Format("  Array size: {0}", arraysize));
    }
}

public class ByteMark
{
    private static int[] s_randw;

    public static void OutputString(String s)
    {
        Console.WriteLine(s);
        if (global.write_to_file)
        {
            global.ofile.WriteLine(s);
            global.ofile.Flush();
        }
    }

    /****************************
    ** TicksToSecs
    ** Converts ticks to seconds.  Converts ticks to integer
    ** seconds, discarding any fractional amount.
    */
    public static int TicksToSecs(long tickamount)
    {
        return ((int)(tickamount / global.TICKS_PER_SEC));
    }

    /****************************
    ** TicksToFracSecs
    ** Converts ticks to fractional seconds.  In other words,
    ** this returns the exact conversion from ticks to
    ** seconds.
    */
    public static double TicksToFracSecs(long tickamount)
    {
        return ((double)tickamount / (double)global.TICKS_PER_SEC);
    }

    public static long StartStopwatch()
    {
        //DateTime t = DateTime.Now;
        //return(t.Ticks);
        return Environment.TickCount;
    }

    public static long StopStopwatch(long start)
    {
        //DateTime t = DateTime.Now;
        //Console.WriteLine(t.Ticks - start);
        //return(t.Ticks-start);
        long x = Environment.TickCount - start;
        //Console.WriteLine(x);
        return x;
    }

    /****************************
    *         randwc()          *
    *****************************
    ** Returns int random modulo num.
    */
    public static int randwc(int num)
    {
        return (randnum(0) % num);
    }

    /***************************
    **      abs_randwc()      **
    ****************************
    ** Same as randwc(), only this routine returns only
    ** positive numbers.
    */
    public static int abs_randwc(int num)
    {
        int temp;       /* Temporary storage */

        temp = randwc(num);
        if (temp < 0) temp = 0 - temp;

        return temp;
    }

    /****************************
    *        randnum()          *
    *****************************
    ** Second order linear congruential generator.
    ** Constants suggested by J. G. Skellam.
    ** If val==0, returns next member of sequence.
    **    val!=0, restart generator.
    */
    public static int randnum(int lngval)
    {
        int interm;

        if (lngval != 0L)
        { s_randw[0] = 13; s_randw[1] = 117; }

        unchecked
        {
            interm = (s_randw[0] * 254754 + s_randw[1] * 529562) % 999563;
        }
        s_randw[1] = s_randw[0];
        s_randw[0] = interm;
        return (interm);
    }

    [GlobalSetup]
    public void Setup()
    {
        s_randw = new int[2] { 13, 117 };
        global.min_ticks = global.MINIMUM_TICKS;
        global.min_secs = global.MINIMUM_SECONDS;
        global.allstats = false;
        global.custrun = false;
        global.align = 8;
        global.write_to_file = false;
    }

    const int NumericSortJaggedIterations = 10;

    [Benchmark]
    public void BenchNumericSortJagged()
    {
        NumericSortJagged t = new NumericSortJagged();
        t.numarrays = 200;
        t.adjust = 1;

        for (int i = 0; i < NumericSortJaggedIterations; i++)
            t.Run();
    }

    const int NumericSortRectangularIterations = 5;

    [Benchmark]
    public void BenchNumericSortRectangular()
    {
        NumericSortRect t = new NumericSortRect();
        t.numarrays = 200;
        t.adjust = 1;

        for (int i = 0; i < NumericSortRectangularIterations; i++)
            t.Run();
    }

    const int StringSortIterations = 15;

    [Benchmark]
    public void BenchStringSort()
    {
        StringSort t = new StringSort();
        t.numarrays = 40;
        t.adjust = 1;

        for (int i = 0; i < StringSortIterations; i++)
            t.Run();
    }

    const int BitOpsIterations = 100000;

    [Benchmark]
    public void BenchBitOps()
    {
        BitOps t = new BitOps();
        t.adjust = 1;

        for (int i = 0; i < BitOpsIterations; i++)
            t.Run();
    }

    const int EmFloatIterations = 10;

    [Benchmark]
    public void BenchEmFloat()
    {
        EmFloatStruct t = new EMFloat();
        t.loops = 50;
        t.adjust = 1;

        for (int i = 0; i < EmFloatIterations; i++)
            t.Run();
    }

    const int EmFloatClassIterations = 2;

    [Benchmark]
    public void BenchEmFloatClass()
    {
        EmFloatStruct t = new EMFloatClass();
        t.loops = 50;
        t.adjust = 1;

        for (int i = 0; i < EmFloatClassIterations; i++)
            t.Run();
    }

    const int FourierIterations = 300;

    [Benchmark]
    public void BenchFourier()
    {
        FourierStruct t = new Fourier();
        t.adjust = 1;

        for (int i = 0; i < FourierIterations; i++)
            t.Run();
    }

    const int AssignJaggedIterations = 2;

    [Benchmark]
    public void BenchAssignJagged()
    {
        AssignStruct t = new AssignJagged();
        t.numarrays = 25;
        t.adjust = 1;

        for (int i = 0; i < AssignJaggedIterations; i++)
            t.Run();
    }

    const int AssignRectangularIterations = 5;

    [Benchmark]
    public void BenchAssignRectangular()
    {
        AssignStruct t = new AssignRect();
        t.numarrays = 10;
        t.adjust = 1;

        for (int i = 0; i < AssignRectangularIterations; i++)
            t.Run();
    }

    const int IDEAEncryptionIterations = 50;

    [Benchmark]
    public void BenchIDEAEncryption()
    {
        IDEAStruct t = new IDEAEncryption();
        t.loops = 100;
        t.adjust = 1;

        for (int i = 0; i < IDEAEncryptionIterations; i++)
            t.Run();
    }

    const int NeuralJaggedIterations = 10;

    [Benchmark]
    public void BenchNeuralJagged()
    {
        NNetStruct t = new NeuralJagged();
        t.loops = 3;
        t.adjust = 1;

        for (int i = 0; i < NeuralJaggedIterations; i++)
            t.Run();
    }

    const int NeuralIterations = 20;

    [Benchmark]
    public void BenchNeural()
    {
        NNetStruct t = new Neural();
        t.loops = 1;
        t.adjust = 1;

        for (int i = 0; i < NeuralIterations; i++)
            t.Run();
    }

    const int LUDecompIterations = 10;

    [Benchmark]
    public void BenchLUDecomp()
    {
        LUStruct t = new LUDecomp();
        t.numarrays = 250;
        t.adjust = 1;

        for (int i = 0; i < LUDecompIterations; i++)
            t.Run();
    }
}
