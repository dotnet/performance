``` ini

BenchmarkDotNet=v0.11.3, OS=Windows 10.0.17763.253 (1809/October2018Update/Redstone5)
Intel Core i7-6700 CPU 3.40GHz (Skylake), 1 CPU, 8 logical and 4 physical cores
.NET Core SDK=3.0.100-preview-010096
  [Host]     : .NET Core 2.1.7 (CoreCLR 4.6.27129.04, CoreFX 4.6.27129.04), 64bit RyuJIT
  Job-DMQXGE : .NET Core 2.1.7 (CoreCLR 4.6.27129.04, CoreFX 4.6.27129.04), 64bit RyuJIT

BuildConfiguration=Release  Toolchain=netcoreapp2.1  MaxIterationCount=20  
WarmupCount=1  

```
|                      Method |      Mean |     Error |    StdDev | Extra Metric |
|---------------------------- |----------:|----------:|----------:|-------------:|
|         MakeIrisPredictions |  5.179 ms | 0.0842 ms | 0.0788 ms |            - |
|    MakeSentimentPredictions | 59.198 ms | 0.4721 ms | 0.4185 ms |            - |
| MakeBreastCancerPredictions |  2.339 ms | 0.0353 ms | 0.0313 ms |            - |
