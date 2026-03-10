// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// ThreadPool scheduling efficiency benchmark.
///
/// Models "transactions" that flow through a sequence of ThreadPool work items,
/// each doing a calibrated amount of CPU work (in µsec). Measures actual
/// wall-clock latency per transaction and compares against the theoretical
/// minimum (sum of per-step work durations).
///
/// Sweeps concurrency levels to show how scheduling efficiency degrades
/// under contention.
/// </summary>
class Program
{
    // ───────────────────────── Calibrated work kernel ─────────────────────────

    /// <summary>Iterations of the FP-divide loop per microsecond, calibrated at startup.</summary>
    static double s_iterationsPerUsec;

    /// <summary>
    /// Reciprocal FP divisions that nearly cancel out, keeping the value stable.
    /// Two dependent divisions per iteration create a serial data-dependency chain.
    /// </summary>
    [MethodImpl(MethodImplOptions.NoInlining)]
    static double FdivWork(int iterations)
    {
        double divd = 123456789.0;
        for (int i = 0; i < iterations; i++)
        {
            divd /= 1.0000001;
            divd /= 0.9999999;
        }
        return divd;
    }

    /// <summary>
    /// Busy-wait for approximately <paramref name="usec"/> microseconds of CPU work.
    /// </summary>
    [MethodImpl(MethodImplOptions.NoInlining)]
    static double FdivWaitUsec(double usec)
    {
        int iterations = (int)(usec * s_iterationsPerUsec);
        return FdivWork(iterations);
    }

    /// <summary>
    /// Calibrate <see cref="s_iterationsPerUsec"/> by timing a known iteration count.
    /// </summary>
    static void Calibrate()
    {
        // Warm up JIT
        FdivWork(100_000);

        // Calibrate using burst sizes similar to actual workloads (not one long run).
        // Median of many short bursts avoids turbo-boost bias.
        const int burstIterations = 200_000; // ~1–2ms burst, similar to typical work items
        const int calibrationRounds = 15;
        double[] samples = new double[calibrationRounds];

        for (int round = 0; round < calibrationRounds; round++)
        {
            var sw = Stopwatch.StartNew();
            FdivWork(burstIterations);
            sw.Stop();
            samples[round] = burstIterations / sw.Elapsed.TotalMicroseconds;
        }

        Array.Sort(samples);
        s_iterationsPerUsec = samples[calibrationRounds / 2]; // median

        Console.WriteLine($"Calibration: {s_iterationsPerUsec:F1} iterations/µsec " +
                          $"(median of {calibrationRounds} rounds, {burstIterations:N0} iterations each)");

        // Verify accuracy across several durations
        foreach (int targetUsec in new[] { 100, 1000, 5000 })
        {
            var verify = Stopwatch.StartNew();
            FdivWaitUsec(targetUsec);
            verify.Stop();
            double actual = verify.Elapsed.TotalMicroseconds;
            double error = (actual - targetUsec) / targetUsec * 100;
            Console.WriteLine($"  Verify: FdivWaitUsec({targetUsec}) = {actual:F0} µsec (error: {error:+0.0;-0.0}%)");
        }
        Console.WriteLine();
    }

    // ───────────────────────── Transaction model ─────────────────────────

    /// <summary>One step of a transaction: do <see cref="WorkUsec"/> of work.</summary>
    readonly record struct TransactionStep(int QueueId, double WorkUsec);

    /// <summary>
    /// A transaction is a sequence of steps. Each step is executed as a
    /// ThreadPool work item; completing one step queues the next.
    /// </summary>
    sealed class Transaction
    {
        public int Id;
        public TransactionStep[] Steps = Array.Empty<TransactionStep>();
        public double TheoreticalUsec; // sum of all step WorkUsec values
        public double ActualUsec;      // measured wall-clock latency
    }

    // ───────────────────────── Workload generation ─────────────────────────

    /// <summary>
    /// Generate a batch of transactions with random per-step work durations.
    /// Work durations are log-uniformly distributed between
    /// <paramref name="minWorkUsec"/> and <paramref name="maxWorkUsec"/>.
    /// </summary>
    static Transaction[] GenerateWorkload(
        int count,
        int stepsPerTransaction,
        double minWorkUsec,
        double maxWorkUsec,
        int numQueues,
        int seed)
    {
        var rng = new Random(seed);
        var transactions = new Transaction[count];
        double logMin = Math.Log(minWorkUsec);
        double logMax = Math.Log(maxWorkUsec);

        for (int t = 0; t < count; t++)
        {
            var steps = new TransactionStep[stepsPerTransaction];
            double theoretical = 0;

            for (int s = 0; s < stepsPerTransaction; s++)
            {
                int queueId = rng.Next(numQueues);
                // Log-uniform distribution spans orders of magnitude
                double workUsec = Math.Exp(logMin + rng.NextDouble() * (logMax - logMin));
                steps[s] = new TransactionStep(queueId, workUsec);
                theoretical += workUsec;
            }

            transactions[t] = new Transaction
            {
                Id = t,
                Steps = steps,
                TheoreticalUsec = theoretical
            };
        }

        return transactions;
    }

    // ───────────────────────── Transaction execution ─────────────────────────

    /// <summary>
    /// Execute a single transaction through the ThreadPool.
    /// Each step is queued as a work item; completing step N queues step N+1.
    /// </summary>
    static void ExecuteTransaction(Transaction txn, CountdownEvent allDone)
    {
        var sw = Stopwatch.StartNew();
        ExecuteStep(txn, 0, sw, allDone);
    }

    static void ExecuteStep(Transaction txn, int stepIndex, Stopwatch sw, CountdownEvent allDone)
    {
        if (stepIndex >= txn.Steps.Length)
        {
            // All steps complete — record latency
            sw.Stop();
            txn.ActualUsec = sw.Elapsed.TotalMicroseconds;
            allDone.Signal();
            return;
        }

        ThreadPool.UnsafeQueueUserWorkItem(_ =>
        {
            FdivWaitUsec(txn.Steps[stepIndex].WorkUsec);
            ExecuteStep(txn, stepIndex + 1, sw, allDone);
        }, null);
    }

    // ───────────────────────── Concurrency sweep ─────────────────────────

    /// <summary>
    /// Run a batch of transactions at a given concurrency level.
    /// Returns the completed transactions with measured latencies.
    /// </summary>
    static Transaction[] RunAtConcurrency(
        int concurrency,
        int totalTransactions,
        int stepsPerTransaction,
        double minWorkUsec,
        double maxWorkUsec,
        int numQueues)
    {
        // Use a fixed seed per concurrency level for reproducibility
        var transactions = GenerateWorkload(
            totalTransactions, stepsPerTransaction,
            minWorkUsec, maxWorkUsec, numQueues,
            seed: 42 + concurrency);

        // Semaphore limits in-flight transactions
        using var throttle = new SemaphoreSlim(concurrency);
        using var allDone = new CountdownEvent(totalTransactions);

        var overallSw = Stopwatch.StartNew();

        for (int i = 0; i < totalTransactions; i++)
        {
            throttle.Wait();
            var txn = transactions[i];
            // Wrap execution to release semaphore on completion
            var originalAllDone = allDone;
            int txnIndex = i;
            ExecuteTransactionThrottled(transactions[txnIndex], originalAllDone, throttle);
        }

        allDone.Wait();
        overallSw.Stop();

        return transactions;
    }

    static void ExecuteTransactionThrottled(
        Transaction txn, CountdownEvent allDone, SemaphoreSlim throttle)
    {
        var sw = Stopwatch.StartNew();
        ExecuteStepThrottled(txn, 0, sw, allDone, throttle);
    }

    static void ExecuteStepThrottled(
        Transaction txn, int stepIndex, Stopwatch sw,
        CountdownEvent allDone, SemaphoreSlim throttle)
    {
        if (stepIndex >= txn.Steps.Length)
        {
            sw.Stop();
            txn.ActualUsec = sw.Elapsed.TotalMicroseconds;
            throttle.Release();
            allDone.Signal();
            return;
        }

        ThreadPool.UnsafeQueueUserWorkItem(_ =>
        {
            FdivWaitUsec(txn.Steps[stepIndex].WorkUsec);
            ExecuteStepThrottled(txn, stepIndex + 1, sw, allDone, throttle);
        }, null);
    }

    // ───────────────────────── Statistics & reporting ─────────────────────────

    static double Percentile(double[] sorted, double p)
    {
        double idx = p / 100.0 * (sorted.Length - 1);
        int lo = (int)Math.Floor(idx);
        int hi = (int)Math.Ceiling(idx);
        if (lo == hi) return sorted[lo];
        return sorted[lo] + (sorted[hi] - sorted[lo]) * (idx - lo);
    }

    static void PrintResults(int concurrency, Transaction[] transactions)
    {
        double[] efficiencies = transactions
            .Select(t => t.TheoreticalUsec / t.ActualUsec * 100.0)
            .OrderBy(e => e)
            .ToArray();

        double[] overheadsUsec = transactions
            .Select(t => t.ActualUsec - t.TheoreticalUsec)
            .OrderBy(o => o)
            .ToArray();

        double medianEff = Percentile(efficiencies, 50);
        double p95Eff = Percentile(efficiencies, 5); // lower tail = worst efficiency
        double p99Eff = Percentile(efficiencies, 1);
        double meanEff = efficiencies.Average();

        double medianOverhead = Percentile(overheadsUsec, 50);
        double p95Overhead = Percentile(overheadsUsec, 95);

        double totalTheoretical = transactions.Sum(t => t.TheoreticalUsec);
        double totalActual = transactions.Sum(t => t.ActualUsec);

        Console.WriteLine($"  {concurrency,5}  │ {meanEff,8:F1}%  {medianEff,8:F1}%  " +
                          $"{p95Eff,8:F1}%  {p99Eff,8:F1}%  │ " +
                          $"{medianOverhead,10:F0}  {p95Overhead,10:F0}  │ " +
                          $"{totalTheoretical / 1000:F0}/{totalActual / 1000:F0} ms");
    }

    static void PrintExampleTransactions(Transaction[] transactions, int count = 5)
    {
        Console.WriteLine("  Sample transactions:");
        Console.WriteLine($"  {"Txn",4}  {"Steps",-40}  {"Theoretical",12}  {"Actual",10}  {"Efficiency",10}");

        foreach (var txn in transactions.Take(count))
        {
            string steps = string.Join(" → ",
                txn.Steps.Select(s => $"Q{s.QueueId}:{s.WorkUsec:F0}µs"));
            if (steps.Length > 40) steps = steps[..37] + "...";

            double eff = txn.TheoreticalUsec / txn.ActualUsec * 100;
            Console.WriteLine($"  {txn.Id,4}  {steps,-40}  {txn.TheoreticalUsec,10:F0} µs  " +
                              $"{txn.ActualUsec,8:F0} µs  {eff,8:F1}%");
        }
        Console.WriteLine();
    }

    // ───────────────────────── Main ─────────────────────────

    static void Main(string[] args)
    {
        Console.WriteLine("ThreadPool Scheduling Efficiency Benchmark");
        Console.WriteLine($"ProcessorCount: {Environment.ProcessorCount}");
        Console.WriteLine($".NET: {Environment.Version}");
        Console.WriteLine();

        Calibrate();

        // ── Configuration ──
        int totalTransactions = 200;
        int stepsPerTransaction = 3;
        double minWorkUsec = 5;       // minimum per-step work (µsec)
        double maxWorkUsec = 5000;    // maximum per-step work (µsec)
        int numQueues = 8;            // logical queue IDs (for labeling)

        int[] concurrencyLevels = { 1, 2, 4, 8, 16, 32, 64 };
        // Cap at reasonable multiple of processor count
        concurrencyLevels = concurrencyLevels
            .Where(c => c <= Environment.ProcessorCount * 4)
            .Append(Environment.ProcessorCount)
            .Distinct()
            .Order()
            .ToArray();

        Console.WriteLine($"Workload: {totalTransactions} transactions × {stepsPerTransaction} steps/txn");
        Console.WriteLine($"Per-step work: {minWorkUsec}–{maxWorkUsec} µsec (log-uniform)");
        Console.WriteLine($"Logical queues: {numQueues}");
        Console.WriteLine();

        // ── Warm-up run ──
        Console.WriteLine("Warming up...");
        RunAtConcurrency(1, 20, stepsPerTransaction, minWorkUsec, maxWorkUsec, numQueues);
        Console.WriteLine();

        // ── Show sample transactions at concurrency=1 ──
        Console.WriteLine("── Single-transaction baseline (concurrency=1) ──");
        var baseline = RunAtConcurrency(1, totalTransactions, stepsPerTransaction,
            minWorkUsec, maxWorkUsec, numQueues);
        PrintExampleTransactions(baseline);

        // ── Concurrency sweep ──
        Console.WriteLine("── Concurrency sweep ──");
        Console.WriteLine($"  {"Conc",5}  │ {"Mean",8}  {"Median",8}  {"P5",8}  {"P1",8}  │ " +
                          $"{"Med OH µs",10}  {"P95 OH µs",10}  │ Theoretical/Actual");
        Console.WriteLine("  " + new string('─', 105));

        foreach (int conc in concurrencyLevels)
        {
            var results = RunAtConcurrency(conc, totalTransactions, stepsPerTransaction,
                minWorkUsec, maxWorkUsec, numQueues);
            PrintResults(conc, results);
        }

        Console.WriteLine();
        Console.WriteLine("Efficiency = TheoreticalTime / ActualTime × 100%");
        Console.WriteLine("  100% = zero scheduling overhead; lower = more overhead from queueing/contention");
        Console.WriteLine("Overhead = ActualTime − TheoreticalTime (µsec of pure scheduling cost)");
    }
}
