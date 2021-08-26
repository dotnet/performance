// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using BenchmarkDotNet.Validators;

namespace BenchmarkDotNet.Extensions
{
    /// <summary>
    /// This class makes sure that every benchmark that returns an awaitable object belongs to the "NoWASM" category.
    /// CI uses it to filter out multithreaded benchmarks, which are not supported by WASM.
    /// </summary>
    public class NoWasmValidator : IValidator
    {
        private readonly string _noWasmCategory;

        public bool TreatsWarningsAsErrors => true;

        public NoWasmValidator(string noWasmCategory) => _noWasmCategory = noWasmCategory;

        public IEnumerable<ValidationError> Validate(ValidationParameters validationParameters)
            => validationParameters.Benchmarks
                .Where(benchmark => IsAsyncMethod(benchmark.Descriptor.WorkloadMethod) && !benchmark.Descriptor.Categories.Any(category => category.Equals(_noWasmCategory, StringComparison.Ordinal)))
                .Select(benchmark => benchmark.Descriptor.GetFilterName())
                .Distinct()
                .Select(benchmarkId =>
                    new ValidationError(
                        isCritical: TreatsWarningsAsErrors,
                        $"{benchmarkId} returns an awaitable object and has no: {_noWasmCategory} category applied. Use [BenchmarkCategory(Categories.NoWASM)]")
                );

        private bool IsAsyncMethod(MethodInfo workloadMethod)
        {
            Type returnType = workloadMethod.ReturnType;

            return returnType == typeof(Task) 
                || returnType == typeof(ValueTask) 
                || (returnType.IsGenericType && (returnType.GetGenericTypeDefinition() == typeof(Task<>) || returnType.GetGenericTypeDefinition() == typeof(ValueTask<>)));
        }
    }
}