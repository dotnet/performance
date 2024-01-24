using BenchmarkDotNet.Attributes;

namespace Akade.IndexedSet.Benchmarks;

[BenchmarkCategory(Categories.AkadeIndexedSet)]
public class MultiValueIndexBenchmarks
{
    private record Order(int ProductId, decimal Price);
    private readonly List<Order> _orders;
    private readonly List<int> _productIds;
    private readonly IndexedSet<Order> _indexedSet;

    public MultiValueIndexBenchmarks()
    {
        Random r = new(42);
        _orders = Enumerable.Range(0, 1000)
                            .Select(_ => new Order(r.Next(1, 10), (decimal)r.NextDouble() * 100m))
                            .ToList();

        _productIds = Enumerable.Range(1, 10)
                                .ToList();

        _indexedSet = _orders.ToIndexedSet()
                             .WithIndex(x => x.ProductId)
                             .Build();
    }

    [Benchmark]
    public decimal Multivalue_IndexedSet()
    {
        decimal total = 0;
        foreach (int productId in _productIds)
        {
            foreach (Order product in _indexedSet.Where(x => x.ProductId, productId))
            {
                total += product.Price;
            }
        }
        return total;
    }

}
