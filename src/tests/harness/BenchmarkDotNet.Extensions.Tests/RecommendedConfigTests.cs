using BenchmarkDotNet.Configs;
using System.Collections.Immutable;
using System.IO;
using Xunit;

namespace BenchmarkDotNet.Extensions.Tests
{
    public class RecommendedConfigTests
    {
        [Fact]
        public void EmptyMandatoryCategoriesOmitsValidator()
        {
            IConfig config = RecommendedConfig.Create(
                new DirectoryInfo(Path.GetTempPath()),
                ImmutableHashSet<string>.Empty);

            Assert.DoesNotContain(config.GetValidators(), validator => validator is MandatoryCategoryValidator);
        }

        [Fact]
        public void MandatoryCategoriesAddValidator()
        {
            IConfig config = RecommendedConfig.Create(
                new DirectoryInfo(Path.GetTempPath()),
                ImmutableHashSet.Create("Required"));

            Assert.Contains(config.GetValidators(), validator => validator is MandatoryCategoryValidator);
        }
    }
}
