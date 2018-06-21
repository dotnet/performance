// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//
// .NET SIMD to solve Burgers' equation
//
// Benchmark based on
// https://github.com/taumuon/SIMD-Vectorisation-Burgers-Equation-CSharp
// http://www.taumuon.co.uk/2014/10/net-simd-to-solve-burgers-equation.html

using System;
using System.Linq;
using System.Numerics;
using BenchmarkDotNet.Attributes;
using Benchmarks;

[BenchmarkCategory(Categories.CoreCLR)]
public class Burgers
{
    private static double BurgersAnalytical(double t, double x, double nu)
    {
        return -2 * nu * (-(-8 * t + 2 * x) * Math.Exp(-Math.Pow((-4 * t + x), 2) / (4 * nu * (t + 1))) / (4 * nu * (t + 1)) - (-8 * t + 2 * x - 12.5663706143592) * Math.Exp(-Math.Pow(-4 * t + x - 6.28318530717959, 2) / (4 * nu * (t + 1))) / (4 * nu * (t + 1))) / (Math.Exp(-Math.Pow(-4 * t + x - 6.28318530717959, 2) / (4 * nu * (t + 1))) + Math.Exp(-Math.Pow(-4 * t + x, 2) / (4 * nu * (t + 1)))) + 4;
    }

    private static double[] linspace(double first, double last, int num)
    {
        var step = (last - first) / (double)num;
        return Enumerable.Range(0, num).Select(v => (v * step) + first).ToArray();
    }

    private static double[] GetAnalytical(double[] x, double t, double nu)
    {
        double[] u = new double[x.Length];

        for (int i = 0; i < x.Length; ++i)
        {
            u[i] = BurgersAnalytical(t, x[i], nu);
        }

        return u;
    }

    private static double[] GetCalculated0(int nt, int nx, double dx, double dt, double nu, double[] initial)
    {
        double[] u = new double[nx];
        Array.Copy(initial, u, u.Length);

        for (int tStep = 0; tStep < nt; tStep++)
        {
            double[] un = new double[nx];
            Array.Copy(u, un, u.Length);

            for (int i = 1; i < nx - 1; i++)
            {
                u[i] = un[i] - un[i] * dt / dx * (un[i] - un[i - 1]) + Math.Pow(nu * dt / dx, 2.0) *
                        (un[i + 1] - 2 * un[i] + un[i - 1]);
            }

            u[0] = un[0] - un[0] * dt / dx * (un[0] - un[nx - 1]) + Math.Pow(nu * dt / dx, 2.0) *
                        (un[1] - 2 * un[0] + un[nx - 1]);

            u[nx - 1] = un[nx - 1] - un[nx - 1] * dt / dx * (un[nx - 1] - un[nx - 2]) + Math.Pow(nu * dt / dx, 2.0) *
                        (un[0] - 2 * un[nx - 1] + un[nx - 2]);
        }

        return u;
    }

    // Reduce new array allocation and copying, ping-pong between them
    private static double[] GetCalculated1(int nt, int nx, double dx, double dt, double nu, double[] initial)
    {
        double[] u = new double[nx];
        double[] un = new double[nx];
        Array.Copy(initial, un, un.Length);

        for (int tStep = 0; tStep < nt; tStep++)
        {
            for (int i = 1; i < nx - 1; i++)
            {
                u[i] = un[i] - un[i] * dt / dx * (un[i] - un[i - 1]) + Math.Pow(nu * dt / dx, 2.0) *
                        (un[i + 1] - 2 * un[i] + un[i - 1]);
            }

            u[0] = un[0] - un[0] * dt / dx * (un[0] - un[nx - 1]) + Math.Pow(nu * dt / dx, 2.0) *
                        (un[1] - 2 * un[0] + un[nx - 1]);

            u[nx - 1] = un[nx - 1] - un[nx - 1] * dt / dx * (un[nx - 1] - un[nx - 2]) + Math.Pow(nu * dt / dx, 2.0) *
                        (un[0] - 2 * un[nx - 1] + un[nx - 2]);

            double[] swap = u;
            u = un;
            un = swap;
        }

        return un;
    }

    // Pull calculation of (nu * dt / dx)^2 out into a variable
    private static double[] GetCalculated2(int nt, int nx, double dx, double dt, double nu, double[] initial)
    {
        double[] u = new double[nx];
        double[] un = new double[nx];
        Array.Copy(initial, un, un.Length);

        double factor = Math.Pow(nu * dt / dx, 2.0);

        for (int tStep = 0; tStep < nt; tStep++)
        {
            for (int i = 1; i < nx - 1; i++)
            {
                u[i] = un[i] - un[i] * dt / dx * (un[i] - un[i - 1]) + factor *
                        (un[i + 1] - 2 * un[i] + un[i - 1]);
            }

            u[0] = un[0] - un[0] * dt / dx * (un[0] - un[nx - 1]) + factor *
                        (un[1] - 2 * un[0] + un[nx - 1]);

            u[nx - 1] = un[nx - 1] - un[nx - 1] * dt / dx * (un[nx - 1] - un[nx - 2]) + factor *
                        (un[0] - 2 * un[nx - 1] + un[nx - 2]);

            double[] swap = u;
            u = un;
            un = swap;
        }

        return un;
    }

    // SIMD
    private static double[] GetCalculated3(int nt, int nx, double dx, double dt, double nu, double[] initial)
    {
        var nx2 = nx + (Vector<double>.Count - (nx % Vector<double>.Count));

        double[] u = new double[nx2];
        double[] un = new double[nx2];
        Array.Copy(initial, un, initial.Length);

        double factor = Math.Pow(nu * dt / dx, 2.0);

        for (int tStep = 0; tStep < nt; tStep++)
        {
            for (int i = 1; i < nx2 - Vector<double>.Count + 1; i += Vector<double>.Count)
            {
                var vectorIn0 = new Vector<double>(un, i);
                var vectorInPrev = new Vector<double>(un, i - 1);
                var vectorInNext = new Vector<double>(un, i + 1);

                var vectorOut = vectorIn0 - vectorIn0 * (dt / dx) * (vectorIn0 - vectorInPrev) + factor *
                    (vectorInNext - 2.0 * vectorIn0 + vectorInPrev);

                vectorOut.CopyTo(u, i);
            }

            u[0] = un[0] - un[0] * dt / dx * (un[0] - un[nx - 1]) + factor *
                        (un[1] - 2 * un[0] + un[nx - 1]);

            u[nx - 1] = un[nx - 1] - un[nx - 1] * dt / dx * (un[nx - 1] - un[nx - 2]) + factor *
                        (un[0] - 2 * un[nx - 1] + un[nx - 2]);

            double[] swap = u;
            u = un;
            un = swap;
        }

        return un;
    }


    const int nx = 10001;
    const int nt = 10000;
    const double nu = 0.07;
    
    double dx, dt;
    double[] x, initial;
    
    [GlobalSetup]
    public void Setup()
    {
        dx = 2.0 * Math.PI / (nx - 1.0);
        dt = dx * nu;
        x = linspace(0.0, 2.0 * Math.PI, nx);
        initial = GetAnalytical(x, 0.0, nu);
    }

    [Benchmark(Description = "Burgers_0")]
    public double[] Test0() => GetCalculated0(nt, nx, dx, dt, nu, initial);

    [Benchmark(Description = "Burgers_1")]
    public double[] Test1() => GetCalculated1(nt, nx, dx, dt, nu, initial);

    [Benchmark(Description = "Burgers_2")]
    public double[] Test2() => GetCalculated2(nt, nx, dx, dt, nu, initial);

    [Benchmark(Description = "Burgers_3")]
    public double[] Test3() => GetCalculated3(nt * 2, nx, dx, dt, nu, initial);
}

