// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
/// <license>
/// This is a port of the SciMark2a Java Benchmark to C# by
/// Chris Re (cmr28@cornell.edu) and Werner Vogels (vogels@cs.cornell.edu)
///
/// For details on the original authors see http://math.nist.gov/scimark2
///
/// This software is likely to burn your processor, bitflip your memory chips
/// anihilate your screen and corrupt all your disks, so you it at your
/// own risk.
/// </license>


using BenchmarkDotNet.Attributes;
using MicroBenchmarks;

namespace SciMark2
{
    [BenchmarkCategory(Categories.Runtime, Categories.JIT, Categories.SciMark)]
    public class kernel 
    {
        double[] inputFFT;
        
        double[][]  inputSOR;

        private double[] inputSparseMultX;
        private double[] inputSparseMultY;
        private double[] inputSparseMultVal;
        private int[] inputSparseMultCol;
        private int[] inputSparseMultRow;

        private double[][] inputLU;
        private int[] inputLUPivot;
        private double[][] inputLUA;

        [GlobalSetup(Target = nameof(benchFFT))]
        public void SetupFFT()
        {
            Random R = new SciMark2.Random(Constants.RANDOM_SEED);
            int N = Constants.FFT_SIZE;
            inputFFT = RandomVector(2 * N, R);
        }
        
        [Benchmark]
        public void benchFFT()
        {
            long Iterations = 20000;

            innerFFT(inputFFT, Iterations);
        }

        private static void innerFFT(double[] x, long Iterations)
        {
            for (int i = 0; i < Iterations; i++)
            {
                FFT.transform(x); // forward transform
                FFT.inverse(x);   // backward transform
            }
        }

        [GlobalSetup(Target = nameof(benchSOR))]
        public void SetupSOR()
        {
            Random R = new SciMark2.Random(Constants.RANDOM_SEED);
            int N = Constants.SOR_SIZE;
            
            inputSOR = RandomMatrix(N, N, R);
        }

        [Benchmark]
        public void benchSOR()
        {
            int Iterations = 20000;

            SOR.execute(1.25, inputSOR, Iterations);
        }

        [Benchmark]
        public void benchMonteCarlo()
        {
            int Iterations = 40000000;
            MonteCarlo.integrate(Iterations);
        }

        [GlobalSetup(Target = nameof(benchSparseMult))]
        public void SetupSparseMult()
        {
            Random R = new SciMark2.Random(Constants.RANDOM_SEED);
            
            int N = Constants.SPARSE_SIZE_M;
            int nz = Constants.SPARSE_SIZE_nz;

            inputSparseMultX = RandomVector(N, R);
            inputSparseMultY = new double[N];
            int nr = nz / N; // average number of nonzeros per row
            int anz = nr * N; // _actual_ number of nonzeros
            inputSparseMultVal = RandomVector(anz, R);
            inputSparseMultCol = new int[anz];
            inputSparseMultRow = new int[N + 1];

            inputSparseMultRow[0] = 0;
            for (int r = 0; r < N; r++)
            {
                // initialize elements for row r

                int rowr = inputSparseMultRow[r];
                inputSparseMultRow[r + 1] = rowr + nr;
                int step = r / nr;
                if (step < 1)
                    step = 1;
                // take at least unit steps

                for (int i = 0; i < nr; i++)
                    inputSparseMultCol[rowr + i] = i * step;
            }
        }

        [Benchmark]
        public void benchSparseMult()
        {
            int Iterations = 100000;
            
            SparseCompRow.matmult(inputSparseMultY, inputSparseMultVal, inputSparseMultRow, inputSparseMultCol, inputSparseMultX, Iterations);
        }

        [GlobalSetup(Target = nameof(benchmarkLU))]
        public void SetupLU()
        {
            Random R = new SciMark2.Random(Constants.RANDOM_SEED);
            
            int N = Constants.LU_SIZE;

            inputLUA = RandomMatrix(N, N, R);
            inputLU = new double[N][];
            for (int i = 0; i < N; i++)
            {
                inputLU[i] = new double[N];
            }
            inputLUPivot = new int[N];
        }

        [Benchmark]
        public void benchmarkLU()
        {
            int Iterations = 2000;

            for (int i = 0; i < Iterations; i++)
            {
                CopyMatrix(inputLU, inputLUA);
                LU.factor(inputLU, inputLUPivot);
            }
        }

        private static void CopyMatrix(double[][] B, double[][] A)
        {
            int M = A.Length;
            int N = A[0].Length;

            int remainder = N & 3; // N mod 4;

            for (int i = 0; i < M; i++)
            {
                double[] Bi = B[i];
                double[] Ai = A[i];
                for (int j = 0; j < remainder; j++)
                    Bi[j] = Ai[j];
                for (int j = remainder; j < N; j += 4)
                {
                    Bi[j] = Ai[j];
                    Bi[j + 1] = Ai[j + 1];
                    Bi[j + 2] = Ai[j + 2];
                    Bi[j + 3] = Ai[j + 3];
                }
            }
        }

        private static double[][] RandomMatrix(int M, int N, Random R)
        {
            double[][] A = new double[M][];
            for (int i = 0; i < M; i++)
            {
                A[i] = new double[N];
            }

            for (int i = 0; i < N; i++)
                for (int j = 0; j < N; j++)
                    A[i][j] = R.nextDouble();
            return A;
        }

        private static double[] RandomVector(int N, Random R)
        {
            double[] A = new double[N];

            for (int i = 0; i < N; i++)
                A[i] = R.nextDouble();
            return A;
        }

        private static double[] matvec(double[][] A, double[] x)
        {
            int N = x.Length;
            double[] y = new double[N];

            matvec(A, x, y);

            return y;
        }

        private static void matvec(double[][] A, double[] x, double[] y)
        {
            int M = A.Length;
            int N = A[0].Length;

            for (int i = 0; i < M; i++)
            {
                double sum = 0.0;
                double[] Ai = A[i];
                for (int j = 0; j < N; j++)
                    sum += Ai[j] * x[j];

                y[i] = sum;
            }
        }
    }
}
