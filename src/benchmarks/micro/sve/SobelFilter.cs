using System;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.Arm;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Filters;
using MicroBenchmarks;

namespace SveBenchmarks
{
    [BenchmarkCategory(Categories.Runtime)]
    [OperatingSystemsArchitectureFilter(allowed: true, System.Runtime.InteropServices.Architecture.Arm64)]
    [Config(typeof(Config))]
    public class SobelFilter
    {
        private class Config : ManualConfig
        {
            public Config()
            {
                AddFilter(new SimpleFilter(_ => Sve.IsSupported));
            }
        }

        [Params(9, 64, 527)]
        public int Size;

        private float[] _source;
        private float[] _kx;
        private float[] _ky;
        private float[] _temp;
        private float[] _result;

        [GlobalSetup]
        public virtual void Setup()
        {
            _source = ValuesGenerator.Array<float>(Size * Size);
            _temp = new float[Size * Size];
            _result = new float[Size * Size];
            // A 3x3 Sobel filter can be decomposed into two 1x3/3x1 vectors.
            // Here the x Sobel operator is used, the y operator is just kx and ky swapped.
            _kx = new float[3] {-1,  0,  1};
            _ky = new float[3] { 1,  2,  1};
        }

        [GlobalCleanup]
        public virtual void Verify()
        {
            float[] current = (float[])_result.Clone();
            Setup();
            Scalar();
            float[] scalar = (float[])_result.Clone();
            // Check that the result is the same as the scalar result.
            for (int i = 0; i < current.Length; i++)
            {
                Debug.Assert(current[i] == scalar[i]);
            }
        }

        // The following algorithms are adapted from Arm "SVE Programming Examples":
        // https://developer.arm.com/documentation/dai0548/latest/ (example C1)

        [Benchmark]
        public unsafe void Scalar()
        {
            // The image is a Size * Size square.
            int img_size = Size;
            // The output image size is 2-pixel smaller in each direction.
            int out_size = img_size - 2;
            fixed (float* input = _source, temp = _temp, output = _result)
            fixed (float* kx = _kx, ky = _ky)
            {
                // Convolve the horizontal component first.
                // The result is save to the temp array.
                for (int j = 0; j < img_size; j++)
                {
                    for (int i = 0; i < out_size; i++)
                    {
                        float res = 0.0F;
                        for (int k = 0; k < 3; k++)
                        {
                            res += kx[k] * input[j * img_size + i + k];
                        }
                        temp[j * out_size + i] = res;
                    }
                }
                // Then convolve the vertical component.
                // Using the temp array as input.
                for (int j = 0; j < out_size; j++)
                {
                    for (int i = 0; i < out_size; i++)
                    {
                        float res = 0.0F;
                        for (int k = 0; k < 3; k++)
                        {
                            res += ky[k] * temp[(j + k) * out_size + i];
                        }
                        output[j * out_size + i] = res;
                    }
                }
            }
        }

        [Benchmark]
        public unsafe void Vector128SobelFilter()
        {
            fixed (float* input = _source, temp = _temp, output = _result)
            fixed (float* kx = _kx, ky = _ky)
            {
                int img_size = Size;
                int out_size = img_size - 2;

                Vector128<float> resVec;

                // Load coefficients of the filter, grouping the first two as vector.
                Vector64<float> kx01 = AdvSimd.LoadVector64(kx);
                Vector128<float> kx2 = Vector128.Create(kx[2]);
                Vector64<float> ky01 = AdvSimd.LoadVector64(ky);
                Vector128<float> ky2 = Vector128.Create(ky[2]);

                for (int j = 0; j < img_size; j++)
                {
                    // Load the elements from input and output the intermediate result to temp.
                    float* in_ptr = input + j * img_size;
                    float* out_ptr = temp + j * out_size;

                    int i = 0;
                    for (; i <= out_size - 4; i += 4)
                    {
                        // Load input elements from the next 3 columns.
                        Vector128<float> col0 = AdvSimd.LoadVector128(in_ptr + i);
                        Vector128<float> col1 = AdvSimd.LoadVector128(in_ptr + i + 1);
                        Vector128<float> col2 = AdvSimd.LoadVector128(in_ptr + i + 2);

                        // Multiply the coefficient vectors.
                        resVec = AdvSimd.MultiplyBySelectedScalar(col0, kx01, 0);
                        resVec = AdvSimd.Arm64.FusedMultiplyAddBySelectedScalar(resVec, col1, kx01, 1);
                        resVec = AdvSimd.FusedMultiplyAdd(resVec, col2, kx2);

                        AdvSimd.Store(out_ptr + i, resVec);
                    }
                    // Handle the remaining columns in scalar.
                    for (; i < out_size; i++)
                    {
                        float res = kx[0] * in_ptr[i];
                        res += kx[1] * in_ptr[i + 1];
                        res += kx[2] * in_ptr[i + 2];
                        out_ptr[i] = res;
                    }
                }
                for (int j = 0; j < out_size; j++)
                {
                    // Load the elements from temp and store result to the final output.
                    float* in_ptr = temp + j * out_size;
                    float* out_ptr = output + j * out_size;

                    int i = 0;
                    for (; i <= out_size - 4; i += 4)
                    {
                        // Load input elements from the next 3 rows.
                        Vector128<float> row0 = AdvSimd.LoadVector128(in_ptr + i);
                        Vector128<float> row1 = AdvSimd.LoadVector128(in_ptr + i + out_size);
                        Vector128<float> row2 = AdvSimd.LoadVector128(in_ptr + i + 2 * out_size);

                        // Multiply the coefficient vectors.
                        resVec = AdvSimd.MultiplyBySelectedScalar(row0, ky01, 0);
                        resVec = AdvSimd.Arm64.FusedMultiplyAddBySelectedScalar(resVec, row1, ky01, 1);
                        resVec = AdvSimd.FusedMultiplyAdd(resVec, row2, ky2);

                        AdvSimd.Store(out_ptr + i, resVec);
                    }
                    // Handle the remaining columns in scalar.
                    for (; i < out_size; i++)
                    {
                        float res = ky[0] * in_ptr[i];
                        res += ky[1] * in_ptr[i + out_size];
                        res += ky[2] * in_ptr[i + 2 * out_size];
                        out_ptr[i] = res;
                    }
                }
            }
        }

        [Benchmark]
        public unsafe void SveSobelFilter()
        {
            fixed (float* input = _source, temp = _temp, output = _result)
            fixed (float* kx = _kx, ky = _ky)
            {
                int img_size = Size;
                int out_size = img_size - 2;
                int cntw = (int)Sve.Count32BitElements();

                Vector<float> resVec;
                // Load coefficients of the filter into vectors.
                Vector<float> kxVec = Sve.LoadVector((Vector<float>)Sve.CreateWhileLessThanMask32Bit(0, 3), kx);
                Vector<float> kyVec = Sve.LoadVector((Vector<float>)Sve.CreateWhileLessThanMask32Bit(0, 3), ky);
                for (int j = 0; j < img_size; j++)
                {
                    // Load the elements from input and output the intermediate result to temp.
                    float* in_ptr = input + j * img_size;
                    float* out_ptr = temp + j * out_size;

                    for (int i = 0; i < out_size; i += cntw)
                    {
                        Vector<float> pRow = (Vector<float>)Sve.CreateWhileLessThanMask32Bit(i, out_size);

                        // Load input elements from the next 3 columns.
                        Vector<float> col0 = Sve.LoadVector(pRow, in_ptr + i);
                        Vector<float> col1 = Sve.LoadVector(pRow, in_ptr + i + 1);
                        Vector<float> col2 = Sve.LoadVector(pRow, in_ptr + i + 2);

                        // Multiply the coefficients using lanewise access.
                        resVec = Sve.MultiplyBySelectedScalar(col0, kxVec, 0);
                        resVec = Sve.FusedMultiplyAddBySelectedScalar(resVec, col1, kxVec, 1);
                        resVec = Sve.FusedMultiplyAddBySelectedScalar(resVec, col2, kxVec, 2);

                        Sve.StoreAndZip(pRow, out_ptr + i, resVec);
                    }
                }
                for (int j = 0; j < out_size; j++)
                {
                    // Load the elements from temp and store result to the final output.
                    float* in_ptr = temp + j * out_size;
                    float* out_ptr = output + j * out_size;

                    for (int i = 0; i < out_size; i += cntw)
                    {
                        Vector<float> pRow = (Vector<float>)Sve.CreateWhileLessThanMask32Bit(i, out_size);

                        // Load input elements from the next 3 rows.
                        Vector<float> row0 = Sve.LoadVector(pRow, in_ptr + i);
                        Vector<float> row1 = Sve.LoadVector(pRow, in_ptr + i + out_size);
                        Vector<float> row2 = Sve.LoadVector(pRow, in_ptr + i + 2 * out_size);

                        // Multiply the coefficients using lanewise access.
                        resVec = Sve.MultiplyBySelectedScalar(row0, kyVec, 0);
                        resVec = Sve.FusedMultiplyAddBySelectedScalar(resVec, row1, kyVec, 1);
                        resVec = Sve.FusedMultiplyAddBySelectedScalar(resVec, row2, kyVec, 2);

                        Sve.StoreAndZip(pRow, out_ptr + i, resVec);
                    }
                }
            }
        }

    }
}
