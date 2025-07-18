# Regression Report - linux-x64-tiger

## 1. 41be5e229b - JIT: Graph-based loop inversion (#116017)

**Date:** 2025-06-04 14:39:50
**Commit:** [41be5e229b](https://github.com/dotnet/runtime/commit/41be5e229b30fc3e7aaed9361b9db4487c5bb7f8)
**Affected Tests:** 77

| Test Name | Link | Change | .NET 9 | .NET 10 | Other Changepoints |
|-----------|------|--------|--------|-------|--------------------|
| Burgers.Test0 | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/Burgers.Test0.html) | +546.70% | 287609724.553571 | 1859976778.517857 | None |
| System.Linq.Tests.Perf_Enumerable.SingleWithPredicate_FirstElementMatches(input: Array) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Linq.Tests.Perf_Enumerable.SingleWithPredicate_FirstElementMatches%28input%3A%20Array%29.html) | +153.76% | 42.938261 | 108.960405 | None |
| System.Text.Encodings.Web.Tests.Perf_Encoders.EncodeUtf16(arguments: Url,&lorem ipsum=dolor sit amet,16) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Text.Encodings.Web.Tests.Perf_Encoders.EncodeUtf16%28arguments%3A%20Url%2C%26lorem%20ipsum%3Ddolor%20sit%20amet%2C16%29.html) | +117.06% | 51.955954 | 112.773346 | [2](#2-ffcd1c5442---trust-single-edge-synthetic-profile-116054), [3](#3-ddf8075a2f---jit-visit-blocks-in-rpo-during-lsra-107927), [17](#17-e32148a8bd---jit-add-loop-aware-rpo-and-use-as-lsras-block-sequence-108086) |
| System.Text.Encodings.Web.Tests.Perf_Encoders.EncodeUtf16(arguments: JavaScript,&Hello+<World>!,16) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Text.Encodings.Web.Tests.Perf_Encoders.EncodeUtf16%28arguments%3A%20JavaScript%2C%26Hello%2B%28World%29%21%2C16%29.html) | +83.91% | 45.219853 | 83.163582 | [2](#2-ffcd1c5442---trust-single-edge-synthetic-profile-116054), [3](#3-ddf8075a2f---jit-visit-blocks-in-rpo-during-lsra-107927), [17](#17-e32148a8bd---jit-add-loop-aware-rpo-and-use-as-lsras-block-sequence-108086) |
| Benchstone.BenchF.Simpsn.Test | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/Benchstone.BenchF.Simpsn.Test.html) | +68.23% | 102700654.857143 | 172778442.642857 | None |
| System.Net.Tests.Perf_WebUtility.Decode_NoDecodingRequired | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Net.Tests.Perf_WebUtility.Decode_NoDecodingRequired.html) | +49.73% | 47.921292 | 71.753359 | None |
| Benchstone.BenchI.XposMatrix.Test | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/Benchstone.BenchI.XposMatrix.Test.html) | +61.45% | 12231.521028 | 19748.149585 | [4](#4-1c10ceecbf---jit-add-3-opt-implementation-for-improving-upon-rpo-based-block-layout-103450), [6](#6-ea43e17c---jit-run-profile-repair-after-frontend-phases-111915), [36](#36-aecae2c385---jit-enable-profile-consistency-checking-up-to-morph-111047) |
| Span.Sorting.BubbleSortArray(Size: 512) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/Span.Sorting.BubbleSortArray%28Size%3A%20512%29.html) | +54.92% | 157529.645102 | 244044.942506 | [4](#4-1c10ceecbf---jit-add-3-opt-implementation-for-improving-upon-rpo-based-block-layout-103450) |
| System.Memory.Span<Int32>.IndexOfAnyTwoValues(Size: 512) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Memory.Span%28Int32%29.IndexOfAnyTwoValues%28Size%3A%20512%29.html) | +45.63% | 122.463217 | 178.339434 | [3](#3-ddf8075a2f---jit-visit-blocks-in-rpo-during-lsra-107927) |
| System.Collections.TryGetValueTrue<Int32, Int32>.ImmutableSortedDictionary(Size: 512) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Collections.TryGetValueTrue%28Int32%2C%20Int32%29.ImmutableSortedDictionary%28Size%3A%20512%29.html) | +42.73% | 13411.946128 | 19143.403434 | [31](#31-489a1512f5---remove-ldsfld-quirk-108606) |
| System.Collections.Tests.Perf_Dictionary.Clone(Items: 3000) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Collections.Tests.Perf_Dictionary.Clone%28Items%3A%203000%29.html) | +39.80% | 7103.178665 | 9930.379163 | [5](#5-6d12a304b3---jit-do-greedy-4-opt-for-backward-jumps-in-3-opt-layout-110277) |
| System.Memory.Span<Int32>.IndexOfAnyTwoValues(Size: 33) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Memory.Span%28Int32%29.IndexOfAnyTwoValues%28Size%3A%2033%29.html) | +34.45% | 10.036435 | 13.493959 | [3](#3-ddf8075a2f---jit-visit-blocks-in-rpo-during-lsra-107927) |
| Benchstone.BenchI.BubbleSort2.Test | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/Benchstone.BenchI.BubbleSort2.Test.html) | +32.27% | 30671803.091518 | 40569125.801339 | None |
| System.Collections.Perf_Frozen<NotKnownComparable>.Contains_True(Count: 512) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Collections.Perf_Frozen%28NotKnownComparable%29.Contains_True%28Count%3A%20512%29.html) | +30.54% | 1311.880582 | 1712.486339 | [4](#4-1c10ceecbf---jit-add-3-opt-implementation-for-improving-upon-rpo-based-block-layout-103450), [5](#5-6d12a304b3---jit-do-greedy-4-opt-for-backward-jumps-in-3-opt-layout-110277) |
| System.Collections.Perf_Frozen<NotKnownComparable>.Contains_True(Count: 64) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Collections.Perf_Frozen%28NotKnownComparable%29.Contains_True%28Count%3A%2064%29.html) | +27.79% | 159.425812 | 203.737008 | [4](#4-1c10ceecbf---jit-add-3-opt-implementation-for-improving-upon-rpo-based-block-layout-103450), [5](#5-6d12a304b3---jit-do-greedy-4-opt-for-backward-jumps-in-3-opt-layout-110277) |
| System.Text.Perf_Ascii.EqualsIgnoreCase_ExactlyTheSame_Bytes(Size: 128) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Text.Perf_Ascii.EqualsIgnoreCase_ExactlyTheSame_Bytes%28Size%3A%20128%29.html) | +26.78% | 6.241384 | 7.912695 | None |
| Benchstone.BenchI.AddArray2.Test | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/Benchstone.BenchI.AddArray2.Test.html) | +30.59% | 7695404.598522 | 10049435.565887 | None |
| System.Numerics.Tests.Perf_BigInteger.Equals(arguments: 259 bytes, Same) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Numerics.Tests.Perf_BigInteger.Equals%28arguments%3A%20259%20bytes%2C%20Same%29.html) | +26.94% | 6.740102 | 8.556015 | [10](#10-02127c782a---jit-dont-put-cold-blocks-in-rpo-during-layout-112448) |
| V8.Crypto.Support.Bench | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/V8.Crypto.Support.Bench.html) | +23.79% | 2754340.843303 | 3409516.765423 | None |
| System.Net.Tests.Perf_WebUtility.Decode_DecodingRequired | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Net.Tests.Perf_WebUtility.Decode_DecodingRequired.html) | +23.86% | 89.057201 | 110.304468 | [2](#2-ffcd1c5442---trust-single-edge-synthetic-profile-116054) |
| System.Memory.Span<Int32>.IndexOfAnyThreeValues(Size: 33) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Memory.Span%28Int32%29.IndexOfAnyThreeValues%28Size%3A%2033%29.html) | +23.28% | 13.768474 | 16.973103 | [3](#3-ddf8075a2f---jit-visit-blocks-in-rpo-during-lsra-107927) |
| Benchstone.BenchI.Midpoint.Test | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/Benchstone.BenchI.Midpoint.Test.html) | +22.76% | 266919399.095238 | 327681249.339286 | None |
| Burgers.Test1 | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/Burgers.Test1.html) | +22.54% | 197903751.107143 | 242512062.250000 | [13](#13-5cb6a06da6---jit-add-simple-late-layout-pass-107483) |
| System.Memory.Span<Byte>.StartsWith(Size: 512) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Memory.Span%28Byte%29.StartsWith%28Size%3A%20512%29.html) | +21.26% | 6.891792 | 8.357269 | None |
| System.Tests.Perf_Version.TryFormat4 | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Tests.Perf_Version.TryFormat4.html) | +22.16% | 16.913676 | 20.662248 | [3](#3-ddf8075a2f---jit-visit-blocks-in-rpo-during-lsra-107927) |
| System.Collections.Perf_Frozen<Int16>.Contains_True(Count: 4) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Collections.Perf_Frozen%28Int16%29.Contains_True%28Count%3A%204%29.html) | +21.48% | 12.094405 | 14.692279 | [5](#5-6d12a304b3---jit-do-greedy-4-opt-for-backward-jumps-in-3-opt-layout-110277) |
| System.Tests.Perf_UInt64.TryFormat(value: 18446744073709551615) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Tests.Perf_UInt64.TryFormat%28value%3A%2018446744073709551615%29.html) | +15.69% | 12.928101 | 14.956354 | None |
| System.Runtime.Intrinsics.Tests.Perf_Vector128Int.BitwiseAndBenchmark | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Runtime.Intrinsics.Tests.Perf_Vector128Int.BitwiseAndBenchmark.html) | +12.92% | 1.431301 | 1.616290 | None |
| System.Runtime.Intrinsics.Tests.Perf_Vector128Of<UInt32>.XorBenchmark | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Runtime.Intrinsics.Tests.Perf_Vector128Of%28UInt32%29.XorBenchmark.html) | +14.23% | 1.418032 | 1.619833 | None |
| System.Runtime.Intrinsics.Tests.Perf_Vector128Of<UInt16>.SubtractBenchmark | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Runtime.Intrinsics.Tests.Perf_Vector128Of%28UInt16%29.SubtractBenchmark.html) | +14.18% | 1.435900 | 1.639517 | None |
| System.Runtime.Intrinsics.Tests.Perf_Vector128Of<SByte>.XorBenchmark | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Runtime.Intrinsics.Tests.Perf_Vector128Of%28SByte%29.XorBenchmark.html) | +14.25% | 1.417598 | 1.619620 | None |
| System.Runtime.Intrinsics.Tests.Perf_Vector128Of<Int32>.AddBenchmark | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Runtime.Intrinsics.Tests.Perf_Vector128Of%28Int32%29.AddBenchmark.html) | +15.33% | 1.408297 | 1.624129 | None |
| System.Runtime.Intrinsics.Tests.Perf_Vector128Int.AddBenchmark | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Runtime.Intrinsics.Tests.Perf_Vector128Int.AddBenchmark.html) | +14.63% | 1.420014 | 1.627715 | None |
| System.Text.Perf_Ascii.EqualsIgnoreCase_DifferentCase_Chars(Size: 128) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Text.Perf_Ascii.EqualsIgnoreCase_DifferentCase_Chars%28Size%3A%20128%29.html) | +15.38% | 21.624844 | 24.950440 | None |
| System.Runtime.Intrinsics.Tests.Perf_Vector128Of<UInt32>.MultiplyBenchmark | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Runtime.Intrinsics.Tests.Perf_Vector128Of%28UInt32%29.MultiplyBenchmark.html) | +14.20% | 1.436189 | 1.640184 | None |
| System.Collections.ContainsTrue<Int32>.Stack(Size: 512) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Collections.ContainsTrue%28Int32%29.Stack%28Size%3A%20512%29.html) | +25.25% | 11480.235568 | 14379.105096 | None |
| System.Runtime.Intrinsics.Tests.Perf_Vector128Of<Int16>.AddBenchmark | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Runtime.Intrinsics.Tests.Perf_Vector128Of%28Int16%29.AddBenchmark.html) | +14.47% | 1.414161 | 1.618809 | None |
| System.Runtime.Intrinsics.Tests.Perf_Vector128Int.SubtractBenchmark | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Runtime.Intrinsics.Tests.Perf_Vector128Int.SubtractBenchmark.html) | +13.43% | 1.426356 | 1.617961 | None |
| System.Runtime.Intrinsics.Tests.Perf_Vector128Of<Int16>.ConditionalSelectBenchmark | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Runtime.Intrinsics.Tests.Perf_Vector128Of%28Int16%29.ConditionalSelectBenchmark.html) | +13.91% | 1.421083 | 1.618791 | None |
| System.Runtime.Intrinsics.Tests.Perf_Vector128Of<UInt16>.ConditionalSelectBenchmark | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Runtime.Intrinsics.Tests.Perf_Vector128Of%28UInt16%29.ConditionalSelectBenchmark.html) | +14.48% | 1.438979 | 1.647393 | None |
| System.Runtime.Intrinsics.Tests.Perf_Vector128Of<SByte>.ConditionalSelectBenchmark | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Runtime.Intrinsics.Tests.Perf_Vector128Of%28SByte%29.ConditionalSelectBenchmark.html) | +14.02% | 1.419339 | 1.618317 | None |
| System.Runtime.Intrinsics.Tests.Perf_Vector128Of<Byte>.XorBenchmark | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Runtime.Intrinsics.Tests.Perf_Vector128Of%28Byte%29.XorBenchmark.html) | +14.21% | 1.437035 | 1.641281 | None |
| System.Runtime.Intrinsics.Tests.Perf_Vector128Of<Byte>.SubtractBenchmark | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Runtime.Intrinsics.Tests.Perf_Vector128Of%28Byte%29.SubtractBenchmark.html) | +14.95% | 1.410863 | 1.621803 | None |
| System.Runtime.Intrinsics.Tests.Perf_Vector128Int.XorBenchmark | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Runtime.Intrinsics.Tests.Perf_Vector128Int.XorBenchmark.html) | +14.69% | 1.411549 | 1.618915 | None |
| System.Runtime.Intrinsics.Tests.Perf_Vector128Of<Int32>.MultiplyBenchmark | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Runtime.Intrinsics.Tests.Perf_Vector128Of%28Int32%29.MultiplyBenchmark.html) | +13.42% | 1.428645 | 1.620316 | None |
| System.Runtime.Intrinsics.Tests.Perf_Vector128Of<Int16>.XorBenchmark | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Runtime.Intrinsics.Tests.Perf_Vector128Of%28Int16%29.XorBenchmark.html) | +14.18% | 1.442297 | 1.646871 | None |
| System.Runtime.Intrinsics.Tests.Perf_Vector128Of<UInt16>.XorBenchmark | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Runtime.Intrinsics.Tests.Perf_Vector128Of%28UInt16%29.XorBenchmark.html) | +14.08% | 1.417611 | 1.617197 | None |
| System.Runtime.Intrinsics.Tests.Perf_Vector128Of<UInt32>.SubtractBenchmark | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Runtime.Intrinsics.Tests.Perf_Vector128Of%28UInt32%29.SubtractBenchmark.html) | +14.09% | 1.420528 | 1.620710 | None |
| System.Runtime.Intrinsics.Tests.Perf_Vector128Of<Int16>.MultiplyBenchmark | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Runtime.Intrinsics.Tests.Perf_Vector128Of%28Int16%29.MultiplyBenchmark.html) | +14.41% | 1.418267 | 1.622578 | None |
| System.Buffers.Tests.RentReturnArrayPoolTests<Object>.SingleSerial(RentalSize: 4096, ManipulateArray: False, Async: False, UseSharedPool: True) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Buffers.Tests.RentReturnArrayPoolTests%28Object%29.SingleSerial%28RentalSize%3A%204096%2C%20ManipulateArray%3A%20False%2C%20Async%3A%20False%2C%20UseSharedPool%3A%20True%29.html) | +13.71% | 11.070828 | 12.588648 | None |
| System.Runtime.Intrinsics.Tests.Perf_Vector128Of<UInt16>.MultiplyBenchmark | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Runtime.Intrinsics.Tests.Perf_Vector128Of%28UInt16%29.MultiplyBenchmark.html) | +13.83% | 1.432102 | 1.630208 | None |
| System.Runtime.Intrinsics.Tests.Perf_Vector128Of<Byte>.AddBenchmark | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Runtime.Intrinsics.Tests.Perf_Vector128Of%28Byte%29.AddBenchmark.html) | +14.76% | 1.411417 | 1.619707 | None |
| System.Collections.Perf_Frozen<Int16>.TryGetValue_True(Count: 64) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Collections.Perf_Frozen%28Int16%29.TryGetValue_True%28Count%3A%2064%29.html) | +14.32% | 201.755572 | 230.643421 | None |
| System.Runtime.Intrinsics.Tests.Perf_Vector128Of<Int32>.ConditionalSelectBenchmark | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Runtime.Intrinsics.Tests.Perf_Vector128Of%28Int32%29.ConditionalSelectBenchmark.html) | +14.39% | 1.412996 | 1.616332 | None |
| System.Runtime.Intrinsics.Tests.Perf_Vector128Of<Byte>.BitwiseAndBenchmark | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Runtime.Intrinsics.Tests.Perf_Vector128Of%28Byte%29.BitwiseAndBenchmark.html) | +15.12% | 1.414062 | 1.627917 | None |
| System.Runtime.Intrinsics.Tests.Perf_Vector128Of<UInt32>.AddBenchmark | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Runtime.Intrinsics.Tests.Perf_Vector128Of%28UInt32%29.AddBenchmark.html) | +15.18% | 1.408417 | 1.622149 | None |
| System.Runtime.Intrinsics.Tests.Perf_Vector128Of<Int16>.SubtractBenchmark | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Runtime.Intrinsics.Tests.Perf_Vector128Of%28Int16%29.SubtractBenchmark.html) | +15.41% | 1.409393 | 1.626511 | None |
| System.Runtime.Intrinsics.Tests.Perf_Vector128Of<Int32>.XorBenchmark | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Runtime.Intrinsics.Tests.Perf_Vector128Of%28Int32%29.XorBenchmark.html) | +14.78% | 1.441397 | 1.654372 | None |
| System.Runtime.Intrinsics.Tests.Perf_Vector128Of<SByte>.AddBenchmark | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Runtime.Intrinsics.Tests.Perf_Vector128Of%28SByte%29.AddBenchmark.html) | +14.77% | 1.413804 | 1.622653 | None |
| System.Runtime.Intrinsics.Tests.Perf_Vector128Of<Byte>.ConditionalSelectBenchmark | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Runtime.Intrinsics.Tests.Perf_Vector128Of%28Byte%29.ConditionalSelectBenchmark.html) | +14.54% | 1.415236 | 1.620952 | None |
| System.Runtime.Intrinsics.Tests.Perf_Vector128Of<UInt32>.BitwiseAndBenchmark | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Runtime.Intrinsics.Tests.Perf_Vector128Of%28UInt32%29.BitwiseAndBenchmark.html) | +14.52% | 1.416946 | 1.622744 | None |
| System.Runtime.Intrinsics.Tests.Perf_Vector128Of<UInt16>.BitwiseAndBenchmark | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Runtime.Intrinsics.Tests.Perf_Vector128Of%28UInt16%29.BitwiseAndBenchmark.html) | +14.88% | 1.411593 | 1.621569 | None |
| System.Runtime.Intrinsics.Tests.Perf_Vector128Of<Int32>.BitwiseAndBenchmark | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Runtime.Intrinsics.Tests.Perf_Vector128Of%28Int32%29.BitwiseAndBenchmark.html) | +14.39% | 1.425373 | 1.630498 | None |
| System.Collections.CtorFromCollection<Int32>.SortedSet(Size: 512) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Collections.CtorFromCollection%28Int32%29.SortedSet%28Size%3A%20512%29.html) | +12.36% | 8130.364610 | 9135.005498 | None |
| System.Collections.Perf_Frozen<Int16>.TryGetValue_True(Count: 512) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Collections.Perf_Frozen%28Int16%29.TryGetValue_True%28Count%3A%20512%29.html) | +12.99% | 1631.300329 | 1843.235165 | None |
| System.Runtime.Intrinsics.Tests.Perf_Vector128Of<UInt32>.ConditionalSelectBenchmark | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Runtime.Intrinsics.Tests.Perf_Vector128Of%28UInt32%29.ConditionalSelectBenchmark.html) | +14.47% | 1.417618 | 1.622739 | None |
| System.Runtime.Intrinsics.Tests.Perf_Vector128Of<SByte>.BitwiseAndBenchmark | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Runtime.Intrinsics.Tests.Perf_Vector128Of%28SByte%29.BitwiseAndBenchmark.html) | +14.84% | 1.411879 | 1.621456 | None |
| System.Collections.Tests.Perf_PriorityQueue<Int32, Int32>.Dequeue_And_Enqueue(Size: 100) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Collections.Tests.Perf_PriorityQueue%28Int32%2C%20Int32%29.Dequeue_And_Enqueue%28Size%3A%20100%29.html) | +16.21% | 3693.912058 | 4292.520085 | None |
| System.Collections.Perf_Frozen<Int16>.TryGetValue_True(Count: 4) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Collections.Perf_Frozen%28Int16%29.TryGetValue_True%28Count%3A%204%29.html) | +12.52% | 13.894090 | 15.633671 | None |
| ByteMark.BenchNeuralJagged | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/ByteMark.BenchNeuralJagged.html) | +12.22% | 561704397.178571 | 630318063.142857 | None |
| System.Buffers.Text.Tests.Utf8ParserTests.TryParseInt64(value: -9223372036854775808) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Buffers.Text.Tests.Utf8ParserTests.TryParseInt64%28value%3A%20-9223372036854775808%29.html) | +12.73% | 17.930980 | 20.212854 | None |
| System.Buffers.Text.Tests.Base64Tests.ConvertTryFromBase64Chars(NumberOfBytes: 1000) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Buffers.Text.Tests.Base64Tests.ConvertTryFromBase64Chars%28NumberOfBytes%3A%201000%29.html) | +10.17% | 746.523883 | 822.417701 | [42](#42-3c8bae3ff0---jit-also-run-local-assertion-prop-in-postorder-during-morph-115626) |
| System.Collections.Tests.Perf_BitArray.BitArrayNot(Size: 4) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Collections.Tests.Perf_BitArray.BitArrayNot%28Size%3A%204%29.html) | +10.32% | 4.421564 | 4.877761 | [22](#22-30082a461a---jit-save-pgo-data-in-inline-context-use-it-for-call-optimization-116241), [66](#66-38c8e8f4cc---add-collectionsmarshalasbytesbitarray-116308) |
| System.Collections.Perf_Frozen<NotKnownComparable>.TryGetValue_True(Count: 4) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Collections.Perf_Frozen%28NotKnownComparable%29.TryGetValue_True%28Count%3A%204%29.html) | +8.36% | 11.180756 | 12.115985 | None |
| System.Text.Perf_Ascii.FromUtf16(Size: 128) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Text.Perf_Ascii.FromUtf16%28Size%3A%20128%29.html) | +6.84% | N/A | N/A | None |
| BenchmarksGame.FannkuchRedux_9.RunBench(n: 11, expectedSum: 556355) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/BenchmarksGame.FannkuchRedux_9.RunBench%28n%3A%2011%2C%20expectedSum%3A%20556355%29.html) | +5.32% | N/A | N/A | [4](#4-1c10ceecbf---jit-add-3-opt-implementation-for-improving-upon-rpo-based-block-layout-103450), [7](#7-b146d7512c---jit-move-loop-inversion-to-after-loop-recognition-115850) |
| System.Tests.Perf_UInt32.TryFormat(value: 0) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Tests.Perf_UInt32.TryFormat%28value%3A%200%29.html) | +5.28% | N/A | N/A | None |

---

## 2. ffcd1c5442 - Trust single-edge synthetic profile (#116054)

**Date:** 2025-05-28 16:16:24
**Commit:** [ffcd1c5442](https://github.com/dotnet/runtime/commit/ffcd1c5442a0c6e5317efa46d6ce381003397476)
**Affected Tests:** 40

| Test Name | Link | Change | .NET 9 | .NET 10 | Other Changepoints |
|-----------|------|--------|--------|-------|--------------------|
| System.IO.Hashing.Tests.Crc64_AppendPerf.Append(BufferSize: 256) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.IO.Hashing.Tests.Crc64_AppendPerf.Append%28BufferSize%3A%20256%29.html) | +317.01% | 14.007418 | 58.412864 | None |
| System.IO.Hashing.Tests.Crc64_AppendPerf.Append(BufferSize: 10240) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.IO.Hashing.Tests.Crc64_AppendPerf.Append%28BufferSize%3A%2010240%29.html) | +304.24% | 432.449977 | 1748.141904 | None |
| System.IO.Hashing.Tests.Crc64_AppendPerf.Append(BufferSize: 16) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.IO.Hashing.Tests.Crc64_AppendPerf.Append%28BufferSize%3A%2016%29.html) | +151.80% | 7.311149 | 18.409413 | None |
| System.Tests.Perf_Int32.ParseHex(value: "80000000") | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Tests.Perf_Int32.ParseHex%28value%3A%20%2280000000%22%29.html) | +109.59% | 14.824023 | 31.069864 | None |
| System.Tests.Perf_Int32.ParseHex(value: "7FFFFFFF") | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Tests.Perf_Int32.ParseHex%28value%3A%20%227FFFFFFF%22%29.html) | +110.11% | 14.777813 | 31.048991 | None |
| System.Collections.ContainsFalse<String>.List(Size: 512) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Collections.ContainsFalse%28String%29.List%28Size%3A%20512%29.html) | +106.91% | 243954.531241 | 504777.072629 | [18](#18-023686e6c2---jit-break-up-try-regions-in-compilerfgmovecoldblocks-and-fix-contiguity-later-108914) |
| System.Text.Encodings.Web.Tests.Perf_Encoders.EncodeUtf16(arguments: Url,&lorem ipsum=dolor sit amet,16) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Text.Encodings.Web.Tests.Perf_Encoders.EncodeUtf16%28arguments%3A%20Url%2C%26lorem%20ipsum%3Ddolor%20sit%20amet%2C16%29.html) | +117.06% | 51.955954 | 112.773346 | [1](#1-41be5e229b---jit-graph-based-loop-inversion-116017), [3](#3-ddf8075a2f---jit-visit-blocks-in-rpo-during-lsra-107927), [17](#17-e32148a8bd---jit-add-loop-aware-rpo-and-use-as-lsras-block-sequence-108086) |
| System.Text.Encodings.Web.Tests.Perf_Encoders.EncodeUtf16(arguments: JavaScript,&Hello+<World>!,16) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Text.Encodings.Web.Tests.Perf_Encoders.EncodeUtf16%28arguments%3A%20JavaScript%2C%26Hello%2B%28World%29%21%2C16%29.html) | +83.91% | 45.219853 | 83.163582 | [1](#1-41be5e229b---jit-graph-based-loop-inversion-116017), [3](#3-ddf8075a2f---jit-visit-blocks-in-rpo-during-lsra-107927), [17](#17-e32148a8bd---jit-add-loop-aware-rpo-and-use-as-lsras-block-sequence-108086) |
| System.Memory.Span<Int32>.StartsWith(Size: 4) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Memory.Span%28Int32%29.StartsWith%28Size%3A%204%29.html) | +66.02% | 2.426060 | 4.027732 | None |
| System.Tests.Perf_Int32.ParseHex(value: "3039") | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Tests.Perf_Int32.ParseHex%28value%3A%20%223039%22%29.html) | +61.43% | 11.405345 | 18.412114 | None |
| System.Text.Encodings.Web.Tests.Perf_Encoders.EncodeUtf16(arguments: UnsafeRelaxed,hello "there",16) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Text.Encodings.Web.Tests.Perf_Encoders.EncodeUtf16%28arguments%3A%20UnsafeRelaxed%2Chello%20%22there%22%2C16%29.html) | +55.06% | 26.004876 | 40.322620 | [17](#17-e32148a8bd---jit-add-loop-aware-rpo-and-use-as-lsras-block-sequence-108086) |
| System.Numerics.Tests.Perf_Quaternion.CreateFromRotationMatrixBenchmark | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Numerics.Tests.Perf_Quaternion.CreateFromRotationMatrixBenchmark.html) | +47.68% | 4.721879 | 6.973434 | [62](#62-e0e9f15d06---implement-various-convenience-methods-for-systemnumerics-types-115457) |
| System.Perf_Convert.ToBase64CharArray(binaryDataSize: 1024, formattingOptions: None) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Perf_Convert.ToBase64CharArray%28binaryDataSize%3A%201024%2C%20formattingOptions%3A%20None%29.html) | +55.73% | 99.114203 | 154.355360 | None |
| System.Collections.ContainsTrue<String>.ICollection(Size: 512) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Collections.ContainsTrue%28String%29.ICollection%28Size%3A%20512%29.html) | +48.50% | 121959.575033 | 181108.872513 | None |
| System.Collections.ContainsTrue<String>.List(Size: 512) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Collections.ContainsTrue%28String%29.List%28Size%3A%20512%29.html) | +47.18% | 120550.876415 | 177425.292553 | None |
| System.Collections.ContainsFalse<String>.ICollection(Size: 512) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Collections.ContainsFalse%28String%29.ICollection%28Size%3A%20512%29.html) | +45.45% | 241595.473690 | 351411.314261 | [18](#18-023686e6c2---jit-break-up-try-regions-in-compilerfgmovecoldblocks-and-fix-contiguity-later-108914) |
| System.Text.Encodings.Web.Tests.Perf_Encoders.EncodeUtf16(arguments: Url,�2020,16) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Text.Encodings.Web.Tests.Perf_Encoders.EncodeUtf16%28arguments%3A%20Url%2C%EF%BF%BD2020%2C16%29.html) | +37.56% | 31.168593 | 42.874129 | None |
| System.Text.Json.Tests.Perf_Basic.WriteBasicUtf8(Formatted: False, SkipValidation: False, DataSize: 100000) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Text.Json.Tests.Perf_Basic.WriteBasicUtf8%28Formatted%3A%20False%2C%20SkipValidation%3A%20False%2C%20DataSize%3A%20100000%29.html) | +36.99% | 1307454.111121 | 1791096.677739 | None |
| System.Collections.Concurrent.IsEmpty<String>.Queue(Size: 0) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Collections.Concurrent.IsEmpty%28String%29.Queue%28Size%3A%200%29.html) | +30.61% | 5.413180 | 7.070370 | None |
| System.Memory.Span<Char>.LastIndexOfValue(Size: 512) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Memory.Span%28Char%29.LastIndexOfValue%28Size%3A%20512%29.html) | +32.45% | 12.003508 | 15.898266 | None |
| System.Memory.Span<Int32>.SequenceEqual(Size: 4) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Memory.Span%28Int32%29.SequenceEqual%28Size%3A%204%29.html) | +27.82% | 2.966116 | 3.791309 | None |
| System.Net.Tests.Perf_WebUtility.Decode_DecodingRequired | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Net.Tests.Perf_WebUtility.Decode_DecodingRequired.html) | +23.86% | 89.057201 | 110.304468 | [1](#1-41be5e229b---jit-graph-based-loop-inversion-116017) |
| System.Tests.Perf_Int64.ToString(value: 9223372036854775807) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Tests.Perf_Int64.ToString%28value%3A%209223372036854775807%29.html) | +24.01% | 23.628379 | 29.302703 | None |
| System.Collections.CtorFromCollectionNonGeneric<String>.Hashtable(Size: 512) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Collections.CtorFromCollectionNonGeneric%28String%29.Hashtable%28Size%3A%20512%29.html) | +22.44% | 21830.938395 | 26729.902415 | None |
| System.Collections.Concurrent.IsEmpty<String>.Queue(Size: 512) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Collections.Concurrent.IsEmpty%28String%29.Queue%28Size%3A%20512%29.html) | +32.75% | 5.103955 | 6.775480 | None |
| System.Collections.CtorFromCollectionNonGeneric<Int32>.Hashtable(Size: 512) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Collections.CtorFromCollectionNonGeneric%28Int32%29.Hashtable%28Size%3A%20512%29.html) | +19.51% | 17916.812054 | 21412.376422 | None |
| System.Tests.Perf_Uri.EscapeDataString(input: "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa... | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Tests.Perf_Uri.EscapeDataString%28input%3A%20%22aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa.html) | +15.03% | 37.337376 | 42.949955 | None |
| System.Collections.CreateAddAndClear<String>.HashSet(Size: 512) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Collections.CreateAddAndClear%28String%29.HashSet%28Size%3A%20512%29.html) | +13.89% | 10929.727560 | 12448.304606 | None |
| System.Text.RegularExpressions.Tests.Perf_Regex_Industry_RustLang_Sherlock.Count(Pattern: "\\w+\\s+Holmes\\s+\\w+", Options: NonBacktracking) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Text.RegularExpressions.Tests.Perf_Regex_Industry_RustLang_Sherlock.Count%28Pattern%3A%20%22%5C%5Cw%2B%5C%5Cs%2BHolmes%5C%5Cs%2B%5C%5Cw%2B%22%2C%20Options%3A%20NonBacktracking%29.html) | +14.55% | 2175212.709672 | 2491693.698137 | None |
| System.Tests.Perf_Int64.ToString(value: 12345) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Tests.Perf_Int64.ToString%28value%3A%2012345%29.html) | +9.66% | 14.612125 | 16.022986 | None |
| System.Collections.TryAddDefaultSize<Int32>.Dictionary(Count: 512) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Collections.TryAddDefaultSize%28Int32%29.Dictionary%28Count%3A%20512%29.html) | +8.33% | 6108.629044 | 6617.362547 | None |
| System.Buffers.Text.Tests.Base64Tests.ConvertToBase64CharArray(NumberOfBytes: 1000) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Buffers.Text.Tests.Base64Tests.ConvertToBase64CharArray%28NumberOfBytes%3A%201000%29.html) | +10.68% | N/A | N/A | None |
| System.Tests.Perf_String.Format_OneArg(s: "Testing {0}, {0:C}, {0:D5}, {0:E} - {0:F4}{0:G}{0:N}  {0:X} !!", o: 8) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Tests.Perf_String.Format_OneArg%28s%3A%20%22Testing%20%7B0%7D%2C%20%7B0%3AC%7D%2C%20%7B0%3AD5%7D%2C%20%7B0%3AE%7D%20-%20%7B0%3AF4%7D%7B0%3AG%7D%7B0%3AN%7D%20%20%7B0%3AX%7D%20%21%21%22%2C%20o%3A%208%29.html) | +8.23% | 365.329711 | 395.384266 | None |
| System.Collections.CtorFromCollection<Int32>.ConcurrentBag(Size: 512) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Collections.CtorFromCollection%28Int32%29.ConcurrentBag%28Size%3A%20512%29.html) | +7.40% | 6178.465669 | 6635.953541 | None |
| System.Tests.Perf_UInt32.ToString(value: 0) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Tests.Perf_UInt32.ToString%28value%3A%200%29.html) | +8.03% | 4.205907 | 4.543818 | None |
| System.Memory.Span<Int32>.StartsWith(Size: 33) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Memory.Span%28Int32%29.StartsWith%28Size%3A%2033%29.html) | +8.23% | 3.921109 | 4.243648 | None |
| System.IO.Tests.Perf_Path.GetFileNameWithoutExtension | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.IO.Tests.Perf_Path.GetFileNameWithoutExtension.html) | +10.32% | 22.774167 | 25.124055 | None |
| System.Tests.Perf_Uri.EscapeDataString(input: "üüüüüüüüüüüüüüüüüüüüüüüüüüüüüüüüüüüüüüüüüüüüüüüüüüüüüüüüüüüüüüüüüüüüüüüüüüüüüüüüüüüüüüüüüüüüüüüüüüüüüüüüüüüüüüüüüüüüüüüüüüüüüüüüüüüüüüüüüüüüüüüüüüüüüüüüü... | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Tests.Perf_Uri.EscapeDataString%28input%3A%20%22%C3%BC%C3%BC%C3%BC%C3%BC%C3%BC%C3%BC%C3%BC%C3%BC%C3%BC%C3%BC%C3%BC%C3%BC%C3%BC%C3%BC%C3%BC%C3%BC%C3%BC%C3%BC%C3%BC%C3%BC%C3%BC%C3%BC%C3%BC%C3%BC%C3%BC%C3%BC%C3%BC%C3%BC%C3%BC%C3%BC%C3%BC%C3%BC%C3%BC%C3%BC%C3%BC%C3%BC%C3%BC%C3%BC%C3%BC%C3%BC%C3%BC%C3%BC%C3%BC%C3%BC%C3%BC%C3%BC%C3%BC%C3%BC%C3%BC%C3%BC%C3%BC%C3%BC%C3%BC%C3%BC%C3%BC%C3%BC%C3%BC%C3%BC%C3%BC%C3%BC%C3%BC%C3%BC%C3%BC%C3%BC%C3%BC%C3%BC%C3%BC%C3%BC%C3%BC%C3%BC%C3%BC%C3%BC%C3%BC%C3%BC%C3%BC%C3%BC%C3%BC%C3%BC%C3%BC%C3%BC%C3%BC%C3%BC%C3%BC%C3%BC%C3%BC%C3%BC%C3%BC%C3%BC%C3%BC%C3%BC%C3%BC%C3%BC%C3%BC%C3%BC%C3%BC%C3%BC%C3%BC%C3%BC%C3%BC%C3%BC%C3%BC%C3%BC%C3%BC%C3%BC%C3%BC%C3%BC%C3%BC%C3%BC%C3%BC%C3%BC%C3%BC%C3%BC%C3%BC%C3%BC%C3%BC%C3%BC%C3%BC%C3%BC%C3%BC%C3%BC%C3%BC%C3%BC%C3%BC%C3%BC%C3%BC%C3%BC%C3%BC%C3%BC%C3%BC%C3%BC%C3%BC%C3%BC%C3%BC%C3%BC%C3%BC%C3%BC%C3%BC%C3%BC%C3%BC%C3%BC%C3%BC%C3%BC%C3%BC%C3%BC%C3%BC%C3%BC%C3%BC%C3%BC%C3%BC%C3%BC%C3%BC%C3%BC%C3%BC.html) | +6.88% | N/A | N/A | None |
| System.Tests.Perf_Enum.GetValuesAsUnderlyingType_Generic | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Tests.Perf_Enum.GetValuesAsUnderlyingType_Generic.html) | +5.48% | 21.759192 | 22.952629 | None |
| System.Tests.Perf_Enum.GetNames_Generic | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Tests.Perf_Enum.GetNames_Generic.html) | +6.56% | 21.039155 | 22.418945 | None |

---

## 3. ddf8075a2f - JIT: Visit blocks in RPO during LSRA (#107927)

**Date:** 2024-09-20 18:38:45
**Commit:** [ddf8075a2f](https://github.com/dotnet/runtime/commit/ddf8075a2fa3044554ded41c375a82a318ae01eb)
**Affected Tests:** 39

| Test Name | Link | Change | .NET 9 | .NET 10 | Other Changepoints |
|-----------|------|--------|--------|-------|--------------------|
| System.Text.Encodings.Web.Tests.Perf_Encoders.EncodeUtf16(arguments: Url,&lorem ipsum=dolor sit amet,16) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Text.Encodings.Web.Tests.Perf_Encoders.EncodeUtf16%28arguments%3A%20Url%2C%26lorem%20ipsum%3Ddolor%20sit%20amet%2C16%29.html) | +117.06% | 51.955954 | 112.773346 | [1](#1-41be5e229b---jit-graph-based-loop-inversion-116017), [2](#2-ffcd1c5442---trust-single-edge-synthetic-profile-116054), [17](#17-e32148a8bd---jit-add-loop-aware-rpo-and-use-as-lsras-block-sequence-108086) |
| System.Text.Encodings.Web.Tests.Perf_Encoders.EncodeUtf16(arguments: JavaScript,&Hello+<World>!,16) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Text.Encodings.Web.Tests.Perf_Encoders.EncodeUtf16%28arguments%3A%20JavaScript%2C%26Hello%2B%28World%29%21%2C16%29.html) | +83.91% | 45.219853 | 83.163582 | [1](#1-41be5e229b---jit-graph-based-loop-inversion-116017), [2](#2-ffcd1c5442---trust-single-edge-synthetic-profile-116054), [17](#17-e32148a8bd---jit-add-loop-aware-rpo-and-use-as-lsras-block-sequence-108086) |
| System.Buffers.Tests.ReadOnlySequenceTests<Byte>.IterateGetPositionArray | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Buffers.Tests.ReadOnlySequenceTests%28Byte%29.IterateGetPositionArray.html) | +62.42% | 13.821776 | 22.448740 | [4](#4-1c10ceecbf---jit-add-3-opt-implementation-for-improving-upon-rpo-based-block-layout-103450), [5](#5-6d12a304b3---jit-do-greedy-4-opt-for-backward-jumps-in-3-opt-layout-110277) |
| System.Collections.ContainsFalse<String>.ImmutableArray(Size: 512) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Collections.ContainsFalse%28String%29.ImmutableArray%28Size%3A%20512%29.html) | +73.75% | 240957.811365 | 418653.725827 | None |
| System.Text.Encodings.Web.Tests.Perf_Encoders.EncodeUtf16(arguments: Url,&lorem ipsum=dolor sit amet,512) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Text.Encodings.Web.Tests.Perf_Encoders.EncodeUtf16%28arguments%3A%20Url%2C%26lorem%20ipsum%3Ddolor%20sit%20amet%2C512%29.html) | +54.16% | 91.276555 | 140.711793 | None |
| System.Memory.Span<Int32>.IndexOfAnyTwoValues(Size: 512) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Memory.Span%28Int32%29.IndexOfAnyTwoValues%28Size%3A%20512%29.html) | +45.63% | 122.463217 | 178.339434 | [1](#1-41be5e229b---jit-graph-based-loop-inversion-116017) |
| System.Collections.ContainsFalse<String>.LinkedList(Size: 512) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Collections.ContainsFalse%28String%29.LinkedList%28Size%3A%20512%29.html) | +53.17% | 469338.269717 | 718887.366510 | None |
| System.Tests.Perf_Int32.TryParse(value: "-2147483648") | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Tests.Perf_Int32.TryParse%28value%3A%20%22-2147483648%22%29.html) | +27.01% | 15.086563 | 19.160966 | [7](#7-b146d7512c---jit-move-loop-inversion-to-after-loop-recognition-115850) |
| System.Memory.Span<Int32>.IndexOfAnyTwoValues(Size: 33) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Memory.Span%28Int32%29.IndexOfAnyTwoValues%28Size%3A%2033%29.html) | +34.45% | 10.036435 | 13.493959 | [1](#1-41be5e229b---jit-graph-based-loop-inversion-116017) |
| System.Text.RegularExpressions.Tests.Perf_Regex_Industry_RustLang_Sherlock.Count(Pattern: "zqj", Options: NonBacktracking) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Text.RegularExpressions.Tests.Perf_Regex_Industry_RustLang_Sherlock.Count%28Pattern%3A%20%22zqj%22%2C%20Options%3A%20NonBacktracking%29.html) | +35.63% | 36386.185077 | 49351.061448 | [8](#8-34545d790e---jit-dont-mark-callees-noinline-for-non-fatal-observations-with-pgo-114821) |
| System.Memory.Span<Int32>.LastIndexOfAnyValues(Size: 512) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Memory.Span%28Int32%29.LastIndexOfAnyValues%28Size%3A%20512%29.html) | +32.19% | 128.974332 | 170.486824 | None |
| System.Memory.Span<Int32>.IndexOfAnyThreeValues(Size: 512) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Memory.Span%28Int32%29.IndexOfAnyThreeValues%28Size%3A%20512%29.html) | +31.38% | 179.061302 | 235.245169 | None |
| System.Linq.Tests.Perf_OrderBy.OrderByCustomComparer(NumberOfPeople: 512) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Linq.Tests.Perf_OrderBy.OrderByCustomComparer%28NumberOfPeople%3A%20512%29.html) | +40.98% | 38597.408984 | 54416.211853 | None |
| System.Tests.Perf_Int128.ParseSpan(value: "170141183460469231731687303715884105727") | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Tests.Perf_Int128.ParseSpan%28value%3A%20%22170141183460469231731687303715884105727%22%29.html) | +27.09% | 75.429986 | 95.864294 | [6](#6-ea43e17c---jit-run-profile-repair-after-frontend-phases-111915), [26](#26-16782a4481---jit-recompute-test-block-weights-after-loop-inversion-112197) |
| System.Tests.Perf_Int128.Parse(value: "170141183460469231731687303715884105727") | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Tests.Perf_Int128.Parse%28value%3A%20%22170141183460469231731687303715884105727%22%29.html) | +26.14% | 76.128031 | 96.028541 | [6](#6-ea43e17c---jit-run-profile-repair-after-frontend-phases-111915) |
| System.Memory.Span<Int32>.IndexOfAnyThreeValues(Size: 33) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Memory.Span%28Int32%29.IndexOfAnyThreeValues%28Size%3A%2033%29.html) | +23.28% | 13.768474 | 16.973103 | [1](#1-41be5e229b---jit-graph-based-loop-inversion-116017) |
| System.Tests.Perf_Version.TryFormat4 | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Tests.Perf_Version.TryFormat4.html) | +22.16% | 16.913676 | 20.662248 | [1](#1-41be5e229b---jit-graph-based-loop-inversion-116017) |
| System.Globalization.Tests.StringSearch.IndexOf_Word_NotFound(Options: (en-US, Ordinal, False)) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Globalization.Tests.StringSearch.IndexOf_Word_NotFound%28Options%3A%20%28en-US%2C%20Ordinal%2C%20False%29%29.html) | +20.52% | 18.726898 | 22.568890 | None |
| System.Memory.Span<Int32>.LastIndexOfAnyValues(Size: 33) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Memory.Span%28Int32%29.LastIndexOfAnyValues%28Size%3A%2033%29.html) | +20.20% | 10.944000 | 13.155101 | None |
| System.Linq.Tests.Perf_Enumerable.OrderByThenBy(input: IEnumerable) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Linq.Tests.Perf_Enumerable.OrderByThenBy%28input%3A%20IEnumerable%29.html) | +16.98% | 2032.197565 | 2377.330645 | None |
| System.Collections.ContainsKeyTrue<String, String>.FrozenDictionary(Size: 512) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Collections.ContainsKeyTrue%28String%2C%20String%29.FrozenDictionary%28Size%3A%20512%29.html) | +17.73% | 5107.537873 | 6013.126537 | [49](#49-bf369fd44e---jit-account-for-newly-unreachable-blocks-in-morph-109394) |
| System.Collections.ContainsKeyFalse<String, String>.FrozenDictionary(Size: 512) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Collections.ContainsKeyFalse%28String%2C%20String%29.FrozenDictionary%28Size%3A%20512%29.html) | +20.53% | 4961.621773 | 5980.295514 | None |
| System.Memory.Span<Byte>.SequenceCompareTo(Size: 512) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Memory.Span%28Byte%29.SequenceCompareTo%28Size%3A%20512%29.html) | +16.56% | 11.234156 | 13.094407 | None |
| System.Tests.Perf_UInt32.ParseSpan(value: "0") | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Tests.Perf_UInt32.ParseSpan%28value%3A%20%220%22%29.html) | +12.97% | 8.262927 | 9.334832 | None |
| System.Tests.Perf_Int32.Parse(value: "-2147483648") | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Tests.Perf_Int32.Parse%28value%3A%20%22-2147483648%22%29.html) | +16.71% | 15.694320 | 18.316560 | [7](#7-b146d7512c---jit-move-loop-inversion-to-after-loop-recognition-115850) |
| System.Numerics.Tests.Perf_BigInteger.Remainder(arguments: 1024,512 bits) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Numerics.Tests.Perf_BigInteger.Remainder%28arguments%3A%201024%2C512%20bits%29.html) | +43.33% | 446.845960 | 640.460712 | None |
| System.Tests.Perf_UInt64.TryParseHex(value: "FFFFFFFFFFFFFFFF") | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Tests.Perf_UInt64.TryParseHex%28value%3A%20%22FFFFFFFFFFFFFFFF%22%29.html) | +12.05% | 19.356449 | 21.689244 | None |
| System.Tests.Perf_String.Replace_String(text: "This is a very nice sentence", oldValue: "bad", newValue: "nice") | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Tests.Perf_String.Replace_String%28text%3A%20%22This%20is%20a%20very%20nice%20sentence%22%2C%20oldValue%3A%20%22bad%22%2C%20newValue%3A%20%22nice%22%29.html) | +11.58% | 10.992894 | 12.265702 | None |
| System.Tests.Perf_UInt16.Parse(value: "12345") | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Tests.Perf_UInt16.Parse%28value%3A%20%2212345%22%29.html) | +9.68% | 12.796635 | 14.034732 | None |
| System.Text.RegularExpressions.Tests.Perf_Regex_Industry_RustLang_Sherlock.Count(Pattern: "zqj", Options: None) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Text.RegularExpressions.Tests.Perf_Regex_Industry_RustLang_Sherlock.Count%28Pattern%3A%20%22zqj%22%2C%20Options%3A%20None%29.html) | +9.95% | 36692.409968 | 40344.345958 | None |
| System.Text.RegularExpressions.Tests.Perf_Regex_Industry_BoostDocs_Simple.IsMatch(Id: 3, Options: NonBacktracking) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Text.RegularExpressions.Tests.Perf_Regex_Industry_BoostDocs_Simple.IsMatch%28Id%3A%203%2C%20Options%3A%20NonBacktracking%29.html) | +9.24% | N/A | N/A | None |
| System.Tests.Perf_UInt32.TryParse(value: "4294967295") | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Tests.Perf_UInt32.TryParse%28value%3A%20%224294967295%22%29.html) | +10.37% | 14.210496 | 15.684469 | None |
| System.Tests.Perf_Int32.ParseSpan(value: "2147483647") | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Tests.Perf_Int32.ParseSpan%28value%3A%20%222147483647%22%29.html) | +7.60% | 15.226411 | 16.384223 | [7](#7-b146d7512c---jit-move-loop-inversion-to-after-loop-recognition-115850) |
| System.Tests.Perf_Int32.ParseSpan(value: "-2147483648") | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Tests.Perf_Int32.ParseSpan%28value%3A%20%22-2147483648%22%29.html) | +7.54% | 14.961611 | 16.089049 | None |
| System.Globalization.Tests.StringSearch.IsSuffix_DifferentLastChar(Options: (en-US, IgnoreCase, False)) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Globalization.Tests.StringSearch.IsSuffix_DifferentLastChar%28Options%3A%20%28en-US%2C%20IgnoreCase%2C%20False%29%29.html) | +5.62% | 10.077244 | 10.643837 | None |
| MicroBenchmarks.Serializers.Json_ToStream<IndexViewModel>.JsonNet_ | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/MicroBenchmarks.Serializers.Json_ToStream%28IndexViewModel%29.JsonNet_.html) | +5.81% | 23827.491295 | 25212.162601 | None |
| System.Globalization.Tests.StringSearch.IsSuffix_DifferentLastChar(Options: (, IgnoreCase, False)) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Globalization.Tests.StringSearch.IsSuffix_DifferentLastChar%28Options%3A%20%28%2C%20IgnoreCase%2C%20False%29%29.html) | +5.82% | 10.069208 | 10.655704 | None |
| System.Text.RegularExpressions.Tests.Perf_Regex_Industry_Leipzig.Count(Pattern: "Twain", Options: NonBacktracking) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Text.RegularExpressions.Tests.Perf_Regex_Industry_Leipzig.Count%28Pattern%3A%20%22Twain%22%2C%20Options%3A%20NonBacktracking%29.html) | +6.33% | N/A | N/A | None |
| System.Tests.Perf_Int64.Parse(value: "-9223372036854775808") | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Tests.Perf_Int64.Parse%28value%3A%20%22-9223372036854775808%22%29.html) | +6.34% | 21.923670 | 23.314672 | [7](#7-b146d7512c---jit-move-loop-inversion-to-after-loop-recognition-115850) |

---

## 4. 1c10ceecbf - JIT: Add 3-opt implementation for improving upon RPO-based block layout (#103450)

**Date:** 2024-11-04 18:18:38
**Commit:** [1c10ceecbf](https://github.com/dotnet/runtime/commit/1c10ceecbf)
**Affected Tests:** 21

| Test Name | Link | Change | .NET 9 | .NET 10 | Other Changepoints |
|-----------|------|--------|--------|-------|--------------------|
| System.Buffers.Tests.ReadOnlySequenceTests<Byte>.IterateGetPositionArray | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Buffers.Tests.ReadOnlySequenceTests%28Byte%29.IterateGetPositionArray.html) | +62.42% | 13.821776 | 22.448740 | [3](#3-ddf8075a2f---jit-visit-blocks-in-rpo-during-lsra-107927), [5](#5-6d12a304b3---jit-do-greedy-4-opt-for-backward-jumps-in-3-opt-layout-110277) |
| Benchstone.BenchI.XposMatrix.Test | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/Benchstone.BenchI.XposMatrix.Test.html) | +61.45% | 12231.521028 | 19748.149585 | [1](#1-41be5e229b---jit-graph-based-loop-inversion-116017), [6](#6-ea43e17c---jit-run-profile-repair-after-frontend-phases-111915), [36](#36-aecae2c385---jit-enable-profile-consistency-checking-up-to-morph-111047) |
| Span.Sorting.BubbleSortArray(Size: 512) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/Span.Sorting.BubbleSortArray%28Size%3A%20512%29.html) | +54.92% | 157529.645102 | 244044.942506 | [1](#1-41be5e229b---jit-graph-based-loop-inversion-116017) |
| System.Text.Perf_Utf8Encoding.GetByteCount(Input: Cyrillic) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Text.Perf_Utf8Encoding.GetByteCount%28Input%3A%20Cyrillic%29.html) | +43.91% | 6326.429031 | 9104.221038 | None |
| System.Collections.Perf_Frozen<NotKnownComparable>.Contains_True(Count: 512) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Collections.Perf_Frozen%28NotKnownComparable%29.Contains_True%28Count%3A%20512%29.html) | +30.54% | 1311.880582 | 1712.486339 | [1](#1-41be5e229b---jit-graph-based-loop-inversion-116017), [5](#5-6d12a304b3---jit-do-greedy-4-opt-for-backward-jumps-in-3-opt-layout-110277) |
| Benchstone.MDBenchI.MDMidpoint.Test | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/Benchstone.MDBenchI.MDMidpoint.Test.html) | +31.86% | 317956694.500000 | 419255291.910714 | [5](#5-6d12a304b3---jit-do-greedy-4-opt-for-backward-jumps-in-3-opt-layout-110277), [6](#6-ea43e17c---jit-run-profile-repair-after-frontend-phases-111915) |
| System.Text.Perf_Utf8Encoding.GetByteCount(Input: Chinese) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Text.Perf_Utf8Encoding.GetByteCount%28Input%3A%20Chinese%29.html) | +32.70% | 8082.782142 | 10726.182058 | None |
| System.Collections.Perf_Frozen<NotKnownComparable>.Contains_True(Count: 64) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Collections.Perf_Frozen%28NotKnownComparable%29.Contains_True%28Count%3A%2064%29.html) | +27.79% | 159.425812 | 203.737008 | [1](#1-41be5e229b---jit-graph-based-loop-inversion-116017), [5](#5-6d12a304b3---jit-do-greedy-4-opt-for-backward-jumps-in-3-opt-layout-110277) |
| System.Text.Perf_Utf8Encoding.GetByteCount(Input: Greek) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Text.Perf_Utf8Encoding.GetByteCount%28Input%3A%20Greek%29.html) | +32.67% | 8744.696373 | 11601.644054 | None |
| Benchstone.BenchI.Puzzle.Test | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/Benchstone.BenchI.Puzzle.Test.html) | +25.76% | 280463545.632653 | 352714644.500000 | None |
| System.Text.Json.Document.Tests.Perf_DocumentParse.Parse(IsDataIndented: True, TestRandomAccess: False, TestCase: Json400KB) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Text.Json.Document.Tests.Perf_DocumentParse.Parse%28IsDataIndented%3A%20True%2C%20TestRandomAccess%3A%20False%2C%20TestCase%3A%20Json400KB%29.html) | +18.63% | 660790.466327 | 783908.736827 | None |
| Benchstone.BenchI.HeapSort.Test | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/Benchstone.BenchI.HeapSort.Test.html) | +17.70% | 250616.221085 | 294971.420746 | [5](#5-6d12a304b3---jit-do-greedy-4-opt-for-backward-jumps-in-3-opt-layout-110277) |
| System.Text.Perf_Utf8Encoding.GetByteCount(Input: EnglishMostlyAscii) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Text.Perf_Utf8Encoding.GetByteCount%28Input%3A%20EnglishMostlyAscii%29.html) | +16.23% | 18434.410042 | 21426.415114 | None |
| ByteMark.BenchNumericSortRectangular | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/ByteMark.BenchNumericSortRectangular.html) | +8.60% | 808365046.089286 | 877864984.122449 | None |
| System.Collections.Tests.Perf_PriorityQueue<Int32, Int32>.Dequeue_And_Enqueue(Size: 1000) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Collections.Tests.Perf_PriorityQueue%28Int32%2C%20Int32%29.Dequeue_And_Enqueue%28Size%3A%201000%29.html) | +12.97% | 108003.170907 | 122005.946466 | None |
| Span.IndexerBench.CoveredIndex3(length: 1024) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/Span.IndexerBench.CoveredIndex3%28length%3A%201024%29.html) | +13.82% | 811.680994 | 923.879747 | None |
| Microsoft.Extensions.Primitives.StringSegmentBenchmark.IndexOf | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/Microsoft.Extensions.Primitives.StringSegmentBenchmark.IndexOf.html) | +12.77% | 5.034268 | 5.677320 | None |
| Struct.FilteredSpanEnumerator.Sum | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/Struct.FilteredSpanEnumerator.Sum.html) | +12.60% | 4397.094781 | 4951.102706 | None |
| Benchstone.MDBenchF.MDInvMt.Test | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/Benchstone.MDBenchF.MDInvMt.Test.html) | +9.73% | 2703355.513500 | 2966274.296000 | None |
| System.Text.RegularExpressions.Tests.Perf_Regex_Industry_RustLang_Sherlock.Count(Pattern: "Sherlock\|Holmes\|Watson", Options: None) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Text.RegularExpressions.Tests.Perf_Regex_Industry_RustLang_Sherlock.Count%28Pattern%3A%20%22Sherlock%7CHolmes%7CWatson%22%2C%20Options%3A%20None%29.html) | +9.29% | 240052.597495 | 262343.575186 | None |
| BenchmarksGame.FannkuchRedux_9.RunBench(n: 11, expectedSum: 556355) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/BenchmarksGame.FannkuchRedux_9.RunBench%28n%3A%2011%2C%20expectedSum%3A%20556355%29.html) | +5.32% | N/A | N/A | [1](#1-41be5e229b---jit-graph-based-loop-inversion-116017), [7](#7-b146d7512c---jit-move-loop-inversion-to-after-loop-recognition-115850) |

---

## 5. 6d12a304b3 - JIT: Do greedy 4-opt for backward jumps in 3-opt layout (#110277)

**Date:** 2024-12-03 21:25:35
**Commit:** [6d12a304b3](https://github.com/dotnet/runtime/commit/6d12a304b3068f8a9308a1aec4f3b95dd636a693)
**Affected Tests:** 20

| Test Name | Link | Change | .NET 9 | .NET 10 | Other Changepoints |
|-----------|------|--------|--------|-------|--------------------|
| System.Tests.Perf_Char.Char_ToUpperInvariant(input: "Hello World!") | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Tests.Perf_Char.Char_ToUpperInvariant%28input%3A%20%22Hello%20World%21%22%29.html) | +85.29% | 8.687607 | 16.097684 | None |
| System.Collections.ContainsKeyFalse<Int32, Int32>.ImmutableDictionary(Size: 512) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Collections.ContainsKeyFalse%28Int32%2C%20Int32%29.ImmutableDictionary%28Size%3A%20512%29.html) | +63.62% | 9853.618257 | 16122.428785 | None |
| System.Buffers.Tests.ReadOnlySequenceTests<Byte>.IterateGetPositionArray | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Buffers.Tests.ReadOnlySequenceTests%28Byte%29.IterateGetPositionArray.html) | +62.42% | 13.821776 | 22.448740 | [3](#3-ddf8075a2f---jit-visit-blocks-in-rpo-during-lsra-107927), [4](#4-1c10ceecbf---jit-add-3-opt-implementation-for-improving-upon-rpo-based-block-layout-103450) |
| SeekUnroll.Test(boxedIndex: 19) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/SeekUnroll.Test%28boxedIndex%3A%2019%29.html) | +61.96% | 1216021179.946428 | 1969517653.535714 | None |
| System.Collections.IterateForEach<Int32>.Dictionary(Size: 512) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Collections.IterateForEach%28Int32%29.Dictionary%28Size%3A%20512%29.html) | +51.80% | 375.550518 | 570.067155 | None |
| System.Collections.IterateForEach<Int32>.HashSet(Size: 512) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Collections.IterateForEach%28Int32%29.HashSet%28Size%3A%20512%29.html) | +52.75% | 373.503637 | 570.529894 | None |
| System.Collections.Tests.Perf_Dictionary.Clone(Items: 3000) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Collections.Tests.Perf_Dictionary.Clone%28Items%3A%203000%29.html) | +39.80% | 7103.178665 | 9930.379163 | [1](#1-41be5e229b---jit-graph-based-loop-inversion-116017) |
| System.Collections.CtorFromCollection<Int32>.Dictionary(Size: 512) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Collections.CtorFromCollection%28Int32%29.Dictionary%28Size%3A%20512%29.html) | +37.03% | 1281.207446 | 1755.690698 | None |
| System.Collections.Perf_Frozen<NotKnownComparable>.Contains_True(Count: 512) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Collections.Perf_Frozen%28NotKnownComparable%29.Contains_True%28Count%3A%20512%29.html) | +30.54% | 1311.880582 | 1712.486339 | [1](#1-41be5e229b---jit-graph-based-loop-inversion-116017), [4](#4-1c10ceecbf---jit-add-3-opt-implementation-for-improving-upon-rpo-based-block-layout-103450) |
| Benchstone.MDBenchI.MDMidpoint.Test | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/Benchstone.MDBenchI.MDMidpoint.Test.html) | +31.86% | 317956694.500000 | 419255291.910714 | [4](#4-1c10ceecbf---jit-add-3-opt-implementation-for-improving-upon-rpo-based-block-layout-103450), [6](#6-ea43e17c---jit-run-profile-repair-after-frontend-phases-111915) |
| System.Collections.Perf_Frozen<NotKnownComparable>.Contains_True(Count: 64) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Collections.Perf_Frozen%28NotKnownComparable%29.Contains_True%28Count%3A%2064%29.html) | +27.79% | 159.425812 | 203.737008 | [1](#1-41be5e229b---jit-graph-based-loop-inversion-116017), [4](#4-1c10ceecbf---jit-add-3-opt-implementation-for-improving-upon-rpo-based-block-layout-103450) |
| Struct.GSeq.FilterSkipMapSum | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/Struct.GSeq.FilterSkipMapSum.html) | +20.82% | 9884.702677 | 11942.546012 | None |
| System.Collections.ContainsTrueComparer<Int32>.ImmutableHashSet(Size: 512) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Collections.ContainsTrueComparer%28Int32%29.ImmutableHashSet%28Size%3A%20512%29.html) | +22.66% | 10198.014617 | 12509.257850 | None |
| System.Collections.TryGetValueFalse<BigStruct, BigStruct>.SortedDictionary(Size: 512) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Collections.TryGetValueFalse%28BigStruct%2C%20BigStruct%29.SortedDictionary%28Size%3A%20512%29.html) | +16.28% | 21053.492609 | 24480.362216 | None |
| System.Collections.Perf_Frozen<Int16>.Contains_True(Count: 4) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Collections.Perf_Frozen%28Int16%29.Contains_True%28Count%3A%204%29.html) | +21.48% | 12.094405 | 14.692279 | [1](#1-41be5e229b---jit-graph-based-loop-inversion-116017) |
| Benchstone.BenchI.HeapSort.Test | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/Benchstone.BenchI.HeapSort.Test.html) | +17.70% | 250616.221085 | 294971.420746 | [4](#4-1c10ceecbf---jit-add-3-opt-implementation-for-improving-upon-rpo-based-block-layout-103450) |
| System.Collections.ContainsTrueComparer<Int32>.SortedSet(Size: 512) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Collections.ContainsTrueComparer%28Int32%29.SortedSet%28Size%3A%20512%29.html) | +11.53% | 15742.299606 | 17558.095687 | None |
| System.Collections.IndexerSet<Int32>.IList(Size: 512) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Collections.IndexerSet%28Int32%29.IList%28Size%3A%20512%29.html) | +12.08% | 515.876053 | 578.215416 | None |
| System.Collections.TryGetValueFalse<Int32, Int32>.ImmutableDictionary(Size: 512) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Collections.TryGetValueFalse%28Int32%2C%20Int32%29.ImmutableDictionary%28Size%3A%20512%29.html) | +10.32% | 10726.075052 | 11832.988165 | None |
| System.Collections.CreateAddAndClear<Int32>.SortedDictionary(Size: 512) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Collections.CreateAddAndClear%28Int32%29.SortedDictionary%28Size%3A%20512%29.html) | +5.64% | 32293.636974 | 34114.727222 | None |

---

## 6. ea43e17c - JIT: Run profile repair after frontend phases (#111915)

**Date:** 2025-02-21 16:40:21
**Commit:** [ea43e17c](https://github.com/dotnet/runtime/commit/ea43e17c)
**Affected Tests:** 20

| Test Name | Link | Change | .NET 9 | .NET 10 | Other Changepoints |
|-----------|------|--------|--------|-------|--------------------|
| Benchstone.BenchI.XposMatrix.Test | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/Benchstone.BenchI.XposMatrix.Test.html) | +61.45% | 12231.521028 | 19748.149585 | [1](#1-41be5e229b---jit-graph-based-loop-inversion-116017), [4](#4-1c10ceecbf---jit-add-3-opt-implementation-for-improving-upon-rpo-based-block-layout-103450), [36](#36-aecae2c385---jit-enable-profile-consistency-checking-up-to-morph-111047) |
| System.Tests.Perf_Int64.TryParse(value: "9223372036854775807") | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Tests.Perf_Int64.TryParse%28value%3A%20%229223372036854775807%22%29.html) | +53.20% | 20.146144 | 30.864106 | None |
| System.Tests.Perf_Int64.ParseSpan(value: "9223372036854775807") | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Tests.Perf_Int64.ParseSpan%28value%3A%20%229223372036854775807%22%29.html) | +45.36% | 21.452664 | 31.183639 | None |
| System.Tests.Perf_Int64.TryParseSpan(value: "9223372036854775807") | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Tests.Perf_Int64.TryParseSpan%28value%3A%20%229223372036854775807%22%29.html) | +44.60% | 21.271424 | 30.758262 | None |
| System.Tests.Perf_Int64.Parse(value: "9223372036854775807") | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Tests.Perf_Int64.Parse%28value%3A%20%229223372036854775807%22%29.html) | +42.28% | 21.192366 | 30.152397 | None |
| Benchstone.MDBenchI.MDMidpoint.Test | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/Benchstone.MDBenchI.MDMidpoint.Test.html) | +31.86% | 317956694.500000 | 419255291.910714 | [4](#4-1c10ceecbf---jit-add-3-opt-implementation-for-improving-upon-rpo-based-block-layout-103450), [5](#5-6d12a304b3---jit-do-greedy-4-opt-for-backward-jumps-in-3-opt-layout-110277) |
| System.Tests.Perf_Int128.ParseSpan(value: "170141183460469231731687303715884105727") | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Tests.Perf_Int128.ParseSpan%28value%3A%20%22170141183460469231731687303715884105727%22%29.html) | +27.09% | 75.429986 | 95.864294 | [3](#3-ddf8075a2f---jit-visit-blocks-in-rpo-during-lsra-107927), [26](#26-16782a4481---jit-recompute-test-block-weights-after-loop-inversion-112197) |
| System.Tests.Perf_Int128.TryParseSpan(value: "170141183460469231731687303715884105727") | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Tests.Perf_Int128.TryParseSpan%28value%3A%20%22170141183460469231731687303715884105727%22%29.html) | +27.53% | 75.656432 | 96.487566 | [26](#26-16782a4481---jit-recompute-test-block-weights-after-loop-inversion-112197) |
| System.Tests.Perf_Int128.Parse(value: "170141183460469231731687303715884105727") | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Tests.Perf_Int128.Parse%28value%3A%20%22170141183460469231731687303715884105727%22%29.html) | +26.14% | 76.128031 | 96.028541 | [3](#3-ddf8075a2f---jit-visit-blocks-in-rpo-during-lsra-107927) |
| System.Tests.Perf_Int128.TryParse(value: "170141183460469231731687303715884105727") | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Tests.Perf_Int128.TryParse%28value%3A%20%22170141183460469231731687303715884105727%22%29.html) | +28.53% | 75.891943 | 97.547552 | [26](#26-16782a4481---jit-recompute-test-block-weights-after-loop-inversion-112197) |
| System.Tests.Perf_Int128.TryParse(value: "12345") | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Tests.Perf_Int128.TryParse%28value%3A%20%2212345%22%29.html) | +20.65% | 15.159332 | 18.289968 | None |
| Microsoft.Extensions.Primitives.Performance.StringValuesBenchmark.Indexer_FirstElement_String | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/Microsoft.Extensions.Primitives.Performance.StringValuesBenchmark.Indexer_FirstElement_String.html) | +14.95% | 3.007789 | 3.457492 | None |
| System.Tests.Perf_UInt64.ParseSpan(value: "12345") | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Tests.Perf_UInt64.ParseSpan%28value%3A%20%2212345%22%29.html) | +13.41% | 11.552466 | 13.101816 | None |
| System.Tests.Perf_DateTime.ObjectEquals | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Tests.Perf_DateTime.ObjectEquals.html) | +13.75% | 2.046081 | 2.327471 | None |
| Benchmark.GetChildKeysTests.AddChainedConfigurationWithSplitting | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/Benchmark.GetChildKeysTests.AddChainedConfigurationWithSplitting.html) | +10.88% | 195826.263962 | 217141.087011 | None |
| System.Tests.Perf_UInt64.TryParse(value: "12345") | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Tests.Perf_UInt64.TryParse%28value%3A%20%2212345%22%29.html) | +11.24% | 11.945981 | 13.288597 | None |
| System.Tests.Perf_UInt64.ParseSpan(value: "18446744073709551615") | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Tests.Perf_UInt64.ParseSpan%28value%3A%20%2218446744073709551615%22%29.html) | +14.59% | 20.749307 | 23.776318 | None |
| Benchmark.GetChildKeysTests.AddChainedConfigurationWithCommonPaths | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/Benchmark.GetChildKeysTests.AddChainedConfigurationWithCommonPaths.html) | +12.37% | 192576.722680 | 216391.322101 | None |
| System.Tests.Perf_UInt64.Parse(value: "18446744073709551615") | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Tests.Perf_UInt64.Parse%28value%3A%20%2218446744073709551615%22%29.html) | +10.32% | 22.611164 | 24.944786 | None |
| System.Tests.Perf_Int32.TryParse(value: "12345") | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Tests.Perf_Int32.TryParse%28value%3A%20%2212345%22%29.html) | +9.23% | 10.580130 | 11.556880 | [45](#45-b06d5e241c---add-a-searchvalues-implementation-for-values-with-unique-low-nibbles-106900) |

---

## 7. b146d7512c - JIT: Move loop inversion to after loop recognition (#115850)

**Date:** 2025-06-14 17:22:46
**Commit:** [b146d7512c](https://github.com/dotnet/runtime/commit/b146d7512ce67051e127ab48dc2d4f65d30e818f)
**Affected Tests:** 20

| Test Name | Link | Change | .NET 9 | .NET 10 | Other Changepoints |
|-----------|------|--------|--------|-------|--------------------|
| SeekUnroll.Test(boxedIndex: 27) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/SeekUnroll.Test%28boxedIndex%3A%2027%29.html) | +62.97% | 1478559636.535714 | 2409581662.089286 | None |
| System.Tests.Perf_Int32.TryParse(value: "-2147483648") | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Tests.Perf_Int32.TryParse%28value%3A%20%22-2147483648%22%29.html) | +27.01% | 15.086563 | 19.160966 | [3](#3-ddf8075a2f---jit-visit-blocks-in-rpo-during-lsra-107927) |
| System.Tests.Perf_Int64.ParseSpan(value: "-9223372036854775808") | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Tests.Perf_Int64.ParseSpan%28value%3A%20%22-9223372036854775808%22%29.html) | +27.36% | 21.659631 | 27.585544 | [28](#28-39a31f082e---virtual-stub-indirect-call-profiling-116453) |
| System.Tests.Perf_Int32.TryParseSpan(value: "-2147483648") | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Tests.Perf_Int32.TryParseSpan%28value%3A%20%22-2147483648%22%29.html) | +23.63% | 15.487797 | 19.148063 | None |
| System.Tests.Perf_Int64.TryParse(value: "-9223372036854775808") | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Tests.Perf_Int64.TryParse%28value%3A%20%22-9223372036854775808%22%29.html) | +21.60% | 22.534868 | 27.401980 | [28](#28-39a31f082e---virtual-stub-indirect-call-profiling-116453) |
| System.Tests.Perf_Int32.Parse(value: "-2147483648") | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Tests.Perf_Int32.Parse%28value%3A%20%22-2147483648%22%29.html) | +16.71% | 15.694320 | 18.316560 | [3](#3-ddf8075a2f---jit-visit-blocks-in-rpo-during-lsra-107927) |
| PerfLabTests.CastingPerf.ScalarValueTypeObj | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/PerfLabTests.CastingPerf.ScalarValueTypeObj.html) | +11.91% | 219183.366795 | 245286.082449 | None |
| System.Globalization.Tests.StringSearch.IndexOf_Word_NotFound(Options: (en-US, OrdinalIgnoreCase, False)) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Globalization.Tests.StringSearch.IndexOf_Word_NotFound%28Options%3A%20%28en-US%2C%20OrdinalIgnoreCase%2C%20False%29%29.html) | +14.58% | 27.853364 | 31.915445 | None |
| PerfLabTests.CastingPerf.ObjrefValueTypeObj | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/PerfLabTests.CastingPerf.ObjrefValueTypeObj.html) | +10.87% | 217873.199735 | 241557.221241 | None |
| System.Tests.Perf_Int32.ParseSpan(value: "2147483647") | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Tests.Perf_Int32.ParseSpan%28value%3A%20%222147483647%22%29.html) | +7.60% | 15.226411 | 16.384223 | [3](#3-ddf8075a2f---jit-visit-blocks-in-rpo-during-lsra-107927) |
| System.Tests.Perf_SByte.TryParse(value: "-128") | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Tests.Perf_SByte.TryParse%28value%3A%20%22-128%22%29.html) | +6.69% | 12.866810 | 13.727697 | [13](#13-5cb6a06da6---jit-add-simple-late-layout-pass-107483) |
| System.Tests.Perf_Int16.Parse(value: "32767") | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Tests.Perf_Int16.Parse%28value%3A%20%2232767%22%29.html) | +7.26% | 14.013097 | 15.029960 | [13](#13-5cb6a06da6---jit-add-simple-late-layout-pass-107483), [23](#23-54b86f1843---remove-the-rest-of-the-simdashwintrinsic-support-106594) |
| System.Tests.Perf_Int16.TryParse(value: "32767") | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Tests.Perf_Int16.TryParse%28value%3A%20%2232767%22%29.html) | +5.93% | 13.997726 | 14.827708 | None |
| BenchmarksGame.FannkuchRedux_9.RunBench(n: 11, expectedSum: 556355) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/BenchmarksGame.FannkuchRedux_9.RunBench%28n%3A%2011%2C%20expectedSum%3A%20556355%29.html) | +5.32% | N/A | N/A | [1](#1-41be5e229b---jit-graph-based-loop-inversion-116017), [4](#4-1c10ceecbf---jit-add-3-opt-implementation-for-improving-upon-rpo-based-block-layout-103450) |
| System.Tests.Perf_Int64.Parse(value: "-9223372036854775808") | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Tests.Perf_Int64.Parse%28value%3A%20%22-9223372036854775808%22%29.html) | +6.34% | 21.923670 | 23.314672 | [3](#3-ddf8075a2f---jit-visit-blocks-in-rpo-during-lsra-107927) |
| ArrayDeAbstraction.foreach_member_array_via_interface | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/ArrayDeAbstraction.foreach_member_array_via_interface.html) | -62.34% | 744.363833 | 280.330949 | [32](#32-6bc04bfdb1---jit-empty-array-enumerator-opt-109237) |
| ArrayDeAbstraction.foreach_opaque_array_via_interface | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/ArrayDeAbstraction.foreach_opaque_array_via_interface.html) | -63.94% | 749.656252 | 270.355618 | [25](#25-f6c74b8df8---jit-conditional-escape-analysis-and-cloning-111473) |
| ArrayDeAbstraction.foreach_static_array_via_interface | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/ArrayDeAbstraction.foreach_static_array_via_interface.html) | -63.74% | 754.186649 | 273.453218 | [32](#32-6bc04bfdb1---jit-empty-array-enumerator-opt-109237) |
| ArrayDeAbstraction.foreach_static_array_via_interface_property | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/ArrayDeAbstraction.foreach_static_array_via_interface_property.html) | -63.82% | 740.984680 | 268.063193 | [25](#25-f6c74b8df8---jit-conditional-escape-analysis-and-cloning-111473) |
| ArrayDeAbstraction.foreach_member_array_via_interface_property | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/ArrayDeAbstraction.foreach_member_array_via_interface_property.html) | -63.98% | 744.615059 | 268.182378 | [25](#25-f6c74b8df8---jit-conditional-escape-analysis-and-cloning-111473) |

---

## 8. 34545d790e - JIT: don't mark callees noinline for non-fatal observations with pgo (#114821)

**Date:** 2025-04-21 02:03:19
**Commit:** [34545d790e](https://github.com/dotnet/runtime/commit/34545d790e0f92be34b13f0d41b7df93f04bbe02)
**Affected Tests:** 15

| Test Name | Link | Change | .NET 9 | .NET 10 | Other Changepoints |
|-----------|------|--------|--------|-------|--------------------|
| System.Collections.ContainsFalse<String>.Queue(Size: 512) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Collections.ContainsFalse%28String%29.Queue%28Size%3A%20512%29.html) | +69.08% | 240390.293429 | 406458.781155 | [18](#18-023686e6c2---jit-break-up-try-regions-in-compilerfgmovecoldblocks-and-fix-contiguity-later-108914) |
| System.Collections.ContainsTrue<String>.Queue(Size: 512) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Collections.ContainsTrue%28String%29.Queue%28Size%3A%20512%29.html) | +46.73% | 121191.139988 | 177829.110090 | None |
| System.Buffers.Text.Tests.Utf8ParserTests.TryParseUInt64(value: 18446744073709551615) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Buffers.Text.Tests.Utf8ParserTests.TryParseUInt64%28value%3A%2018446744073709551615%29.html) | +37.98% | 14.088025 | 19.439361 | None |
| System.Text.RegularExpressions.Tests.Perf_Regex_Industry_RustLang_Sherlock.Count(Pattern: "zqj", Options: NonBacktracking) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Text.RegularExpressions.Tests.Perf_Regex_Industry_RustLang_Sherlock.Count%28Pattern%3A%20%22zqj%22%2C%20Options%3A%20NonBacktracking%29.html) | +35.63% | 36386.185077 | 49351.061448 | [3](#3-ddf8075a2f---jit-visit-blocks-in-rpo-during-lsra-107927) |
| System.Collections.ContainsFalse<String>.Stack(Size: 512) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Collections.ContainsFalse%28String%29.Stack%28Size%3A%20512%29.html) | +32.81% | 355039.396041 | 471542.660001 | None |
| System.Text.Json.Tests.Perf_Basic.WriteBasicUtf8(Formatted: False, SkipValidation: True, DataSize: 100000) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Text.Json.Tests.Perf_Basic.WriteBasicUtf8%28Formatted%3A%20False%2C%20SkipValidation%3A%20True%2C%20DataSize%3A%20100000%29.html) | +19.89% | 1291544.368175 | 1548408.690352 | None |
| System.IO.Tests.Perf_Path.GetFullPathForLegacyLength | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.IO.Tests.Perf_Path.GetFullPathForLegacyLength.html) | +21.29% | 253.170739 | 307.062091 | None |
| System.IO.Tests.Perf_FileInfo.ctor_str | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.IO.Tests.Perf_FileInfo.ctor_str.html) | +20.52% | 90.586943 | 109.171329 | None |
| System.IO.Tests.Perf_Path.GetFullPathNoRedundantSegments | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.IO.Tests.Perf_Path.GetFullPathNoRedundantSegments.html) | +23.74% | 152.842298 | 189.131345 | None |
| System.Tests.Perf_Double.ToStringWithFormat(value: -1.7976931348623157E+308, format: "F50") | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Tests.Perf_Double.ToStringWithFormat%28value%3A%20-1.7976931348623157E%2B308%2C%20format%3A%20%22F50%22%29.html) | +9.13% | 19921.697771 | 21739.654021 | None |
| System.Text.RegularExpressions.Tests.Perf_Regex_Industry_SliceSlice.Count(Options: IgnoreCase, NonBacktracking) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Text.RegularExpressions.Tests.Perf_Regex_Industry_SliceSlice.Count%28Options%3A%20IgnoreCase%2C%20NonBacktracking%29.html) | +5.45% | 850726730.928571 | 897093928.214286 | None |
| System.Memory.Span<Byte>.LastIndexOfValue(Size: 512) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Memory.Span%28Byte%29.LastIndexOfValue%28Size%3A%20512%29.html) | +9.22% | 7.079979 | 7.732780 | None |
| System.Tests.Perf_Double.ToStringWithFormat(value: 1.7976931348623157E+308, format: "F50") | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Tests.Perf_Double.ToStringWithFormat%28value%3A%201.7976931348623157E%2B308%2C%20format%3A%20%22F50%22%29.html) | +11.40% | 19908.637297 | 22177.305142 | None |
| System.Buffers.Tests.RentReturnArrayPoolTests<Object>.SingleParallel(RentalSize: 4096, ManipulateArray: True, Async: True, UseSharedPool: False) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Buffers.Tests.RentReturnArrayPoolTests%28Object%29.SingleParallel%28RentalSize%3A%204096%2C%20ManipulateArray%3A%20True%2C%20Async%3A%20True%2C%20UseSharedPool%3A%20False%29.html) | +8.19% | 4142.313485 | 4481.385603 | None |
| System.Buffers.Tests.RentReturnArrayPoolTests<Byte>.SingleParallel(RentalSize: 4096, ManipulateArray: False, Async: False, UseSharedPool: False) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Buffers.Tests.RentReturnArrayPoolTests%28Byte%29.SingleParallel%28RentalSize%3A%204096%2C%20ManipulateArray%3A%20False%2C%20Async%3A%20False%2C%20UseSharedPool%3A%20False%29.html) | +10.23% | 538.819632 | 593.936989 | None |

---

## 9. 6f221b41da - Ensure that math calls into the CRT are tracked as needing vzeroupper (#112011)

**Date:** 2025-02-01 19:06:23
**Commit:** [6f221b41da](https://github.com/dotnet/runtime/commit/6f221b41da8b4fbd09dcf7ac4b796ff3c86cbeb9)
**Affected Tests:** 14

| Test Name | Link | Change | .NET 9 | .NET 10 | Other Changepoints |
|-----------|------|--------|--------|-------|--------------------|
| System.MathBenchmarks.Single.AsinPi | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.MathBenchmarks.Single.AsinPi.html) | +18.26% | 25671.908671 | 30359.696271 | None |
| System.MathBenchmarks.Single.AcosPi | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.MathBenchmarks.Single.AcosPi.html) | +14.17% | 33193.478127 | 37898.335769 | None |
| System.MathBenchmarks.Double.AtanPi | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.MathBenchmarks.Double.AtanPi.html) | +15.18% | 32633.023704 | 37585.443249 | None |
| System.MathBenchmarks.Double.AsinPi | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.MathBenchmarks.Double.AsinPi.html) | +11.86% | 31266.991216 | 34974.364881 | None |
| System.Numerics.Tensors.Tests.Perf_FloatingPointTensorPrimitives<Single>.AtanPi(BufferLength: 128) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Numerics.Tensors.Tests.Perf_FloatingPointTensorPrimitives%28Single%29.AtanPi%28BufferLength%3A%20128%29.html) | +11.74% | 793.094958 | 886.192206 | None |
| System.Numerics.Tensors.Tests.Perf_FloatingPointTensorPrimitives<Double>.AtanPi(BufferLength: 3079) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Numerics.Tensors.Tests.Perf_FloatingPointTensorPrimitives%28Double%29.AtanPi%28BufferLength%3A%203079%29.html) | +11.05% | 19608.530057 | 21774.769232 | None |
| System.MathBenchmarks.Double.ExpM1 | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.MathBenchmarks.Double.ExpM1.html) | +7.77% | 23782.095276 | 25629.340933 | None |
| System.MathBenchmarks.Double.AcosPi | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.MathBenchmarks.Double.AcosPi.html) | +10.41% | 32674.333496 | 36075.967701 | None |
| System.MathBenchmarks.Single.AtanPi | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.MathBenchmarks.Single.AtanPi.html) | +12.78% | 34110.287807 | 38470.171798 | None |
| System.Numerics.Tensors.Tests.Perf_FloatingPointTensorPrimitives<Single>.AtanPi(BufferLength: 3079) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Numerics.Tensors.Tests.Perf_FloatingPointTensorPrimitives%28Single%29.AtanPi%28BufferLength%3A%203079%29.html) | +7.06% | N/A | N/A | None |
| System.MathBenchmarks.Single.Sinh | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.MathBenchmarks.Single.Sinh.html) | +5.96% | 65743.177342 | 69660.052650 | None |
| System.Numerics.Tensors.Tests.Perf_FloatingPointTensorPrimitives<Double>.AtanPi(BufferLength: 128) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Numerics.Tensors.Tests.Perf_FloatingPointTensorPrimitives%28Double%29.AtanPi%28BufferLength%3A%20128%29.html) | +16.06% | 819.571205 | 951.186741 | None |
| System.MathBenchmarks.Double.Atan | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.MathBenchmarks.Double.Atan.html) | +5.30% | 27634.602900 | 29099.808415 | None |
| System.MathBenchmarks.Double.Log | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.MathBenchmarks.Double.Log.html) | +5.13% | 23012.792637 | 24194.231902 | None |

---

## 10. 02127c782a - JIT: Don't put cold blocks in RPO during layout (#112448)

**Date:** 2025-02-14 17:16:23
**Commit:** [02127c782a](https://github.com/dotnet/runtime/commit/02127c782adbf0cded3ed0778d4bf694e5e75996)
**Affected Tests:** 13

| Test Name | Link | Change | .NET 9 | .NET 10 | Other Changepoints |
|-----------|------|--------|--------|-------|--------------------|
| System.IO.Tests.BinaryReaderTests.ReadHalf | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.IO.Tests.BinaryReaderTests.ReadHalf.html) | +29.52% | 3.824617 | 4.953669 | None |
| System.Numerics.Tests.Perf_BigInteger.Equals(arguments: 259 bytes, Same) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Numerics.Tests.Perf_BigInteger.Equals%28arguments%3A%20259%20bytes%2C%20Same%29.html) | +26.94% | 6.740102 | 8.556015 | [1](#1-41be5e229b---jit-graph-based-loop-inversion-116017) |
| System.Text.Json.Tests.Perf_Strings.WriteStringsUtf16(Formatted: True, SkipValidation: True, Escaped: AllEscaped) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Text.Json.Tests.Perf_Strings.WriteStringsUtf16%28Formatted%3A%20True%2C%20SkipValidation%3A%20True%2C%20Escaped%3A%20AllEscaped%29.html) | +20.41% | 49095127.767857 | 59116587.782143 | [15](#15-75b550d7d3---implement-writestringvaluesegment-defined-in-issue-67337-101356) |
| System.Text.Tests.Perf_Encoding.GetByteCount(size: 512, encName: "ascii") | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Text.Tests.Perf_Encoding.GetByteCount%28size%3A%20512%2C%20encName%3A%20%22ascii%22%29.html) | +20.85% | 4.289198 | 5.183584 | [21](#21-254b55a49e---enable-loop-cloning-for-spans-113575) |
| System.Text.Json.Tests.Perf_Strings.WriteStringsUtf16(Formatted: False, SkipValidation: False, Escaped: AllEscaped) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Text.Json.Tests.Perf_Strings.WriteStringsUtf16%28Formatted%3A%20False%2C%20SkipValidation%3A%20False%2C%20Escaped%3A%20AllEscaped%29.html) | +19.22% | 49669685.478571 | 59216982.635714 | [15](#15-75b550d7d3---implement-writestringvaluesegment-defined-in-issue-67337-101356) |
| System.Text.Json.Tests.Perf_Strings.WriteStringsUtf16(Formatted: False, SkipValidation: True, Escaped: AllEscaped) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Text.Json.Tests.Perf_Strings.WriteStringsUtf16%28Formatted%3A%20False%2C%20SkipValidation%3A%20True%2C%20Escaped%3A%20AllEscaped%29.html) | +20.95% | 48108475.453571 | 58185699.503571 | [15](#15-75b550d7d3---implement-writestringvaluesegment-defined-in-issue-67337-101356), [30](#30-1434eeef6c---jit-run-new-block-layout-only-in-backend-107634) |
| System.Memory.Span<Int32>.StartsWith(Size: 512) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Memory.Span%28Int32%29.StartsWith%28Size%3A%20512%29.html) | +16.67% | 17.327527 | 20.215985 | None |
| System.Tests.Perf_UInt64.TryParse(value: "0") | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Tests.Perf_UInt64.TryParse%28value%3A%20%220%22%29.html) | +15.60% | 8.538093 | 9.870225 | None |
| System.Globalization.Tests.StringSearch.IsSuffix_SecondHalf(Options: (en-US, Ordinal, False)) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Globalization.Tests.StringSearch.IsSuffix_SecondHalf%28Options%3A%20%28en-US%2C%20Ordinal%2C%20False%29%29.html) | +8.39% | 9.046126 | 9.804864 | None |
| System.Memory.Span<Int32>.EndsWith(Size: 512) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Memory.Span%28Int32%29.EndsWith%28Size%3A%20512%29.html) | +5.60% | 18.997371 | 20.061165 | None |
| System.Memory.Span<Char>.StartsWith(Size: 512) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Memory.Span%28Char%29.StartsWith%28Size%3A%20512%29.html) | +10.85% | 10.166963 | 11.269997 | None |
| System.Text.Tests.Perf_StringBuilder.ToString_MultipleSegments(length: 100) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Text.Tests.Perf_StringBuilder.ToString_MultipleSegments%28length%3A%20100%29.html) | +5.37% | 29.191730 | 30.759765 | None |
| System.Memory.Span<Char>.SequenceEqual(Size: 512) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Memory.Span%28Char%29.SequenceEqual%28Size%3A%20512%29.html) | +5.18% | 16.742760 | 17.609650 | None |

---

## 11. 217525ae6f - Workaround for #106521 (#106578)

**Date:** 2024-08-18 17:03:53
**Commit:** [217525ae6f](https://github.com/dotnet/runtime/commit/217525ae6f6a117a0780620ed4fb1b94e03fd4d6)
**Affected Tests:** 11

| Test Name | Link | Change | .NET 9 | .NET 10 | Other Changepoints |
|-----------|------|--------|--------|-------|--------------------|
| System.Text.RegularExpressions.Tests.Perf_Regex_Common.CtorInvoke(Options: Compiled) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Text.RegularExpressions.Tests.Perf_Regex_Common.CtorInvoke%28Options%3A%20Compiled%29.html) | +9.71% | 193963.481279 | 212806.425792 | None |
| System.Net.Security.Tests.SslStreamTests.ReadWriteAsync | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Net.Security.Tests.SslStreamTests.ReadWriteAsync.html) | +10.94% | 14900.327715 | 16530.299199 | None |
| System.Net.Security.Tests.SslStreamTests.LargeReadWriteAsync | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Net.Security.Tests.SslStreamTests.LargeReadWriteAsync.html) | +7.85% | 17598.423352 | 18980.764755 | None |
| System.Collections.ContainsFalse<Int32>.Stack(Size: 512) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Collections.ContainsFalse%28Int32%29.Stack%28Size%3A%20512%29.html) | +5.78% | 17351.537650 | 18354.256809 | None |
| System.Numerics.Tensors.Tests.Perf_NumberTensorPrimitives<Int32>.Max(BufferLength: 3079) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Numerics.Tensors.Tests.Perf_NumberTensorPrimitives%28Int32%29.Max%28BufferLength%3A%203079%29.html) | +5.29% | 121.763569 | 128.207676 | None |
| System.Net.Sockets.Tests.SocketSendReceivePerfTest.ReceiveFromAsyncThenSendToAsync_Task | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Net.Sockets.Tests.SocketSendReceivePerfTest.ReceiveFromAsyncThenSendToAsync_Task.html) | +5.87% | 137372038.760000 | 145437754.080000 | None |
| System.Numerics.Tests.Perf_BigInteger.Parse(numberString: 1234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012... | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Numerics.Tests.Perf_BigInteger.Parse%28numberString%3A%201234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012.html) | +5.70% | 788354.775063 | 833275.862658 | None |
| System.Buffers.Tests.RentReturnArrayPoolTests<Object>.SingleSerial(RentalSize: 4096, ManipulateArray: False, Async: False, UseSharedPool: False) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Buffers.Tests.RentReturnArrayPoolTests%28Object%29.SingleSerial%28RentalSize%3A%204096%2C%20ManipulateArray%3A%20False%2C%20Async%3A%20False%2C%20UseSharedPool%3A%20False%29.html) | +5.68% | N/A | N/A | [12](#12-34f1db49db---jit-use-root-compiler-instance-for-sufficient-pgo-observation-115119) |
| System.IO.Compression.Brotli.Compress_WithState(level: Optimal, file: "alice29.txt") | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.IO.Compression.Brotli.Compress_WithState%28level%3A%20Optimal%2C%20file%3A%20%22alice29.txt%22%29.html) | +5.83% | 184894088.875000 | 195676447.553571 | [20](#20-b382a451a9---fix-build-with--dclr_cmake_use_system_brotlitrue-110816), [44](#44-1b5c48dc59---upgrade-vendored-brotli-dependency-to-v110-106994) |
| System.Net.Http.Tests.SocketsHttpHandlerPerfTest.Get_EnumerateHeaders_Validated(ssl: True, chunkedResponse: False, responseLength: 1) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Net.Http.Tests.SocketsHttpHandlerPerfTest.Get_EnumerateHeaders_Validated%28ssl%3A%20True%2C%20chunkedResponse%3A%20False%2C%20responseLength%3A%201%29.html) | +5.07% | N/A | N/A | None |
| Benchstone.BenchF.InProd.Test | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/Benchstone.BenchF.InProd.Test.html) | +5.22% | 1241351983.285714 | 1306105080.178572 | [14](#14-21ab780ed4---remove-unsafe-bool-casts-111024) |

---

## 12. 34f1db49db - JIT: use root compiler instance for sufficient PGO observation (#115119)

**Date:** 2025-05-19 14:21:16
**Commit:** [34f1db49db](https://github.com/dotnet/runtime/commit/34f1db49dbf702697483ee2809d493f5ef441768)
**Affected Tests:** 10

| Test Name | Link | Change | .NET 9 | .NET 10 | Other Changepoints |
|-----------|------|--------|--------|-------|--------------------|
| System.Collections.ContainsTrue<String>.ImmutableArray(Size: 512) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Collections.ContainsTrue%28String%29.ImmutableArray%28Size%3A%20512%29.html) | +89.50% | 123532.901615 | 234090.738264 | None |
| System.Globalization.Tests.StringEquality.Compare_DifferentFirstChar(Count: 1024, Options: (en-US, OrdinalIgnoreCase)) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Globalization.Tests.StringEquality.Compare_DifferentFirstChar%28Count%3A%201024%2C%20Options%3A%20%28en-US%2C%20OrdinalIgnoreCase%29%29.html) | +52.67% | 7.423893 | 11.334040 | None |
| System.Globalization.Tests.StringEquality.Compare_DifferentFirstChar(Count: 1024, Options: (en-US, Ordinal)) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Globalization.Tests.StringEquality.Compare_DifferentFirstChar%28Count%3A%201024%2C%20Options%3A%20%28en-US%2C%20Ordinal%29%29.html) | +26.95% | 9.318577 | 11.829608 | None |
| System.Buffers.Tests.NonStandardArrayPoolTests<Byte>.RentNoReturn(RentalSize: 64, UseSharedPool: False) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Buffers.Tests.NonStandardArrayPoolTests%28Byte%29.RentNoReturn%28RentalSize%3A%2064%2C%20UseSharedPool%3A%20False%29.html) | +22.83% | 30.503947 | 37.467160 | None |
| System.Globalization.Tests.StringEquality.Compare_Same_Upper(Count: 1024, Options: (en-US, Ordinal)) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Globalization.Tests.StringEquality.Compare_Same_Upper%28Count%3A%201024%2C%20Options%3A%20%28en-US%2C%20Ordinal%29%29.html) | +14.43% | 12.268733 | 14.039374 | None |
| System.Buffers.Tests.NonStandardArrayPoolTests<Object>.RentNoReturn(RentalSize: 64, UseSharedPool: False) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Buffers.Tests.NonStandardArrayPoolTests%28Object%29.RentNoReturn%28RentalSize%3A%2064%2C%20UseSharedPool%3A%20False%29.html) | +14.48% | 45.557886 | 52.154611 | None |
| System.Buffers.Tests.RentReturnArrayPoolTests<Object>.MultipleSerial(RentalSize: 4096, ManipulateArray: False, Async: False, UseSharedPool: False) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Buffers.Tests.RentReturnArrayPoolTests%28Object%29.MultipleSerial%28RentalSize%3A%204096%2C%20ManipulateArray%3A%20False%2C%20Async%3A%20False%2C%20UseSharedPool%3A%20False%29.html) | +12.99% | 239.337441 | 270.435404 | None |
| System.Buffers.Tests.RentReturnArrayPoolTests<Byte>.MultipleSerial(RentalSize: 4096, ManipulateArray: False, Async: False, UseSharedPool: False) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Buffers.Tests.RentReturnArrayPoolTests%28Byte%29.MultipleSerial%28RentalSize%3A%204096%2C%20ManipulateArray%3A%20False%2C%20Async%3A%20False%2C%20UseSharedPool%3A%20False%29.html) | +15.04% | 231.001064 | 265.737735 | None |
| Microsoft.Extensions.Configuration.ConfigurationBinderBenchmarks.Get(ConfigurationProvidersCount: 8, KeysCountPerProvider: 10) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/Microsoft.Extensions.Configuration.ConfigurationBinderBenchmarks.Get%28ConfigurationProvidersCount%3A%208%2C%20KeysCountPerProvider%3A%2010%29.html) | +7.59% | 245154.895492 | 263769.811138 | None |
| System.Buffers.Tests.RentReturnArrayPoolTests<Object>.SingleSerial(RentalSize: 4096, ManipulateArray: False, Async: False, UseSharedPool: False) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Buffers.Tests.RentReturnArrayPoolTests%28Object%29.SingleSerial%28RentalSize%3A%204096%2C%20ManipulateArray%3A%20False%2C%20Async%3A%20False%2C%20UseSharedPool%3A%20False%29.html) | +5.68% | N/A | N/A | [11](#11-217525ae6f---workaround-for-106521-106578) |

---

## 13. 5cb6a06da6 - JIT: Add simple late layout pass (#107483)

**Date:** 2024-09-10 02:38:23
**Commit:** [5cb6a06da6](https://github.com/dotnet/runtime/commit/5cb6a06da634ee4be4f426711e9c5f66535a78c8)
**Affected Tests:** 6

| Test Name | Link | Change | .NET 9 | .NET 10 | Other Changepoints |
|-----------|------|--------|--------|-------|--------------------|
| System.Tests.Perf_Int32.TryParse(value: "2147483647") | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Tests.Perf_Int32.TryParse%28value%3A%20%222147483647%22%29.html) | +24.09% | 14.077723 | 17.469050 | None |
| Burgers.Test1 | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/Burgers.Test1.html) | +22.54% | 197903751.107143 | 242512062.250000 | [1](#1-41be5e229b---jit-graph-based-loop-inversion-116017) |
| System.Tests.Perf_UInt16.TryParse(value: "65535") | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Tests.Perf_UInt16.TryParse%28value%3A%20%2265535%22%29.html) | +8.24% | 12.362412 | 13.381327 | None |
| System.Tests.Perf_SByte.TryParse(value: "-128") | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Tests.Perf_SByte.TryParse%28value%3A%20%22-128%22%29.html) | +6.69% | 12.866810 | 13.727697 | [7](#7-b146d7512c---jit-move-loop-inversion-to-after-loop-recognition-115850) |
| System.Tests.Perf_Int16.Parse(value: "32767") | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Tests.Perf_Int16.Parse%28value%3A%20%2232767%22%29.html) | +7.26% | 14.013097 | 15.029960 | [7](#7-b146d7512c---jit-move-loop-inversion-to-after-loop-recognition-115850), [23](#23-54b86f1843---remove-the-rest-of-the-simdashwintrinsic-support-106594) |
| System.Tests.Perf_UInt16.TryParse(value: "12345") | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Tests.Perf_UInt16.TryParse%28value%3A%20%2212345%22%29.html) | +8.97% | 11.871620 | 12.936651 | None |

---

## 14. 21ab780ed4 - Remove unsafe `bool` casts (#111024)

**Date:** 2025-01-28 23:31:49
**Commit:** [21ab780ed4](https://github.com/dotnet/runtime/commit/21ab780ed4aad5d3f37b92c9015f0a8051dd4a69)
**Affected Tests:** 6

| Test Name | Link | Change | .NET 9 | .NET 10 | Other Changepoints |
|-----------|------|--------|--------|-------|--------------------|
| System.Buffers.Text.Tests.Utf8FormatterTests.FormatterUInt64(value: 12345) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Buffers.Text.Tests.Utf8FormatterTests.FormatterUInt64%28value%3A%2012345%29.html) | +25.05% | 5.807021 | 7.261802 | None |
| System.Tests.Perf_Half.HalfToSingle(value: 12344) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Tests.Perf_Half.HalfToSingle%28value%3A%2012344%29.html) | +16.12% | 1.973655 | 2.291733 | None |
| System.Tests.Perf_Half.HalfToSingle(value: 6.1E-05) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Tests.Perf_Half.HalfToSingle%28value%3A%206.1E-05%29.html) | +15.67% | 1.980188 | 2.290533 | None |
| System.Tests.Perf_Half.HalfToSingle(value: NaN) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Tests.Perf_Half.HalfToSingle%28value%3A%20NaN%29.html) | +15.31% | 1.974071 | 2.276263 | None |
| System.Buffers.Text.Tests.Utf8FormatterTests.FormatterUInt64(value: 18446744073709551615) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Buffers.Text.Tests.Utf8FormatterTests.FormatterUInt64%28value%3A%2018446744073709551615%29.html) | +9.06% | 15.300937 | 16.686591 | None |
| Benchstone.BenchF.InProd.Test | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/Benchstone.BenchF.InProd.Test.html) | +5.22% | 1241351983.285714 | 1306105080.178572 | [11](#11-217525ae6f---workaround-for-106521-106578) |

---

## 15. 75b550d7d3 - Implement WriteStringValueSegment defined in Issue 67337 (#101356)

**Date:** 2024-12-26 21:20:31
**Commit:** [75b550d7d3](https://github.com/dotnet/runtime/commit/75b550d7d3b5b27a74b5bff9c1cb09c42f4fb3ab)
**Affected Tests:** 5

| Test Name | Link | Change | .NET 9 | .NET 10 | Other Changepoints |
|-----------|------|--------|--------|-------|--------------------|
| System.Text.Json.Tests.Perf_Strings.WriteStringsUtf16(Formatted: True, SkipValidation: True, Escaped: AllEscaped) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Text.Json.Tests.Perf_Strings.WriteStringsUtf16%28Formatted%3A%20True%2C%20SkipValidation%3A%20True%2C%20Escaped%3A%20AllEscaped%29.html) | +20.41% | 49095127.767857 | 59116587.782143 | [10](#10-02127c782a---jit-dont-put-cold-blocks-in-rpo-during-layout-112448) |
| System.Text.Json.Tests.Perf_Strings.WriteStringsUtf16(Formatted: True, SkipValidation: False, Escaped: AllEscaped) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Text.Json.Tests.Perf_Strings.WriteStringsUtf16%28Formatted%3A%20True%2C%20SkipValidation%3A%20False%2C%20Escaped%3A%20AllEscaped%29.html) | +19.48% | 49067059.228571 | 58623669.014286 | None |
| System.Text.Json.Tests.Perf_Strings.WriteStringsUtf16(Formatted: False, SkipValidation: False, Escaped: AllEscaped) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Text.Json.Tests.Perf_Strings.WriteStringsUtf16%28Formatted%3A%20False%2C%20SkipValidation%3A%20False%2C%20Escaped%3A%20AllEscaped%29.html) | +19.22% | 49669685.478571 | 59216982.635714 | [10](#10-02127c782a---jit-dont-put-cold-blocks-in-rpo-during-layout-112448) |
| System.Text.Json.Tests.Perf_Strings.WriteStringsUtf16(Formatted: False, SkipValidation: True, Escaped: AllEscaped) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Text.Json.Tests.Perf_Strings.WriteStringsUtf16%28Formatted%3A%20False%2C%20SkipValidation%3A%20True%2C%20Escaped%3A%20AllEscaped%29.html) | +20.95% | 48108475.453571 | 58185699.503571 | [10](#10-02127c782a---jit-dont-put-cold-blocks-in-rpo-during-layout-112448), [30](#30-1434eeef6c---jit-run-new-block-layout-only-in-backend-107634) |
| System.Text.Json.Tests.Perf_Ctor.Ctor(Formatted: True, SkipValidation: True) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Text.Json.Tests.Perf_Ctor.Ctor%28Formatted%3A%20True%2C%20SkipValidation%3A%20True%29.html) | +6.68% | 13.892886 | 14.821025 | None |

---

## 16. f93aa8a3d7 - Preserve trailing extra data when updating ZIP files (#113306)

**Date:** 2025-04-15 01:11:26
**Commit:** [f93aa8a3d7](https://github.com/dotnet/runtime/commit/f93aa8a3d74304ea3bc58a1127afdbed1e2398dc)
**Affected Tests:** 5

| Test Name | Link | Change | .NET 9 | .NET 10 | Other Changepoints |
|-----------|------|--------|--------|-------|--------------------|
| System.IO.Compression.Brotli.Decompress_WithState(level: Fastest, file: "TestDocument.pdf") | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.IO.Compression.Brotli.Decompress_WithState%28level%3A%20Fastest%2C%20file%3A%20%22TestDocument.pdf%22%29.html) | +7.06% | 235801.397028 | 252445.077506 | None |
| System.IO.Compression.Brotli.Decompress_WithoutState(level: Fastest, file: "TestDocument.pdf") | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.IO.Compression.Brotli.Decompress_WithoutState%28level%3A%20Fastest%2C%20file%3A%20%22TestDocument.pdf%22%29.html) | +7.29% | 232884.411854 | 249859.900833 | None |
| System.IO.Compression.Brotli.Decompress_WithoutState(level: Optimal, file: "TestDocument.pdf") | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.IO.Compression.Brotli.Decompress_WithoutState%28level%3A%20Optimal%2C%20file%3A%20%22TestDocument.pdf%22%29.html) | +6.52% | 263165.736337 | 280334.586275 | None |
| System.IO.Compression.Brotli.Decompress(level: Optimal, file: "TestDocument.pdf") | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.IO.Compression.Brotli.Decompress%28level%3A%20Optimal%2C%20file%3A%20%22TestDocument.pdf%22%29.html) | +5.80% | 269534.961189 | 285166.063833 | None |
| System.IO.Compression.Brotli.Decompress_WithState(level: Optimal, file: "TestDocument.pdf") | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.IO.Compression.Brotli.Decompress_WithState%28level%3A%20Optimal%2C%20file%3A%20%22TestDocument.pdf%22%29.html) | +5.73% | 260165.288325 | 275081.361229 | None |

---

## 17. e32148a8bd - JIT: Add loop-aware RPO, and use as LSRA's block sequence (#108086)

**Date:** 2024-10-10 04:40:22
**Commit:** [e32148a8bd](https://github.com/dotnet/runtime/commit/e32148a8bd)
**Affected Tests:** 4

| Test Name | Link | Change | .NET 9 | .NET 10 | Other Changepoints |
|-----------|------|--------|--------|-------|--------------------|
| System.Text.Encodings.Web.Tests.Perf_Encoders.EncodeUtf16(arguments: Url,&lorem ipsum=dolor sit amet,16) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Text.Encodings.Web.Tests.Perf_Encoders.EncodeUtf16%28arguments%3A%20Url%2C%26lorem%20ipsum%3Ddolor%20sit%20amet%2C16%29.html) | +117.06% | 51.955954 | 112.773346 | [1](#1-41be5e229b---jit-graph-based-loop-inversion-116017), [2](#2-ffcd1c5442---trust-single-edge-synthetic-profile-116054), [3](#3-ddf8075a2f---jit-visit-blocks-in-rpo-during-lsra-107927) |
| System.Text.Encodings.Web.Tests.Perf_Encoders.EncodeUtf16(arguments: JavaScript,&Hello+<World>!,16) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Text.Encodings.Web.Tests.Perf_Encoders.EncodeUtf16%28arguments%3A%20JavaScript%2C%26Hello%2B%28World%29%21%2C16%29.html) | +83.91% | 45.219853 | 83.163582 | [1](#1-41be5e229b---jit-graph-based-loop-inversion-116017), [2](#2-ffcd1c5442---trust-single-edge-synthetic-profile-116054), [3](#3-ddf8075a2f---jit-visit-blocks-in-rpo-during-lsra-107927) |
| System.Text.Encodings.Web.Tests.Perf_Encoders.EncodeUtf16(arguments: UnsafeRelaxed,hello "there",16) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Text.Encodings.Web.Tests.Perf_Encoders.EncodeUtf16%28arguments%3A%20UnsafeRelaxed%2Chello%20%22there%22%2C16%29.html) | +55.06% | 26.004876 | 40.322620 | [2](#2-ffcd1c5442---trust-single-edge-synthetic-profile-116054) |
| System.Text.Encodings.Web.Tests.Perf_Encoders.EncodeUtf16(arguments: JavaScript,&Hello+<World>!,512) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Text.Encodings.Web.Tests.Perf_Encoders.EncodeUtf16%28arguments%3A%20JavaScript%2C%26Hello%2B%28World%29%21%2C512%29.html) | +35.28% | 80.483604 | 108.878899 | None |

---

## 18. 023686e6c2 - JIT: Break up try regions in `Compiler::fgMoveColdBlocks`, and fix contiguity later (#108914)

**Date:** 2024-10-18 18:59:01
**Commit:** [023686e6c2](https://github.com/dotnet/runtime/commit/023686e6c2b0e6d8161680fff14b0703cd041ca5)
**Affected Tests:** 4

| Test Name | Link | Change | .NET 9 | .NET 10 | Other Changepoints |
|-----------|------|--------|--------|-------|--------------------|
| System.Collections.ContainsFalse<String>.List(Size: 512) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Collections.ContainsFalse%28String%29.List%28Size%3A%20512%29.html) | +106.91% | 243954.531241 | 504777.072629 | [2](#2-ffcd1c5442---trust-single-edge-synthetic-profile-116054) |
| System.Collections.ContainsFalse<String>.Queue(Size: 512) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Collections.ContainsFalse%28String%29.Queue%28Size%3A%20512%29.html) | +69.08% | 240390.293429 | 406458.781155 | [8](#8-34545d790e---jit-dont-mark-callees-noinline-for-non-fatal-observations-with-pgo-114821) |
| System.Collections.ContainsFalse<String>.ICollection(Size: 512) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Collections.ContainsFalse%28String%29.ICollection%28Size%3A%20512%29.html) | +45.45% | 241595.473690 | 351411.314261 | [2](#2-ffcd1c5442---trust-single-edge-synthetic-profile-116054) |
| System.Globalization.Tests.StringSearch.LastIndexOf_Word_NotFound(Options: (en-US, OrdinalIgnoreCase, False)) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Globalization.Tests.StringSearch.LastIndexOf_Word_NotFound%28Options%3A%20%28en-US%2C%20OrdinalIgnoreCase%2C%20False%29%29.html) | +8.07% | 715.587533 | 773.341058 | None |

---

## 19. cf7a7444c2 - Replace use of target dependent `TestZ` intrinsic (#104488)

**Date:** 2024-11-27 01:41:48
**Commit:** [cf7a7444c2](https://github.com/dotnet/runtime/commit/cf7a7444c255e0400f1ab078f85d8e3ad746bfb1)
**Affected Tests:** 4

| Test Name | Link | Change | .NET 9 | .NET 10 | Other Changepoints |
|-----------|------|--------|--------|-------|--------------------|
| System.Text.Perf_Utf8Encoding.GetByteCount(Input: EnglishAllAscii) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Text.Perf_Utf8Encoding.GetByteCount%28Input%3A%20EnglishAllAscii%29.html) | +28.04% | 5158.704253 | 6605.182492 | None |
| System.Text.Tests.Perf_Encoding.GetByteCount(size: 512, encName: "utf-8") | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Text.Tests.Perf_Encoding.GetByteCount%28size%3A%20512%2C%20encName%3A%20%22utf-8%22%29.html) | +21.51% | 18.858929 | 22.915256 | None |
| System.IO.Tests.BinaryWriterExtendedTests.WriteAsciiString(StringLengthInChars: 2000000) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.IO.Tests.BinaryWriterExtendedTests.WriteAsciiString%28StringLengthInChars%3A%202000000%29.html) | +11.10% | 160515.387446 | 178326.508561 | None |
| System.Text.Tests.Perf_Encoding.GetBytes(size: 512, encName: "utf-8") | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Text.Tests.Perf_Encoding.GetBytes%28size%3A%20512%2C%20encName%3A%20%22utf-8%22%29.html) | +7.84% | 67.904301 | 73.227414 | None |

---

## 20. b382a451a9 - Fix build with -DCLR_CMAKE_USE_SYSTEM_BROTLI=true (#110816)

**Date:** 2024-12-20 18:11:21
**Commit:** [b382a451a9](https://github.com/dotnet/runtime/commit/b382a451a967eb16968b65238547e6ee795d9d91)
**Affected Tests:** 4

| Test Name | Link | Change | .NET 9 | .NET 10 | Other Changepoints |
|-----------|------|--------|--------|-------|--------------------|
| System.IO.Compression.Brotli.Compress_WithoutState(level: Optimal, file: "alice29.txt") | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.IO.Compression.Brotli.Compress_WithoutState%28level%3A%20Optimal%2C%20file%3A%20%22alice29.txt%22%29.html) | +6.32% | 183399607.964286 | 194983134.071429 | None |
| System.IO.Compression.Brotli.Compress_WithState(level: Optimal, file: "alice29.txt") | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.IO.Compression.Brotli.Compress_WithState%28level%3A%20Optimal%2C%20file%3A%20%22alice29.txt%22%29.html) | +5.83% | 184894088.875000 | 195676447.553571 | [11](#11-217525ae6f---workaround-for-106521-106578), [44](#44-1b5c48dc59---upgrade-vendored-brotli-dependency-to-v110-106994) |
| System.IO.Compression.Brotli.Compress_WithState(level: Optimal, file: "sum") | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.IO.Compression.Brotli.Compress_WithState%28level%3A%20Optimal%2C%20file%3A%20%22sum%22%29.html) | +5.04% | 43735999.646429 | 45942201.646429 | None |
| System.IO.Compression.Brotli.Compress_WithoutState(level: Optimal, file: "sum") | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.IO.Compression.Brotli.Compress_WithoutState%28level%3A%20Optimal%2C%20file%3A%20%22sum%22%29.html) | +5.34% | 43407512.672000 | 45724258.840000 | None |

---

## 21. 254b55a49e - Enable Loop Cloning for Spans (#113575)

**Date:** 2025-03-20 01:07:06
**Commit:** [254b55a49e](https://github.com/dotnet/runtime/commit/254b55a49e)
**Affected Tests:** 4

| Test Name | Link | Change | .NET 9 | .NET 10 | Other Changepoints |
|-----------|------|--------|--------|-------|--------------------|
| Span.IndexerBench.CoveredIndex1(length: 1024) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/Span.IndexerBench.CoveredIndex1%28length%3A%201024%29.html) | +65.35% | 664.137253 | 1098.183449 | None |
| System.Text.RegularExpressions.Tests.Perf_Regex_Industry_BoostDocs_Simple.IsMatch(Id: 8, Options: Compiled) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Text.RegularExpressions.Tests.Perf_Regex_Industry_BoostDocs_Simple.IsMatch%28Id%3A%208%2C%20Options%3A%20Compiled%29.html) | -7.29% | 35.337766 | 32.760171 | None |
| System.Text.Tests.Perf_Encoding.GetByteCount(size: 512, encName: "ascii") | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Text.Tests.Perf_Encoding.GetByteCount%28size%3A%20512%2C%20encName%3A%20%22ascii%22%29.html) | +20.85% | 4.289198 | 5.183584 | [10](#10-02127c782a---jit-dont-put-cold-blocks-in-rpo-during-layout-112448) |
| System.Text.Tests.Perf_Encoding.GetByteCount(size: 16, encName: "ascii") | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Text.Tests.Perf_Encoding.GetByteCount%28size%3A%2016%2C%20encName%3A%20%22ascii%22%29.html) | +22.05% | 4.256564 | 5.195136 | None |

---

## 22. 30082a461a - JIT: save pgo data in inline context, use it for call optimization (#116241)

**Date:** 2025-06-03 20:09:30
**Commit:** [30082a461a](https://github.com/dotnet/runtime/commit/30082a461a68e3305b507910aba7457bdc98115c)
**Affected Tests:** 4

| Test Name | Link | Change | .NET 9 | .NET 10 | Other Changepoints |
|-----------|------|--------|--------|-------|--------------------|
| BenchmarksGame.ReverseComplement_1.RunBench | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/BenchmarksGame.ReverseComplement_1.RunBench.html) | +35.84% | 421661.013484 | 572779.655435 | None |
| XmlDocumentTests.XmlNodeTests.Perf_XmlNode.GetAttributes | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/XmlDocumentTests.XmlNodeTests.Perf_XmlNode.GetAttributes.html) | +16.19% | 3.715258 | 4.316894 | None |
| System.Collections.Tests.Perf_BitArray.BitArrayNot(Size: 4) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Collections.Tests.Perf_BitArray.BitArrayNot%28Size%3A%204%29.html) | +10.32% | 4.421564 | 4.877761 | [1](#1-41be5e229b---jit-graph-based-loop-inversion-116017), [66](#66-38c8e8f4cc---add-collectionsmarshalasbytesbitarray-116308) |
| System.Threading.Tests.Perf_Thread.CurrentThread | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Threading.Tests.Perf_Thread.CurrentThread.html) | +8.06% | 4.784655 | 5.170356 | None |

---

## 23. 54b86f1843 - Remove the rest of the SimdAsHWIntrinsic support (#106594)

**Date:** 2024-10-31 19:46:24
**Commit:** [54b86f1843](https://github.com/dotnet/runtime/commit/54b86f18439397f51fbf4b14f6127a337446f3cf)
**Affected Tests:** 3

| Test Name | Link | Change | .NET 9 | .NET 10 | Other Changepoints |
|-----------|------|--------|--------|-------|--------------------|
| System.Numerics.Tests.Perf_VectorOf<Single>.SquareRootBenchmark | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Numerics.Tests.Perf_VectorOf%28Single%29.SquareRootBenchmark.html) | +368.33% | 1.412193 | 6.613705 | None |
| System.Numerics.Tests.Perf_VectorOf<Double>.SquareRootBenchmark | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Numerics.Tests.Perf_VectorOf%28Double%29.SquareRootBenchmark.html) | +125.49% | 2.499447 | 5.635950 | None |
| System.Tests.Perf_Int16.Parse(value: "32767") | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Tests.Perf_Int16.Parse%28value%3A%20%2232767%22%29.html) | +7.26% | 14.013097 | 15.029960 | [7](#7-b146d7512c---jit-move-loop-inversion-to-after-loop-recognition-115850), [13](#13-5cb6a06da6---jit-add-simple-late-layout-pass-107483) |

---

## 24. ccc9c52e61 - JIT: Move profile consistency checks to after loop opts (#111285)

**Date:** 2025-01-21 20:27:00
**Commit:** [ccc9c52e61](https://github.com/dotnet/runtime/commit/ccc9c52e6137464f5b369c5d4f9d17f24287342b)
**Affected Tests:** 3

| Test Name | Link | Change | .NET 9 | .NET 10 | Other Changepoints |
|-----------|------|--------|--------|-------|--------------------|
| System.Text.Perf_Ascii.ToLowerInPlace_Chars(Size: 6) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Text.Perf_Ascii.ToLowerInPlace_Chars%28Size%3A%206%29.html) | +20.00% | 4.268645 | 5.122570 | None |
| System.Text.Perf_Ascii.ToUpperInPlace_Chars(Size: 6) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Text.Perf_Ascii.ToUpperInPlace_Chars%28Size%3A%206%29.html) | +22.12% | 4.227484 | 5.162656 | None |
| System.Memory.ReadOnlySpan.IndexOfString(input: "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAXAAAAAAAAAAAAAAAAAAAAAAAAAAAAA", value: "x", comparisonType: InvariantCultureIgno... | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Memory.ReadOnlySpan.IndexOfString%28input%3A%20%22AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAXAAAAAAAAAAAAAAAAAAAAAAAAAAAAA%22%2C%20value%3A%20%22x%22%2C%20comparisonType%3A%20InvariantCultureIgno.html) | +18.14% | 205.139866 | 242.351302 | None |

---

## 25. f6c74b8df8 - Jit: Conditional Escape Analysis and Cloning (#111473)

**Date:** 2025-02-04 15:26:07
**Commit:** [f6c74b8df8](https://github.com/dotnet/runtime/commit/f6c74b8df8)
**Affected Tests:** 3

| Test Name | Link | Change | .NET 9 | .NET 10 | Other Changepoints |
|-----------|------|--------|--------|-------|--------------------|
| ArrayDeAbstraction.foreach_opaque_array_via_interface | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/ArrayDeAbstraction.foreach_opaque_array_via_interface.html) | -63.94% | 749.656252 | 270.355618 | [7](#7-b146d7512c---jit-move-loop-inversion-to-after-loop-recognition-115850) |
| ArrayDeAbstraction.foreach_static_array_via_interface_property | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/ArrayDeAbstraction.foreach_static_array_via_interface_property.html) | -63.82% | 740.984680 | 268.063193 | [7](#7-b146d7512c---jit-move-loop-inversion-to-after-loop-recognition-115850) |
| ArrayDeAbstraction.foreach_member_array_via_interface_property | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/ArrayDeAbstraction.foreach_member_array_via_interface_property.html) | -63.98% | 744.615059 | 268.182378 | [7](#7-b146d7512c---jit-move-loop-inversion-to-after-loop-recognition-115850) |

---

## 26. 16782a4481 - JIT: Recompute test block weights after loop inversion (#112197)

**Date:** 2025-02-06 16:29:54
**Commit:** [16782a4481](https://github.com/dotnet/runtime/commit/16782a4481f389e68314b16ea628c5af80f22783)
**Affected Tests:** 3

| Test Name | Link | Change | .NET 9 | .NET 10 | Other Changepoints |
|-----------|------|--------|--------|-------|--------------------|
| System.Tests.Perf_Int128.ParseSpan(value: "170141183460469231731687303715884105727") | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Tests.Perf_Int128.ParseSpan%28value%3A%20%22170141183460469231731687303715884105727%22%29.html) | +27.09% | 75.429986 | 95.864294 | [3](#3-ddf8075a2f---jit-visit-blocks-in-rpo-during-lsra-107927), [6](#6-ea43e17c---jit-run-profile-repair-after-frontend-phases-111915) |
| System.Tests.Perf_Int128.TryParseSpan(value: "170141183460469231731687303715884105727") | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Tests.Perf_Int128.TryParseSpan%28value%3A%20%22170141183460469231731687303715884105727%22%29.html) | +27.53% | 75.656432 | 96.487566 | [6](#6-ea43e17c---jit-run-profile-repair-after-frontend-phases-111915) |
| System.Tests.Perf_Int128.TryParse(value: "170141183460469231731687303715884105727") | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Tests.Perf_Int128.TryParse%28value%3A%20%22170141183460469231731687303715884105727%22%29.html) | +28.53% | 75.891943 | 97.547552 | [6](#6-ea43e17c---jit-run-profile-repair-after-frontend-phases-111915) |

---

## 27. 76750df493 - Remove helper method frames from Monitors (#113242)

**Date:** 2025-03-24 21:36:23
**Commit:** [76750df493](https://github.com/dotnet/runtime/commit/76750df493f2692c13459fc77c065594ea1f628e)
**Affected Tests:** 3

| Test Name | Link | Change | .NET 9 | .NET 10 | Other Changepoints |
|-----------|------|--------|--------|-------|--------------------|
| System.Collections.TryGetValueTrue<BigStruct, BigStruct>.ImmutableSortedDictionary(Size: 512) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Collections.TryGetValueTrue%28BigStruct%2C%20BigStruct%29.ImmutableSortedDictionary%28Size%3A%20512%29.html) | +49.36% | 13185.578268 | 19693.577856 | None |
| System.Collections.Concurrent.Count<String>.Bag(Size: 512) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Collections.Concurrent.Count%28String%29.Bag%28Size%3A%20512%29.html) | +15.77% | 34.736922 | 40.213446 | None |
| System.Collections.Concurrent.Count<Int32>.Bag(Size: 512) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Collections.Concurrent.Count%28Int32%29.Bag%28Size%3A%20512%29.html) | +10.52% | 36.127639 | 39.929652 | None |

---

## 28. 39a31f082e - Virtual stub indirect call profiling (#116453)

**Date:** 2025-06-17 00:35:31
**Commit:** [39a31f082e](https://github.com/dotnet/runtime/commit/39a31f082e77fb8893016c30c0858f0e5f8c89ea)
**Affected Tests:** 3

| Test Name | Link | Change | .NET 9 | .NET 10 | Other Changepoints |
|-----------|------|--------|--------|-------|--------------------|
| System.Tests.Perf_Int64.ParseSpan(value: "-9223372036854775808") | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Tests.Perf_Int64.ParseSpan%28value%3A%20%22-9223372036854775808%22%29.html) | +27.36% | 21.659631 | 27.585544 | [7](#7-b146d7512c---jit-move-loop-inversion-to-after-loop-recognition-115850) |
| System.Tests.Perf_Int64.TryParse(value: "-9223372036854775808") | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Tests.Perf_Int64.TryParse%28value%3A%20%22-9223372036854775808%22%29.html) | +21.60% | 22.534868 | 27.401980 | [7](#7-b146d7512c---jit-move-loop-inversion-to-after-loop-recognition-115850) |
| System.Runtime.Intrinsics.Tests.Perf_Vector128Of<Int16>.BitwiseAndBenchmark | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Runtime.Intrinsics.Tests.Perf_Vector128Of%28Int16%29.BitwiseAndBenchmark.html) | +15.08% | 1.412920 | 1.625973 | None |

---

## 29. 915c681a7c - Update dependencies from https://dev.azure.com/dnceng/internal/_git/dotnet-optimization build 20240912.2 (#107863)

**Date:** 2024-09-22 17:06:35
**Commit:** [915c681a7c](https://github.com/dotnet/runtime/commit/915c681a7c826d4fdd392511e02e7d9ac4218efe)
**Affected Tests:** 2

| Test Name | Link | Change | .NET 9 | .NET 10 | Other Changepoints |
|-----------|------|--------|--------|-------|--------------------|
| System.Collections.ContainsTrue<Int32>.SortedSet(Size: 512) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Collections.ContainsTrue%28Int32%29.SortedSet%28Size%3A%20512%29.html) | +23.03% | 14777.816779 | 18181.506791 | None |
| System.Collections.ContainsKeyFalse<Int32, Int32>.ImmutableSortedDictionary(Size: 512) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Collections.ContainsKeyFalse%28Int32%2C%20Int32%29.ImmutableSortedDictionary%28Size%3A%20512%29.html) | +7.75% | N/A | N/A | None |

---

## 30. 1434eeef6c - JIT: Run new block layout only in backend (#107634)

**Date:** 2024-10-21 04:22:46
**Commit:** [1434eeef6c](https://github.com/dotnet/runtime/commit/1434eeef6c9548c8be39cb0bb3aed11808146195)
**Affected Tests:** 2

| Test Name | Link | Change | .NET 9 | .NET 10 | Other Changepoints |
|-----------|------|--------|--------|-------|--------------------|
| System.Collections.Tests.Perf_BitArray.BitArrayNot(Size: 512) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Collections.Tests.Perf_BitArray.BitArrayNot%28Size%3A%20512%29.html) | +20.62% | 10.607692 | 12.794792 | None |
| System.Text.Json.Tests.Perf_Strings.WriteStringsUtf16(Formatted: False, SkipValidation: True, Escaped: AllEscaped) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Text.Json.Tests.Perf_Strings.WriteStringsUtf16%28Formatted%3A%20False%2C%20SkipValidation%3A%20True%2C%20Escaped%3A%20AllEscaped%29.html) | +20.95% | 48108475.453571 | 58185699.503571 | [10](#10-02127c782a---jit-dont-put-cold-blocks-in-rpo-during-layout-112448), [15](#15-75b550d7d3---implement-writestringvaluesegment-defined-in-issue-67337-101356) |

---

## 31. 489a1512f5 - Remove ldsfld quirk (#108606)

**Date:** 2024-10-22 01:12:46
**Commit:** [489a1512f5](https://github.com/dotnet/runtime/commit/489a1512f55961e91e46054f06eaecafb94ce5ee)
**Affected Tests:** 2

| Test Name | Link | Change | .NET 9 | .NET 10 | Other Changepoints |
|-----------|------|--------|--------|-------|--------------------|
| System.Collections.TryGetValueTrue<Int32, Int32>.ImmutableSortedDictionary(Size: 512) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Collections.TryGetValueTrue%28Int32%2C%20Int32%29.ImmutableSortedDictionary%28Size%3A%20512%29.html) | +42.73% | 13411.946128 | 19143.403434 | [1](#1-41be5e229b---jit-graph-based-loop-inversion-116017) |
| System.Collections.AddGivenSize<String>.List(Size: 512) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Collections.AddGivenSize%28String%29.List%28Size%3A%20512%29.html) | +15.76% | 1340.137505 | 1551.305664 | [48](#48-789bc64ad4---unblock-inlining-of-generics-with-static-fields-109256) |

---

## 32. 6bc04bfdb1 - JIT: empty array enumerator opt (#109237)

**Date:** 2024-10-28 00:13:44
**Commit:** [6bc04bfdb1](https://github.com/dotnet/runtime/commit/6bc04bfdb1)
**Affected Tests:** 2

| Test Name | Link | Change | .NET 9 | .NET 10 | Other Changepoints |
|-----------|------|--------|--------|-------|--------------------|
| ArrayDeAbstraction.foreach_member_array_via_interface | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/ArrayDeAbstraction.foreach_member_array_via_interface.html) | -62.34% | 744.363833 | 280.330949 | [7](#7-b146d7512c---jit-move-loop-inversion-to-after-loop-recognition-115850) |
| ArrayDeAbstraction.foreach_static_array_via_interface | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/ArrayDeAbstraction.foreach_static_array_via_interface.html) | -63.74% | 754.186649 | 273.453218 | [7](#7-b146d7512c---jit-move-loop-inversion-to-after-loop-recognition-115850) |

---

## 33. 1c4c0093a6 - Randomized allocation sampling (#104955)

**Date:** 2024-11-14 00:52:44
**Commit:** [1c4c0093a6](https://github.com/dotnet/runtime/commit/1c4c0093a6f2008bafc034e35e08d019bef44cef)
**Affected Tests:** 2

| Test Name | Link | Change | .NET 9 | .NET 10 | Other Changepoints |
|-----------|------|--------|--------|-------|--------------------|
| System.Tests.Perf_GC<Byte>.AllocateArray(length: 1000, pinned: False) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Tests.Perf_GC%28Byte%29.AllocateArray%28length%3A%201000%2C%20pinned%3A%20False%29.html) | +10.09% | 61.174714 | 67.349717 | None |
| System.Tests.Perf_GC<Char>.AllocateArray(length: 1000, pinned: False) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Tests.Perf_GC%28Char%29.AllocateArray%28length%3A%201000%2C%20pinned%3A%20False%29.html) | +6.63% | 92.239588 | 98.354318 | None |

---

## 34. c7f41499a5 - Lazily-initialize RegexCompiler's cached reflection members (#109283)

**Date:** 2024-12-06 18:16:01
**Commit:** [c7f41499a5](https://github.com/dotnet/runtime/commit/c7f41499a5b96e05f9fc172e53f2fea1fef2b428)
**Affected Tests:** 2

| Test Name | Link | Change | .NET 9 | .NET 10 | Other Changepoints |
|-----------|------|--------|--------|-------|--------------------|
| System.Text.RegularExpressions.Tests.Perf_Regex_Common.Ctor(Options: IgnoreCase, Compiled) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Text.RegularExpressions.Tests.Perf_Regex_Common.Ctor%28Options%3A%20IgnoreCase%2C%20Compiled%29.html) | +16.81% | 35042.508079 | 40931.970510 | None |
| System.Text.RegularExpressions.Tests.Perf_Regex_Common.CtorInvoke(Options: IgnoreCase, Compiled) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Text.RegularExpressions.Tests.Perf_Regex_Common.CtorInvoke%28Options%3A%20IgnoreCase%2C%20Compiled%29.html) | +11.18% | 228897.958118 | 254478.454976 | None |

---

## 35. cdfafde684 - [main] Update dependencies from dnceng/internal/dotnet-optimization (#110308)

**Date:** 2024-12-18 13:24:44
**Commit:** [cdfafde684](https://github.com/dotnet/runtime/commit/cdfafde684f4cf62db38dd0168362f43a15c89c1)
**Affected Tests:** 2

| Test Name | Link | Change | .NET 9 | .NET 10 | Other Changepoints |
|-----------|------|--------|--------|-------|--------------------|
| System.Tests.Perf_Char.Char_ToLowerInvariant(input: "Hello World!") | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Tests.Perf_Char.Char_ToLowerInvariant%28input%3A%20%22Hello%20World%21%22%29.html) | +41.56% | 7.697366 | 10.896437 | None |
| System.Tests.Perf_Byte.TryParse(value: "255") | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Tests.Perf_Byte.TryParse%28value%3A%20%22255%22%29.html) | +6.84% | 11.863491 | 12.674941 | None |

---

## 36. aecae2c385 - JIT: Enable profile consistency checking up to morph (#111047)

**Date:** 2025-01-07 17:00:14
**Commit:** [aecae2c385](https://github.com/dotnet/runtime/commit/aecae2c3853ea793ede98906320312ca6c199ec1)
**Affected Tests:** 2

| Test Name | Link | Change | .NET 9 | .NET 10 | Other Changepoints |
|-----------|------|--------|--------|-------|--------------------|
| Benchstone.BenchI.XposMatrix.Test | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/Benchstone.BenchI.XposMatrix.Test.html) | +61.45% | 12231.521028 | 19748.149585 | [1](#1-41be5e229b---jit-graph-based-loop-inversion-116017), [4](#4-1c10ceecbf---jit-add-3-opt-implementation-for-improving-upon-rpo-based-block-layout-103450), [6](#6-ea43e17c---jit-run-profile-repair-after-frontend-phases-111915) |
| Microsoft.Extensions.Primitives.Performance.StringValuesBenchmark.Indexer_FirstElement_Array | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/Microsoft.Extensions.Primitives.Performance.StringValuesBenchmark.Indexer_FirstElement_Array.html) | +16.16% | 3.097582 | 3.598054 | None |

---

## 37. 27b25483e0 - Remove a few more instances of unsafe code in STJ (#114490)

**Date:** 2025-04-13 06:59:35
**Commit:** [27b25483e0](https://github.com/dotnet/runtime/commit/27b25483e06a14af2aaf6f6b6b9b6e527a3b69bf)
**Affected Tests:** 2

| Test Name | Link | Change | .NET 9 | .NET 10 | Other Changepoints |
|-----------|------|--------|--------|-------|--------------------|
| System.Net.NetworkInformation.Tests.PhysicalAddressTests.PAMedium | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Net.NetworkInformation.Tests.PhysicalAddressTests.PAMedium.html) | +5.65% | 14.461262 | 15.277686 | None |
| System.Perf_Convert.ToHexString | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Perf_Convert.ToHexString.html) | +5.56% | 31.483682 | 33.233707 | None |

---

## 38. 2ba0c4d944 - Fix build with enabled FEATURE_ENABLE_NO_ADDRESS_SPACE_RANDOMIZATION (#112689)

**Date:** 2025-04-21 20:17:25
**Commit:** [2ba0c4d944](https://github.com/dotnet/runtime/commit/2ba0c4d9440366366328a37ea1f2a6616564c849)
**Affected Tests:** 2

| Test Name | Link | Change | .NET 9 | .NET 10 | Other Changepoints |
|-----------|------|--------|--------|-------|--------------------|
| System.Collections.IndexerSet<Int32>.SortedList(Size: 512) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Collections.IndexerSet%28Int32%29.SortedList%28Size%3A%20512%29.html) | +11.93% | 17732.499285 | 19847.370215 | None |
| System.Buffers.Tests.RentReturnArrayPoolTests<Byte>.SingleSerial(RentalSize: 4096, ManipulateArray: False, Async: False, UseSharedPool: True) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Buffers.Tests.RentReturnArrayPoolTests%28Byte%29.SingleSerial%28RentalSize%3A%204096%2C%20ManipulateArray%3A%20False%2C%20Async%3A%20False%2C%20UseSharedPool%3A%20True%29.html) | +8.52% | 11.604297 | 12.592757 | None |

---

## 39. 0ac2caf41a - Tar: Adjust the way we write GNU longlink and longpath metadata (#114940)

**Date:** 2025-04-24 15:49:11
**Commit:** [0ac2caf41a](https://github.com/dotnet/runtime/commit/0ac2caf41a88c56a287ab790e92eaf3ccf846fc8)
**Affected Tests:** 2

| Test Name | Link | Change | .NET 9 | .NET 10 | Other Changepoints |
|-----------|------|--------|--------|-------|--------------------|
| System.Formats.Tar.Tests.Perf_TarWriter.UstarTarEntry_WriteEntry | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Formats.Tar.Tests.Perf_TarWriter.UstarTarEntry_WriteEntry.html) | +14.39% | 200.374721 | 229.217896 | None |
| System.Formats.Tar.Tests.Perf_TarWriter.V7TarEntry_WriteEntry | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Formats.Tar.Tests.Perf_TarWriter.V7TarEntry_WriteEntry.html) | +13.57% | 186.447493 | 211.740735 | None |

---

## 40. 4158fca8fc - JIT: revise may/must point to stack analysis (#115080)

**Date:** 2025-04-27 01:34:42
**Commit:** [4158fca8fc](https://github.com/dotnet/runtime/commit/4158fca8fc562796170dd451d132ab3d02ecc804)
**Affected Tests:** 2

| Test Name | Link | Change | .NET 9 | .NET 10 | Other Changepoints |
|-----------|------|--------|--------|-------|--------------------|
| ArrayDeAbstraction.foreach_opaque_array_via_interface_in_loop | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/ArrayDeAbstraction.foreach_opaque_array_via_interface_in_loop.html) | -9.74% | 988.975437 | 892.689969 | None |
| ArrayDeAbstraction.foreach_member_array_via_interface_property_in_loop | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/ArrayDeAbstraction.foreach_member_array_via_interface_property_in_loop.html) | -8.86% | 969.545202 | 883.604330 | None |

---

## 41. bfa124fa79 - JIT: Speed up floating to integer casts on x86/x64 (#114410)

**Date:** 2025-05-02 21:17:29
**Commit:** [bfa124fa79](https://github.com/dotnet/runtime/commit/bfa124fa79d8fb89d18bc40b26158407490393ef)
**Affected Tests:** 2

| Test Name | Link | Change | .NET 9 | .NET 10 | Other Changepoints |
|-----------|------|--------|--------|-------|--------------------|
| System.Buffers.Text.Tests.Base64Tests.Base64DecodeDestinationTooSmall(NumberOfBytes: 1000) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Buffers.Text.Tests.Base64Tests.Base64DecodeDestinationTooSmall%28NumberOfBytes%3A%201000%29.html) | +12.03% | 61.816755 | 69.253326 | None |
| System.Numerics.Tensors.Tests.Perf_NumberTensorPrimitives<Double>.IndexOfMax(BufferLength: 128) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Numerics.Tensors.Tests.Perf_NumberTensorPrimitives%28Double%29.IndexOfMax%28BufferLength%3A%20128%29.html) | +19.30% | 57.775863 | 68.924326 | None |

---

## 42. 3c8bae3ff0 - JIT: also run local assertion prop in postorder during morph (#115626)

**Date:** 2025-05-16 22:16:17
**Commit:** [3c8bae3ff0](https://github.com/dotnet/runtime/commit/3c8bae3ff0906f590c6eec61eb114eac205ac2cc)
**Affected Tests:** 2

| Test Name | Link | Change | .NET 9 | .NET 10 | Other Changepoints |
|-----------|------|--------|--------|-------|--------------------|
| System.Collections.IterateForEach<Int32>.Stack(Size: 512) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Collections.IterateForEach%28Int32%29.Stack%28Size%3A%20512%29.html) | +12.79% | 1006.313036 | 1134.983234 | None |
| System.Buffers.Text.Tests.Base64Tests.ConvertTryFromBase64Chars(NumberOfBytes: 1000) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Buffers.Text.Tests.Base64Tests.ConvertTryFromBase64Chars%28NumberOfBytes%3A%201000%29.html) | +10.17% | 746.523883 | 822.417701 | [1](#1-41be5e229b---jit-graph-based-loop-inversion-116017) |

---

## 43. 2d5a2ee095 - Remove canceled AsyncOperations from channel queues (#116021)

**Date:** 2025-06-02 15:59:11
**Commit:** [2d5a2ee095](https://github.com/dotnet/runtime/commit/2d5a2ee09518e3afad75ea9bc40df0a548bcfa36)
**Affected Tests:** 2

| Test Name | Link | Change | .NET 9 | .NET 10 | Other Changepoints |
|-----------|------|--------|--------|-------|--------------------|
| System.Threading.Channels.Tests.BoundedChannelPerfTests.TryWriteThenTryRead | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Threading.Channels.Tests.BoundedChannelPerfTests.TryWriteThenTryRead.html) | +18.81% | 37.816292 | 44.928353 | None |
| System.Threading.Channels.Tests.UnboundedChannelPerfTests.TryWriteThenTryRead | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Threading.Channels.Tests.UnboundedChannelPerfTests.TryWriteThenTryRead.html) | +10.71% | 31.255626 | 34.603370 | None |

---

## 44. 1b5c48dc59 - Upgrade vendored Brotli dependency to v1.1.0 (#106994)

**Date:** 2024-08-28 18:34:28
**Commit:** [1b5c48dc59](https://github.com/dotnet/runtime/commit/1b5c48dc5958e20b4aa0f4cbfc21fddb8f81052c)
**Affected Tests:** 1

| Test Name | Link | Change | .NET 9 | .NET 10 | Other Changepoints |
|-----------|------|--------|--------|-------|--------------------|
| System.IO.Compression.Brotli.Compress_WithState(level: Optimal, file: "alice29.txt") | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.IO.Compression.Brotli.Compress_WithState%28level%3A%20Optimal%2C%20file%3A%20%22alice29.txt%22%29.html) | +5.83% | 184894088.875000 | 195676447.553571 | [11](#11-217525ae6f---workaround-for-106521-106578), [20](#20-b382a451a9---fix-build-with--dclr_cmake_use_system_brotlitrue-110816) |

---

## 45. b06d5e241c - Add a SearchValues implementation for values with unique low nibbles (#106900)

**Date:** 2024-09-10 14:16:51
**Commit:** [b06d5e241c](https://github.com/dotnet/runtime/commit/b06d5e241c8cbc6b991b901dacc8bcc354984b1d)
**Affected Tests:** 1

| Test Name | Link | Change | .NET 9 | .NET 10 | Other Changepoints |
|-----------|------|--------|--------|-------|--------------------|
| System.Tests.Perf_Int32.TryParse(value: "12345") | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Tests.Perf_Int32.TryParse%28value%3A%20%2212345%22%29.html) | +9.23% | 10.580130 | 11.556880 | [6](#6-ea43e17c---jit-run-profile-repair-after-frontend-phases-111915) |

---

## 46. a38ab4c0bc - Remove HelperMethodFrames (HMF) from Reflection code paths (#108415)

**Date:** 2024-10-03 03:53:57
**Commit:** [a38ab4c0bc](https://github.com/dotnet/runtime/commit/a38ab4c0bc3780754259be600db1501cc2907a84)
**Affected Tests:** 1

| Test Name | Link | Change | .NET 9 | .NET 10 | Other Changepoints |
|-----------|------|--------|--------|-------|--------------------|
| System.Reflection.Invoke.Field_SetStatic_struct | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Reflection.Invoke.Field_SetStatic_struct.html) | +5.16% | 39.618578 | 41.664751 | None |

---

## 47. 4bf7a2b238 - Improve codegen around unsigned comparisons (#105593)

**Date:** 2024-10-04 14:32:10
**Commit:** [4bf7a2b238](https://github.com/dotnet/runtime/commit/4bf7a2b2385d0730448b698d3011182687e5a9c2)
**Affected Tests:** 1

| Test Name | Link | Change | .NET 9 | .NET 10 | Other Changepoints |
|-----------|------|--------|--------|-------|--------------------|
| System.Tests.Perf_String.ToUpperInvariant(s: "TEST") | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Tests.Perf_String.ToUpperInvariant%28s%3A%20%22TEST%22%29.html) | +21.31% | 5.454580 | 6.616886 | None |

---

## 48. 789bc64ad4 - unblock inlining of generics with static fields (#109256)

**Date:** 2024-10-27 15:01:56
**Commit:** [789bc64ad4](https://github.com/dotnet/runtime/commit/789bc64ad46b855bc7c0646e35e3ccf371960629)
**Affected Tests:** 1

| Test Name | Link | Change | .NET 9 | .NET 10 | Other Changepoints |
|-----------|------|--------|--------|-------|--------------------|
| System.Collections.AddGivenSize<String>.List(Size: 512) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Collections.AddGivenSize%28String%29.List%28Size%3A%20512%29.html) | +15.76% | 1340.137505 | 1551.305664 | [31](#31-489a1512f5---remove-ldsfld-quirk-108606) |

---

## 49. bf369fd44e - JIT: account for newly unreachable blocks in morph (#109394)

**Date:** 2024-11-01 18:13:03
**Commit:** [bf369fd44e](https://github.com/dotnet/runtime/commit/bf369fd44e4029e8a2453cd619acfa3e67d30a43)
**Affected Tests:** 1

| Test Name | Link | Change | .NET 9 | .NET 10 | Other Changepoints |
|-----------|------|--------|--------|-------|--------------------|
| System.Collections.ContainsKeyTrue<String, String>.FrozenDictionary(Size: 512) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Collections.ContainsKeyTrue%28String%2C%20String%29.FrozenDictionary%28Size%3A%20512%29.html) | +17.73% | 5107.537873 | 6013.126537 | [3](#3-ddf8075a2f---jit-visit-blocks-in-rpo-during-lsra-107927) |

---

## 50. 53cc1ddeec - Remove redundant sign/zero extension for SIMD broadcasts (#108824)

**Date:** 2024-11-25 08:31:38
**Commit:** [53cc1ddeec](https://github.com/dotnet/runtime/commit/53cc1ddeec661d03d65d0e2949f3486e2162d80f)
**Affected Tests:** 1

| Test Name | Link | Change | .NET 9 | .NET 10 | Other Changepoints |
|-----------|------|--------|--------|-------|--------------------|
| System.Memory.Span<Char>.LastIndexOfAnyValues(Size: 4) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Memory.Span%28Char%29.LastIndexOfAnyValues%28Size%3A%204%29.html) | +17.07% | 2.922074 | 3.420840 | None |

---

## 51. dfb2b8a861 - Update dependencies from dotnet/roslyn (#110105)

**Date:** 2024-12-12 11:25:30
**Commit:** [dfb2b8a861](https://github.com/dotnet/runtime/commit/dfb2b8a861fc97ce90c8f31c886d4d27c5b36f46)
**Affected Tests:** 1

| Test Name | Link | Change | .NET 9 | .NET 10 | Other Changepoints |
|-----------|------|--------|--------|-------|--------------------|
| System.Xml.Linq.Perf_XElementList.Enumerator | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Xml.Linq.Perf_XElementList.Enumerator.html) | +21.99% | 147.893044 | 180.413967 | None |

---

## 52. 95814d0f99 - [main] Update dependencies from dnceng/internal/dotnet-optimization (#110904)

**Date:** 2025-01-02 10:31:10
**Commit:** [95814d0f99](https://github.com/dotnet/runtime/commit/95814d0f99fd876fdc2f6b5b4cf5d5fc94adaea9)
**Affected Tests:** 1

| Test Name | Link | Change | .NET 9 | .NET 10 | Other Changepoints |
|-----------|------|--------|--------|-------|--------------------|
| System.Text.Perf_Ascii.IsValid_Chars(Size: 128) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Text.Perf_Ascii.IsValid_Chars%28Size%3A%20128%29.html) | +21.62% | 4.935867 | 6.002944 | None |

---

## 53. d2cdcdd693 - JIT: Relax address exposure check for tailcalls (#111397)

**Date:** 2025-01-15 09:15:38
**Commit:** [d2cdcdd693](https://github.com/dotnet/runtime/commit/d2cdcdd69391bbfde1a8fb4aa275a6cc393ca65b)
**Affected Tests:** 1

| Test Name | Link | Change | .NET 9 | .NET 10 | Other Changepoints |
|-----------|------|--------|--------|-------|--------------------|
| System.Text.Perf_Ascii.IsValid_Chars(Size: 6) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Text.Perf_Ascii.IsValid_Chars%28Size%3A%206%29.html) | +9.30% | 2.656210 | 2.903174 | None |

---

## 54. 2a2b7dc72b - JIT: Fix profile maintenance in `optSetBlockWeights`, funclet creation (#111736)

**Date:** 2025-01-23 19:40:52
**Commit:** [2a2b7dc72b](https://github.com/dotnet/runtime/commit/2a2b7dc72b5642dd24ca37623327e765a9730dd7)
**Affected Tests:** 1

| Test Name | Link | Change | .NET 9 | .NET 10 | Other Changepoints |
|-----------|------|--------|--------|-------|--------------------|
| System.Memory.Span<Int32>.Fill(Size: 512) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Memory.Span%28Int32%29.Fill%28Size%3A%20512%29.html) | +10.69% | 24.311001 | 26.910972 | None |

---

## 55. c37cfcc645 - JIT: Use fgCalledCount in inlinee weight computation (#112499)

**Date:** 2025-02-18 17:26:52
**Commit:** [c37cfcc645](https://github.com/dotnet/runtime/commit/c37cfcc6459605e7cd1e1311c6dc74ee087ec08c)
**Affected Tests:** 1

| Test Name | Link | Change | .NET 9 | .NET 10 | Other Changepoints |
|-----------|------|--------|--------|-------|--------------------|
| Benchstone.MDBenchI.MDArray2.Test | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/Benchstone.MDBenchI.MDArray2.Test.html) | +22.77% | 1349393098.607143 | 1656599991.589286 | None |

---

## 56. 496824c97b - Avoid `Unsafe.As` in `BitConverter` (#112616)

**Date:** 2025-03-07 22:10:27
**Commit:** [496824c97b](https://github.com/dotnet/runtime/commit/496824c97b9cf072682dea1a789cac6ca1875692)
**Affected Tests:** 1

| Test Name | Link | Change | .NET 9 | .NET 10 | Other Changepoints |
|-----------|------|--------|--------|-------|--------------------|
| System.Tests.Perf_Byte.TryParse(value: "0") | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Tests.Perf_Byte.TryParse%28value%3A%20%220%22%29.html) | +5.50% | 9.007999 | 9.503419 | None |

---

## 57. a45130eb80 - JIT: Enable containment for BMI2 shift and rotate instructions (#111778)

**Date:** 2025-03-18 16:15:54
**Commit:** [a45130eb80](https://github.com/dotnet/runtime/commit/a45130eb80)
**Affected Tests:** 1

| Test Name | Link | Change | .NET 9 | .NET 10 | Other Changepoints |
|-----------|------|--------|--------|-------|--------------------|
| System.Collections.Tests.Perf_BitArray.BitArrayLeftShift(Size: 512) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Collections.Tests.Perf_BitArray.BitArrayLeftShift%28Size%3A%20512%29.html) | +34.28% | 153.259354 | 205.794787 | None |

---

## 58. dc88476f10 - JIT: enable inlining methods with EH (#112998)

**Date:** 2025-03-20 19:52:01
**Commit:** [dc88476f10](https://github.com/dotnet/runtime/commit/dc88476f102123edebd6b2d2efe5a56146f60094)
**Affected Tests:** 1

| Test Name | Link | Change | .NET 9 | .NET 10 | Other Changepoints |
|-----------|------|--------|--------|-------|--------------------|
| System.IO.Tests.BinaryReaderTests.ReadBool | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.IO.Tests.BinaryReaderTests.ReadBool.html) | +13.88% | 2.656224 | 3.025028 | None |

---

## 59. f9fc62ab41 - [main] Update dependencies from dnceng/internal/dotnet-optimization (#112832)

**Date:** 2025-03-26 11:50:18
**Commit:** [f9fc62ab41](https://github.com/dotnet/runtime/commit/f9fc62ab41d53d331544b4da5a187b036df8c1bb)
**Affected Tests:** 1

| Test Name | Link | Change | .NET 9 | .NET 10 | Other Changepoints |
|-----------|------|--------|--------|-------|--------------------|
| Microsoft.Extensions.Primitives.StringSegmentBenchmark.Equals_Object_Valid | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/Microsoft.Extensions.Primitives.StringSegmentBenchmark.Equals_Object_Valid.html) | +17.75% | 6.146948 | 7.237974 | None |

---

## 60. 33b5215c15 - Smaller funclet prologs/epilogs (x64) (#115284)

**Date:** 2025-05-10 05:40:58
**Commit:** [33b5215c15](https://github.com/dotnet/runtime/commit/33b5215c15b16ad9e2738c325f6b562702c308d3)
**Affected Tests:** 1

| Test Name | Link | Change | .NET 9 | .NET 10 | Other Changepoints |
|-----------|------|--------|--------|-------|--------------------|
| System.Collections.ContainsTrueComparer<Int32>.HashSet(Size: 512) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Collections.ContainsTrueComparer%28Int32%29.HashSet%28Size%3A%20512%29.html) | +55.95% | 2610.504462 | 4071.208610 | None |

---

## 61. fd8933aac2 - Share implementation of ComWrappers between CoreCLR and NativeAOT (#113907)

**Date:** 2025-05-10 17:16:10
**Commit:** [fd8933aac2](https://github.com/dotnet/runtime/commit/fd8933aac237d2f3103de071ec4bc1547bfef16c)
**Affected Tests:** 1

| Test Name | Link | Change | .NET 9 | .NET 10 | Other Changepoints |
|-----------|------|--------|--------|-------|--------------------|
| Interop.ComWrappersTests.ParallelRCWLookUp | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/Interop.ComWrappersTests.ParallelRCWLookUp.html) | +27.16% | 313726947.464286 | 398935684.035714 | None |

---

## 62. e0e9f15d06 - Implement various convenience methods for System.Numerics types (#115457)

**Date:** 2025-05-12 19:51:25
**Commit:** [e0e9f15d06](https://github.com/dotnet/runtime/commit/e0e9f15d06b775325c874674bfca51d18c8f5075)
**Affected Tests:** 1

| Test Name | Link | Change | .NET 9 | .NET 10 | Other Changepoints |
|-----------|------|--------|--------|-------|--------------------|
| System.Numerics.Tests.Perf_Quaternion.CreateFromRotationMatrixBenchmark | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Numerics.Tests.Perf_Quaternion.CreateFromRotationMatrixBenchmark.html) | +47.68% | 4.721879 | 6.973434 | [2](#2-ffcd1c5442---trust-single-edge-synthetic-profile-116054) |

---

## 63. ac7097cb47 - Update dependencies from https://dev.azure.com/dnceng/internal/_git/dotnet-optimization build 20250506.1 (#115464)

**Date:** 2025-06-02 10:40:04
**Commit:** [ac7097cb47](https://github.com/dotnet/runtime/commit/ac7097cb4761d9c71e212bf07e4d916d1571c96b)
**Affected Tests:** 1

| Test Name | Link | Change | .NET 9 | .NET 10 | Other Changepoints |
|-----------|------|--------|--------|-------|--------------------|
| System.IO.Tests.Perf_StreamWriter.WriteCharArray(writeLength: 100) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.IO.Tests.Perf_StreamWriter.WriteCharArray%28writeLength%3A%20100%29.html) | +12.51% | 247439911.964286 | 278391499.214286 | None |

---

## 64. e82789be73 - remove double -> int/uint cast helpers (#116484)

**Date:** 2025-06-11 16:09:36
**Commit:** [e82789be73](https://github.com/dotnet/runtime/commit/e82789be7360c936df7553cc84213c9e829789d0)
**Affected Tests:** 1

| Test Name | Link | Change | .NET 9 | .NET 10 | Other Changepoints |
|-----------|------|--------|--------|-------|--------------------|
| System.IO.Tests.BinaryWriterTests.WriteSingle | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.IO.Tests.BinaryWriterTests.WriteSingle.html) | +11.84% | 2.422528 | 2.709312 | None |

---

## 65. d3e2f5e13a - JIT: Exclude `BB_UNITY_WEIGHT` scaling from `BasicBlock::isBBWeightCold` (#116548)

**Date:** 2025-06-13 21:53:16
**Commit:** [d3e2f5e13a](https://github.com/dotnet/runtime/commit/d3e2f5e13aac894737a90ba8494ad57465ba639f)
**Affected Tests:** 1

| Test Name | Link | Change | .NET 9 | .NET 10 | Other Changepoints |
|-----------|------|--------|--------|-------|--------------------|
| System.Perf_Convert.ToBase64CharArray(binaryDataSize: 1024, formattingOptions: InsertLineBreaks) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Perf_Convert.ToBase64CharArray%28binaryDataSize%3A%201024%2C%20formattingOptions%3A%20InsertLineBreaks%29.html) | +8.19% | 1270.795040 | 1374.818125 | None |

---

## 66. 38c8e8f4cc - Add CollectionsMarshal.AsBytes(BitArray) (#116308)

**Date:** 2025-06-17 17:13:37
**Commit:** [38c8e8f4cc](https://github.com/dotnet/runtime/commit/38c8e8f4cc1be3abd20f675771f208360b11b52c)
**Affected Tests:** 1

| Test Name | Link | Change | .NET 9 | .NET 10 | Other Changepoints |
|-----------|------|--------|--------|-------|--------------------|
| System.Collections.Tests.Perf_BitArray.BitArrayNot(Size: 4) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04/System.Collections.Tests.Perf_BitArray.BitArrayNot%28Size%3A%204%29.html) | +10.32% | 4.421564 | 4.877761 | [1](#1-41be5e229b---jit-graph-based-loop-inversion-116017), [22](#22-30082a461a---jit-save-pgo-data-in-inline-context-use-it-for-call-optimization-116241) |

---
