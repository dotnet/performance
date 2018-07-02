# Porting from xunit-performance to BenchmarkDotNet

## Key differences

* BenchmarkDotNet does not require the user to provide the number of iterations and invocations per iterations, it implements a smart heuristic based on standard error and runs the benchmark until the results are not stable.
* BenchmarkDotNet runs every benchmark in a separate process, process isolation allows avoiding side-effects. The more memory allocated by given benchmark, the bigger the difference.
* BenchmarkDotNet takes care of a few extra things:
  * it consumes the result returned from the benchmark to avoid dead code elimination
  * it wraps benchmarked method into a delegate to prevent from inlining
  * it does manual loop unrolling for inner iterations
  * & more.
 * To make previous two points possible, BenchmarkDotNet generates and builds a dedicated project for benchmarks.
 * BenchmarkDotNet was designed to make accurate nano-benchmarks with repeatable results possible, to achieve that it does many things, including overhead calculation and subtraction (it benchmarks an empty method with the same signature and subtract the average value from results).
 * BenchmarkDotNet remove outliers by default (this repo is configured to remove only the upper outliers)

## Inversion of control

xunit-performance requires the user to follow the certain pattern: a user is supposed to iterate over the iterations and wrap benchmarked code into a `using` block. 


```cs
[Benchmark]
void TestMethod()
{
    foreach (var iteration in Benchmark.Iterations)
        using (iteration.StartMeasurement())
        {
            // Code to be measured goes here.
        }
}
```

BenchmarkDotNet does a similar thing, but one level above. It means that the method with `[Benchmark]` attribute **must contain only the code that we want to measure**.

 ```cs
[Benchmark]
public void TestMethod()
{
    // Code to be measured goes here.
}
```

and somewhere inside BenchmarkDotNet Engine, the tool does something similar to:

```cs
foreach (var iteration in Benchmark.Iterations)
    using (iteration.StartMeasurement())
    {
        TestMethod();
    }
```

## Setup and Cleanup

xunit-performance makes it very easy to setup and cleanup the benchmarks:

```cs
[Benchmark]
void TestMethod()
{
    // Any per-test-case setup can go here.
    foreach (var iteration in Benchmark.Iterations)
    {
        // Any per-iteration setup can go here.
        using (iteration.StartMeasurement())
        {
            // Code to be measured goes here.
        }
        // ...per-iteration cleanup
    }
    // ...per-test-case cleanup
}
```

With BenchmarkDotNet it's more complicated, you need to move the setup and cleanup code to corresponding separate methods with: `[GlobalSetup]`, `[GlobalCleanup]`, `[IterationSetup]` and `[IterationCleanup]` attributes.

```cs
[GlobalSetup]
public void Setup()
{
  // Any per-test-case setup can go here.
}

[IterationSeutp]
public void IterationSetup()
{
  // Any per-iteration setup can go here.
}

[IterationCleanup]
public void IterationCleanup()
{
  // ...per-iteration cleanup
}

[GlobalCleanup]
public void Cleanup()
{
  // ...per-test-case cleanup
}
```

Which in practice very often means that during the port you need to move the things that were local variables to fields.

Moreover, if you have multiple benchmarks in a single type, you need to tell BDN which setup/cleanup targets which benchmark. To do that you need to provide `Target`. Example:

```cs
[GlobalSetup(Target = nameof(TestMethod))]
public void SetupTestMethod()
{
  // TestMethod setup can go here.
}
```

## Arguments

BenchmarkDotNet provides same features as `[InlineData]` and `[MemberData]` in xunit. They are named differently: `[Arguments]` and `[ArgumentsSource]`. The other difference is that BDN requires the method/getter provided in `ArgumentsSource` to be public. Moreover, if benchmarked method uses `ref` for any argument, the argument is going to be passed by reference to the benchmark. 

```cs
[Benchmark]
[InlineData(100)]
public void Method(int length)
```

```cs
[Benchmark]
[Arguments(100)]
public void Method(int length)
```

If you need to use the argument in the setup method, then instead of using `Arguments` you should use `Params`. Which write the value to a field that can be accessed during the setup/cleanup ([docs](https://benchmarkdotnet.org/Advanced/Params.htm)). Example:

```cs
int[] array;

[Params(100, 1000, 10000)]
public int Size { get; set; }

[GlobalSetup]
public void PrepareArray() => array = new int[Size];
```

However, `[Params]` are applied to all the benchmarks in given type. So if few benchmarks require different `[Params]` values you have to split them into separate types (I am not very proud of it, but this is how it works, at least for now).

## Method Result

BenchmarkDotNet consumes the result returned from a benchmark and writes it to a `volatile` field ([code](https://github.com/dotnet/BenchmarkDotNet/blob/94863ab4d024eca04d061423e5aad498feff386b/src/BenchmarkDotNet/Engines/Consumer.cs)).

So if existing xunit-performance benchmark does something like:

```cs
public static volatile object VolatileObject;

[MethodImpl(MethodImplOptions.NoInlining)]
static void Escape(object obj) {
    VolatileObject = obj;
}

[Benchmark]
public void Test() {
    foreach (var iteration in Benchmark.Iterations) {
        using (iteration.StartMeasurement()) {
            Escape(Bench());
        }
    }
}
```

You should remove the `Escape` method and just return the result from benchmark.

```cs
[Benchmark]
public object Test() => Bench(); // object is just an example return type, you don't have to return object
````

## Method inlining

BenchmarkDotNet prevents from inlining the benchmarked method by wrapping it into a delegate (delegates can not be inlined as of today). The cost of delegate invocation is excluded by running separate run for Overhead calculation.

So if any benchmark has `[MethodImpl(MethodImplOptions.NoInlining)]` attribute, you can just remove it.

## Derived type

BenchmarkDotNet creates a type which derives from type with benchmarks. So the type with benchmarks must **not** be **sealed** and it can **not** be **static** and it has to be **public**.

## Preserving IDs

The results from CoreCLR and CoreFX benchmark are exported to BenchView. Their xunit full name is used as an ID in the BenchView. To combine the old and new data in BenchView, we can't:

* change namespace, type name, method name, argument name, argument value
* remove any arguments, even if they are not used anymore

If given benchmark has a wonderful name like `Test` or `Bench`, you still can't change it! However, to ease your pain you can use `Description` property of `BenchmarkAttribute` which is going to display the provided value in results, but export xunit-style id to the json file consumed by BenchView importer.

```cs
[Benchmark(Description = nameof(FannkuchRedux_5))]
[Arguments(10, 38)]
public int RunBench(int n, int expectedSum) => Bench(n, false); // expectedSum argument needs to remain to keep old benchmark id in BenchView, do NOT remove it
```

The ID is exported to json and called `FullName`, an example:

```json
   "Benchmarks":[
      {
         "FullName":"System.Threading.Tasks.ValueTaskPerfTest.Await_FromResult",
```

## Scaling of the results

Let's consider following xunit-performance benchmark:

```cs
[Benchmark(InnerIterationCount = 7)]
public void Sleep()
{
    Benchmark.Iterate(() => Thread.Sleep(Timespan.FromMinutes(1)));
}
```

xunit-performance runs given benchmark 7 times per every iteration (**InnerIterationCount = 7**). 
BenchmarkDotNet calculates `InnerIterationCount` (called `InvocationCount` in BDN) based on the provided `IterationTime`.

But the key difference is that BenchmarkDotNet scales the results according to `InnerIterationCount` and xunit-performance does not.

So in this particular case, xunit-performance reports `7 minutes` and BDN reports `1 minute`.

We don't want to break the scaling of the existing results stored in BenchView and only because of that, we copy `InnerIterationCount` to new benchmark. So given benchmark should be:

```cs
[Benchmark]
public void Sleep()
{
    for (int i = 0; i < 7; i++)
        Thread.Sleep(Timespan.FromMinutes(1);
}
```

New benchmarks, which are not ports, should not do that and let BenchmarkDotNet scale the results.

## Comparing the results

After you have ported the code, you should close all applications, run the benchmarks with both tools and compare the results.

To make it easier I implemented a simple `ResultValidator` class which imports results from both tools, scales them and exports to CSV. Personally I use Excel to compare all the numbers.

You should compare:
 * the average and median
 * min and max
 * distribution (the more narrow, the better)
 * allocated memory

What is desired:
 * more narrow distribution
 * similar timings for benchmarks without side-effects

If both tools produce very different results you should do a sanity check and implement a simple `Stopwatch` based solution. Please include a note and the results in the PR.

If BenchmarkDotNet produces wrong results, file an issue (so far after porting 300 benchmarks it did not happen!).

 
