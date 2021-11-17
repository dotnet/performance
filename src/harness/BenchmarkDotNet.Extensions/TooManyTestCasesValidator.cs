// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BenchmarkDotNet.Validators;

namespace BenchmarkDotNet.Extensions
{
    /// <summary>Issues an error when a benchmark has too many tests cases.</summary>
    public class TooManyTestCasesValidator : IValidator
    {
        private const int Limit = 16;

        public static readonly IValidator FailOnError = new TooManyTestCasesValidator();

        public bool TreatsWarningsAsErrors => true;

        public IEnumerable<ValidationError> Validate(ValidationParameters validationParameters)
        {
            var byDescriptor = validationParameters.Benchmarks
                .Where(benchmark => !SkipValidation(benchmark.Descriptor.WorkloadMethod))
                .GroupBy(benchmark => (benchmark.Descriptor, benchmark.Job)); // descriptor = type + method

            return byDescriptor.Where(benchmarkCase => benchmarkCase.Count() > Limit).Select(group =>
                new ValidationError(
                    isCritical: true,
                    message: $"{group.Key.Descriptor.Type.Name}.{group.Key.Descriptor.WorkloadMethod.Name} has {group.Count()} test cases. It MUST NOT have more than {Limit} test cases. We don't have inifinite amount of time to run all the benchmarks!!",
                    benchmarkCase: group.First()));
        }

        private static bool SkipValidation(MemberInfo member)
        {
            while (member is not null)
            {
                if (member.IsDefined(typeof(SkipTooManyTestCasesValidatorAttribute), inherit: true))
                {
                    return true;
                }
                member = member.DeclaringType;
            }

            return false;
        }
    }

    /// <summary>Disables <see cref="TooManyTestCasesValidator"/> for the target benchmark.</summary>
    /// <remarks>Used to override the validation when the large number of cases is intentional and warranted.</remarks>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public sealed class SkipTooManyTestCasesValidatorAttribute : Attribute
    {
    }
}