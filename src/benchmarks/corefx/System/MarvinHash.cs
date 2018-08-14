using System.Linq;
using System.Text;
using BenchmarkDotNet.Attributes;

namespace System
{
    public class MarvinHash
    {
        private const ulong HashSeed = 0x4FB61A001BDBCCLU;
        private const int RandomSeed = 12345;
        
        public delegate int ComputeHash32Delegate(ReadOnlySpan<byte> data, ulong seed);

        private ComputeHash32Delegate _delegate;
        private byte[] _bytes;
        private string _string;

        [Params(128, 256, 1024)]
        public int BytesCount;

        [GlobalSetup]
        public void Setup()
        {
            // Marvin is internall, this is why we do this hack to call the method
            var marvinType = Type.GetType("System.Marvin", throwOnError: true);
            var computeHashMethod = marvinType.GetMethods().Single(method => method.Name == "ComputeHash32" && method.GetParameters().Length == 2);
            _delegate = (ComputeHash32Delegate)computeHashMethod.CreateDelegate(typeof(ComputeHash32Delegate));
            
            
            var random = new Random(RandomSeed);
            _bytes = new byte[BytesCount];
            random.NextBytes(_bytes);

            _string = new string(Enumerable.Repeat('a', BytesCount / 2).ToArray());
            if (_string.Length != BytesCount / 2)
                throw new InvalidOperationException($"Should not happen {_string.Length} {BytesCount}");
        }

        [Benchmark]
        public int ComputeHash32() => _delegate.Invoke(new ReadOnlySpan<byte>(_bytes), HashSeed);

        [Benchmark]
        public int StringGetHashCode() => _string.GetHashCode();
    }
}