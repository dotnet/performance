using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Filters;
using BenchmarkDotNet.Running;
using System;
using System.Buffers.Binary;
using System.Security.Cryptography;

public class PartitionFilter : IFilter
{
    private readonly int? _partitionsCount;
    private readonly int? _partitionIndex; // indexed from 0
    private readonly SHA256 _sha256;
 
    public PartitionFilter(int? partitionCount, int? partitionIndex)
    {
        _partitionsCount = partitionCount;
        _partitionIndex = partitionIndex;
        _sha256 = SHA256.Create();
    }
 
    public bool Predicate(BenchmarkCase benchmarkCase)
    {
        if (!_partitionsCount.HasValue || !_partitionIndex.HasValue)
            return true; // the filter is not enabled so it does not filter anything out and can be added to RecommendedConfig

        // Hash the test name to ensure a balanced partitioning strategy that is not dependent on the order of benchmarks
        var testName = FullNameProvider.GetBenchmarkName(benchmarkCase);
        var hash = _sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(testName));

        // Use the first 4 bytes of the hash to determine the partition
        var hashAsNum = BinaryPrimitives.ReadUInt32LittleEndian(hash.AsSpan(0, 4));
        return hashAsNum % _partitionsCount.Value == _partitionIndex.Value;
    }
}