using System;
using System.Linq;
using MicroBenchmarks;
using Xunit;

namespace Tests
{
    public class UniqueValuesGeneratorTests
    {
        [Fact]
        public void UnsupportedTypesThrow() 
            => Assert.Throws<NotImplementedException>(() => ValuesGenerator.ArrayOfUniqueValues<UniqueValuesGeneratorTests>(1));
        
        [Fact]
        public void GeneratedArraysContainOnlyUniqueValues()
        {
            AssertGeneratedArraysContainOnlyUniqueValues<int>(1024);
            AssertGeneratedArraysContainOnlyUniqueValues<string>(1024);
        }

        private void AssertGeneratedArraysContainOnlyUniqueValues<T>(int size)
        {
            var generatedArray = ValuesGenerator.ArrayOfUniqueValues<T>(size);

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
            var generatedArray = ValuesGenerator.ArrayOfUniqueValues<T>(size);
            
            for (int i = 0; i < 10; i++)
            {
                var anotherGeneratedArray = ValuesGenerator.ArrayOfUniqueValues<T>(size);
                
                Assert.Equal(generatedArray, anotherGeneratedArray);
            }
        }

        [Fact]
        public void GeneratedStringsContainOnlySimpleLettersAndDigits()
        {
            var strings = ValuesGenerator.ArrayOfUniqueValues<string>(1024);

            foreach (var text in strings)
            {
                Assert.All(text, character => 
                    Assert.True(
                        char.IsDigit(character) 
                        || (character >= 'a' && character <= 'z') 
                        || (character >= 'A' && character <= 'Z')));
            }
        }
    }
}
