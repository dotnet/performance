// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Validators;

namespace MicroBenchmarks
{
    /// <summary>
    /// this class makes sure that every benchmark belongs to either a CoreFX, CoreCLR or ThirdParty category
    /// for CoreCLR CI jobs we want to run only benchmarks form CoreCLR category
    /// the same goes for CoreFX
    /// </summary>
    public class MandatoryCategoryValidator : IValidator
    {
        public static readonly IValidator FailOnError = new MandatoryCategoryValidator();

        public bool TreatsWarningsAsErrors => true;

        public IEnumerable<ValidationError> Validate(ValidationParameters validationParameters)
            => validationParameters.Benchmarks
                .Where(benchmark => !benchmark.Descriptor.Categories.Any(category => category == Categories.MachineLearning))
                .Select(benchmark => benchmark.Descriptor.GetFilterName())
                .Distinct()
                .Select(benchmarkId =>
                    new ValidationError(
                        isCritical: TreatsWarningsAsErrors,
                        $"{benchmarkId} does not belong to {Categories.MachineLearning}. Use [BenchmarkCategory(Categories.$)]")
                );
    }
}