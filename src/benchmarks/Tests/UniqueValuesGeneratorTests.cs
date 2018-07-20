using System;
using System.Linq;
using Benchmarks;
using Xunit;

namespace Tests
{
    public class UniqueValuesGeneratorTests
    {
        [Fact]
        public void UnsupportedTypesThrow() 
            => Assert.Throws<NotImplementedException>(() => UniqueValuesGenerator.GenerateArray<UniqueValuesGeneratorTests>(1));
        
        [Fact]
        public void GeneratedArraysContainOnlyUniqueValues()
        {
            AssertGeneratedArraysContainOnlyUniqueValues<int>(1024);
            AssertGeneratedArraysContainOnlyUniqueValues<string>(1024);
        }

        private void AssertGeneratedArraysContainOnlyUniqueValues<T>(int size)
        {
            var generatedArray = UniqueValuesGenerator.GenerateArray<T>(size);

            var distinct = generatedArray.Distinct().ToArray();
            
            Assert.Equal(distinct, generatedArray);
        }

        [Fact]
        public void GeneratedArraysContainAlwaysSameValues()
        {
            AssertGeneratedArraysContainAlwaysSameValues<int>(1024);
            AssertGeneratedArraysContainAlwaysSameValues<string>(1024);
        }
        
        private void AssertGeneratedArraysContainAlwaysSameValues<T>(int size)
        {
            var generatedArray = UniqueValuesGenerator.GenerateArray<T>(size);
            
            for (int i = 0; i < 10; i++)
            {
                var anotherGeneratedArray = UniqueValuesGenerator.GenerateArray<T>(size);
                
                Assert.Equal(generatedArray, anotherGeneratedArray);
            }
        }
    }
}
