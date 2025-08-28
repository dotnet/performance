# Regression Report - windows-x64-tiger

## 1. ffcd1c5442 - Trust single-edge synthetic profile (#116054)

**Date:** 2025-05-28 16:16:24
**Commit:** [ffcd1c5442](https://github.com/dotnet/runtime/commit/ffcd1c5442a0c6e5317efa46d6ce381003397476)
**Affected Tests:** 61

| Test Name | Link | Change | .NET 9 | .NET 10 | Other Changepoints |
|-----------|------|--------|--------|-------|--------------------|
| System.IO.Hashing.Tests.Crc64_AppendPerf.Append(BufferSize: 256) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/System.IO.Hashing.Tests.Crc64_AppendPerf.Append%28BufferSize%3A%20256%29.html) | +412.79% | 21.595596 | 110.739071 | None |
| System.IO.Hashing.Tests.Crc64_AppendPerf.Append(BufferSize: 10240) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/System.IO.Hashing.Tests.Crc64_AppendPerf.Append%28BufferSize%3A%2010240%29.html) | +338.34% | 608.026998 | 2665.240759 | None |
| System.Tests.Perf_Int32.ParseHex(value: "80000000") | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/System.Tests.Perf_Int32.ParseHex%28value%3A%20%2280000000%22%29.html) | +157.96% | 16.785287 | 43.299179 | None |
| System.Tests.Perf_Int32.ParseHex(value: "7FFFFFFF") | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/System.Tests.Perf_Int32.ParseHex%28value%3A%20%227FFFFFFF%22%29.html) | +163.17% | 16.776877 | 44.152127 | None |
| System.IO.Hashing.Tests.Crc64_AppendPerf.Append(BufferSize: 16) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/System.IO.Hashing.Tests.Crc64_AppendPerf.Append%28BufferSize%3A%2016%29.html) | +149.67% | 10.381918 | 25.920650 | None |
| System.Text.Encodings.Web.Tests.Perf_Encoders.EncodeUtf16(arguments: Url,&lorem ipsum=dolor sit amet,16) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/System.Text.Encodings.Web.Tests.Perf_Encoders.EncodeUtf16%28arguments%3A%20Url%2C%26lorem%20ipsum%3Ddolor%20sit%20amet%2C16%29.html) | +84.12% | 80.355329 | 147.954117 | None |
| System.Memory.Span<Char>.Fill(Size: 4) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/System.Memory.Span%28Char%29.Fill%28Size%3A%204%29.html) | +68.38% | 3.799040 | 6.396643 | None |
| System.Text.Encodings.Web.Tests.Perf_Encoders.EncodeUtf16(arguments: JavaScript,&Hello+<World>!,16) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/System.Text.Encodings.Web.Tests.Perf_Encoders.EncodeUtf16%28arguments%3A%20JavaScript%2C%26Hello%2B%28World%29%21%2C16%29.html) | +63.87% | 66.764197 | 109.409526 | None |
| System.Perf_Convert.ToBase64CharArray(binaryDataSize: 1024, formattingOptions: None) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/System.Perf_Convert.ToBase64CharArray%28binaryDataSize%3A%201024%2C%20formattingOptions%3A%20None%29.html) | +56.89% | 141.904163 | 222.633218 | None |
| System.Text.Json.Tests.Perf_Basic.WriteBasicUtf8(Formatted: False, SkipValidation: False, DataSize: 100000) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/System.Text.Json.Tests.Perf_Basic.WriteBasicUtf8%28Formatted%3A%20False%2C%20SkipValidation%3A%20False%2C%20DataSize%3A%20100000%29.html) | +48.54% | 1758698.500545 | 2612319.738277 | None |
| System.Collections.ContainsTrueComparer<Int32>.HashSet(Size: 512) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/System.Collections.ContainsTrueComparer%28Int32%29.HashSet%28Size%3A%20512%29.html) | +59.09% | 3396.831016 | 5404.125565 | None |
| System.Net.Tests.Perf_WebUtility.Decode_NoDecodingRequired | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/System.Net.Tests.Perf_WebUtility.Decode_NoDecodingRequired.html) | +55.94% | 57.972514 | 90.399890 | [4](#4-b146d7512c---jit-move-loop-inversion-to-after-loop-recognition-115850) |
| System.Numerics.Tests.Perf_BigInteger.Subtract(arguments: 16,16 bits) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/System.Numerics.Tests.Perf_BigInteger.Subtract%28arguments%3A%2016%2C16%20bits%29.html) | +51.00% | 7.837329 | 11.834330 | None |
| System.Tests.Perf_Int32.ParseHex(value: "3039") | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/System.Tests.Perf_Int32.ParseHex%28value%3A%20%223039%22%29.html) | +62.03% | 16.074694 | 26.045832 | None |
| System.Text.RegularExpressions.Tests.Perf_Regex_Industry_RustLang_Sherlock.Count(Pattern: "\\w+\\s+Holmes", Options: NonBacktracking) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/System.Text.RegularExpressions.Tests.Perf_Regex_Industry_RustLang_Sherlock.Count%28Pattern%3A%20%22%5C%5Cw%2B%5C%5Cs%2BHolmes%22%2C%20Options%3A%20NonBacktracking%29.html) | +49.67% | 2481196.183473 | 3713627.433473 | [6](#6-1c10ceecbf---jit-add-3-opt-implementation-for-improving-upon-rpo-based-block-layout-103450), [8](#8-d3e2f5e13a---jit-exclude-bb_unity_weight-scaling-from-basicblockisbbweightcold-116548) |
| System.Collections.ContainsFalse<String>.List(Size: 512) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/System.Collections.ContainsFalse%28String%29.List%28Size%3A%20512%29.html) | +44.82% | 344105.540858 | 498350.614834 | [2](#2-ddf8075a2f---jit-visit-blocks-in-rpo-during-lsra-107927) |
| System.Numerics.Tests.Perf_VectorConvert.Widen_float | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/System.Numerics.Tests.Perf_VectorConvert.Widen_float.html) | +50.34% | 1047.578678 | 1574.937821 | None |
| System.Tests.Perf_String.Format_MultipleArgs | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/System.Tests.Perf_String.Format_MultipleArgs.html) | +48.50% | 183.727697 | 272.840016 | None |
| System.Buffers.Text.Tests.Utf8ParserTests.TryParseInt64(value: 9223372036854775807) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/System.Buffers.Text.Tests.Utf8ParserTests.TryParseInt64%28value%3A%209223372036854775807%29.html) | +43.07% | 22.856572 | 32.700806 | None |
| System.Numerics.Tests.Perf_BigInteger.Add(arguments: 16,16 bits) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/System.Numerics.Tests.Perf_BigInteger.Add%28arguments%3A%2016%2C16%20bits%29.html) | +44.51% | 8.242740 | 11.911399 | None |
| System.Text.Encodings.Web.Tests.Perf_Encoders.EncodeUtf16(arguments: Url,�2020,16) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/System.Text.Encodings.Web.Tests.Perf_Encoders.EncodeUtf16%28arguments%3A%20Url%2C%EF%BF%BD2020%2C16%29.html) | +32.27% | 41.007119 | 54.241473 | None |
| CscBench.CompileTest | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/CscBench.CompileTest.html) | +25.78% | 270839391.071429 | 340661466.071429 | None |
| System.Collections.Concurrent.IsEmpty<String>.Queue(Size: 0) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/System.Collections.Concurrent.IsEmpty%28String%29.Queue%28Size%3A%200%29.html) | +27.40% | 6.794028 | 8.655274 | None |
| System.Collections.CtorFromCollectionNonGeneric<Int32>.Hashtable(Size: 512) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/System.Collections.CtorFromCollectionNonGeneric%28Int32%29.Hashtable%28Size%3A%20512%29.html) | +30.85% | 20481.277753 | 26798.934538 | [2](#2-ddf8075a2f---jit-visit-blocks-in-rpo-during-lsra-107927) |
| System.Collections.CtorFromCollectionNonGeneric<String>.Hashtable(Size: 512) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/System.Collections.CtorFromCollectionNonGeneric%28String%29.Hashtable%28Size%3A%20512%29.html) | +31.04% | 28913.111303 | 37888.660883 | [30](#30-1ddfa144d9---jit-tail-merge-returns-with-multiple-statements-109670) |
| System.Text.Encodings.Web.Tests.Perf_Encoders.EncodeUtf16(arguments: Url,&lorem ipsum=dolor sit amet,512) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/System.Text.Encodings.Web.Tests.Perf_Encoders.EncodeUtf16%28arguments%3A%20Url%2C%26lorem%20ipsum%3Ddolor%20sit%20amet%2C512%29.html) | +27.24% | 138.621166 | 176.378702 | None |
| System.Tests.Perf_Int64.ToString(value: 9223372036854775807) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/System.Tests.Perf_Int64.ToString%28value%3A%209223372036854775807%29.html) | +29.14% | 30.896153 | 39.899440 | None |
| System.Collections.ContainsFalse<String>.ICollection(Size: 512) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/System.Collections.ContainsFalse%28String%29.ICollection%28Size%3A%20512%29.html) | +21.32% | 342799.077463 | 415884.455382 | [17](#17-39a31f082e---virtual-stub-indirect-call-profiling-116453) |
| System.Memory.Span<Int32>.SequenceEqual(Size: 4) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/System.Memory.Span%28Int32%29.SequenceEqual%28Size%3A%204%29.html) | +24.30% | 3.032083 | 3.768794 | None |
| System.Memory.Span<Byte>.StartsWith(Size: 4) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/System.Memory.Span%28Byte%29.StartsWith%28Size%3A%204%29.html) | +22.85% | 4.370038 | 5.368736 | None |
| System.Tests.Perf_Random.NextBytes_span | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/System.Tests.Perf_Random.NextBytes_span.html) | +15.53% | 3250.036349 | 3754.623068 | [4](#4-b146d7512c---jit-move-loop-inversion-to-after-loop-recognition-115850) |
| System.Buffers.Text.Tests.Utf8FormatterTests.FormatterDateTimeOffsetNow(value: 12/30/2017 3:45:22 AM -08:00) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/System.Buffers.Text.Tests.Utf8FormatterTests.FormatterDateTimeOffsetNow%28value%3A%2012/30/2017%203%3A45%3A22%20AM%20-08%3A00%29.html) | +18.03% | 27.633106 | 32.615377 | None |
| System.Text.Encodings.Web.Tests.Perf_Encoders.EncodeUtf16(arguments: UnsafeRelaxed,hello "there",16) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/System.Text.Encodings.Web.Tests.Perf_Encoders.EncodeUtf16%28arguments%3A%20UnsafeRelaxed%2Chello%20%22there%22%2C16%29.html) | +20.01% | 39.345780 | 47.216972 | None |
| System.Text.Encodings.Web.Tests.Perf_Encoders.EncodeUtf16(arguments: JavaScript,&Hello+<World>!,512) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/System.Text.Encodings.Web.Tests.Perf_Encoders.EncodeUtf16%28arguments%3A%20JavaScript%2C%26Hello%2B%28World%29%21%2C512%29.html) | +16.48% | 123.206043 | 143.506418 | None |
| System.Tests.Perf_Enum.GetNames_Generic | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/System.Tests.Perf_Enum.GetNames_Generic.html) | +17.41% | 21.567061 | 25.322679 | None |
| System.Tests.Perf_Enum.InterpolateIntoSpan_NonFlags(value: 42) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/System.Tests.Perf_Enum.InterpolateIntoSpan_NonFlags%28value%3A%2042%29.html) | +15.27% | 109.758833 | 126.516247 | None |
| System.Tests.Perf_Enum.InterpolateIntoSpan_Flags(value: 32) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/System.Tests.Perf_Enum.InterpolateIntoSpan_Flags%28value%3A%2032%29.html) | +18.24% | 107.844124 | 127.514315 | None |
| System.Collections.Tests.Perf_Dictionary.Clone(Items: 3000) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/System.Collections.Tests.Perf_Dictionary.Clone%28Items%3A%203000%29.html) | +15.93% | 11860.228588 | 13749.916725 | [3](#3-41be5e229b---jit-graph-based-loop-inversion-116017) |
| System.Tests.Perf_Uri.EscapeDataString(input: "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa... | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/System.Tests.Perf_Uri.EscapeDataString%28input%3A%20%22aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa.html) | +12.68% | 53.307156 | 60.067724 | None |
| System.Memory.Span<Int32>.StartsWith(Size: 33) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/System.Memory.Span%28Int32%29.StartsWith%28Size%3A%2033%29.html) | +13.43% | 4.703137 | 5.334773 | None |
| System.Collections.Concurrent.IsEmpty<String>.Queue(Size: 512) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/System.Collections.Concurrent.IsEmpty%28String%29.Queue%28Size%3A%20512%29.html) | +13.41% | 6.638652 | 7.528714 | None |
| System.Net.Tests.Perf_WebUtility.Decode_DecodingRequired | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/System.Net.Tests.Perf_WebUtility.Decode_DecodingRequired.html) | +13.56% | 116.057277 | 131.794345 | None |
| System.Tests.Perf_Int64.ToString(value: 12345) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/System.Tests.Perf_Int64.ToString%28value%3A%2012345%29.html) | +12.64% | 17.559100 | 19.778929 | None |
| System.Collections.CreateAddAndRemove<String>.HashSet(Size: 512) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/System.Collections.CreateAddAndRemove%28String%29.HashSet%28Size%3A%20512%29.html) | +13.94% | 27672.102200 | 31528.255142 | None |
| System.Tests.Perf_Enum.InterpolateIntoSpan_Flags(value: Red, Green) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/System.Tests.Perf_Enum.InterpolateIntoSpan_Flags%28value%3A%20Red%2C%20Green%29.html) | +12.62% | 115.890770 | 130.521739 | None |
| System.Collections.TryAddDefaultSize<Int32>.Dictionary(Count: 512) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/System.Collections.TryAddDefaultSize%28Int32%29.Dictionary%28Count%3A%20512%29.html) | +10.90% | 7713.187603 | 8553.957769 | None |
| System.Collections.CtorDefaultSize<String>.ConcurrentQueue | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/System.Collections.CtorDefaultSize%28String%29.ConcurrentQueue.html) | +9.92% | 87.366142 | 96.028864 | None |
| System.Tests.Perf_Enum.InterpolateIntoSpan_NonFlags(value: Saturday) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/System.Tests.Perf_Enum.InterpolateIntoSpan_NonFlags%28value%3A%20Saturday%29.html) | +10.62% | 111.105252 | 122.904580 | None |
| System.Tests.Perf_Enum.InterpolateIntoSpan_Flags(value: Red) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/System.Tests.Perf_Enum.InterpolateIntoSpan_Flags%28value%3A%20Red%29.html) | +10.64% | 86.414720 | 95.608148 | None |
| System.Tests.Perf_Enum.GetValuesAsUnderlyingType_Generic | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/System.Tests.Perf_Enum.GetValuesAsUnderlyingType_Generic.html) | +7.48% | 22.886183 | 24.599089 | None |
| System.Text.RegularExpressions.Tests.Perf_Regex_Industry_Mariomkas.Count(Pattern: "(?:(?:25[0-5]\|2[0-4][0-9]\|[01]?[0-9][0-9])\\.){3}(?:25[0-5]\|2[0-4][0-9]\|[01]?[0-9][0-9])", Options: NonBacktracking) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/System.Text.RegularExpressions.Tests.Perf_Regex_Industry_Mariomkas.Count%28Pattern%3A%20%22%28%3F%3A%28%3F%3A25%5B0-5%5D%7C2%5B0-4%5D%5B0-9%5D%7C%5B01%5D%3F%5B0-9%5D%5B0-9%5D%29%5C%5C.%29%7B3%7D%28%3F%3A25%5B0-5%5D%7C2%5B0-4%5D%5B0-9%5D%7C%5B01%5D%3F%5B0-9%5D%5B0-9%5D%29%22%2C%20Options%3A%20NonBacktracking%29.html) | +8.23% | 3002921.624564 | 3249919.686411 | None |
| System.Text.RegularExpressions.Tests.Perf_Regex_Industry_Mariomkas.Count(Pattern: "[\\w]+://[^/\\s?#]+[^\\s?#]+(?:\\?[^\\s#]*)?(?:#[^\\s]*)?", Options: NonBacktracking) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/System.Text.RegularExpressions.Tests.Perf_Regex_Industry_Mariomkas.Count%28Pattern%3A%20%22%5B%5C%5Cw%5D%2B%3A//%5B%5E/%5C%5Cs%3F%23%5D%2B%5B%5E%5C%5Cs%3F%23%5D%2B%28%3F%3A%5C%5C%3F%5B%5E%5C%5Cs%23%5D%2A%29%3F%28%3F%3A%23%5B%5E%5C%5Cs%5D%2A%29%3F%22%2C%20Options%3A%20NonBacktracking%29.html) | +9.22% | 3349182.330827 | 3658010.667293 | None |
| System.IO.Compression.ZLib.Compress(level: Fastest, file: "TestDocument.pdf") | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/System.IO.Compression.ZLib.Compress%28level%3A%20Fastest%2C%20file%3A%20%22TestDocument.pdf%22%29.html) | +8.96% | 1417771.814123 | 1544798.072240 | None |
| System.IO.Compression.Gzip.Compress(level: Fastest, file: "TestDocument.pdf") | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/System.IO.Compression.Gzip.Compress%28level%3A%20Fastest%2C%20file%3A%20%22TestDocument.pdf%22%29.html) | +9.12% | 1417182.877551 | 1546442.642857 | None |
| System.Text.Json.Tests.Perf_Basic.WriteBasicUtf8(Formatted: False, SkipValidation: False, DataSize: 10) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/System.Text.Json.Tests.Perf_Basic.WriteBasicUtf8%28Formatted%3A%20False%2C%20SkipValidation%3A%20False%2C%20DataSize%3A%2010%29.html) | +8.73% | 614.268311 | 667.890853 | None |
| System.Collections.CreateAddAndRemove<Int32>.Dictionary(Size: 512) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/System.Collections.CreateAddAndRemove%28Int32%29.Dictionary%28Size%3A%20512%29.html) | +8.33% | 10417.714904 | 11285.233662 | None |
| System.Tests.Perf_Enum.InterpolateIntoString(value: 32) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/System.Tests.Perf_Enum.InterpolateIntoString%28value%3A%2032%29.html) | +7.84% | 139.146834 | 150.055648 | [10](#10-ea43e17c95---jit-run-profile-repair-after-frontend-phases-111915) |
| System.Tests.Perf_String.Format_OneArg(s: "Testing {0}, {0:C}, {0:D5}, {0:E} - {0:F4}{0:G}{0:N}  {0:X} !!", o: 8) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/System.Tests.Perf_String.Format_OneArg%28s%3A%20%22Testing%20%7B0%7D%2C%20%7B0%3AC%7D%2C%20%7B0%3AD5%7D%2C%20%7B0%3AE%7D%20-%20%7B0%3AF4%7D%7B0%3AG%7D%7B0%3AN%7D%20%20%7B0%3AX%7D%20%21%21%22%2C%20o%3A%208%29.html) | +6.24% | N/A | N/A | None |
| System.Text.RegularExpressions.Tests.Perf_Regex_Industry_BoostDocs_Simple.IsMatch(Id: 2, Options: NonBacktracking) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/System.Text.RegularExpressions.Tests.Perf_Regex_Industry_BoostDocs_Simple.IsMatch%28Id%3A%202%2C%20Options%3A%20NonBacktracking%29.html) | +8.13% | 153.217207 | 165.677578 | [2](#2-ddf8075a2f---jit-visit-blocks-in-rpo-during-lsra-107927), [5](#5-6d12a304b3---jit-do-greedy-4-opt-for-backward-jumps-in-3-opt-layout-110277) |
| System.Text.Json.Tests.Perf_Base64.WriteByteArrayAsBase64_NoEscaping(NumberOfBytes: 100) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/System.Text.Json.Tests.Perf_Base64.WriteByteArrayAsBase64_NoEscaping%28NumberOfBytes%3A%20100%29.html) | +7.59% | 66.756039 | 71.825701 | None |
| System.Text.Json.Tests.Perf_Base64.WriteByteArrayAsBase64_HeavyEscaping(NumberOfBytes: 100) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/System.Text.Json.Tests.Perf_Base64.WriteByteArrayAsBase64_HeavyEscaping%28NumberOfBytes%3A%20100%29.html) | +10.83% | 66.650253 | 73.865521 | None |

---

## 2. ddf8075a2f - JIT: Visit blocks in RPO during LSRA (#107927)

**Date:** 2024-09-20 18:38:45
**Commit:** [ddf8075a2f](https://github.com/dotnet/runtime/commit/ddf8075a2fa3044554ded41c375a82a318ae01eb)
**Affected Tests:** 40

| Test Name | Link | Change | .NET 9 | .NET 10 | Other Changepoints |
|-----------|------|--------|--------|-------|--------------------|
| GuardedDevirtualization.TwoClassInterface.Call(testInput: pB = 1.00) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/GuardedDevirtualization.TwoClassInterface.Call%28testInput%3A%20pB%20%3D%201.00%29.html) | +98.94% | 0.634053 | 1.261365 | None |
| GuardedDevirtualization.ThreeClassVirtual.Call(testInput: pB=0.00 pD=1.00) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/GuardedDevirtualization.ThreeClassVirtual.Call%28testInput%3A%20pB%3D0.00%20pD%3D1.00%29.html) | +98.81% | 0.634654 | 1.261769 | None |
| GuardedDevirtualization.TwoClassVirtual.Call(testInput: pB = 1.00) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/GuardedDevirtualization.TwoClassVirtual.Call%28testInput%3A%20pB%20%3D%201.00%29.html) | +98.91% | 0.634048 | 1.261155 | None |
| GuardedDevirtualization.ThreeClassInterface.Call(testInput: pB=0.00 pD=0.00) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/GuardedDevirtualization.ThreeClassInterface.Call%28testInput%3A%20pB%3D0.00%20pD%3D0.00%29.html) | +98.64% | 0.633895 | 1.259162 | None |
| GuardedDevirtualization.ThreeClassInterface.Call(testInput: pB=0.00 pD=1.00) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/GuardedDevirtualization.ThreeClassInterface.Call%28testInput%3A%20pB%3D0.00%20pD%3D1.00%29.html) | +98.71% | 0.634273 | 1.260336 | None |
| GuardedDevirtualization.ThreeClassVirtual.Call(testInput: pB=1.00 pD=0.00) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/GuardedDevirtualization.ThreeClassVirtual.Call%28testInput%3A%20pB%3D1.00%20pD%3D0.00%29.html) | +98.64% | 0.634036 | 1.259452 | None |
| GuardedDevirtualization.ThreeClassVirtual.Call(testInput: pB=0.00 pD=0.00) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/GuardedDevirtualization.ThreeClassVirtual.Call%28testInput%3A%20pB%3D0.00%20pD%3D0.00%29.html) | +98.89% | 0.633281 | 1.259502 | None |
| GuardedDevirtualization.ThreeClassInterface.Call(testInput: pB=1.00 pD=0.00) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/GuardedDevirtualization.ThreeClassInterface.Call%28testInput%3A%20pB%3D1.00%20pD%3D0.00%29.html) | +98.51% | 0.634404 | 1.259338 | None |
| GuardedDevirtualization.TwoClassInterface.Call(testInput: pB = 0.00) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/GuardedDevirtualization.TwoClassInterface.Call%28testInput%3A%20pB%20%3D%200.00%29.html) | +98.43% | 0.634332 | 1.258708 | None |
| GuardedDevirtualization.TwoClassVirtual.Call(testInput: pB = 0.00) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/GuardedDevirtualization.TwoClassVirtual.Call%28testInput%3A%20pB%20%3D%200.00%29.html) | +98.55% | 0.634289 | 1.259379 | None |
| System.Linq.Tests.Perf_Enumerable.ToArray(input: IEnumerable) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/System.Linq.Tests.Perf_Enumerable.ToArray%28input%3A%20IEnumerable%29.html) | +74.92% | 345.846911 | 604.938740 | None |
| SciMark2.kernel.benchSparseMult | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/SciMark2.kernel.benchSparseMult.html) | +67.64% | 596219400.000000 | 999489944.642857 | [3](#3-41be5e229b---jit-graph-based-loop-inversion-116017), [52](#52-e9a91f90b8---jit-skip-parameter-register-preferencing-in-osr-methods-112452) |
| GuardedDevirtualization.TwoClassVirtual.Call(testInput: pB = 0.10) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/GuardedDevirtualization.TwoClassVirtual.Call%28testInput%3A%20pB%20%3D%200.10%29.html) | +61.24% | 0.816818 | 1.317040 | [17](#17-39a31f082e---virtual-stub-indirect-call-profiling-116453) |
| GuardedDevirtualization.TwoClassInterface.Call(testInput: pB = 0.90) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/GuardedDevirtualization.TwoClassInterface.Call%28testInput%3A%20pB%20%3D%200.90%29.html) | +46.39% | 0.981077 | 1.436204 | None |
| System.Collections.ContainsFalse<String>.List(Size: 512) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/System.Collections.ContainsFalse%28String%29.List%28Size%3A%20512%29.html) | +44.82% | 344105.540858 | 498350.614834 | [1](#1-ffcd1c5442---trust-single-edge-synthetic-profile-116054) |
| GuardedDevirtualization.TwoClassInterface.Call(testInput: pB = 0.10) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/GuardedDevirtualization.TwoClassInterface.Call%28testInput%3A%20pB%20%3D%200.10%29.html) | +49.78% | 0.893733 | 1.338633 | None |
| System.Linq.Tests.Perf_Enumerable.SingleWithPredicate_LastElementMatches(input: Array) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/System.Linq.Tests.Perf_Enumerable.SingleWithPredicate_LastElementMatches%28input%3A%20Array%29.html) | +37.25% | 78.807073 | 108.166392 | None |
| System.Collections.CtorFromCollectionNonGeneric<Int32>.Hashtable(Size: 512) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/System.Collections.CtorFromCollectionNonGeneric%28Int32%29.Hashtable%28Size%3A%20512%29.html) | +30.85% | 20481.277753 | 26798.934538 | [1](#1-ffcd1c5442---trust-single-edge-synthetic-profile-116054) |
| System.Linq.Tests.Perf_Enumerable.Range | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/System.Linq.Tests.Perf_Enumerable.Range.html) | +30.78% | 183.824038 | 240.404896 | [6](#6-1c10ceecbf---jit-add-3-opt-implementation-for-improving-upon-rpo-based-block-layout-103450) |
| System.Text.RegularExpressions.Tests.Perf_Regex_Industry_Leipzig.Count(Pattern: ".{2,4}(Tom\|Sawyer\|Huckleberry\|Finn)", Options: NonBacktracking) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/System.Text.RegularExpressions.Tests.Perf_Regex_Industry_Leipzig.Count%28Pattern%3A%20%22.%7B2%2C4%7D%28Tom%7CSawyer%7CHuckleberry%7CFinn%29%22%2C%20Options%3A%20NonBacktracking%29.html) | +27.11% | 61615057.142857 | 78321169.642857 | None |
| System.Linq.Tests.Perf_OrderBy.OrderByCustomComparer(NumberOfPeople: 512) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/System.Linq.Tests.Perf_OrderBy.OrderByCustomComparer%28NumberOfPeople%3A%20512%29.html) | +24.61% | 59394.573975 | 74013.716343 | [5](#5-6d12a304b3---jit-do-greedy-4-opt-for-backward-jumps-in-3-opt-layout-110277) |
| System.Memory.Span<Int32>.IndexOfAnyTwoValues(Size: 33) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/System.Memory.Span%28Int32%29.IndexOfAnyTwoValues%28Size%3A%2033%29.html) | +17.27% | 12.338027 | 14.468909 | None |
| System.Linq.Tests.Perf_Enumerable.OrderByThenBy(input: IEnumerable) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/System.Linq.Tests.Perf_Enumerable.OrderByThenBy%28input%3A%20IEnumerable%29.html) | +15.34% | 2536.636583 | 2925.648552 | None |
| System.Perf_Convert.FromBase64Chars | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/System.Perf_Convert.FromBase64Chars.html) | +15.43% | 79.029134 | 91.224871 | None |
| GuardedDevirtualization.TwoClassInterface.Call(testInput: pB = 0.20) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/GuardedDevirtualization.TwoClassInterface.Call%28testInput%3A%20pB%20%3D%200.20%29.html) | +14.49% | 1.248690 | 1.429590 | None |
| System.Perf_Convert.FromBase64String | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/System.Perf_Convert.FromBase64String.html) | +13.58% | 78.858261 | 89.565927 | None |
| System.Memory.Span<Int32>.IndexOfAnyTwoValues(Size: 512) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/System.Memory.Span%28Int32%29.IndexOfAnyTwoValues%28Size%3A%20512%29.html) | +11.92% | 164.851249 | 184.502626 | None |
| System.Linq.Tests.Perf_Enumerable.WhereSingleOrDefault_LastElementMatches(input: Array) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/System.Linq.Tests.Perf_Enumerable.WhereSingleOrDefault_LastElementMatches%28input%3A%20Array%29.html) | +12.57% | 164.860808 | 185.582252 | None |
| System.Tests.Perf_UInt64.TryFormat(value: 0) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/System.Tests.Perf_UInt64.TryFormat%28value%3A%200%29.html) | +8.30% | 4.061976 | 4.399071 | None |
| ByteMark.BenchNumericSortJagged | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/ByteMark.BenchNumericSortJagged.html) | +10.71% | 1420763260.714286 | 1572935039.285714 | None |
| System.Text.RegularExpressions.Tests.Perf_Regex_Industry_Leipzig.Count(Pattern: ".{0,2}(Tom\|Sawyer\|Huckleberry\|Finn)", Options: NonBacktracking) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/System.Text.RegularExpressions.Tests.Perf_Regex_Industry_Leipzig.Count%28Pattern%3A%20%22.%7B0%2C2%7D%28Tom%7CSawyer%7CHuckleberry%7CFinn%29%22%2C%20Options%3A%20NonBacktracking%29.html) | +27.30% | 59972611.309524 | 76346657.142857 | None |
| System.Linq.Tests.Perf_Enumerable.WhereSelect(input: List) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/System.Linq.Tests.Perf_Enumerable.WhereSelect%28input%3A%20List%29.html) | +9.23% | 415.086376 | 453.379708 | None |
| MicroBenchmarks.Serializers.Json_ToStream<LoginViewModel>.DataContractJsonSerializer_ | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/MicroBenchmarks.Serializers.Json_ToStream%28LoginViewModel%29.DataContractJsonSerializer_.html) | +9.95% | 754.752280 | 829.833201 | None |
| System.Perf_Convert.ToBase64String(formattingOptions: InsertLineBreaks) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/System.Perf_Convert.ToBase64String%28formattingOptions%3A%20InsertLineBreaks%29.html) | +6.95% | 1905.849612 | 2038.364901 | [7](#7-34545d790e---jit-dont-mark-callees-noinline-for-non-fatal-observations-with-pgo-114821) |
| System.Memory.Slice<String>.ReadOnlyMemorySpanStartLength | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/System.Memory.Slice%28String%29.ReadOnlyMemorySpanStartLength.html) | +8.87% | 2.807280 | 3.056173 | None |
| System.Buffers.Tests.RentReturnArrayPoolTests<Object>.SingleSerial(RentalSize: 4096, ManipulateArray: False, Async: False, UseSharedPool: False) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/System.Buffers.Tests.RentReturnArrayPoolTests%28Object%29.SingleSerial%28RentalSize%3A%204096%2C%20ManipulateArray%3A%20False%2C%20Async%3A%20False%2C%20UseSharedPool%3A%20False%29.html) | +6.71% | 37.282017 | 39.784410 | None |
| System.Text.RegularExpressions.Tests.Perf_Regex_Industry_BoostDocs_Simple.IsMatch(Id: 2, Options: NonBacktracking) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/System.Text.RegularExpressions.Tests.Perf_Regex_Industry_BoostDocs_Simple.IsMatch%28Id%3A%202%2C%20Options%3A%20NonBacktracking%29.html) | +8.13% | 153.217207 | 165.677578 | [1](#1-ffcd1c5442---trust-single-edge-synthetic-profile-116054), [5](#5-6d12a304b3---jit-do-greedy-4-opt-for-backward-jumps-in-3-opt-layout-110277) |
| System.Text.RegularExpressions.Tests.Perf_Regex_Industry_RustLang_Sherlock.Count(Pattern: "\\p{L}", Options: NonBacktracking) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/System.Text.RegularExpressions.Tests.Perf_Regex_Industry_RustLang_Sherlock.Count%28Pattern%3A%20%22%5C%5Cp%7BL%7D%22%2C%20Options%3A%20NonBacktracking%29.html) | +5.12% | 25348815.535714 | 26647048.750000 | None |
| System.Memory.Span<Byte>.IndexOfAnyTwoValues(Size: 4) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/System.Memory.Span%28Byte%29.IndexOfAnyTwoValues%28Size%3A%204%29.html) | +5.81% | 5.030917 | 5.323267 | [3](#3-41be5e229b---jit-graph-based-loop-inversion-116017), [53](#53-343b0349fa---jit-fix-edge-likelihoods-in-fgoptimizebranch-113235) |
| Microsoft.Extensions.DependencyInjection.ActivatorUtilitiesBenchmark.CreateInstance_1_WithAttrFirst | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/Microsoft.Extensions.DependencyInjection.ActivatorUtilitiesBenchmark.CreateInstance_1_WithAttrFirst.html) | +10.92% | 68.391585 | 75.856763 | None |

---

## 3. 41be5e229b - JIT: Graph-based loop inversion (#116017)

**Date:** 2025-06-04 14:39:50
**Commit:** [41be5e229b](https://github.com/dotnet/runtime/commit/41be5e229b30fc3e7aaed9361b9db4487c5bb7f8)
**Affected Tests:** 40

| Test Name | Link | Change | .NET 9 | .NET 10 | Other Changepoints |
|-----------|------|--------|--------|-------|--------------------|
| Burgers.Test0 | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/Burgers.Test0.html) | +1164.90% | 303518264.285714 | 3839188166.071428 | None |
| System.Globalization.Tests.StringEquality.Compare_Same_Upper(Count: 1024, Options: (en-US, OrdinalIgnoreCase)) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/System.Globalization.Tests.StringEquality.Compare_Same_Upper%28Count%3A%201024%2C%20Options%3A%20%28en-US%2C%20OrdinalIgnoreCase%29%29.html) | +152.42% | 1097.182800 | 2769.504408 | None |
| Benchstone.BenchI.BenchE.Test | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/Benchstone.BenchI.BenchE.Test.html) | +144.52% | 346168132.142857 | 846445492.857143 | None |
| SciMark2.kernel.benchSparseMult | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/SciMark2.kernel.benchSparseMult.html) | +67.64% | 596219400.000000 | 999489944.642857 | [2](#2-ddf8075a2f---jit-visit-blocks-in-rpo-during-lsra-107927), [52](#52-e9a91f90b8---jit-skip-parameter-register-preferencing-in-osr-methods-112452) |
| Benchstone.BenchI.XposMatrix.Test | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/Benchstone.BenchI.XposMatrix.Test.html) | +55.71% | 17748.288783 | 27635.661970 | None |
| Benchstone.BenchI.AddArray2.Test | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/Benchstone.BenchI.AddArray2.Test.html) | +26.34% | 10121979.092262 | 12788495.461310 | None |
| System.Numerics.Tests.Perf_BigInteger.Equals(arguments: 259 bytes, DiffLastByte) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/System.Numerics.Tests.Perf_BigInteger.Equals%28arguments%3A%20259%20bytes%2C%20DiffLastByte%29.html) | +27.38% | 8.754111 | 11.150855 | [4](#4-b146d7512c---jit-move-loop-inversion-to-after-loop-recognition-115850) |
| JetStream.TimeSeriesSegmentation.MaximizeSchwarzCriterion | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/JetStream.TimeSeriesSegmentation.MaximizeSchwarzCriterion.html) | +28.92% | 68931008.333333 | 88867979.166667 | [13](#13-5cb6a06da6---jit-add-simple-late-layout-pass-107483) |
| System.Collections.Perf_Frozen<NotKnownComparable>.Contains_True(Count: 64) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/System.Collections.Perf_Frozen%28NotKnownComparable%29.Contains_True%28Count%3A%2064%29.html) | +18.71% | 223.922756 | 265.814927 | [5](#5-6d12a304b3---jit-do-greedy-4-opt-for-backward-jumps-in-3-opt-layout-110277) |
| System.Collections.Perf_Frozen<Int16>.TryGetValue_True(Count: 64) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/System.Collections.Perf_Frozen%28Int16%29.TryGetValue_True%28Count%3A%2064%29.html) | +21.66% | 251.420046 | 305.868990 | None |
| System.Collections.Perf_Frozen<Int16>.TryGetValue_True(Count: 512) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/System.Collections.Perf_Frozen%28Int16%29.TryGetValue_True%28Count%3A%20512%29.html) | +22.50% | 2009.641539 | 2461.867657 | None |
| System.Collections.Perf_Frozen<Int16>.Contains_True(Count: 64) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/System.Collections.Perf_Frozen%28Int16%29.Contains_True%28Count%3A%2064%29.html) | +19.81% | 222.588525 | 266.693157 | [5](#5-6d12a304b3---jit-do-greedy-4-opt-for-backward-jumps-in-3-opt-layout-110277) |
| Benchstone.BenchI.BubbleSort2.Test | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/Benchstone.BenchI.BubbleSort2.Test.html) | +26.28% | 51737303.571429 | 65335271.785714 | None |
| System.Collections.Perf_Frozen<Int16>.Contains_True(Count: 512) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/System.Collections.Perf_Frozen%28Int16%29.Contains_True%28Count%3A%20512%29.html) | +20.50% | 1827.220076 | 2201.820127 | [4](#4-b146d7512c---jit-move-loop-inversion-to-after-loop-recognition-115850), [5](#5-6d12a304b3---jit-do-greedy-4-opt-for-backward-jumps-in-3-opt-layout-110277), [25](#25-647b4f5792---jit-avoid-creating-unnecessary-temp-for-indirect-virtual-stub-calls-116875) |
| System.Collections.Perf_Frozen<NotKnownComparable>.TryGetValue_True(Count: 4) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/System.Collections.Perf_Frozen%28NotKnownComparable%29.TryGetValue_True%28Count%3A%204%29.html) | +17.47% | 15.121632 | 17.763100 | [4](#4-b146d7512c---jit-move-loop-inversion-to-after-loop-recognition-115850) |
| System.Collections.Perf_Frozen<NotKnownComparable>.Contains_True(Count: 512) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/System.Collections.Perf_Frozen%28NotKnownComparable%29.Contains_True%28Count%3A%20512%29.html) | +15.19% | 1936.094601 | 2230.171164 | None |
| System.Numerics.Tests.Perf_Vector3.NormalizeBenchmark | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/System.Numerics.Tests.Perf_Vector3.NormalizeBenchmark.html) | +16.78% | 2.827932 | 3.302495 | None |
| System.Collections.Tests.Perf_Dictionary.Clone(Items: 3000) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/System.Collections.Tests.Perf_Dictionary.Clone%28Items%3A%203000%29.html) | +15.93% | 11860.228588 | 13749.916725 | [1](#1-ffcd1c5442---trust-single-edge-synthetic-profile-116054) |
| ByteMark.BenchNeuralJagged | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/ByteMark.BenchNeuralJagged.html) | +16.27% | 683733548.214286 | 795007276.785714 | None |
| System.Tests.Perf_Guid.EqualsOperator | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/System.Tests.Perf_Guid.EqualsOperator.html) | +13.56% | 2.010841 | 2.283570 | None |
| System.Tests.Perf_Version.Ctor3 | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/System.Tests.Perf_Version.Ctor3.html) | +10.72% | 9.102197 | 10.078266 | None |
| System.Buffers.Text.Tests.Utf8FormatterTests.FormatterUInt64(value: 12345) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/System.Buffers.Text.Tests.Utf8FormatterTests.FormatterUInt64%28value%3A%2012345%29.html) | +11.32% | 8.906934 | 9.915216 | [47](#47-4020e05efd---clean-up-in-numberformattingcs-110955) |
| SciMark2.kernel.benchFFT | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/SciMark2.kernel.benchFFT.html) | +12.74% | 758637001.785714 | 855265426.785714 | None |
| System.Buffers.Text.Tests.Utf8ParserTests.TryParseUInt64(value: 18446744073709551615) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/System.Buffers.Text.Tests.Utf8ParserTests.TryParseUInt64%28value%3A%2018446744073709551615%29.html) | +9.45% | 24.381646 | 26.685149 | None |
| System.Collections.ContainsKeyFalse<Int32, Int32>.SortedDictionary(Size: 512) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/System.Collections.ContainsKeyFalse%28Int32%2C%20Int32%29.SortedDictionary%28Size%3A%20512%29.html) | +10.55% | 24827.577159 | 27447.013257 | None |
| System.Collections.TryGetValueTrue<Int32, Int32>.ImmutableSortedDictionary(Size: 512) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/System.Collections.TryGetValueTrue%28Int32%2C%20Int32%29.ImmutableSortedDictionary%28Size%3A%20512%29.html) | +5.21% | 22473.835259 | 23645.628456 | [4](#4-b146d7512c---jit-move-loop-inversion-to-after-loop-recognition-115850) |
| BenchmarksGame.FannkuchRedux_9.RunBench(n: 11, expectedSum: 556355) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/BenchmarksGame.FannkuchRedux_9.RunBench%28n%3A%2011%2C%20expectedSum%3A%20556355%29.html) | +8.45% | 269902880.357143 | 292711453.571429 | None |
| System.Collections.CreateAddAndClear<Int32>.LinkedList(Size: 512) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/System.Collections.CreateAddAndClear%28Int32%29.LinkedList%28Size%3A%20512%29.html) | +9.07% | 7265.154551 | 7923.898824 | None |
| Struct.GSeq.FilterSkipMapSum | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/Struct.GSeq.FilterSkipMapSum.html) | +7.92% | 14579.793369 | 15733.969299 | None |
| System.Numerics.Tests.Perf_Matrix4x4.InequalityOperatorBenchmark | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/System.Numerics.Tests.Perf_Matrix4x4.InequalityOperatorBenchmark.html) | +7.97% | 2.808241 | 3.032034 | None |
| System.Numerics.Tests.Perf_Matrix4x4.EqualityOperatorBenchmark | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/System.Numerics.Tests.Perf_Matrix4x4.EqualityOperatorBenchmark.html) | +7.87% | 2.808650 | 3.029592 | None |
| System.Collections.ContainsKeyTrue<Int32, Int32>.FrozenDictionary(Size: 512) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/System.Collections.ContainsKeyTrue%28Int32%2C%20Int32%29.FrozenDictionary%28Size%3A%20512%29.html) | +8.03% | 1999.159140 | 2159.680516 | None |
| System.Collections.Perf_Frozen<NotKnownComparable>.TryGetValue_True(Count: 64) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/System.Collections.Perf_Frozen%28NotKnownComparable%29.TryGetValue_True%28Count%3A%2064%29.html) | +7.06% | 267.850124 | 286.760475 | None |
| System.Collections.Perf_Frozen<NotKnownComparable>.TryGetValue_True(Count: 512) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/System.Collections.Perf_Frozen%28NotKnownComparable%29.TryGetValue_True%28Count%3A%20512%29.html) | +7.62% | 2146.181046 | 2309.807335 | None |
| System.Collections.CtorFromCollection<Int32>.Dictionary(Size: 512) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/System.Collections.CtorFromCollection%28Int32%29.Dictionary%28Size%3A%20512%29.html) | +7.01% | N/A | N/A | None |
| System.Text.RegularExpressions.Tests.Perf_Regex_Industry_RustLang_Sherlock.Count(Pattern: "Holmes.{0,25}Watson\|Watson.{0,25}Holmes", Options: None) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/System.Text.RegularExpressions.Tests.Perf_Regex_Industry_RustLang_Sherlock.Count%28Pattern%3A%20%22Holmes.%7B0%2C25%7DWatson%7CWatson.%7B0%2C25%7DHolmes%22%2C%20Options%3A%20None%29.html) | +6.86% | N/A | N/A | None |
| System.Tests.Perf_Enum.GetName_Generic_Flags | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/System.Tests.Perf_Enum.GetName_Generic_Flags.html) | +5.42% | 10.856085 | 11.444012 | None |
| System.Memory.Span<Byte>.IndexOfAnyTwoValues(Size: 4) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/System.Memory.Span%28Byte%29.IndexOfAnyTwoValues%28Size%3A%204%29.html) | +5.81% | 5.030917 | 5.323267 | [2](#2-ddf8075a2f---jit-visit-blocks-in-rpo-during-lsra-107927), [53](#53-343b0349fa---jit-fix-edge-likelihoods-in-fgoptimizebranch-113235) |
| System.Numerics.Tests.Perf_VectorOf<Byte>.GreaterThanOrEqualBenchmark | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/System.Numerics.Tests.Perf_VectorOf%28Byte%29.GreaterThanOrEqualBenchmark.html) | +5.10% | 2.011391 | 2.114019 | [15](#15-8b00880aad---jit-shrink-data-section-for-const-vector-loads-114040) |
| System.Collections.Perf_SingleCharFrozenDictionary.TryGetValue_True_FrozenDictionary(Count: 1000) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/System.Collections.Perf_SingleCharFrozenDictionary.TryGetValue_True_FrozenDictionary%28Count%3A%201000%29.html) | +5.42% | 6244.371811 | 6582.595290 | None |

---

## 4. b146d7512c - JIT: Move loop inversion to after loop recognition (#115850)

**Date:** 2025-06-14 17:22:46
**Commit:** [b146d7512c](https://github.com/dotnet/runtime/commit/b146d7512ce67051e127ab48dc2d4f65d30e818f)
**Affected Tests:** 22

| Test Name | Link | Change | .NET 9 | .NET 10 | Other Changepoints |
|-----------|------|--------|--------|-------|--------------------|
| System.Net.Tests.Perf_WebUtility.Decode_NoDecodingRequired | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/System.Net.Tests.Perf_WebUtility.Decode_NoDecodingRequired.html) | +55.94% | 57.972514 | 90.399890 | [1](#1-ffcd1c5442---trust-single-edge-synthetic-profile-116054) |
| Benchstone.BenchI.IniArray.Test | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/Benchstone.BenchI.IniArray.Test.html) | +41.33% | 75125806.547619 | 106172767.261905 | None |
| System.Tests.Perf_Boolean.TryParse(value: "true") | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/System.Tests.Perf_Boolean.TryParse%28value%3A%20%22true%22%29.html) | +30.83% | 3.281270 | 4.292922 | [10](#10-ea43e17c95---jit-run-profile-repair-after-frontend-phases-111915), [20](#20-f9fc62ab41---main-update-dependencies-from-dncenginternaldotnet-optimization-112832) |
| System.Tests.Perf_Boolean.TryParse(value: "TRUE") | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/System.Tests.Perf_Boolean.TryParse%28value%3A%20%22TRUE%22%29.html) | +30.84% | 3.280853 | 4.292728 | [10](#10-ea43e17c95---jit-run-profile-repair-after-frontend-phases-111915), [20](#20-f9fc62ab41---main-update-dependencies-from-dncenginternaldotnet-optimization-112832) |
| System.Numerics.Tests.Perf_BigInteger.Equals(arguments: 259 bytes, DiffLastByte) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/System.Numerics.Tests.Perf_BigInteger.Equals%28arguments%3A%20259%20bytes%2C%20DiffLastByte%29.html) | +27.38% | 8.754111 | 11.150855 | [3](#3-41be5e229b---jit-graph-based-loop-inversion-116017) |
| System.Collections.ContainsFalse<Int32>.Span(Size: 512) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/System.Collections.ContainsFalse%28Int32%29.Span%28Size%3A%20512%29.html) | +27.35% | 21864.979248 | 27845.941860 | None |
| System.Collections.Perf_Frozen<Int16>.Contains_True(Count: 512) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/System.Collections.Perf_Frozen%28Int16%29.Contains_True%28Count%3A%20512%29.html) | +20.50% | 1827.220076 | 2201.820127 | [3](#3-41be5e229b---jit-graph-based-loop-inversion-116017), [5](#5-6d12a304b3---jit-do-greedy-4-opt-for-backward-jumps-in-3-opt-layout-110277), [25](#25-647b4f5792---jit-avoid-creating-unnecessary-temp-for-indirect-virtual-stub-calls-116875) |
| System.Tests.Perf_Random.NextBytes_span | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/System.Tests.Perf_Random.NextBytes_span.html) | +15.53% | 3250.036349 | 3754.623068 | [1](#1-ffcd1c5442---trust-single-edge-synthetic-profile-116054) |
| System.Tests.Perf_Char.Char_ToLowerInvariant(input: "Hello World!") | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/System.Tests.Perf_Char.Char_ToLowerInvariant%28input%3A%20%22Hello%20World%21%22%29.html) | +17.13% | 10.334773 | 12.105378 | None |
| System.Collections.ContainsFalse<Int32>.ImmutableHashSet(Size: 512) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/System.Collections.ContainsFalse%28Int32%29.ImmutableHashSet%28Size%3A%20512%29.html) | +13.98% | 19382.518694 | 22092.195871 | None |
| System.Collections.Perf_Frozen<NotKnownComparable>.TryGetValue_True(Count: 4) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/System.Collections.Perf_Frozen%28NotKnownComparable%29.TryGetValue_True%28Count%3A%204%29.html) | +17.47% | 15.121632 | 17.763100 | [3](#3-41be5e229b---jit-graph-based-loop-inversion-116017) |
| System.Tests.Perf_Int32.TryParseSpan(value: "12345") | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/System.Tests.Perf_Int32.TryParseSpan%28value%3A%20%2212345%22%29.html) | +14.15% | 13.218957 | 15.089380 | [8](#8-d3e2f5e13a---jit-exclude-bb_unity_weight-scaling-from-basicblockisbbweightcold-116548) |
| System.IO.Tests.BinaryReaderTests.ReadBool | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/System.IO.Tests.BinaryReaderTests.ReadBool.html) | +12.65% | 3.721051 | 4.191616 | [21](#21-dc88476f10---jit-enable-inlining-methods-with-eh-112998) |
| System.Tests.Perf_Int32.TryParse(value: "12345") | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/System.Tests.Perf_Int32.TryParse%28value%3A%20%2212345%22%29.html) | +8.32% | 13.131458 | 14.224362 | [8](#8-d3e2f5e13a---jit-exclude-bb_unity_weight-scaling-from-basicblockisbbweightcold-116548) |
| System.Collections.TryGetValueTrue<Int32, Int32>.ImmutableSortedDictionary(Size: 512) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/System.Collections.TryGetValueTrue%28Int32%2C%20Int32%29.ImmutableSortedDictionary%28Size%3A%20512%29.html) | +5.21% | 22473.835259 | 23645.628456 | [3](#3-41be5e229b---jit-graph-based-loop-inversion-116017) |
| System.Tests.Perf_UInt64.TryParse(value: "12345") | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/System.Tests.Perf_UInt64.TryParse%28value%3A%20%2212345%22%29.html) | +8.38% | N/A | N/A | None |
| Benchstone.MDBenchI.MDMulMatrix.Test | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/Benchstone.MDBenchI.MDMulMatrix.Test.html) | +7.38% | 1177442353.571429 | 1264368491.071428 | [10](#10-ea43e17c95---jit-run-profile-repair-after-frontend-phases-111915), [13](#13-5cb6a06da6---jit-add-simple-late-layout-pass-107483) |
| System.Collections.IterateForEach<Int32>.FrozenDictionary(Size: 512) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/System.Collections.IterateForEach%28Int32%29.FrozenDictionary%28Size%3A%20512%29.html) | +7.23% | N/A | N/A | None |
| Microsoft.Extensions.Configuration.ConfigurationBinderBenchmarks.Get(ConfigurationProvidersCount: 16, KeysCountPerProvider: 20) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/Microsoft.Extensions.Configuration.ConfigurationBinderBenchmarks.Get%28ConfigurationProvidersCount%3A%2016%2C%20KeysCountPerProvider%3A%2020%29.html) | +7.87% | 2383701.470588 | 2571241.999300 | [7](#7-34545d790e---jit-dont-mark-callees-noinline-for-non-fatal-observations-with-pgo-114821) |
| System.Numerics.Tests.Perf_Vector2.TransformNormalByMatrix4x4Benchmark | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/System.Numerics.Tests.Perf_Vector2.TransformNormalByMatrix4x4Benchmark.html) | +5.64% | 2.214541 | 2.339390 | None |
| System.Tests.Perf_UInt32.TryParse(value: "4294967295") | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/System.Tests.Perf_UInt32.TryParse%28value%3A%20%224294967295%22%29.html) | +7.56% | 20.382222 | 21.922824 | None |
| System.Tests.Perf_Int64.TryParseSpan(value: "12345") | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/System.Tests.Perf_Int64.TryParseSpan%28value%3A%20%2212345%22%29.html) | +5.29% | N/A | N/A | [8](#8-d3e2f5e13a---jit-exclude-bb_unity_weight-scaling-from-basicblockisbbweightcold-116548) |

---

## 5. 6d12a304b3 - JIT: Do greedy 4-opt for backward jumps in 3-opt layout (#110277)

**Date:** 2024-12-03 21:25:35
**Commit:** [6d12a304b3](https://github.com/dotnet/runtime/commit/6d12a304b3068f8a9308a1aec4f3b95dd636a693)
**Affected Tests:** 18

| Test Name | Link | Change | .NET 9 | .NET 10 | Other Changepoints |
|-----------|------|--------|--------|-------|--------------------|
| System.Collections.IterateForNonGeneric<String>.ArrayList(Size: 512) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/System.Collections.IterateForNonGeneric%28String%29.ArrayList%28Size%3A%20512%29.html) | +57.42% | 414.761986 | 652.934814 | None |
| System.Collections.IterateForNonGeneric<Int32>.ArrayList(Size: 512) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/System.Collections.IterateForNonGeneric%28Int32%29.ArrayList%28Size%3A%20512%29.html) | +57.45% | 414.727107 | 652.979192 | None |
| Benchmark.GetChildKeysTests.AddChainedConfigurationEmpty | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/Benchmark.GetChildKeysTests.AddChainedConfigurationEmpty.html) | +45.60% | 11834931.547619 | 17231858.758503 | [7](#7-34545d790e---jit-dont-mark-callees-noinline-for-non-fatal-observations-with-pgo-114821), [9](#9-34f1db49db---jit-use-root-compiler-instance-for-sufficient-pgo-observation-115119) |
| System.Buffers.Tests.ReadOnlySequenceTests<Char>.IterateGetPositionArray | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/System.Buffers.Tests.ReadOnlySequenceTests%28Char%29.IterateGetPositionArray.html) | +25.58% | 23.739434 | 29.812534 | None |
| System.Collections.Perf_Frozen<NotKnownComparable>.Contains_True(Count: 64) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/System.Collections.Perf_Frozen%28NotKnownComparable%29.Contains_True%28Count%3A%2064%29.html) | +18.71% | 223.922756 | 265.814927 | [3](#3-41be5e229b---jit-graph-based-loop-inversion-116017) |
| System.Text.Perf_Utf8Encoding.GetByteCount(Input: Cyrillic) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/System.Text.Perf_Utf8Encoding.GetByteCount%28Input%3A%20Cyrillic%29.html) | +24.57% | 10400.021151 | 12955.556356 | None |
| GuardedDevirtualization.TwoClassVirtual.Call(testInput: pB = 0.80) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/GuardedDevirtualization.TwoClassVirtual.Call%28testInput%3A%20pB%20%3D%200.80%29.html) | +24.76% | 1.072963 | 1.338625 | [17](#17-39a31f082e---virtual-stub-indirect-call-profiling-116453) |
| System.Text.Perf_Utf8Encoding.GetByteCount(Input: Greek) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/System.Text.Perf_Utf8Encoding.GetByteCount%28Input%3A%20Greek%29.html) | +13.34% | 13613.014606 | 15429.139459 | None |
| System.Collections.Perf_Frozen<Int16>.Contains_True(Count: 64) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/System.Collections.Perf_Frozen%28Int16%29.Contains_True%28Count%3A%2064%29.html) | +19.81% | 222.588525 | 266.693157 | [3](#3-41be5e229b---jit-graph-based-loop-inversion-116017) |
| System.Collections.Perf_Frozen<Int16>.Contains_True(Count: 512) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/System.Collections.Perf_Frozen%28Int16%29.Contains_True%28Count%3A%20512%29.html) | +20.50% | 1827.220076 | 2201.820127 | [3](#3-41be5e229b---jit-graph-based-loop-inversion-116017), [4](#4-b146d7512c---jit-move-loop-inversion-to-after-loop-recognition-115850), [25](#25-647b4f5792---jit-avoid-creating-unnecessary-temp-for-indirect-virtual-stub-calls-116875) |
| System.Linq.Tests.Perf_OrderBy.OrderByCustomComparer(NumberOfPeople: 512) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/System.Linq.Tests.Perf_OrderBy.OrderByCustomComparer%28NumberOfPeople%3A%20512%29.html) | +24.61% | 59394.573975 | 74013.716343 | [2](#2-ddf8075a2f---jit-visit-blocks-in-rpo-during-lsra-107927) |
| GuardedDevirtualization.TwoClassVirtual.Call(testInput: pB = 0.20) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/GuardedDevirtualization.TwoClassVirtual.Call%28testInput%3A%20pB%20%3D%200.20%29.html) | +18.00% | 1.129382 | 1.332712 | [17](#17-39a31f082e---virtual-stub-indirect-call-profiling-116453), [25](#25-647b4f5792---jit-avoid-creating-unnecessary-temp-for-indirect-virtual-stub-calls-116875) |
| System.Linq.Tests.Perf_Enumerable.OrderByDescending(input: IEnumerable) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/System.Linq.Tests.Perf_Enumerable.OrderByDescending%28input%3A%20IEnumerable%29.html) | +13.77% | 2396.536587 | 2726.617020 | None |
| System.Text.Perf_Utf8Encoding.GetByteCount(Input: Chinese) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/System.Text.Perf_Utf8Encoding.GetByteCount%28Input%3A%20Chinese%29.html) | +18.27% | 12837.549894 | 15183.157397 | None |
| System.Collections.TryGetValueTrue<Int32, Int32>.SortedDictionary(Size: 512) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/System.Collections.TryGetValueTrue%28Int32%2C%20Int32%29.SortedDictionary%28Size%3A%20512%29.html) | +13.72% | 19846.662016 | 22569.486297 | None |
| System.Collections.CreateAddAndClear<Int32>.ICollection(Size: 512) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/System.Collections.CreateAddAndClear%28Int32%29.ICollection%28Size%3A%20512%29.html) | +10.09% | 1203.192912 | 1324.601363 | None |
| System.Buffers.Tests.ReadOnlySequenceTests<Byte>.IterateGetPositionArray | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/System.Buffers.Tests.ReadOnlySequenceTests%28Byte%29.IterateGetPositionArray.html) | +9.95% | 20.451863 | 22.487158 | None |
| System.Text.RegularExpressions.Tests.Perf_Regex_Industry_BoostDocs_Simple.IsMatch(Id: 2, Options: NonBacktracking) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/System.Text.RegularExpressions.Tests.Perf_Regex_Industry_BoostDocs_Simple.IsMatch%28Id%3A%202%2C%20Options%3A%20NonBacktracking%29.html) | +8.13% | 153.217207 | 165.677578 | [1](#1-ffcd1c5442---trust-single-edge-synthetic-profile-116054), [2](#2-ddf8075a2f---jit-visit-blocks-in-rpo-during-lsra-107927) |

---

## 6. 1c10ceecbf - JIT: Add 3-opt implementation for improving upon RPO-based block layout (#103450)

**Date:** 2024-11-04 18:18:38
**Commit:** [1c10ceecbf](https://github.com/dotnet/runtime/commit/1c10ceecbf5356c33c67f6325072d753707f854e)
**Affected Tests:** 17

| Test Name | Link | Change | .NET 9 | .NET 10 | Other Changepoints |
|-----------|------|--------|--------|-------|--------------------|
| Span.Sorting.BubbleSortSpan(Size: 512) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/Span.Sorting.BubbleSortSpan%28Size%3A%20512%29.html) | +56.16% | 212227.260218 | 331405.043971 | [55](#55-48ace183c4---jit-re-introduce-late-fgoptimizebranch-pass-113491) |
| System.Text.RegularExpressions.Tests.Perf_Regex_Industry_RustLang_Sherlock.Count(Pattern: "\\w+\\s+Holmes", Options: NonBacktracking) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/System.Text.RegularExpressions.Tests.Perf_Regex_Industry_RustLang_Sherlock.Count%28Pattern%3A%20%22%5C%5Cw%2B%5C%5Cs%2BHolmes%22%2C%20Options%3A%20NonBacktracking%29.html) | +49.67% | 2481196.183473 | 3713627.433473 | [1](#1-ffcd1c5442---trust-single-edge-synthetic-profile-116054), [8](#8-d3e2f5e13a---jit-exclude-bb_unity_weight-scaling-from-basicblockisbbweightcold-116548) |
| System.Collections.Tests.Perf_Dictionary.ContainsValue(Items: 3000) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/System.Collections.Tests.Perf_Dictionary.ContainsValue%28Items%3A%203000%29.html) | +28.82% | 4382961.365236 | 5646123.799261 | None |
| Benchstone.MDBenchI.MDArray2.Test | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/Benchstone.MDBenchI.MDArray2.Test.html) | +42.29% | 1341308426.785714 | 1908533569.642857 | [10](#10-ea43e17c95---jit-run-profile-repair-after-frontend-phases-111915), [34](#34-c37cfcc645---jit-use-fgcalledcount-in-inlinee-weight-computation-112499) |
| Span.IndexerBench.CoveredIndex3(length: 1024) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/Span.IndexerBench.CoveredIndex3%28length%3A%201024%29.html) | +45.57% | 1138.611554 | 1657.499134 | None |
| System.Linq.Tests.Perf_Enumerable.Range | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/System.Linq.Tests.Perf_Enumerable.Range.html) | +30.78% | 183.824038 | 240.404896 | [2](#2-ddf8075a2f---jit-visit-blocks-in-rpo-during-lsra-107927) |
| System.Globalization.Tests.StringSearch.LastIndexOf_Word_NotFound(Options: (en-US, None, False)) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/System.Globalization.Tests.StringSearch.LastIndexOf_Word_NotFound%28Options%3A%20%28en-US%2C%20None%2C%20False%29%29.html) | +22.72% | 583.228213 | 715.710595 | None |
| System.Globalization.Tests.StringSearch.LastIndexOf_Word_NotFound(Options: (, None, False)) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/System.Globalization.Tests.StringSearch.LastIndexOf_Word_NotFound%28Options%3A%20%28%2C%20None%2C%20False%29%29.html) | +22.35% | 584.733432 | 715.411813 | None |
| System.Globalization.Tests.StringSearch.LastIndexOf_Word_NotFound(Options: (en-US, IgnoreNonSpace, False)) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/System.Globalization.Tests.StringSearch.LastIndexOf_Word_NotFound%28Options%3A%20%28en-US%2C%20IgnoreNonSpace%2C%20False%29%29.html) | +22.59% | 583.902297 | 715.827313 | None |
| System.Tests.Perf_Char.Char_IsLower(input: "Good afternoon, Constable!") | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/System.Tests.Perf_Char.Char_IsLower%28input%3A%20%22Good%20afternoon%2C%20Constable%21%22%29.html) | +18.06% | 22.250899 | 26.268849 | None |
| Span.IndexerBench.CoveredIndex2(length: 1024) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/Span.IndexerBench.CoveredIndex2%28length%3A%201024%29.html) | +18.37% | 827.219825 | 979.153300 | None |
| Benchstone.BenchI.Puzzle.Test | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/Benchstone.BenchI.Puzzle.Test.html) | +8.03% | 407405442.857143 | 440127821.428571 | None |
| System.Collections.IndexerSet<Int32>.SortedDictionary(Size: 512) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/System.Collections.IndexerSet%28Int32%29.SortedDictionary%28Size%3A%20512%29.html) | +15.29% | 20842.574711 | 24029.640304 | None |
| Struct.FilteredSpanEnumerator.Sum | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/Struct.FilteredSpanEnumerator.Sum.html) | +12.37% | 6260.211559 | 7034.765805 | None |
| System.Buffers.Text.Tests.Utf8FormatterTests.FormatterUInt32(value: 0) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/System.Buffers.Text.Tests.Utf8FormatterTests.FormatterUInt32%28value%3A%200%29.html) | +12.65% | 5.105344 | 5.750995 | None |
| System.Text.RegularExpressions.Tests.Perf_Regex_Industry_RustLang_Sherlock.Count(Pattern: "(?i)Sherlock\|Holmes\|Watson\|Irene\|Adler\|John\|Baker", Options: None) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/System.Text.RegularExpressions.Tests.Perf_Regex_Industry_RustLang_Sherlock.Count%28Pattern%3A%20%22%28%3Fi%29Sherlock%7CHolmes%7CWatson%7CIrene%7CAdler%7CJohn%7CBaker%22%2C%20Options%3A%20None%29.html) | +9.43% | 27055817.658730 | 29606272.817460 | None |
| ByteMark.BenchNumericSortRectangular | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/ByteMark.BenchNumericSortRectangular.html) | +6.76% | 1120886460.000000 | 1196654188.000000 | [50](#50-397863948b---main-update-dependencies-from-dncenginternaldotnet-optimization-111326) |

---

## 7. 34545d790e - JIT: don't mark callees noinline for non-fatal observations with pgo (#114821)

**Date:** 2025-04-21 02:03:19
**Commit:** [34545d790e](https://github.com/dotnet/runtime/commit/34545d790e0f92be34b13f0d41b7df93f04bbe02)
**Affected Tests:** 16

| Test Name | Link | Change | .NET 9 | .NET 10 | Other Changepoints |
|-----------|------|--------|--------|-------|--------------------|
| Benchmark.GetChildKeysTests.AddChainedConfigurationEmpty | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/Benchmark.GetChildKeysTests.AddChainedConfigurationEmpty.html) | +45.60% | 11834931.547619 | 17231858.758503 | [5](#5-6d12a304b3---jit-do-greedy-4-opt-for-backward-jumps-in-3-opt-layout-110277), [9](#9-34f1db49db---jit-use-root-compiler-instance-for-sufficient-pgo-observation-115119) |
| System.Text.Json.Tests.Perf_Basic.WriteBasicUtf8(Formatted: False, SkipValidation: True, DataSize: 100000) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/System.Text.Json.Tests.Perf_Basic.WriteBasicUtf8%28Formatted%3A%20False%2C%20SkipValidation%3A%20True%2C%20DataSize%3A%20100000%29.html) | +18.55% | 1808378.059349 | 2143873.293067 | None |
| System.Buffers.Tests.RentReturnArrayPoolTests<Object>.MultipleSerial(RentalSize: 4096, ManipulateArray: False, Async: False, UseSharedPool: False) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/System.Buffers.Tests.RentReturnArrayPoolTests%28Object%29.MultipleSerial%28RentalSize%3A%204096%2C%20ManipulateArray%3A%20False%2C%20Async%3A%20False%2C%20UseSharedPool%3A%20False%29.html) | +12.94% | 330.124499 | 372.847015 | [9](#9-34f1db49db---jit-use-root-compiler-instance-for-sufficient-pgo-observation-115119) |
| System.Buffers.Tests.RentReturnArrayPoolTests<Byte>.MultipleSerial(RentalSize: 4096, ManipulateArray: False, Async: False, UseSharedPool: False) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/System.Buffers.Tests.RentReturnArrayPoolTests%28Byte%29.MultipleSerial%28RentalSize%3A%204096%2C%20ManipulateArray%3A%20False%2C%20Async%3A%20False%2C%20UseSharedPool%3A%20False%29.html) | +11.48% | 333.130791 | 371.380620 | [9](#9-34f1db49db---jit-use-root-compiler-instance-for-sufficient-pgo-observation-115119) |
| System.Tests.Perf_Double.ToStringWithFormat(value: 1.7976931348623157E+308, format: "F50") | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/System.Tests.Perf_Double.ToStringWithFormat%28value%3A%201.7976931348623157E%2B308%2C%20format%3A%20%22F50%22%29.html) | +10.42% | 27832.451570 | 30731.960698 | None |
| System.Tests.Perf_Array.ArrayAssign2D | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/System.Tests.Perf_Array.ArrayAssign2D.html) | +13.07% | 582381.554573 | 658517.719459 | [31](#31-eb456e6121---move-unboxing-helpers-to-managed-code-109135) |
| Microsoft.Extensions.Configuration.ConfigurationBinderBenchmarks.Get(ConfigurationProvidersCount: 16, KeysCountPerProvider: 10) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/Microsoft.Extensions.Configuration.ConfigurationBinderBenchmarks.Get%28ConfigurationProvidersCount%3A%2016%2C%20KeysCountPerProvider%3A%2010%29.html) | +6.46% | 1051312.786317 | 1119253.948162 | None |
| System.Tests.Perf_Double.ToStringWithFormat(value: -1.7976931348623157E+308, format: "F50") | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/System.Tests.Perf_Double.ToStringWithFormat%28value%3A%20-1.7976931348623157E%2B308%2C%20format%3A%20%22F50%22%29.html) | +10.56% | 27792.453563 | 30726.552535 | None |
| Microsoft.Extensions.Configuration.ConfigurationBinderBenchmarks.Get(ConfigurationProvidersCount: 32, KeysCountPerProvider: 40) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/Microsoft.Extensions.Configuration.ConfigurationBinderBenchmarks.Get%28ConfigurationProvidersCount%3A%2032%2C%20KeysCountPerProvider%3A%2040%29.html) | +7.52% | 23683130.400000 | 25463222.400000 | None |
| Microsoft.Extensions.Configuration.ConfigurationBinderBenchmarks.Get(ConfigurationProvidersCount: 32, KeysCountPerProvider: 10) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/Microsoft.Extensions.Configuration.ConfigurationBinderBenchmarks.Get%28ConfigurationProvidersCount%3A%2032%2C%20KeysCountPerProvider%3A%2010%29.html) | +9.37% | 4673214.319407 | 5110861.725067 | None |
| System.Buffers.Tests.RentReturnArrayPoolTests<Object>.SingleParallel(RentalSize: 4096, ManipulateArray: True, Async: True, UseSharedPool: False) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/System.Buffers.Tests.RentReturnArrayPoolTests%28Object%29.SingleParallel%28RentalSize%3A%204096%2C%20ManipulateArray%3A%20True%2C%20Async%3A%20True%2C%20UseSharedPool%3A%20False%29.html) | +7.26% | 5770.818720 | 6189.554320 | None |
| Microsoft.Extensions.Configuration.ConfigurationBinderBenchmarks.Get(ConfigurationProvidersCount: 8, KeysCountPerProvider: 40) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/Microsoft.Extensions.Configuration.ConfigurationBinderBenchmarks.Get%28ConfigurationProvidersCount%3A%208%2C%20KeysCountPerProvider%3A%2040%29.html) | +5.80% | 1428004.053777 | 1510843.565271 | None |
| Microsoft.Extensions.Configuration.ConfigurationBinderBenchmarks.Get(ConfigurationProvidersCount: 32, KeysCountPerProvider: 20) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/Microsoft.Extensions.Configuration.ConfigurationBinderBenchmarks.Get%28ConfigurationProvidersCount%3A%2032%2C%20KeysCountPerProvider%3A%2020%29.html) | +7.04% | 10671577.913043 | 11422893.043478 | None |
| Microsoft.Extensions.Configuration.ConfigurationBinderBenchmarks.Get(ConfigurationProvidersCount: 8, KeysCountPerProvider: 20) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/Microsoft.Extensions.Configuration.ConfigurationBinderBenchmarks.Get%28ConfigurationProvidersCount%3A%208%2C%20KeysCountPerProvider%3A%2020%29.html) | +7.00% | N/A | N/A | None |
| Microsoft.Extensions.Configuration.ConfigurationBinderBenchmarks.Get(ConfigurationProvidersCount: 16, KeysCountPerProvider: 20) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/Microsoft.Extensions.Configuration.ConfigurationBinderBenchmarks.Get%28ConfigurationProvidersCount%3A%2016%2C%20KeysCountPerProvider%3A%2020%29.html) | +7.87% | 2383701.470588 | 2571241.999300 | [4](#4-b146d7512c---jit-move-loop-inversion-to-after-loop-recognition-115850) |
| System.Perf_Convert.ToBase64String(formattingOptions: InsertLineBreaks) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/System.Perf_Convert.ToBase64String%28formattingOptions%3A%20InsertLineBreaks%29.html) | +6.95% | 1905.849612 | 2038.364901 | [2](#2-ddf8075a2f---jit-visit-blocks-in-rpo-during-lsra-107927) |

---

## 8. d3e2f5e13a - JIT: Exclude `BB_UNITY_WEIGHT` scaling from `BasicBlock::isBBWeightCold` (#116548)

**Date:** 2025-06-13 21:53:16
**Commit:** [d3e2f5e13a](https://github.com/dotnet/runtime/commit/d3e2f5e13aac894737a90ba8494ad57465ba639f)
**Affected Tests:** 12

| Test Name | Link | Change | .NET 9 | .NET 10 | Other Changepoints |
|-----------|------|--------|--------|-------|--------------------|
| System.Linq.Tests.Perf_Enumerable.AnyWithPredicate_LastElementMatches(input: Array) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/System.Linq.Tests.Perf_Enumerable.AnyWithPredicate_LastElementMatches%28input%3A%20Array%29.html) | +102.15% | 53.173735 | 107.490999 | None |
| System.Text.RegularExpressions.Tests.Perf_Regex_Industry_RustLang_Sherlock.Count(Pattern: "\\w+\\s+Holmes", Options: NonBacktracking) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/System.Text.RegularExpressions.Tests.Perf_Regex_Industry_RustLang_Sherlock.Count%28Pattern%3A%20%22%5C%5Cw%2B%5C%5Cs%2BHolmes%22%2C%20Options%3A%20NonBacktracking%29.html) | +49.67% | 2481196.183473 | 3713627.433473 | [1](#1-ffcd1c5442---trust-single-edge-synthetic-profile-116054), [6](#6-1c10ceecbf---jit-add-3-opt-implementation-for-improving-upon-rpo-based-block-layout-103450) |
| System.Text.RegularExpressions.Tests.Perf_Regex_Industry_RustLang_Sherlock.Count(Pattern: "\\w+\\s+Holmes\\s+\\w+", Options: NonBacktracking) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/System.Text.RegularExpressions.Tests.Perf_Regex_Industry_RustLang_Sherlock.Count%28Pattern%3A%20%22%5C%5Cw%2B%5C%5Cs%2BHolmes%5C%5Cs%2B%5C%5Cw%2B%22%2C%20Options%3A%20NonBacktracking%29.html) | +43.71% | 2584973.255337 | 3714866.995074 | None |
| System.Text.RegularExpressions.Tests.Perf_Regex_Industry_RustLang_Sherlock.Count(Pattern: "Holmes.{0,25}Watson\|Watson.{0,25}Holmes", Options: NonBacktracking) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/System.Text.RegularExpressions.Tests.Perf_Regex_Industry_RustLang_Sherlock.Count%28Pattern%3A%20%22Holmes.%7B0%2C25%7DWatson%7CWatson.%7B0%2C25%7DHolmes%22%2C%20Options%3A%20NonBacktracking%29.html) | +19.99% | 147463.720452 | 176947.503037 | None |
| System.Text.RegularExpressions.Tests.Perf_Regex_Industry_RustLang_Sherlock.Count(Pattern: "\\s[a-zA-Z]{0,12}ing\\s", Options: NonBacktracking) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/System.Text.RegularExpressions.Tests.Perf_Regex_Industry_RustLang_Sherlock.Count%28Pattern%3A%20%22%5C%5Cs%5Ba-zA-Z%5D%7B0%2C12%7Ding%5C%5Cs%22%2C%20Options%3A%20NonBacktracking%29.html) | +31.05% | 3029544.836489 | 3970187.241824 | None |
| System.Tests.Perf_Int32.TryParseSpan(value: "12345") | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/System.Tests.Perf_Int32.TryParseSpan%28value%3A%20%2212345%22%29.html) | +14.15% | 13.218957 | 15.089380 | [4](#4-b146d7512c---jit-move-loop-inversion-to-after-loop-recognition-115850) |
| System.Tests.Perf_Int32.TryParse(value: "12345") | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/System.Tests.Perf_Int32.TryParse%28value%3A%20%2212345%22%29.html) | +8.32% | 13.131458 | 14.224362 | [4](#4-b146d7512c---jit-move-loop-inversion-to-after-loop-recognition-115850) |
| System.Numerics.Tests.Perf_BigInteger.GreatestCommonDivisor(arguments: 65536,65536 bits) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/System.Numerics.Tests.Perf_BigInteger.GreatestCommonDivisor%28arguments%3A%2065536%2C65536%20bits%29.html) | +9.51% | 5243606.978723 | 5742492.765957 | None |
| System.Tests.Perf_UInt32.ParseSpan(value: "12345") | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/System.Tests.Perf_UInt32.ParseSpan%28value%3A%20%2212345%22%29.html) | +7.51% | N/A | N/A | None |
| System.Tests.Perf_Enum.GetName_Generic_NonFlags | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/System.Tests.Perf_Enum.GetName_Generic_NonFlags.html) | +7.38% | 7.053900 | 7.574136 | [13](#13-5cb6a06da6---jit-add-simple-late-layout-pass-107483) |
| System.Tests.Perf_String.ToUpperInvariant(s: "TEST") | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/System.Tests.Perf_String.ToUpperInvariant%28s%3A%20%22TEST%22%29.html) | +5.84% | 8.082444 | 8.554518 | None |
| System.Tests.Perf_Int64.TryParseSpan(value: "12345") | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/System.Tests.Perf_Int64.TryParseSpan%28value%3A%20%2212345%22%29.html) | +5.29% | N/A | N/A | [4](#4-b146d7512c---jit-move-loop-inversion-to-after-loop-recognition-115850) |

---

## 9. 34f1db49db - JIT: use root compiler instance for sufficient PGO observation (#115119)

**Date:** 2025-05-19 14:21:16
**Commit:** [34f1db49db](https://github.com/dotnet/runtime/commit/34f1db49dbf702697483ee2809d493f5ef441768)
**Affected Tests:** 11

| Test Name | Link | Change | .NET 9 | .NET 10 | Other Changepoints |
|-----------|------|--------|--------|-------|--------------------|
| Benchmark.GetChildKeysTests.AddChainedConfigurationEmpty | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/Benchmark.GetChildKeysTests.AddChainedConfigurationEmpty.html) | +45.60% | 11834931.547619 | 17231858.758503 | [5](#5-6d12a304b3---jit-do-greedy-4-opt-for-backward-jumps-in-3-opt-layout-110277), [7](#7-34545d790e---jit-dont-mark-callees-noinline-for-non-fatal-observations-with-pgo-114821) |
| System.Globalization.Tests.StringEquality.Compare_DifferentFirstChar(Count: 1024, Options: (en-US, Ordinal)) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/System.Globalization.Tests.StringEquality.Compare_DifferentFirstChar%28Count%3A%201024%2C%20Options%3A%20%28en-US%2C%20Ordinal%29%29.html) | +30.70% | 11.155881 | 14.580874 | None |
| System.Globalization.Tests.StringEquality.Compare_DifferentFirstChar(Count: 1024, Options: (en-US, OrdinalIgnoreCase)) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/System.Globalization.Tests.StringEquality.Compare_DifferentFirstChar%28Count%3A%201024%2C%20Options%3A%20%28en-US%2C%20OrdinalIgnoreCase%29%29.html) | +30.38% | 10.116473 | 13.189728 | None |
| System.Buffers.Tests.NonStandardArrayPoolTests<Byte>.RentNoReturn(RentalSize: 64, UseSharedPool: False) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/System.Buffers.Tests.NonStandardArrayPoolTests%28Byte%29.RentNoReturn%28RentalSize%3A%2064%2C%20UseSharedPool%3A%20False%29.html) | +21.49% | 40.279382 | 48.935916 | None |
| System.Buffers.Tests.RentReturnArrayPoolTests<Byte>.SingleParallel(RentalSize: 4096, ManipulateArray: True, Async: False, UseSharedPool: False) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/System.Buffers.Tests.RentReturnArrayPoolTests%28Byte%29.SingleParallel%28RentalSize%3A%204096%2C%20ManipulateArray%3A%20True%2C%20Async%3A%20False%2C%20UseSharedPool%3A%20False%29.html) | +9.72% | 3308.170179 | 3629.681190 | None |
| System.Buffers.Tests.RentReturnArrayPoolTests<Object>.MultipleSerial(RentalSize: 4096, ManipulateArray: False, Async: False, UseSharedPool: False) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/System.Buffers.Tests.RentReturnArrayPoolTests%28Object%29.MultipleSerial%28RentalSize%3A%204096%2C%20ManipulateArray%3A%20False%2C%20Async%3A%20False%2C%20UseSharedPool%3A%20False%29.html) | +12.94% | 330.124499 | 372.847015 | [7](#7-34545d790e---jit-dont-mark-callees-noinline-for-non-fatal-observations-with-pgo-114821) |
| System.Buffers.Tests.NonStandardArrayPoolTests<Object>.RentNoReturn(RentalSize: 64, UseSharedPool: False) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/System.Buffers.Tests.NonStandardArrayPoolTests%28Object%29.RentNoReturn%28RentalSize%3A%2064%2C%20UseSharedPool%3A%20False%29.html) | +11.86% | 60.207622 | 67.347867 | None |
| System.Collections.CreateAddAndClear<Int32>.IDictionary(Size: 512) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/System.Collections.CreateAddAndClear%28Int32%29.IDictionary%28Size%3A%20512%29.html) | +11.09% | 7716.210773 | 8571.815352 | None |
| System.Buffers.Tests.RentReturnArrayPoolTests<Byte>.MultipleSerial(RentalSize: 4096, ManipulateArray: False, Async: False, UseSharedPool: False) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/System.Buffers.Tests.RentReturnArrayPoolTests%28Byte%29.MultipleSerial%28RentalSize%3A%204096%2C%20ManipulateArray%3A%20False%2C%20Async%3A%20False%2C%20UseSharedPool%3A%20False%29.html) | +11.48% | 333.130791 | 371.380620 | [7](#7-34545d790e---jit-dont-mark-callees-noinline-for-non-fatal-observations-with-pgo-114821) |
| System.Reflection.Attributes.IsDefinedClassHit | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/System.Reflection.Attributes.IsDefinedClassHit.html) | +7.07% | 485.280161 | 519.603678 | [23](#23-052da60b09---remove-_prefast_-and-_prefix_-115355) |
| System.Collections.CreateAddAndClear<Int32>.Dictionary(Size: 512) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/System.Collections.CreateAddAndClear%28Int32%29.Dictionary%28Size%3A%20512%29.html) | +8.43% | 7722.034945 | 8373.218482 | None |

---

## 10. ea43e17c95 - JIT: Run profile repair after frontend phases (#111915)

**Date:** 2025-02-21 16:40:21
**Commit:** [ea43e17c95](https://github.com/dotnet/runtime/commit/ea43e17c953a1230667c684a9f57d241e8a95171)
**Affected Tests:** 10

| Test Name | Link | Change | .NET 9 | .NET 10 | Other Changepoints |
|-----------|------|--------|--------|-------|--------------------|
| Benchstone.MDBenchI.MDArray2.Test | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/Benchstone.MDBenchI.MDArray2.Test.html) | +42.29% | 1341308426.785714 | 1908533569.642857 | [6](#6-1c10ceecbf---jit-add-3-opt-implementation-for-improving-upon-rpo-based-block-layout-103450), [34](#34-c37cfcc645---jit-use-fgcalledcount-in-inlinee-weight-computation-112499) |
| System.Tests.Perf_Boolean.TryParse(value: "true") | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/System.Tests.Perf_Boolean.TryParse%28value%3A%20%22true%22%29.html) | +30.83% | 3.281270 | 4.292922 | [4](#4-b146d7512c---jit-move-loop-inversion-to-after-loop-recognition-115850), [20](#20-f9fc62ab41---main-update-dependencies-from-dncenginternaldotnet-optimization-112832) |
| System.Tests.Perf_Boolean.TryParse(value: "TRUE") | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/System.Tests.Perf_Boolean.TryParse%28value%3A%20%22TRUE%22%29.html) | +30.84% | 3.280853 | 4.292728 | [4](#4-b146d7512c---jit-move-loop-inversion-to-after-loop-recognition-115850), [20](#20-f9fc62ab41---main-update-dependencies-from-dncenginternaldotnet-optimization-112832) |
| Burgers.Test1 | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/Burgers.Test1.html) | +28.54% | 218617725.000000 | 281003542.857143 | None |
| Microsoft.Extensions.Primitives.Performance.StringValuesBenchmark.Indexer_FirstElement_String | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/Microsoft.Extensions.Primitives.Performance.StringValuesBenchmark.Indexer_FirstElement_String.html) | +19.18% | 3.926032 | 4.679058 | None |
| System.Tests.Perf_DateTime.ObjectEquals | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/System.Tests.Perf_DateTime.ObjectEquals.html) | +13.91% | 2.421835 | 2.758660 | None |
| System.IO.Tests.Perf_Path.Combine | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/System.IO.Tests.Perf_Path.Combine.html) | +12.71% | 7.004453 | 7.894476 | [33](#33-845dc11a2d---jit-move-profile-consistency-checks-to-after-morph-111253) |
| Benchstone.MDBenchI.MDMulMatrix.Test | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/Benchstone.MDBenchI.MDMulMatrix.Test.html) | +7.38% | 1177442353.571429 | 1264368491.071428 | [4](#4-b146d7512c---jit-move-loop-inversion-to-after-loop-recognition-115850), [13](#13-5cb6a06da6---jit-add-simple-late-layout-pass-107483) |
| System.Tests.Perf_Enum.InterpolateIntoString(value: 32) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/System.Tests.Perf_Enum.InterpolateIntoString%28value%3A%2032%29.html) | +7.84% | 139.146834 | 150.055648 | [1](#1-ffcd1c5442---trust-single-edge-synthetic-profile-116054) |
| System.Collections.ContainsTrueComparer<Int32>.ImmutableSortedSet(Size: 512) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/System.Collections.ContainsTrueComparer%28Int32%29.ImmutableSortedSet%28Size%3A%20512%29.html) | +5.34% | 22410.912109 | 23606.723493 | None |

---

## 11. 6f221b41da - Ensure that math calls into the CRT are tracked as needing vzeroupper (#112011)

**Date:** 2025-02-01 19:06:23
**Commit:** [6f221b41da](https://github.com/dotnet/runtime/commit/6f221b41da8b4fbd09dcf7ac4b796ff3c86cbeb9)
**Affected Tests:** 7

| Test Name | Link | Change | .NET 9 | .NET 10 | Other Changepoints |
|-----------|------|--------|--------|-------|--------------------|
| System.MathBenchmarks.Single.AcosPi | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/System.MathBenchmarks.Single.AcosPi.html) | +13.68% | 45652.995456 | 51899.327700 | None |
| System.MathBenchmarks.Double.AtanPi | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/System.MathBenchmarks.Double.AtanPi.html) | +13.04% | 50140.840137 | 56679.084307 | None |
| System.MathBenchmarks.Double.AcosPi | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/System.MathBenchmarks.Double.AcosPi.html) | +9.77% | 63493.177202 | 69693.297791 | None |
| System.Numerics.Tensors.Tests.Perf_FloatingPointTensorPrimitives<Double>.Pow_ScalarBase(BufferLength: 128) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/System.Numerics.Tensors.Tests.Perf_FloatingPointTensorPrimitives%28Double%29.Pow_ScalarBase%28BufferLength%3A%20128%29.html) | +8.84% | 813.278614 | 885.195427 | None |
| System.MathBenchmarks.Single.AsinPi | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/System.MathBenchmarks.Single.AsinPi.html) | +6.48% | 45983.272879 | 48964.438477 | None |
| System.Numerics.Tensors.Tests.Perf_FloatingPointTensorPrimitives<Single>.AtanPi(BufferLength: 3079) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/System.Numerics.Tensors.Tests.Perf_FloatingPointTensorPrimitives%28Single%29.AtanPi%28BufferLength%3A%203079%29.html) | +6.22% | N/A | N/A | None |
| System.Numerics.Tensors.Tests.Perf_FloatingPointTensorPrimitives<Double>.AtanPi(BufferLength: 128) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/System.Numerics.Tensors.Tests.Perf_FloatingPointTensorPrimitives%28Double%29.AtanPi%28BufferLength%3A%20128%29.html) | +5.10% | 1285.963796 | 1351.520558 | None |

---

## 12. 1b5c48dc59 - Upgrade vendored Brotli dependency to v1.1.0 (#106994)

**Date:** 2024-08-28 18:34:28
**Commit:** [1b5c48dc59](https://github.com/dotnet/runtime/commit/1b5c48dc5958e20b4aa0f4cbfc21fddb8f81052c)
**Affected Tests:** 6

| Test Name | Link | Change | .NET 9 | .NET 10 | Other Changepoints |
|-----------|------|--------|--------|-------|--------------------|
| System.IO.Compression.Brotli.Decompress_WithoutState(level: Optimal, file: "TestDocument.pdf") | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/System.IO.Compression.Brotli.Decompress_WithoutState%28level%3A%20Optimal%2C%20file%3A%20%22TestDocument.pdf%22%29.html) | +12.63% | 514434.418262 | 579400.832106 | None |
| System.IO.Compression.Brotli.Decompress_WithState(level: Optimal, file: "TestDocument.pdf") | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/System.IO.Compression.Brotli.Decompress_WithState%28level%3A%20Optimal%2C%20file%3A%20%22TestDocument.pdf%22%29.html) | +12.66% | 514625.047865 | 579777.076583 | None |
| System.IO.Compression.Brotli.Decompress_WithoutState(level: Fastest, file: "TestDocument.pdf") | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/System.IO.Compression.Brotli.Decompress_WithoutState%28level%3A%20Fastest%2C%20file%3A%20%22TestDocument.pdf%22%29.html) | +11.70% | 488157.486669 | 545287.398260 | None |
| System.IO.Compression.Brotli.Decompress_WithState(level: Fastest, file: "TestDocument.pdf") | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/System.IO.Compression.Brotli.Decompress_WithState%28level%3A%20Fastest%2C%20file%3A%20%22TestDocument.pdf%22%29.html) | +11.59% | 489183.088030 | 545885.571289 | None |
| System.IO.Compression.Brotli.Decompress(level: Optimal, file: "TestDocument.pdf") | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/System.IO.Compression.Brotli.Decompress%28level%3A%20Optimal%2C%20file%3A%20%22TestDocument.pdf%22%29.html) | +9.88% | 574874.671162 | 631700.957376 | None |
| System.IO.Compression.Brotli.Decompress(level: Fastest, file: "TestDocument.pdf") | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/System.IO.Compression.Brotli.Decompress%28level%3A%20Fastest%2C%20file%3A%20%22TestDocument.pdf%22%29.html) | +6.04% | 558727.000812 | 592454.399351 | None |

---

## 13. 5cb6a06da6 - JIT: Add simple late layout pass (#107483)

**Date:** 2024-09-10 02:38:23
**Commit:** [5cb6a06da6](https://github.com/dotnet/runtime/commit/5cb6a06da634ee4be4f426711e9c5f66535a78c8)
**Affected Tests:** 5

| Test Name | Link | Change | .NET 9 | .NET 10 | Other Changepoints |
|-----------|------|--------|--------|-------|--------------------|
| BenchmarksGame.ReverseComplement_1.RunBench | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/BenchmarksGame.ReverseComplement_1.RunBench.html) | +37.79% | 474918.025993 | 654375.491199 | None |
| JetStream.TimeSeriesSegmentation.MaximizeSchwarzCriterion | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/JetStream.TimeSeriesSegmentation.MaximizeSchwarzCriterion.html) | +28.92% | 68931008.333333 | 88867979.166667 | [3](#3-41be5e229b---jit-graph-based-loop-inversion-116017) |
| System.Tests.Perf_Int128.TryParse(value: "12345") | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/System.Tests.Perf_Int128.TryParse%28value%3A%20%2212345%22%29.html) | +12.69% | 17.839696 | 20.104318 | None |
| Benchstone.MDBenchI.MDMulMatrix.Test | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/Benchstone.MDBenchI.MDMulMatrix.Test.html) | +7.38% | 1177442353.571429 | 1264368491.071428 | [4](#4-b146d7512c---jit-move-loop-inversion-to-after-loop-recognition-115850), [10](#10-ea43e17c95---jit-run-profile-repair-after-frontend-phases-111915) |
| System.Tests.Perf_Enum.GetName_Generic_NonFlags | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/System.Tests.Perf_Enum.GetName_Generic_NonFlags.html) | +7.38% | 7.053900 | 7.574136 | [8](#8-d3e2f5e13a---jit-exclude-bb_unity_weight-scaling-from-basicblockisbbweightcold-116548) |

---

## 14. 02127c782a - JIT: Don't put cold blocks in RPO during layout (#112448)

**Date:** 2025-02-14 17:16:23
**Commit:** [02127c782a](https://github.com/dotnet/runtime/commit/02127c782adbf0cded3ed0778d4bf694e5e75996)
**Affected Tests:** 5

| Test Name | Link | Change | .NET 9 | .NET 10 | Other Changepoints |
|-----------|------|--------|--------|-------|--------------------|
| System.Numerics.Tests.Perf_BigInteger.Equals(arguments: 259 bytes, Same) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/System.Numerics.Tests.Perf_BigInteger.Equals%28arguments%3A%20259%20bytes%2C%20Same%29.html) | +21.05% | 9.046885 | 10.951312 | [57](#57-33b5215c15---smaller-funclet-prologsepilogs-x64-115284) |
| Microsoft.Extensions.Primitives.StringSegmentBenchmark.Equals_Valid | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/Microsoft.Extensions.Primitives.StringSegmentBenchmark.Equals_Valid.html) | +10.43% | 6.605060 | 7.294275 | None |
| System.Globalization.Tests.StringSearch.IsSuffix_DifferentLastChar(Options: (en-US, IgnoreNonSpace, False)) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/System.Globalization.Tests.StringSearch.IsSuffix_DifferentLastChar%28Options%3A%20%28en-US%2C%20IgnoreNonSpace%2C%20False%29%29.html) | +6.95% | N/A | N/A | None |
| System.Globalization.Tests.StringSearch.IsSuffix_DifferentLastChar(Options: (en-US, None, False)) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/System.Globalization.Tests.StringSearch.IsSuffix_DifferentLastChar%28Options%3A%20%28en-US%2C%20None%2C%20False%29%29.html) | +6.76% | N/A | N/A | None |
| System.Globalization.Tests.StringSearch.IsSuffix_DifferentLastChar(Options: (, None, False)) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/System.Globalization.Tests.StringSearch.IsSuffix_DifferentLastChar%28Options%3A%20%28%2C%20None%2C%20False%29%29.html) | +6.96% | 13.423364 | 14.357651 | None |

---

## 15. 8b00880aad - JIT: Shrink data section for const vector loads (#114040)

**Date:** 2025-05-02 21:04:07
**Commit:** [8b00880aad](https://github.com/dotnet/runtime/commit/8b00880aadf2a5a8e62f2f3e41d5e9c05d64dd58)
**Affected Tests:** 5

| Test Name | Link | Change | .NET 9 | .NET 10 | Other Changepoints |
|-----------|------|--------|--------|-------|--------------------|
| System.Text.Perf_Ascii.EqualsIgnoreCase_ExactlyTheSame_Bytes_Chars(Size: 128) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/System.Text.Perf_Ascii.EqualsIgnoreCase_ExactlyTheSame_Bytes_Chars%28Size%3A%20128%29.html) | +14.25% | 19.133889 | 21.860490 | None |
| System.Text.RegularExpressions.Tests.Perf_Regex_Common.CtorInvoke(Options: IgnoreCase, Compiled) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/System.Text.RegularExpressions.Tests.Perf_Regex_Common.CtorInvoke%28Options%3A%20IgnoreCase%2C%20Compiled%29.html) | +12.94% | 337287.419076 | 380922.283664 | [51](#51-fc8b63c3b3---update-dependencies-from-httpsgithubcomdotnetroslyn-build-2025020510-112224) |
| System.Text.RegularExpressions.Tests.Perf_Regex_Common.CtorInvoke(Options: Compiled) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/System.Text.RegularExpressions.Tests.Perf_Regex_Common.CtorInvoke%28Options%3A%20Compiled%29.html) | +10.14% | 295003.697116 | 324929.031493 | None |
| Benchstone.BenchF.MatInv4.Test | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/Benchstone.BenchF.MatInv4.Test.html) | +8.09% | 2138345.982143 | 2311430.233990 | [34](#34-c37cfcc645---jit-use-fgcalledcount-in-inlinee-weight-computation-112499) |
| System.Numerics.Tests.Perf_VectorOf<Byte>.GreaterThanOrEqualBenchmark | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/System.Numerics.Tests.Perf_VectorOf%28Byte%29.GreaterThanOrEqualBenchmark.html) | +5.10% | 2.011391 | 2.114019 | [3](#3-41be5e229b---jit-graph-based-loop-inversion-116017) |

---

## 16. 30082a461a - JIT: save pgo data in inline context, use it for call optimization (#116241)

**Date:** 2025-06-03 20:09:30
**Commit:** [30082a461a](https://github.com/dotnet/runtime/commit/30082a461a68e3305b507910aba7457bdc98115c)
**Affected Tests:** 5

| Test Name | Link | Change | .NET 9 | .NET 10 | Other Changepoints |
|-----------|------|--------|--------|-------|--------------------|
| System.Tests.Perf_Enum.ToString_Flags(value: Red, Orange, Yellow, Green, Blue) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/System.Tests.Perf_Enum.ToString_Flags%28value%3A%20Red%2C%20Orange%2C%20Yellow%2C%20Green%2C%20Blue%29.html) | +11.99% | 58.137628 | 65.108616 | [54](#54-2ec50bd313---jit-switch-optoptimizelayout-to-pre-layout-optimization-phase-113224) |
| System.Tests.Perf_Array.ArrayAssign3D | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/System.Tests.Perf_Array.ArrayAssign3D.html) | +8.71% | 645752.771577 | 701975.255766 | [31](#31-eb456e6121---move-unboxing-helpers-to-managed-code-109135), [35](#35-c8cb0f87fe---jit-dont-clone-or-unroll-cold-loops-115744) |
| BenchmarksGame.RegexRedux_1.RunBench | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/BenchmarksGame.RegexRedux_1.RunBench.html) | +8.20% | 44962464.081633 | 48648589.642857 | None |
| System.Collections.CtorDefaultSizeNonGeneric.Stack | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/System.Collections.CtorDefaultSizeNonGeneric.Stack.html) | +6.44% | N/A | N/A | None |
| System.Tests.Perf_Enum.ToString_Format_NonFlags(value: 7, format: "G") | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/System.Tests.Perf_Enum.ToString_Format_NonFlags%28value%3A%207%2C%20format%3A%20%22G%22%29.html) | +5.23% | N/A | N/A | [28](#28-1434eeef6c---jit-run-new-block-layout-only-in-backend-107634) |

---

## 17. 39a31f082e - Virtual stub indirect call profiling (#116453)

**Date:** 2025-06-17 00:35:31
**Commit:** [39a31f082e](https://github.com/dotnet/runtime/commit/39a31f082e77fb8893016c30c0858f0e5f8c89ea)
**Affected Tests:** 5

| Test Name | Link | Change | .NET 9 | .NET 10 | Other Changepoints |
|-----------|------|--------|--------|-------|--------------------|
| GuardedDevirtualization.TwoClassVirtual.Call(testInput: pB = 0.10) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/GuardedDevirtualization.TwoClassVirtual.Call%28testInput%3A%20pB%20%3D%200.10%29.html) | +61.24% | 0.816818 | 1.317040 | [2](#2-ddf8075a2f---jit-visit-blocks-in-rpo-during-lsra-107927) |
| System.Collections.ContainsFalse<String>.ICollection(Size: 512) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/System.Collections.ContainsFalse%28String%29.ICollection%28Size%3A%20512%29.html) | +21.32% | 342799.077463 | 415884.455382 | [1](#1-ffcd1c5442---trust-single-edge-synthetic-profile-116054) |
| GuardedDevirtualization.TwoClassVirtual.Call(testInput: pB = 0.80) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/GuardedDevirtualization.TwoClassVirtual.Call%28testInput%3A%20pB%20%3D%200.80%29.html) | +24.76% | 1.072963 | 1.338625 | [5](#5-6d12a304b3---jit-do-greedy-4-opt-for-backward-jumps-in-3-opt-layout-110277) |
| GuardedDevirtualization.TwoClassVirtual.Call(testInput: pB = 0.20) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/GuardedDevirtualization.TwoClassVirtual.Call%28testInput%3A%20pB%20%3D%200.20%29.html) | +18.00% | 1.129382 | 1.332712 | [5](#5-6d12a304b3---jit-do-greedy-4-opt-for-backward-jumps-in-3-opt-layout-110277), [25](#25-647b4f5792---jit-avoid-creating-unnecessary-temp-for-indirect-virtual-stub-calls-116875) |
| System.Text.Json.Serialization.Tests.ReadJson<ImmutableSortedDictionary<String, String>>.DeserializeFromStream(Mode: Reflection) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/System.Text.Json.Serialization.Tests.ReadJson%28ImmutableSortedDictionary%28String%2C%20String%29%29.DeserializeFromStream%28Mode%3A%20Reflection%29.html) | -5.62% | 71509.979193 | 67490.877081 | None |

---

## 18. cf7a7444c2 - Replace use of target dependent `TestZ` intrinsic (#104488)

**Date:** 2024-11-27 01:41:48
**Commit:** [cf7a7444c2](https://github.com/dotnet/runtime/commit/cf7a7444c255e0400f1ab078f85d8e3ad746bfb1)
**Affected Tests:** 4

| Test Name | Link | Change | .NET 9 | .NET 10 | Other Changepoints |
|-----------|------|--------|--------|-------|--------------------|
| System.Text.Perf_Utf8Encoding.GetByteCount(Input: EnglishAllAscii) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/System.Text.Perf_Utf8Encoding.GetByteCount%28Input%3A%20EnglishAllAscii%29.html) | +32.55% | 6958.242653 | 9223.157562 | None |
| System.Text.Tests.Perf_Encoding.GetByteCount(size: 512, encName: "utf-8") | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/System.Text.Tests.Perf_Encoding.GetByteCount%28size%3A%20512%2C%20encName%3A%20%22utf-8%22%29.html) | +19.72% | 25.394609 | 30.401743 | None |
| System.IO.Tests.BinaryWriterExtendedTests.WriteAsciiString(StringLengthInChars: 2000000) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/System.IO.Tests.BinaryWriterExtendedTests.WriteAsciiString%28StringLengthInChars%3A%202000000%29.html) | +13.33% | 207764.382906 | 235458.267088 | None |
| System.Text.Tests.Perf_Encoding.GetBytes(size: 512, encName: "utf-8") | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/System.Text.Tests.Perf_Encoding.GetBytes%28size%3A%20512%2C%20encName%3A%20%22utf-8%22%29.html) | +6.97% | N/A | N/A | None |

---

## 19. 75b550d7d3 - Implement WriteStringValueSegment defined in Issue 67337 (#101356)

**Date:** 2024-12-26 21:20:31
**Commit:** [75b550d7d3](https://github.com/dotnet/runtime/commit/75b550d7d3b5b27a74b5bff9c1cb09c42f4fb3ab)
**Affected Tests:** 4

| Test Name | Link | Change | .NET 9 | .NET 10 | Other Changepoints |
|-----------|------|--------|--------|-------|--------------------|
| System.Text.Json.Tests.Perf_Ctor.Ctor(Formatted: False, SkipValidation: False) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/System.Text.Json.Tests.Perf_Ctor.Ctor%28Formatted%3A%20False%2C%20SkipValidation%3A%20False%29.html) | +5.70% | 17.511885 | 18.510652 | None |
| System.Text.Json.Tests.Perf_Ctor.Ctor(Formatted: False, SkipValidation: True) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/System.Text.Json.Tests.Perf_Ctor.Ctor%28Formatted%3A%20False%2C%20SkipValidation%3A%20True%29.html) | +6.41% | 17.405632 | 18.521971 | None |
| System.Text.Json.Tests.Perf_Ctor.Ctor(Formatted: True, SkipValidation: True) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/System.Text.Json.Tests.Perf_Ctor.Ctor%28Formatted%3A%20True%2C%20SkipValidation%3A%20True%29.html) | +6.22% | 17.392169 | 18.474245 | None |
| System.Text.Json.Tests.Perf_Ctor.Ctor(Formatted: True, SkipValidation: False) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/System.Text.Json.Tests.Perf_Ctor.Ctor%28Formatted%3A%20True%2C%20SkipValidation%3A%20False%29.html) | +6.14% | 17.391817 | 18.460134 | None |

---

## 20. f9fc62ab41 - [main] Update dependencies from dnceng/internal/dotnet-optimization (#112832)

**Date:** 2025-03-26 11:50:18
**Commit:** [f9fc62ab41](https://github.com/dotnet/runtime/commit/f9fc62ab41d53d331544b4da5a187b036df8c1bb)
**Affected Tests:** 4

| Test Name | Link | Change | .NET 9 | .NET 10 | Other Changepoints |
|-----------|------|--------|--------|-------|--------------------|
| System.Tests.Perf_Boolean.TryParse(value: "true") | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/System.Tests.Perf_Boolean.TryParse%28value%3A%20%22true%22%29.html) | +30.83% | 3.281270 | 4.292922 | [4](#4-b146d7512c---jit-move-loop-inversion-to-after-loop-recognition-115850), [10](#10-ea43e17c95---jit-run-profile-repair-after-frontend-phases-111915) |
| System.Tests.Perf_Boolean.TryParse(value: "TRUE") | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/System.Tests.Perf_Boolean.TryParse%28value%3A%20%22TRUE%22%29.html) | +30.84% | 3.280853 | 4.292728 | [4](#4-b146d7512c---jit-move-loop-inversion-to-after-loop-recognition-115850), [10](#10-ea43e17c95---jit-run-profile-repair-after-frontend-phases-111915) |
| System.Numerics.Tensors.Tests.Perf_NumberTensorPrimitives<Int32>.AddMultiply_Vectors(BufferLength: 3079) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/System.Numerics.Tensors.Tests.Perf_NumberTensorPrimitives%28Int32%29.AddMultiply_Vectors%28BufferLength%3A%203079%29.html) | +24.47% | 600.821622 | 747.825796 | None |
| System.Tests.Perf_Array.ArrayCreate3D | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/System.Tests.Perf_Array.ArrayCreate3D.html) | +5.69% | 669.357747 | 707.445998 | None |

---

## 21. dc88476f10 - JIT: enable inlining methods with EH (#112998)

**Date:** 2025-03-20 19:52:01
**Commit:** [dc88476f10](https://github.com/dotnet/runtime/commit/dc88476f102123edebd6b2d2efe5a56146f60094)
**Affected Tests:** 3

| Test Name | Link | Change | .NET 9 | .NET 10 | Other Changepoints |
|-----------|------|--------|--------|-------|--------------------|
| System.Collections.IterateForEach<Int32>.ConcurrentBag(Size: 512) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/System.Collections.IterateForEach%28Int32%29.ConcurrentBag%28Size%3A%20512%29.html) | +14.37% | 1449.769935 | 1658.088605 | None |
| System.IO.Tests.BinaryReaderTests.ReadBool | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/System.IO.Tests.BinaryReaderTests.ReadBool.html) | +12.65% | 3.721051 | 4.191616 | [4](#4-b146d7512c---jit-move-loop-inversion-to-after-loop-recognition-115850) |
| System.Collections.IterateForEach<Int32>.ConcurrentQueue(Size: 512) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/System.Collections.IterateForEach%28Int32%29.ConcurrentQueue%28Size%3A%20512%29.html) | +9.62% | 2491.044444 | 2730.650303 | None |

---

## 22. 0ac2caf41a - Tar: Adjust the way we write GNU longlink and longpath metadata (#114940)

**Date:** 2025-04-24 15:49:11
**Commit:** [0ac2caf41a](https://github.com/dotnet/runtime/commit/0ac2caf41a88c56a287ab790e92eaf3ccf846fc8)
**Affected Tests:** 3

| Test Name | Link | Change | .NET 9 | .NET 10 | Other Changepoints |
|-----------|------|--------|--------|-------|--------------------|
| System.Formats.Tar.Tests.Perf_TarWriter.UstarTarEntry_WriteEntry | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/System.Formats.Tar.Tests.Perf_TarWriter.UstarTarEntry_WriteEntry.html) | +20.76% | 232.498987 | 280.755205 | None |
| System.Formats.Tar.Tests.Perf_TarWriter.V7TarEntry_WriteEntry | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/System.Formats.Tar.Tests.Perf_TarWriter.V7TarEntry_WriteEntry.html) | +14.49% | 216.357063 | 247.716343 | None |
| System.Formats.Tar.Tests.Perf_TarWriter.UstarTarEntry_WriteEntry_Async | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/System.Formats.Tar.Tests.Perf_TarWriter.UstarTarEntry_WriteEntry_Async.html) | +8.88% | 359.008981 | 390.897648 | None |

---

## 23. 052da60b09 - Remove `_PREFAST_` and `_PREFIX_` (#115355)

**Date:** 2025-05-08 03:11:46
**Commit:** [052da60b09](https://github.com/dotnet/runtime/commit/052da60b09439b1b82dd931fa40a46e2b089eadc)
**Affected Tests:** 3

| Test Name | Link | Change | .NET 9 | .NET 10 | Other Changepoints |
|-----------|------|--------|--------|-------|--------------------|
| System.Reflection.Invoke.Field_SetStatic_struct | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/System.Reflection.Invoke.Field_SetStatic_struct.html) | +10.84% | 56.333137 | 62.437013 | [26](#26-a38ab4c0bc---remove-helpermethodframes-hmf-from-reflection-code-paths-108415) |
| System.Reflection.Attributes.IsDefinedClassHit | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/System.Reflection.Attributes.IsDefinedClassHit.html) | +7.07% | 485.280161 | 519.603678 | [9](#9-34f1db49db---jit-use-root-compiler-instance-for-sufficient-pgo-observation-115119) |
| System.Reflection.Attributes.IsDefinedMethodOverrideMissInherit | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/System.Reflection.Attributes.IsDefinedMethodOverrideMissInherit.html) | +8.76% | 539.450844 | 586.690023 | None |

---

## 24. b0d68f75f9 - JIT: Use flowgraph annotations to scale loop blocks in `optSetBlockWeights` (#116120)

**Date:** 2025-05-30 21:16:06
**Commit:** [b0d68f75f9](https://github.com/dotnet/runtime/commit/b0d68f75f992d5af58b4c3ad12e5b8a7c3d6c2a0)
**Affected Tests:** 3

| Test Name | Link | Change | .NET 9 | .NET 10 | Other Changepoints |
|-----------|------|--------|--------|-------|--------------------|
| System.Buffers.Text.Tests.Utf8FormatterTests.FormatterUInt32(value: 4294967295) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/System.Buffers.Text.Tests.Utf8FormatterTests.FormatterUInt32%28value%3A%204294967295%29.html) | +29.08% | 10.507694 | 13.563078 | [44](#44-cdfafde684---main-update-dependencies-from-dncenginternaldotnet-optimization-110308) |
| System.Buffers.Text.Tests.Utf8FormatterTests.FormatterUInt32(value: 12345) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/System.Buffers.Text.Tests.Utf8FormatterTests.FormatterUInt32%28value%3A%2012345%29.html) | +12.17% | 8.086258 | 9.070266 | None |
| System.Collections.CreateAddAndClear<String>.HashSet(Size: 512) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/System.Collections.CreateAddAndClear%28String%29.HashSet%28Size%3A%20512%29.html) | +10.67% | 15327.638269 | 16962.461801 | None |

---

## 25. 647b4f5792 - JIT: avoid creating unnecessary temp for indirect virtual stub calls (#116875)

**Date:** 2025-06-20 22:20:55
**Commit:** [647b4f5792](https://github.com/dotnet/runtime/commit/647b4f5792444c431aca0144ee9144f10d3f7d71)
**Affected Tests:** 3

| Test Name | Link | Change | .NET 9 | .NET 10 | Other Changepoints |
|-----------|------|--------|--------|-------|--------------------|
| System.Collections.Perf_Frozen<Int16>.Contains_True(Count: 512) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/System.Collections.Perf_Frozen%28Int16%29.Contains_True%28Count%3A%20512%29.html) | +20.50% | 1827.220076 | 2201.820127 | [3](#3-41be5e229b---jit-graph-based-loop-inversion-116017), [4](#4-b146d7512c---jit-move-loop-inversion-to-after-loop-recognition-115850), [5](#5-6d12a304b3---jit-do-greedy-4-opt-for-backward-jumps-in-3-opt-layout-110277) |
| SeekUnroll.Test(boxedIndex: 11) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/SeekUnroll.Test%28boxedIndex%3A%2011%29.html) | +20.06% | 1559736482.142857 | 1872685414.285714 | None |
| GuardedDevirtualization.TwoClassVirtual.Call(testInput: pB = 0.20) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/GuardedDevirtualization.TwoClassVirtual.Call%28testInput%3A%20pB%20%3D%200.20%29.html) | +18.00% | 1.129382 | 1.332712 | [5](#5-6d12a304b3---jit-do-greedy-4-opt-for-backward-jumps-in-3-opt-layout-110277), [17](#17-39a31f082e---virtual-stub-indirect-call-profiling-116453) |

---

## 26. a38ab4c0bc - Remove HelperMethodFrames (HMF) from Reflection code paths (#108415)

**Date:** 2024-10-03 03:53:57
**Commit:** [a38ab4c0bc](https://github.com/dotnet/runtime/commit/a38ab4c0bc3780754259be600db1501cc2907a84)
**Affected Tests:** 2

| Test Name | Link | Change | .NET 9 | .NET 10 | Other Changepoints |
|-----------|------|--------|--------|-------|--------------------|
| System.Reflection.Invoke.Field_Set_struct | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/System.Reflection.Invoke.Field_Set_struct.html) | +15.98% | 44.157518 | 51.212642 | None |
| System.Reflection.Invoke.Field_SetStatic_struct | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/System.Reflection.Invoke.Field_SetStatic_struct.html) | +10.84% | 56.333137 | 62.437013 | [23](#23-052da60b09---remove-_prefast_-and-_prefix_-115355) |

---

## 27. dedb7d17ac - unblock cloning of loops where the header is a try begin (#108604)

**Date:** 2024-10-15 21:59:26
**Commit:** [dedb7d17ac](https://github.com/dotnet/runtime/commit/dedb7d17aca56e7a4b892475e9cc83d7f077e0fa)
**Affected Tests:** 2

| Test Name | Link | Change | .NET 9 | .NET 10 | Other Changepoints |
|-----------|------|--------|--------|-------|--------------------|
| System.Linq.Tests.Perf_Enumerable.Count(input: IEnumerable) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/System.Linq.Tests.Perf_Enumerable.Count%28input%3A%20IEnumerable%29.html) | +13.24% | 174.144196 | 197.202301 | [38](#38-76146eec5d---jit-move-hotcold-splitting-phase-to-backend-108639) |
| System.Collections.CtorFromCollection<String>.ConcurrentQueue(Size: 512) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/System.Collections.CtorFromCollection%28String%29.ConcurrentQueue%28Size%3A%20512%29.html) | +5.74% | 7498.559868 | 7928.915010 | None |

---

## 28. 1434eeef6c - JIT: Run new block layout only in backend (#107634)

**Date:** 2024-10-21 04:22:46
**Commit:** [1434eeef6c](https://github.com/dotnet/runtime/commit/1434eeef6c9548c8be39cb0bb3aed11808146195)
**Affected Tests:** 2

| Test Name | Link | Change | .NET 9 | .NET 10 | Other Changepoints |
|-----------|------|--------|--------|-------|--------------------|
| System.Text.Json.Tests.Perf_Strings.WriteStringsUtf16(Formatted: True, SkipValidation: True, Escaped: AllEscaped) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/System.Text.Json.Tests.Perf_Strings.WriteStringsUtf16%28Formatted%3A%20True%2C%20SkipValidation%3A%20True%2C%20Escaped%3A%20AllEscaped%29.html) | +10.22% | 71334026.785714 | 78627727.380952 | None |
| System.Tests.Perf_Enum.ToString_Format_NonFlags(value: 7, format: "G") | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/System.Tests.Perf_Enum.ToString_Format_NonFlags%28value%3A%207%2C%20format%3A%20%22G%22%29.html) | +5.23% | N/A | N/A | [16](#16-30082a461a---jit-save-pgo-data-in-inline-context-use-it-for-call-optimization-116241) |

---

## 29. 489a1512f5 - Remove ldsfld quirk (#108606)

**Date:** 2024-10-22 01:12:46
**Commit:** [489a1512f5](https://github.com/dotnet/runtime/commit/489a1512f55961e91e46054f06eaecafb94ce5ee)
**Affected Tests:** 2

| Test Name | Link | Change | .NET 9 | .NET 10 | Other Changepoints |
|-----------|------|--------|--------|-------|--------------------|
| System.Numerics.Tests.Perf_Vector4.TransformVector3ByMatrix4x4Benchmark | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/System.Numerics.Tests.Perf_Vector4.TransformVector3ByMatrix4x4Benchmark.html) | +9.44% | N/A | N/A | None |
| System.Memory.Slice<String>.MemorySpanStartLength | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/System.Memory.Slice%28String%29.MemorySpanStartLength.html) | +7.45% | 3.031079 | 3.256775 | [39](#39-023686e6c2---jit-break-up-try-regions-in-compilerfgmovecoldblocks-and-fix-contiguity-later-108914) |

---

## 30. 1ddfa144d9 - JIT: tail merge returns with multiple statements (#109670)

**Date:** 2024-11-11 15:53:41
**Commit:** [1ddfa144d9](https://github.com/dotnet/runtime/commit/1ddfa144d93c2bc336c8440fe6bfec7fc4af0d44)
**Affected Tests:** 2

| Test Name | Link | Change | .NET 9 | .NET 10 | Other Changepoints |
|-----------|------|--------|--------|-------|--------------------|
| System.Collections.CtorFromCollectionNonGeneric<String>.Hashtable(Size: 512) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/System.Collections.CtorFromCollectionNonGeneric%28String%29.Hashtable%28Size%3A%20512%29.html) | +31.04% | 28913.111303 | 37888.660883 | [1](#1-ffcd1c5442---trust-single-edge-synthetic-profile-116054) |
| System.Tests.Perf_Int128.TryParseSpan(value: "12345") | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/System.Tests.Perf_Int128.TryParseSpan%28value%3A%20%2212345%22%29.html) | +11.27% | 18.347629 | 20.415811 | None |

---

## 31. eb456e6121 - Move unboxing helpers to managed code (#109135)

**Date:** 2024-11-21 17:55:10
**Commit:** [eb456e6121](https://github.com/dotnet/runtime/commit/eb456e6121195405bfb676e3628be2f5db767409)
**Affected Tests:** 2

| Test Name | Link | Change | .NET 9 | .NET 10 | Other Changepoints |
|-----------|------|--------|--------|-------|--------------------|
| System.Tests.Perf_Array.ArrayAssign2D | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/System.Tests.Perf_Array.ArrayAssign2D.html) | +13.07% | 582381.554573 | 658517.719459 | [7](#7-34545d790e---jit-dont-mark-callees-noinline-for-non-fatal-observations-with-pgo-114821) |
| System.Tests.Perf_Array.ArrayAssign3D | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/System.Tests.Perf_Array.ArrayAssign3D.html) | +8.71% | 645752.771577 | 701975.255766 | [16](#16-30082a461a---jit-save-pgo-data-in-inline-context-use-it-for-call-optimization-116241), [35](#35-c8cb0f87fe---jit-dont-clone-or-unroll-cold-loops-115744) |

---

## 32. 373f048bae - [main] Update dependencies from dotnet/arcade (#110477)

**Date:** 2024-12-16 18:49:10
**Commit:** [373f048bae](https://github.com/dotnet/runtime/commit/373f048bae3c46810bc030ed7c1ee0568ee5ecc0)
**Affected Tests:** 2

| Test Name | Link | Change | .NET 9 | .NET 10 | Other Changepoints |
|-----------|------|--------|--------|-------|--------------------|
| System.Collections.ContainsFalse<String>.Array(Size: 512) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/System.Collections.ContainsFalse%28String%29.Array%28Size%3A%20512%29.html) | +46.85% | 594918.396046 | 873660.223214 | None |
| System.Collections.ContainsTrue<String>.Array(Size: 512) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/System.Collections.ContainsTrue%28String%29.Array%28Size%3A%20512%29.html) | +33.97% | 301927.173913 | 404488.306591 | None |

---

## 33. 845dc11a2d - JIT: Move profile consistency checks to after morph (#111253)

**Date:** 2025-01-14 19:54:44
**Commit:** [845dc11a2d](https://github.com/dotnet/runtime/commit/845dc11a2dd779170dc0ca642a339afb883fdb1a)
**Affected Tests:** 2

| Test Name | Link | Change | .NET 9 | .NET 10 | Other Changepoints |
|-----------|------|--------|--------|-------|--------------------|
| System.IO.Tests.Perf_Path.Combine | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/System.IO.Tests.Perf_Path.Combine.html) | +12.71% | 7.004453 | 7.894476 | [10](#10-ea43e17c95---jit-run-profile-repair-after-frontend-phases-111915) |
| System.IO.Tests.BinaryReaderTests.ReadUInt64 | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/System.IO.Tests.BinaryReaderTests.ReadUInt64.html) | +9.25% | 5.900907 | 6.446611 | [45](#45-95814d0f99---main-update-dependencies-from-dncenginternaldotnet-optimization-110904) |

---

## 34. c37cfcc645 - JIT: Use fgCalledCount in inlinee weight computation (#112499)

**Date:** 2025-02-18 17:26:52
**Commit:** [c37cfcc645](https://github.com/dotnet/runtime/commit/c37cfcc6459605e7cd1e1311c6dc74ee087ec08c)
**Affected Tests:** 2

| Test Name | Link | Change | .NET 9 | .NET 10 | Other Changepoints |
|-----------|------|--------|--------|-------|--------------------|
| Benchstone.MDBenchI.MDArray2.Test | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/Benchstone.MDBenchI.MDArray2.Test.html) | +42.29% | 1341308426.785714 | 1908533569.642857 | [6](#6-1c10ceecbf---jit-add-3-opt-implementation-for-improving-upon-rpo-based-block-layout-103450), [10](#10-ea43e17c95---jit-run-profile-repair-after-frontend-phases-111915) |
| Benchstone.BenchF.MatInv4.Test | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/Benchstone.BenchF.MatInv4.Test.html) | +8.09% | 2138345.982143 | 2311430.233990 | [15](#15-8b00880aad---jit-shrink-data-section-for-const-vector-loads-114040) |

---

## 35. c8cb0f87fe - JIT: Don't clone or unroll cold loops (#115744)

**Date:** 2025-05-21 03:33:00
**Commit:** [c8cb0f87fe](https://github.com/dotnet/runtime/commit/c8cb0f87fe8616619462f517fcbe149b55c94354)
**Affected Tests:** 2

| Test Name | Link | Change | .NET 9 | .NET 10 | Other Changepoints |
|-----------|------|--------|--------|-------|--------------------|
| System.Tests.Perf_UInt16.TryParse(value: "0") | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/System.Tests.Perf_UInt16.TryParse%28value%3A%20%220%22%29.html) | +10.44% | 9.049546 | 9.994569 | None |
| System.Tests.Perf_Array.ArrayAssign3D | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/System.Tests.Perf_Array.ArrayAssign3D.html) | +8.71% | 645752.771577 | 701975.255766 | [16](#16-30082a461a---jit-save-pgo-data-in-inline-context-use-it-for-call-optimization-116241), [31](#31-eb456e6121---move-unboxing-helpers-to-managed-code-109135) |

---

## 36. ac7097cb47 - Update dependencies from https://dev.azure.com/dnceng/internal/_git/dotnet-optimization build 20250506.1 (#115464)

**Date:** 2025-06-02 10:40:04
**Commit:** [ac7097cb47](https://github.com/dotnet/runtime/commit/ac7097cb4761d9c71e212bf07e4d916d1571c96b)
**Affected Tests:** 2

| Test Name | Link | Change | .NET 9 | .NET 10 | Other Changepoints |
|-----------|------|--------|--------|-------|--------------------|
| System.Threading.Channels.Tests.BoundedChannelPerfTests.TryWriteThenTryRead | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/System.Threading.Channels.Tests.BoundedChannelPerfTests.TryWriteThenTryRead.html) | +10.08% | 51.325378 | 56.499731 | None |
| System.Numerics.Tests.Perf_BigInteger.Equals(arguments: 67 bytes, DiffLastByte) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/System.Numerics.Tests.Perf_BigInteger.Equals%28arguments%3A%2067%20bytes%2C%20DiffLastByte%29.html) | +10.46% | 5.893115 | 6.509473 | None |

---

## 37. 5428078859 - Remove Helper Method Frames for Exception, GC and Thread methods (#107218)

**Date:** 2024-09-06 21:11:33
**Commit:** [5428078859](https://github.com/dotnet/runtime/commit/54280788590d6012758302d4056aa45720133be2)
**Affected Tests:** 1

| Test Name | Link | Change | .NET 9 | .NET 10 | Other Changepoints |
|-----------|------|--------|--------|-------|--------------------|
| System.Tests.Perf_GC<Byte>.AllocateUninitializedArray(length: 10000, pinned: False) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/System.Tests.Perf_GC%28Byte%29.AllocateUninitializedArray%28length%3A%2010000%2C%20pinned%3A%20False%29.html) | +6.70% | 128.930735 | 137.570091 | None |

---

## 38. 76146eec5d - JIT: Move hot/cold splitting phase to backend (#108639)

**Date:** 2024-10-10 15:40:56
**Commit:** [76146eec5d](https://github.com/dotnet/runtime/commit/76146eec5db3346536c659aea3505c1f4100873f)
**Affected Tests:** 1

| Test Name | Link | Change | .NET 9 | .NET 10 | Other Changepoints |
|-----------|------|--------|--------|-------|--------------------|
| System.Linq.Tests.Perf_Enumerable.Count(input: IEnumerable) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/System.Linq.Tests.Perf_Enumerable.Count%28input%3A%20IEnumerable%29.html) | +13.24% | 174.144196 | 197.202301 | [27](#27-dedb7d17ac---unblock-cloning-of-loops-where-the-header-is-a-try-begin-108604) |

---

## 39. 023686e6c2 - JIT: Break up try regions in `Compiler::fgMoveColdBlocks`, and fix contiguity later (#108914)

**Date:** 2024-10-18 18:59:01
**Commit:** [023686e6c2](https://github.com/dotnet/runtime/commit/023686e6c2b0e6d8161680fff14b0703cd041ca5)
**Affected Tests:** 1

| Test Name | Link | Change | .NET 9 | .NET 10 | Other Changepoints |
|-----------|------|--------|--------|-------|--------------------|
| System.Memory.Slice<String>.MemorySpanStartLength | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/System.Memory.Slice%28String%29.MemorySpanStartLength.html) | +7.45% | 3.031079 | 3.256775 | [29](#29-489a1512f5---remove-ldsfld-quirk-108606) |

---

## 40. 6cea093a3a - Update a few code paths to ensure the trimmer can do its job (#106777)

**Date:** 2024-10-19 13:57:06
**Commit:** [6cea093a3a](https://github.com/dotnet/runtime/commit/6cea093a3a378e9ebd3885e4c14957e738cc2009)
**Affected Tests:** 1

| Test Name | Link | Change | .NET 9 | .NET 10 | Other Changepoints |
|-----------|------|--------|--------|-------|--------------------|
| System.Text.Json.Tests.Perf_Strings.WriteStringsUtf16(Formatted: True, SkipValidation: False, Escaped: AllEscaped) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/System.Text.Json.Tests.Perf_Strings.WriteStringsUtf16%28Formatted%3A%20True%2C%20SkipValidation%3A%20False%2C%20Escaped%3A%20AllEscaped%29.html) | +16.09% | 71001483.928571 | 82422739.285714 | None |

---

## 41. 26612c84ca - Move static helpers to managed (#108167)

**Date:** 2024-10-30 16:54:08
**Commit:** [26612c84ca](https://github.com/dotnet/runtime/commit/26612c84cad2b5a17e3de3484fa0caadb430c7b6)
**Affected Tests:** 1

| Test Name | Link | Change | .NET 9 | .NET 10 | Other Changepoints |
|-----------|------|--------|--------|-------|--------------------|
| System.Buffers.Tests.RentReturnArrayPoolTests<Object>.MultipleSerial(RentalSize: 4096, ManipulateArray: False, Async: False, UseSharedPool: True) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/System.Buffers.Tests.RentReturnArrayPoolTests%28Object%29.MultipleSerial%28RentalSize%3A%204096%2C%20ManipulateArray%3A%20False%2C%20Async%3A%20False%2C%20UseSharedPool%3A%20True%29.html) | +5.39% | N/A | N/A | None |

---

## 42. 54b86f1843 - Remove the rest of the SimdAsHWIntrinsic support (#106594)

**Date:** 2024-10-31 19:46:24
**Commit:** [54b86f1843](https://github.com/dotnet/runtime/commit/54b86f18439397f51fbf4b14f6127a337446f3cf)
**Affected Tests:** 1

| Test Name | Link | Change | .NET 9 | .NET 10 | Other Changepoints |
|-----------|------|--------|--------|-------|--------------------|
| System.Numerics.Tests.Perf_VectorOf<Single>.SquareRootBenchmark | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/System.Numerics.Tests.Perf_VectorOf%28Single%29.SquareRootBenchmark.html) | +343.73% | 2.069841 | 9.184416 | None |

---

## 43. dfb2b8a861 - Update dependencies from dotnet/roslyn (#110105)

**Date:** 2024-12-12 11:25:30
**Commit:** [dfb2b8a861](https://github.com/dotnet/runtime/commit/dfb2b8a861fc97ce90c8f31c886d4d27c5b36f46)
**Affected Tests:** 1

| Test Name | Link | Change | .NET 9 | .NET 10 | Other Changepoints |
|-----------|------|--------|--------|-------|--------------------|
| System.Xml.Linq.Perf_XElementList.Enumerator | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/System.Xml.Linq.Perf_XElementList.Enumerator.html) | +13.26% | 185.776112 | 210.411357 | None |

---

## 44. cdfafde684 - [main] Update dependencies from dnceng/internal/dotnet-optimization (#110308)

**Date:** 2024-12-18 13:24:44
**Commit:** [cdfafde684](https://github.com/dotnet/runtime/commit/cdfafde684f4cf62db38dd0168362f43a15c89c1)
**Affected Tests:** 1

| Test Name | Link | Change | .NET 9 | .NET 10 | Other Changepoints |
|-----------|------|--------|--------|-------|--------------------|
| System.Buffers.Text.Tests.Utf8FormatterTests.FormatterUInt32(value: 4294967295) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/System.Buffers.Text.Tests.Utf8FormatterTests.FormatterUInt32%28value%3A%204294967295%29.html) | +29.08% | 10.507694 | 13.563078 | [24](#24-b0d68f75f9---jit-use-flowgraph-annotations-to-scale-loop-blocks-in-optsetblockweights-116120) |

---

## 45. 95814d0f99 - [main] Update dependencies from dnceng/internal/dotnet-optimization (#110904)

**Date:** 2025-01-02 10:31:10
**Commit:** [95814d0f99](https://github.com/dotnet/runtime/commit/95814d0f99fd876fdc2f6b5b4cf5d5fc94adaea9)
**Affected Tests:** 1

| Test Name | Link | Change | .NET 9 | .NET 10 | Other Changepoints |
|-----------|------|--------|--------|-------|--------------------|
| System.IO.Tests.BinaryReaderTests.ReadUInt64 | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/System.IO.Tests.BinaryReaderTests.ReadUInt64.html) | +9.25% | 5.900907 | 6.446611 | [33](#33-845dc11a2d---jit-move-profile-consistency-checks-to-after-morph-111253) |

---

## 46. aecae2c385 - JIT: Enable profile consistency checking up to morph (#111047)

**Date:** 2025-01-07 17:00:14
**Commit:** [aecae2c385](https://github.com/dotnet/runtime/commit/aecae2c3853ea793ede98906320312ca6c199ec1)
**Affected Tests:** 1

| Test Name | Link | Change | .NET 9 | .NET 10 | Other Changepoints |
|-----------|------|--------|--------|-------|--------------------|
| Microsoft.Extensions.Primitives.Performance.StringValuesBenchmark.Indexer_FirstElement_Array | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/Microsoft.Extensions.Primitives.Performance.StringValuesBenchmark.Indexer_FirstElement_Array.html) | +12.81% | 4.139284 | 4.669323 | None |

---

## 47. 4020e05efd - Clean up in Number.Formatting.cs (#110955)

**Date:** 2025-01-10 19:29:57
**Commit:** [4020e05efd](https://github.com/dotnet/runtime/commit/4020e05efdfcc6b10eab90aeb8a8b5d80f75786f)
**Affected Tests:** 1

| Test Name | Link | Change | .NET 9 | .NET 10 | Other Changepoints |
|-----------|------|--------|--------|-------|--------------------|
| System.Buffers.Text.Tests.Utf8FormatterTests.FormatterUInt64(value: 12345) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/System.Buffers.Text.Tests.Utf8FormatterTests.FormatterUInt64%28value%3A%2012345%29.html) | +11.32% | 8.906934 | 9.915216 | [3](#3-41be5e229b---jit-graph-based-loop-inversion-116017) |

---

## 48. dde00d376e - Optimize redundant sign extensions in assertprop (#111305)

**Date:** 2025-01-16 12:36:23
**Commit:** [dde00d376e](https://github.com/dotnet/runtime/commit/dde00d376e4b40674488c5c688899d7a343f4eca)
**Affected Tests:** 1

| Test Name | Link | Change | .NET 9 | .NET 10 | Other Changepoints |
|-----------|------|--------|--------|-------|--------------------|
| System.Collections.AddGivenSize<Int32>.Stack(Size: 512) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/System.Collections.AddGivenSize%28Int32%29.Stack%28Size%3A%20512%29.html) | +13.23% | 971.297094 | 1099.778245 | None |

---

## 49. 1af7c2370b - Add a number of additional APIs to the various SIMD accelerated vector types (#111179)

**Date:** 2025-01-16 16:30:16
**Commit:** [1af7c2370b](https://github.com/dotnet/runtime/commit/1af7c2370bce80cba73d442d69f4a2f1b02dcbef)
**Affected Tests:** 1

| Test Name | Link | Change | .NET 9 | .NET 10 | Other Changepoints |
|-----------|------|--------|--------|-------|--------------------|
| System.MathBenchmarks.Double.ILogB | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/System.MathBenchmarks.Double.ILogB.html) | +8.43% | 7854.336984 | 8516.370698 | None |

---

## 50. 397863948b - [main] Update dependencies from dnceng/internal/dotnet-optimization (#111326)

**Date:** 2025-01-21 20:43:48
**Commit:** [397863948b](https://github.com/dotnet/runtime/commit/397863948b5e0ca98ee849053153078acdf3be86)
**Affected Tests:** 1

| Test Name | Link | Change | .NET 9 | .NET 10 | Other Changepoints |
|-----------|------|--------|--------|-------|--------------------|
| ByteMark.BenchNumericSortRectangular | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/ByteMark.BenchNumericSortRectangular.html) | +6.76% | 1120886460.000000 | 1196654188.000000 | [6](#6-1c10ceecbf---jit-add-3-opt-implementation-for-improving-upon-rpo-based-block-layout-103450) |

---

## 51. fc8b63c3b3 - Update dependencies from https://github.com/dotnet/roslyn build 20250205.10 (#112224)

**Date:** 2025-02-06 19:52:45
**Commit:** [fc8b63c3b3](https://github.com/dotnet/runtime/commit/fc8b63c3b36744b4012210e67cabb7fd96c938b5)
**Affected Tests:** 1

| Test Name | Link | Change | .NET 9 | .NET 10 | Other Changepoints |
|-----------|------|--------|--------|-------|--------------------|
| System.Text.RegularExpressions.Tests.Perf_Regex_Common.CtorInvoke(Options: IgnoreCase, Compiled) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/System.Text.RegularExpressions.Tests.Perf_Regex_Common.CtorInvoke%28Options%3A%20IgnoreCase%2C%20Compiled%29.html) | +12.94% | 337287.419076 | 380922.283664 | [15](#15-8b00880aad---jit-shrink-data-section-for-const-vector-loads-114040) |

---

## 52. e9a91f90b8 - JIT: Skip parameter register preferencing in OSR methods (#112452)

**Date:** 2025-02-13 10:06:37
**Commit:** [e9a91f90b8](https://github.com/dotnet/runtime/commit/e9a91f90b84e79a2d21f480778e5d4850579b594)
**Affected Tests:** 1

| Test Name | Link | Change | .NET 9 | .NET 10 | Other Changepoints |
|-----------|------|--------|--------|-------|--------------------|
| SciMark2.kernel.benchSparseMult | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/SciMark2.kernel.benchSparseMult.html) | +67.64% | 596219400.000000 | 999489944.642857 | [2](#2-ddf8075a2f---jit-visit-blocks-in-rpo-during-lsra-107927), [3](#3-41be5e229b---jit-graph-based-loop-inversion-116017) |

---

## 53. 343b0349fa - JIT: Fix edge likelihoods in `fgOptimizeBranch` (#113235)

**Date:** 2025-03-07 07:10:04
**Commit:** [343b0349fa](https://github.com/dotnet/runtime/commit/343b0349fae8d2a32aefd058cc779e9b3647a646)
**Affected Tests:** 1

| Test Name | Link | Change | .NET 9 | .NET 10 | Other Changepoints |
|-----------|------|--------|--------|-------|--------------------|
| System.Memory.Span<Byte>.IndexOfAnyTwoValues(Size: 4) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/System.Memory.Span%28Byte%29.IndexOfAnyTwoValues%28Size%3A%204%29.html) | +5.81% | 5.030917 | 5.323267 | [2](#2-ddf8075a2f---jit-visit-blocks-in-rpo-during-lsra-107927), [3](#3-41be5e229b---jit-graph-based-loop-inversion-116017) |

---

## 54. 2ec50bd313 - JIT: Switch optOptimizeLayout to pre-layout optimization phase (#113224)

**Date:** 2025-03-08 00:17:48
**Commit:** [2ec50bd313](https://github.com/dotnet/runtime/commit/2ec50bd31322da4c261e541a3db209a38be1ca81)
**Affected Tests:** 1

| Test Name | Link | Change | .NET 9 | .NET 10 | Other Changepoints |
|-----------|------|--------|--------|-------|--------------------|
| System.Tests.Perf_Enum.ToString_Flags(value: Red, Orange, Yellow, Green, Blue) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/System.Tests.Perf_Enum.ToString_Flags%28value%3A%20Red%2C%20Orange%2C%20Yellow%2C%20Green%2C%20Blue%29.html) | +11.99% | 58.137628 | 65.108616 | [16](#16-30082a461a---jit-save-pgo-data-in-inline-context-use-it-for-call-optimization-116241) |

---

## 55. 48ace183c4 - JIT: Re-introduce late `fgOptimizeBranch` pass (#113491)

**Date:** 2025-03-19 16:21:55
**Commit:** [48ace183c4](https://github.com/dotnet/runtime/commit/48ace183c442e367738374671a86bd82ed60e7d9)
**Affected Tests:** 1

| Test Name | Link | Change | .NET 9 | .NET 10 | Other Changepoints |
|-----------|------|--------|--------|-------|--------------------|
| Span.Sorting.BubbleSortSpan(Size: 512) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/Span.Sorting.BubbleSortSpan%28Size%3A%20512%29.html) | +56.16% | 212227.260218 | 331405.043971 | [6](#6-1c10ceecbf---jit-add-3-opt-implementation-for-improving-upon-rpo-based-block-layout-103450) |

---

## 56. 630de838ac - Interpreter GC info stage 1: Use templates to specialize gc info encoder/decoder (#113223)

**Date:** 2025-03-20 23:14:52
**Commit:** [630de838ac](https://github.com/dotnet/runtime/commit/630de838aca00a7d7af635ca00312bd7a5d75c5d)
**Affected Tests:** 1

| Test Name | Link | Change | .NET 9 | .NET 10 | Other Changepoints |
|-----------|------|--------|--------|-------|--------------------|
| System.Tests.Perf_GC<Char>.AllocateUninitializedArray(length: 10000, pinned: False) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/System.Tests.Perf_GC%28Char%29.AllocateUninitializedArray%28length%3A%2010000%2C%20pinned%3A%20False%29.html) | +6.25% | 178.127119 | 189.257301 | None |

---

## 57. 33b5215c15 - Smaller funclet prologs/epilogs (x64) (#115284)

**Date:** 2025-05-10 05:40:58
**Commit:** [33b5215c15](https://github.com/dotnet/runtime/commit/33b5215c15b16ad9e2738c325f6b562702c308d3)
**Affected Tests:** 1

| Test Name | Link | Change | .NET 9 | .NET 10 | Other Changepoints |
|-----------|------|--------|--------|-------|--------------------|
| System.Numerics.Tests.Perf_BigInteger.Equals(arguments: 259 bytes, Same) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/System.Numerics.Tests.Perf_BigInteger.Equals%28arguments%3A%20259%20bytes%2C%20Same%29.html) | +21.05% | 9.046885 | 10.951312 | [14](#14-02127c782a---jit-dont-put-cold-blocks-in-rpo-during-layout-112448) |

---

## 58. fd8933aac2 - Share implementation of ComWrappers between CoreCLR and NativeAOT (#113907)

**Date:** 2025-05-10 17:16:10
**Commit:** [fd8933aac2](https://github.com/dotnet/runtime/commit/fd8933aac237d2f3103de071ec4bc1547bfef16c)
**Affected Tests:** 1

| Test Name | Link | Change | .NET 9 | .NET 10 | Other Changepoints |
|-----------|------|--------|--------|-------|--------------------|
| Interop.ComWrappersTests.ParallelRCWLookUp | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/Interop.ComWrappersTests.ParallelRCWLookUp.html) | +60.90% | 410610157.142857 | 660667346.428571 | None |

---

## 59. 3c8bae3ff0 - JIT: also run local assertion prop in postorder during morph (#115626)

**Date:** 2025-05-16 22:16:17
**Commit:** [3c8bae3ff0](https://github.com/dotnet/runtime/commit/3c8bae3ff0906f590c6eec61eb114eac205ac2cc)
**Affected Tests:** 1

| Test Name | Link | Change | .NET 9 | .NET 10 | Other Changepoints |
|-----------|------|--------|--------|-------|--------------------|
| System.Collections.IterateForEach<Int32>.Stack(Size: 512) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_Windows%2010.0.22621/System.Collections.IterateForEach%28Int32%29.Stack%28Size%3A%20512%29.html) | +10.26% | 1467.246764 | 1617.809652 | None |

---
