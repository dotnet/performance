# Changepoint Impact Report - linux-wasm-tiger

## 1. 4020e05efd - Clean up in Number.Formatting.cs (#110955)

**Date:** 2025-01-10 19:29:57
**Commit:** [4020e05efd](https://github.com/dotnet/runtime/commit/4020e05efdfcc6b10eab90aeb8a8b5d80f75786f)
**Affected Tests:** 29

| Test Name | Link | Change | .NET 9 | .NET 10 | Other Changepoints |
|-----------|------|--------|--------|-------|--------------------|
| System.Tests.Perf_UInt32.TryFormat(value: 4294967295) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04_CompilationMode=wasm_RunKind=micro/System.Tests.Perf_UInt32.TryFormat%28value%3A%204294967295%29.html) | +84.25% | 61.956317 | 114.152511 | None |
| System.Tests.Perf_Int32.TryFormat(value: 2147483647) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04_CompilationMode=wasm_RunKind=micro/System.Tests.Perf_Int32.TryFormat%28value%3A%202147483647%29.html) | +79.42% | 63.441487 | 113.829090 | None |
| System.Tests.Perf_UInt64.TryFormat(value: 18446744073709551615) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04_CompilationMode=wasm_RunKind=micro/System.Tests.Perf_UInt64.TryFormat%28value%3A%2018446744073709551615%29.html) | +74.44% | 143.795797 | 250.839690 | None |
| System.Tests.Perf_Int64.TryFormat(value: 9223372036854775807) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04_CompilationMode=wasm_RunKind=micro/System.Tests.Perf_Int64.TryFormat%28value%3A%209223372036854775807%29.html) | +69.99% | 138.687399 | 235.755837 | None |
| System.Tests.Perf_UInt32.ToString(value: 4294967295) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04_CompilationMode=wasm_RunKind=micro/System.Tests.Perf_UInt32.ToString%28value%3A%204294967295%29.html) | +60.46% | 82.531089 | 132.429328 | None |
| System.Tests.Perf_Int64.ToString(value: -9223372036854775808) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04_CompilationMode=wasm_RunKind=micro/System.Tests.Perf_Int64.ToString%28value%3A%20-9223372036854775808%29.html) | +58.78% | 236.117525 | 374.901103 | None |
| PerfLabTests.LowLevelPerf.IntegerFormatting | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04_CompilationMode=wasm_RunKind=micro/PerfLabTests.LowLevelPerf.IntegerFormatting.html) | +58.74% | 10108959.082646 | 16046519.351294 | None |
| System.Tests.Perf_UInt64.ToString(value: 18446744073709551615) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04_CompilationMode=wasm_RunKind=micro/System.Tests.Perf_UInt64.ToString%28value%3A%2018446744073709551615%29.html) | +56.05% | 163.032953 | 254.407094 | None |
| System.Tests.Perf_UInt16.ToString(value: 65535) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04_CompilationMode=wasm_RunKind=micro/System.Tests.Perf_UInt16.ToString%28value%3A%2065535%29.html) | +55.94% | 64.499710 | 100.578509 | None |
| System.Tests.Perf_Int32.ToString(value: -2147483648) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04_CompilationMode=wasm_RunKind=micro/System.Tests.Perf_Int32.ToString%28value%3A%20-2147483648%29.html) | +52.07% | 155.269253 | 236.117218 | None |
| System.Tests.Perf_Int64.TryFormat(value: -9223372036854775808) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04_CompilationMode=wasm_RunKind=micro/System.Tests.Perf_Int64.TryFormat%28value%3A%20-9223372036854775808%29.html) | +50.38% | 290.066476 | 436.205294 | None |
| System.Tests.Perf_Int32.ToString(value: 2147483647) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04_CompilationMode=wasm_RunKind=micro/System.Tests.Perf_Int32.ToString%28value%3A%202147483647%29.html) | +47.17% | 107.557930 | 158.296209 | None |
| System.Tests.Perf_Int128.TryFormat(value: 12345) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04_CompilationMode=wasm_RunKind=micro/System.Tests.Perf_Int128.TryFormat%28value%3A%2012345%29.html) | +46.38% | 98.201400 | 143.749564 | None |
| System.Tests.Perf_UInt64.ToString(value: 12345) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04_CompilationMode=wasm_RunKind=micro/System.Tests.Perf_UInt64.ToString%28value%3A%2012345%29.html) | +45.53% | 92.059373 | 133.977813 | None |
| System.Tests.Perf_Int64.ToString(value: 9223372036854775807) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04_CompilationMode=wasm_RunKind=micro/System.Tests.Perf_Int64.ToString%28value%3A%209223372036854775807%29.html) | +44.86% | 190.329006 | 275.709695 | None |
| System.Tests.Perf_UInt64.TryFormat(value: 12345) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04_CompilationMode=wasm_RunKind=micro/System.Tests.Perf_UInt64.TryFormat%28value%3A%2012345%29.html) | +43.43% | 78.217400 | 112.188405 | None |
| System.Tests.Perf_SByte.ToString(value: -128) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04_CompilationMode=wasm_RunKind=micro/System.Tests.Perf_SByte.ToString%28value%3A%20-128%29.html) | +42.63% | 141.752179 | 202.177343 | None |
| System.Tests.Perf_UInt16.ToString(value: 12345) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04_CompilationMode=wasm_RunKind=micro/System.Tests.Perf_UInt16.ToString%28value%3A%2012345%29.html) | +39.84% | 69.927510 | 97.786073 | None |
| System.Tests.Perf_UInt32.ToString(value: 12345) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04_CompilationMode=wasm_RunKind=micro/System.Tests.Perf_UInt32.ToString%28value%3A%2012345%29.html) | +39.01% | 66.737600 | 92.770080 | None |
| System.Tests.Perf_Byte.ToString(value: 255) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04_CompilationMode=wasm_RunKind=micro/System.Tests.Perf_Byte.ToString%28value%3A%20255%29.html) | +35.73% | 60.235712 | 81.755330 | None |
| System.Tests.Perf_UInt32.TryFormat(value: 12345) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04_CompilationMode=wasm_RunKind=micro/System.Tests.Perf_UInt32.TryFormat%28value%3A%2012345%29.html) | +35.07% | 53.518866 | 72.287034 | None |
| System.Tests.Perf_Int16.ToString(value: 32767) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04_CompilationMode=wasm_RunKind=micro/System.Tests.Perf_Int16.ToString%28value%3A%2032767%29.html) | +34.77% | 87.832730 | 118.376416 | None |
| System.Tests.Perf_Int64.ToString(value: 12345) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04_CompilationMode=wasm_RunKind=micro/System.Tests.Perf_Int64.ToString%28value%3A%2012345%29.html) | +34.59% | 111.765068 | 150.426181 | None |
| System.Tests.Perf_Int32.TryFormat(value: 12345) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04_CompilationMode=wasm_RunKind=micro/System.Tests.Perf_Int32.TryFormat%28value%3A%2012345%29.html) | +34.31% | 53.460164 | 71.800357 | None |
| System.Tests.Perf_Int64.TryFormat(value: 12345) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04_CompilationMode=wasm_RunKind=micro/System.Tests.Perf_Int64.TryFormat%28value%3A%2012345%29.html) | +33.64% | 80.230776 | 107.221777 | None |
| System.Tests.Perf_Int32.ToString(value: 12345) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04_CompilationMode=wasm_RunKind=micro/System.Tests.Perf_Int32.ToString%28value%3A%2012345%29.html) | +28.42% | 95.602738 | 122.774533 | None |
| System.Tests.Perf_Int16.ToString(value: -32768) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04_CompilationMode=wasm_RunKind=micro/System.Tests.Perf_Int16.ToString%28value%3A%20-32768%29.html) | +20.25% | 159.616477 | 191.938633 | None |
| System.Tests.Perf_Int32.TryFormat(value: -2147483648) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04_CompilationMode=wasm_RunKind=micro/System.Tests.Perf_Int32.TryFormat%28value%3A%20-2147483648%29.html) | +17.52% | 219.055048 | 257.441848 | None |
| System.Tests.Perf_Version.ToStringL | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04_CompilationMode=wasm_RunKind=micro/System.Tests.Perf_Version.ToStringL.html) | +12.05% | 696.225174 | 780.122303 | None |

---

## 2. 617f9ee5f3 - Add `TypeName` APIs to simplify metadata lookup. (#111598)

**Date:** 2025-02-10 22:18:46
**Commit:** [617f9ee5f3](https://github.com/dotnet/runtime/commit/617f9ee5f357a52309f21a732df04a87ee16adc9)
**Affected Tests:** 16

| Test Name | Link | Change | .NET 9 | .NET 10 | Other Changepoints |
|-----------|------|--------|--------|-------|--------------------|
| System.Tests.Perf_Type.GetType_FullyQualifiedNames(input: typeof(System.Tests.NestedGeneric<String, Boolean>)) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04_CompilationMode=wasm_RunKind=micro/System.Tests.Perf_Type.GetType_FullyQualifiedNames%28input%3A%20typeof%28System.Tests.NestedGeneric%28String%2C%20Boolean%29%29%29.html) | +1313.84% | 4489.123692 | 63469.170041 | None |
| System.Tests.Perf_Type.GetType_FullyQualifiedNames(input: typeof(System.Collections.Generic.Dictionary<String, Boolean>)) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04_CompilationMode=wasm_RunKind=micro/System.Tests.Perf_Type.GetType_FullyQualifiedNames%28input%3A%20typeof%28System.Collections.Generic.Dictionary%28String%2C%20Boolean%29%29%29.html) | +1074.66% | 4277.733151 | 50248.898411 | None |
| System.Tests.Perf_Type.GetType_FullyQualifiedNames(input: typeof(System.Collections.Generic.Dictionary`2[])) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04_CompilationMode=wasm_RunKind=micro/System.Tests.Perf_Type.GetType_FullyQualifiedNames%28input%3A%20typeof%28System.Collections.Generic.Dictionary%602%5B%5D%29%29.html) | +967.56% | 4959.114837 | 52941.419664 | None |
| System.Tests.Perf_Type.GetType_FullyQualifiedNames(input: typeof(System.Tests.Nested)) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04_CompilationMode=wasm_RunKind=micro/System.Tests.Perf_Type.GetType_FullyQualifiedNames%28input%3A%20typeof%28System.Tests.Nested%29%29.html) | +381.88% | 1876.884631 | 9044.347687 | None |
| System.Tests.Perf_Type.GetType_NonFullyQualifiedNames(input: "System.Int32&") | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04_CompilationMode=wasm_RunKind=micro/System.Tests.Perf_Type.GetType_NonFullyQualifiedNames%28input%3A%20%22System.Int32%26%22%29.html) | +267.58% | 1116.351825 | 4103.451677 | None |
| System.Tests.Perf_Type.GetType_NonFullyQualifiedNames(input: "System.Int32[]") | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04_CompilationMode=wasm_RunKind=micro/System.Tests.Perf_Type.GetType_NonFullyQualifiedNames%28input%3A%20%22System.Int32%5B%5D%22%29.html) | +261.39% | 1172.748601 | 4238.218586 | None |
| System.Tests.Perf_Type.GetType_NonFullyQualifiedNames(input: "System.Int32[,]") | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04_CompilationMode=wasm_RunKind=micro/System.Tests.Perf_Type.GetType_NonFullyQualifiedNames%28input%3A%20%22System.Int32%5B%2C%5D%22%29.html) | +260.76% | 1463.590109 | 5280.020601 | None |
| System.Tests.Perf_Type.GetType_NonFullyQualifiedNames(input: "System.Int32*") | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04_CompilationMode=wasm_RunKind=micro/System.Tests.Perf_Type.GetType_NonFullyQualifiedNames%28input%3A%20%22System.Int32%2A%22%29.html) | +235.96% | 1252.145288 | 4206.667026 | None |
| System.Tests.Perf_Type.GetType_NonFullyQualifiedNames(input: "System.Int32[*]") | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04_CompilationMode=wasm_RunKind=micro/System.Tests.Perf_Type.GetType_NonFullyQualifiedNames%28input%3A%20%22System.Int32%5B%2A%5D%22%29.html) | +205.15% | 1529.042393 | 4665.800492 | None |
| System.Tests.Perf_Type.GetType_NonFullyQualifiedNames(input: "System.Int32") | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04_CompilationMode=wasm_RunKind=micro/System.Tests.Perf_Type.GetType_NonFullyQualifiedNames%28input%3A%20%22System.Int32%22%29.html) | +202.49% | 1028.087654 | 3109.889202 | None |
| System.Tests.Perf_Type.GetType_FullyQualifiedNames(input: typeof(System.Int32[*])) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04_CompilationMode=wasm_RunKind=micro/System.Tests.Perf_Type.GetType_FullyQualifiedNames%28input%3A%20typeof%28System.Int32%5B%2A%5D%29%29.html) | +195.08% | 2111.128821 | 6229.546892 | None |
| System.Tests.Perf_Type.GetType_FullyQualifiedNames(input: typeof(System.Int32[,])) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04_CompilationMode=wasm_RunKind=micro/System.Tests.Perf_Type.GetType_FullyQualifiedNames%28input%3A%20typeof%28System.Int32%5B%2C%5D%29%29.html) | +186.45% | 2275.030753 | 6516.716190 | None |
| System.Tests.Perf_Type.GetType_FullyQualifiedNames(input: typeof(System.Int32*)) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04_CompilationMode=wasm_RunKind=micro/System.Tests.Perf_Type.GetType_FullyQualifiedNames%28input%3A%20typeof%28System.Int32%2A%29%29.html) | +171.76% | 1942.117503 | 5277.843786 | None |
| System.Tests.Perf_Type.GetType_FullyQualifiedNames(input: typeof(System.Int32&)) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04_CompilationMode=wasm_RunKind=micro/System.Tests.Perf_Type.GetType_FullyQualifiedNames%28input%3A%20typeof%28System.Int32%26%29%29.html) | +170.68% | 2017.547717 | 5461.179039 | None |
| System.Tests.Perf_Type.GetType_FullyQualifiedNames(input: typeof(int)) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04_CompilationMode=wasm_RunKind=micro/System.Tests.Perf_Type.GetType_FullyQualifiedNames%28input%3A%20typeof%28int%29%29.html) | +153.27% | 1487.601195 | 3767.628592 | None |
| System.Tests.Perf_Type.GetType_FullyQualifiedNames(input: typeof(System.Int32[])) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04_CompilationMode=wasm_RunKind=micro/System.Tests.Perf_Type.GetType_FullyQualifiedNames%28input%3A%20typeof%28System.Int32%5B%5D%29%29.html) | +149.07% | 2017.272794 | 5024.485819 | None |

---

## 3. 38c8e8f4cc - Add CollectionsMarshal.AsBytes(BitArray) (#116308)

**Date:** 2025-06-17 17:13:37
**Commit:** [38c8e8f4cc](https://github.com/dotnet/runtime/commit/38c8e8f4cc1be3abd20f675771f208360b11b52c)
**Affected Tests:** 16

| Test Name | Link | Change | .NET 9 | .NET 10 | Other Changepoints |
|-----------|------|--------|--------|-------|--------------------|
| System.Collections.Tests.Perf_BitArray.BitArraySetLengthShrink(Size: 4) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04_CompilationMode=wasm_RunKind=micro/System.Collections.Tests.Perf_BitArray.BitArraySetLengthShrink%28Size%3A%204%29.html) | +258.52% | 146.024434 | 523.529690 | None |
| System.Collections.Tests.Perf_BitArray.BitArrayByteArrayCtor(Size: 4) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04_CompilationMode=wasm_RunKind=micro/System.Collections.Tests.Perf_BitArray.BitArrayByteArrayCtor%28Size%3A%204%29.html) | +225.96% | 122.168162 | 398.218594 | None |
| System.Collections.Tests.Perf_BitArray.BitArraySetAll(Size: 4) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04_CompilationMode=wasm_RunKind=micro/System.Collections.Tests.Perf_BitArray.BitArraySetAll%28Size%3A%204%29.html) | +124.77% | 40.342825 | 90.679993 | None |
| System.Collections.Tests.Perf_BitArray.BitArraySetLengthGrow(Size: 4) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04_CompilationMode=wasm_RunKind=micro/System.Collections.Tests.Perf_BitArray.BitArraySetLengthGrow%28Size%3A%204%29.html) | +123.47% | 263.138353 | 588.044018 | None |
| System.Collections.Tests.Perf_BitArray.BitArrayNot(Size: 4) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04_CompilationMode=wasm_RunKind=micro/System.Collections.Tests.Perf_BitArray.BitArrayNot%28Size%3A%204%29.html) | +106.77% | 54.807726 | 113.323652 | None |
| System.Collections.Tests.Perf_BitArray.BitArrayLengthValueCtor(Size: 4) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04_CompilationMode=wasm_RunKind=micro/System.Collections.Tests.Perf_BitArray.BitArrayLengthValueCtor%28Size%3A%204%29.html) | +49.47% | 104.758434 | 156.582087 | None |
| System.Collections.Tests.Perf_BitArray.BitArrayCopyToBoolArray(Size: 512) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04_CompilationMode=wasm_RunKind=micro/System.Collections.Tests.Perf_BitArray.BitArrayCopyToBoolArray%28Size%3A%20512%29.html) | +46.49% | 21190.015980 | 31041.556665 | None |
| System.Collections.Tests.Perf_BitArray.BitArraySet(Size: 512) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04_CompilationMode=wasm_RunKind=micro/System.Collections.Tests.Perf_BitArray.BitArraySet%28Size%3A%20512%29.html) | +43.43% | 3000.165220 | 4303.234791 | None |
| System.Collections.Tests.Perf_BitArray.BitArrayCopyToBoolArray(Size: 4) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04_CompilationMode=wasm_RunKind=micro/System.Collections.Tests.Perf_BitArray.BitArrayCopyToBoolArray%28Size%3A%204%29.html) | +40.23% | 258.991662 | 363.192835 | None |
| System.Collections.Tests.Perf_BitArray.BitArrayGet(Size: 512) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04_CompilationMode=wasm_RunKind=micro/System.Collections.Tests.Perf_BitArray.BitArrayGet%28Size%3A%20512%29.html) | +33.37% | 22327.617996 | 29778.297288 | None |
| System.Collections.Tests.Perf_BitArray.BitArrayGet(Size: 4) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04_CompilationMode=wasm_RunKind=micro/System.Collections.Tests.Perf_BitArray.BitArrayGet%28Size%3A%204%29.html) | +31.06% | 186.617019 | 244.575946 | None |
| System.Collections.Tests.Perf_BitArray.BitArraySet(Size: 4) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04_CompilationMode=wasm_RunKind=micro/System.Collections.Tests.Perf_BitArray.BitArraySet%28Size%3A%204%29.html) | +22.97% | 35.836272 | 44.068757 | None |
| System.Collections.Tests.Perf_BitArray.BitArrayNot(Size: 512) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04_CompilationMode=wasm_RunKind=micro/System.Collections.Tests.Perf_BitArray.BitArrayNot%28Size%3A%20512%29.html) | +22.88% | 174.778231 | 214.771295 | None |
| System.Collections.Tests.Perf_BitArray.BitArrayRightShift(Size: 4) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04_CompilationMode=wasm_RunKind=micro/System.Collections.Tests.Perf_BitArray.BitArrayRightShift%28Size%3A%204%29.html) | +18.76% | 46.273131 | 54.952078 | None |
| System.Collections.Tests.Perf_BitArray.BitArrayLeftShift(Size: 512) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04_CompilationMode=wasm_RunKind=micro/System.Collections.Tests.Perf_BitArray.BitArrayLeftShift%28Size%3A%20512%29.html) | +16.07% | 834.699696 | 968.810927 | None |
| System.Collections.Tests.Perf_BitArray.BitArrayRightShift(Size: 512) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04_CompilationMode=wasm_RunKind=micro/System.Collections.Tests.Perf_BitArray.BitArrayRightShift%28Size%3A%20512%29.html) | +13.50% | 870.066222 | 987.517249 | None |

---

## 4. 4b98d321ef - Add FrozenDictionary specialization for integers / enums (#111886)

**Date:** 2025-01-28 20:33:05
**Commit:** [4b98d321ef](https://github.com/dotnet/runtime/commit/4b98d321ef5a2b8211c28727d5b2521a20417549)
**Affected Tests:** 6

| Test Name | Link | Change | .NET 9 | .NET 10 | Other Changepoints |
|-----------|------|--------|--------|-------|--------------------|
| System.Collections.Perf_Frozen<Int16>.ToFrozenDictionary(Count: 4) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04_CompilationMode=wasm_RunKind=micro/System.Collections.Perf_Frozen%28Int16%29.ToFrozenDictionary%28Count%3A%204%29.html) | +293.41% | 750.018862 | 2950.659936 | None |
| System.Collections.Perf_Frozen<NotKnownComparable>.ToFrozenDictionary(Count: 4) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04_CompilationMode=wasm_RunKind=micro/System.Collections.Perf_Frozen%28NotKnownComparable%29.ToFrozenDictionary%28Count%3A%204%29.html) | +150.23% | 1112.861037 | 2784.700453 | None |
| System.Collections.CtorFromCollection<Int32>.FrozenDictionaryOptimized(Size: 512) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04_CompilationMode=wasm_RunKind=micro/System.Collections.CtorFromCollection%28Int32%29.FrozenDictionaryOptimized%28Size%3A%20512%29.html) | +41.32% | 80545.896302 | 113825.821136 | None |
| System.Collections.Perf_Frozen<Int16>.ToFrozenDictionary(Count: 64) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04_CompilationMode=wasm_RunKind=micro/System.Collections.Perf_Frozen%28Int16%29.ToFrozenDictionary%28Count%3A%2064%29.html) | +36.40% | 16432.393229 | 22414.097572 | None |
| System.Collections.Perf_Frozen<NotKnownComparable>.ToFrozenDictionary(Count: 64) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04_CompilationMode=wasm_RunKind=micro/System.Collections.Perf_Frozen%28NotKnownComparable%29.ToFrozenDictionary%28Count%3A%2064%29.html) | +31.52% | 20944.823981 | 27545.839757 | None |
| System.Collections.Perf_Frozen<Int16>.ToFrozenDictionary(Count: 512) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04_CompilationMode=wasm_RunKind=micro/System.Collections.Perf_Frozen%28Int16%29.ToFrozenDictionary%28Count%3A%20512%29.html) | +31.03% | 118836.520849 | 155707.789101 | None |

---

## 5. 92e7ab035a - [mono][wasm] AOT more methods containing EH clauses (#111318)

**Date:** 2025-02-13 02:06:09
**Commit:** [92e7ab035a](https://github.com/dotnet/runtime/commit/92e7ab035acfb04f57df85324785c7acab2a1040)
**Affected Tests:** 3

| Test Name | Link | Change | .NET 9 | .NET 10 | Other Changepoints |
|-----------|------|--------|--------|-------|--------------------|
| System.Linq.Tests.Perf_Enumerable.WhereSelect(input: Array) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04_CompilationMode=wasm_RunKind=micro/System.Linq.Tests.Perf_Enumerable.WhereSelect%28input%3A%20Array%29.html) | +110.27% | 11280.908416 | 23720.462950 | None |
| System.Linq.Tests.Perf_Enumerable.WhereSelect(input: List) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04_CompilationMode=wasm_RunKind=micro/System.Linq.Tests.Perf_Enumerable.WhereSelect%28input%3A%20List%29.html) | +74.53% | 14078.512112 | 24570.674554 | None |
| System.Linq.Tests.Perf_Enumerable.WhereSelect(input: IEnumerable) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04_CompilationMode=wasm_RunKind=micro/System.Linq.Tests.Perf_Enumerable.WhereSelect%28input%3A%20IEnumerable%29.html) | +54.93% | 21931.030415 | 33978.297963 | None |

---

## 6. e0e9f15d06 - Implement various convenience methods for System.Numerics types (#115457)

**Date:** 2025-05-12 19:51:25
**Commit:** [e0e9f15d06](https://github.com/dotnet/runtime/commit/e0e9f15d06b775325c874674bfca51d18c8f5075)
**Affected Tests:** 3

| Test Name | Link | Change | .NET 9 | .NET 10 | Other Changepoints |
|-----------|------|--------|--------|-------|--------------------|
| System.Numerics.Tests.Perf_Matrix4x4.CreateFromScalars | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04_CompilationMode=wasm_RunKind=micro/System.Numerics.Tests.Perf_Matrix4x4.CreateFromScalars.html) | +25.11% | 57.778323 | 72.287782 | None |
| System.Numerics.Tests.Perf_Matrix3x2.CreateFromScalars | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04_CompilationMode=wasm_RunKind=micro/System.Numerics.Tests.Perf_Matrix3x2.CreateFromScalars.html) | +13.86% | 17.532156 | 19.961819 | None |
| System.Numerics.Tests.Perf_Vector3.TransformNormalByMatrix4x4Benchmark | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04_CompilationMode=wasm_RunKind=micro/System.Numerics.Tests.Perf_Vector3.TransformNormalByMatrix4x4Benchmark.html) | +9.37% | 115.949935 | 126.819668 | None |

---

## 7. a3fe47ef1a - [wasm] stop using mmap/munmap (#108512)

**Date:** 2024-10-24 17:15:16
**Commit:** [a3fe47ef1a](https://github.com/dotnet/runtime/commit/a3fe47ef1a8def24e8d64c305172199ae5a4ed07)
**Affected Tests:** 2

| Test Name | Link | Change | .NET 9 | .NET 10 | Other Changepoints |
|-----------|------|--------|--------|-------|--------------------|
| System.Collections.Perf_LengthBucketsFrozenDictionary.TryGetValue_False_FrozenDictionary(Count: 10000, ItemsPerBucket: 5) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04_CompilationMode=wasm_RunKind=micro/System.Collections.Perf_LengthBucketsFrozenDictionary.TryGetValue_False_FrozenDictionary%28Count%3A%2010000%2C%20ItemsPerBucket%3A%205%29.html) | +89.69% | 739675.124841 | 1403068.864790 | None |
| System.Memory.Slice<Byte>.ReadOnlySpanStart | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04_CompilationMode=wasm_RunKind=micro/System.Memory.Slice%28Byte%29.ReadOnlySpanStart.html) | +47.37% | 27.405958 | 40.388321 | None |

---

## 8. 3f0a23d76d - Dedup remaining MemoryExtensions Span overloads (#109501)

**Date:** 2024-11-06 21:17:47
**Commit:** [3f0a23d76d](https://github.com/dotnet/runtime/commit/3f0a23d76d6133f2d507f69d2158afcf54ae7e76)
**Affected Tests:** 2

| Test Name | Link | Change | .NET 9 | .NET 10 | Other Changepoints |
|-----------|------|--------|--------|-------|--------------------|
| System.Memory.Span<Byte>.IndexOfValue(Size: 4) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04_CompilationMode=wasm_RunKind=micro/System.Memory.Span%28Byte%29.IndexOfValue%28Size%3A%204%29.html) | +17.48% | 35.713347 | 41.956817 | None |
| System.Memory.Span<Char>.LastIndexOfValue(Size: 4) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04_CompilationMode=wasm_RunKind=micro/System.Memory.Span%28Char%29.LastIndexOfValue%28Size%3A%204%29.html) | +13.49% | 34.696679 | 39.378230 | None |

---

## 9. 37b1764e19 - Optimize BigInteger.Divide (#96895)

**Date:** 2025-02-04 19:13:15
**Commit:** [37b1764e19](https://github.com/dotnet/runtime/commit/37b1764e19aceaa545d8433c490b850538b8905a)
**Affected Tests:** 2

| Test Name | Link | Change | .NET 9 | .NET 10 | Other Changepoints |
|-----------|------|--------|--------|-------|--------------------|
| System.Numerics.Tests.Perf_BigInteger.ModPow(arguments: 1024,1024,64 bits) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04_CompilationMode=wasm_RunKind=micro/System.Numerics.Tests.Perf_BigInteger.ModPow%28arguments%3A%201024%2C1024%2C64%20bits%29.html) | +17.08% | 1008978.450499 | 1181298.831305 | None |
| System.Numerics.Tests.Perf_BigInteger.ModPow(arguments: 16384,16384,64 bits) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04_CompilationMode=wasm_RunKind=micro/System.Numerics.Tests.Perf_BigInteger.ModPow%28arguments%3A%2016384%2C16384%2C64%20bits%29.html) | +8.75% | 18208499.472884 | 19801751.704750 | None |

---

## 10. 959c10e41f - Generate an unconditional bailout for MINT_SWITCH in jiterpreter traces instead of aborting the trace (#107323)

**Date:** 2024-09-04 18:19:26
**Commit:** [959c10e41f](https://github.com/dotnet/runtime/commit/959c10e41f1180482a7bc25e2bba16d27aca343a)
**Affected Tests:** 1

| Test Name | Link | Change | .NET 9 | .NET 10 | Other Changepoints |
|-----------|------|--------|--------|-------|--------------------|
| PerfLabTests.DictionaryExpansion.ExpandDictionaries | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04_CompilationMode=wasm_RunKind=micro/PerfLabTests.DictionaryExpansion.ExpandDictionaries.html) | +86.46% | 3506.427872 | 6538.125441 | [11](#11-5c4686f831---wasm-implement-mint_switch-opcode-in-jiterpreter-107423) |

---

## 11. 5c4686f831 - [wasm] Implement MINT_SWITCH opcode in jiterpreter (#107423)

**Date:** 2024-09-07 06:47:24
**Commit:** [5c4686f831](https://github.com/dotnet/runtime/commit/5c4686f831d34c2c127e943d0f0d144793eeb0ad)
**Affected Tests:** 1

| Test Name | Link | Change | .NET 9 | .NET 10 | Other Changepoints |
|-----------|------|--------|--------|-------|--------------------|
| PerfLabTests.DictionaryExpansion.ExpandDictionaries | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04_CompilationMode=wasm_RunKind=micro/PerfLabTests.DictionaryExpansion.ExpandDictionaries.html) | +86.46% | 3506.427872 | 6538.125441 | [10](#10-959c10e41f---generate-an-unconditional-bailout-for-mint_switch-in-jiterpreter-traces-instead-of-aborting-the-trace-107323) |

---

## 12. 11f3549e83 - Optimize DateTimeOffset (#111112)

**Date:** 2025-01-09 02:59:16
**Commit:** [11f3549e83](https://github.com/dotnet/runtime/commit/11f3549e8392f2220aeadfa34aa578ccb47b80c0)
**Affected Tests:** 1

| Test Name | Link | Change | .NET 9 | .NET 10 | Other Changepoints |
|-----------|------|--------|--------|-------|--------------------|
| System.Tests.Perf_DateTimeOffset.GetUtcNow | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04_CompilationMode=wasm_RunKind=micro/System.Tests.Perf_DateTimeOffset.GetUtcNow.html) | +30.30% | 174.367182 | 227.200151 | None |

---

## 13. 0b6404153e - Implement AddSaturate, SubtractSaturate and NarrowWithSaturation on the Vector types (#115525)

**Date:** 2025-05-14 16:22:05
**Commit:** [0b6404153e](https://github.com/dotnet/runtime/commit/0b6404153e16685f47d166625373f6635cf5631d)
**Affected Tests:** 1

| Test Name | Link | Change | .NET 9 | .NET 10 | Other Changepoints |
|-----------|------|--------|--------|-------|--------------------|
| System.Perf_Convert.FromHexString | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04_CompilationMode=wasm_RunKind=micro/System.Perf_Convert.FromHexString.html) | +106.90% | 559.831600 | 1158.302439 | None |

---

## 14. 707831ea82 - [WIP] Interpreter boxing/unboxing (#115549)

**Date:** 2025-05-15 19:30:31
**Commit:** [707831ea82](https://github.com/dotnet/runtime/commit/707831ea82f68971010920685f7b91423fafd55b)
**Affected Tests:** 1

| Test Name | Link | Change | .NET 9 | .NET 10 | Other Changepoints |
|-----------|------|--------|--------|-------|--------------------|
| StoreBlock.AnyLocation.CopyBlock32 | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04_CompilationMode=wasm_RunKind=micro/StoreBlock.AnyLocation.CopyBlock32.html) | +5.41% | 4.335316 | 4.569778 | None |

---

## 15. 2d5a2ee095 - Remove canceled AsyncOperations from channel queues (#116021)

**Date:** 2025-06-02 15:59:11
**Commit:** [2d5a2ee095](https://github.com/dotnet/runtime/commit/2d5a2ee09518e3afad75ea9bc40df0a548bcfa36)
**Affected Tests:** 1

| Test Name | Link | Change | .NET 9 | .NET 10 | Other Changepoints |
|-----------|------|--------|--------|-------|--------------------|
| System.Threading.Channels.Tests.BoundedChannelPerfTests.TryWriteThenTryRead | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_x64_ubuntu%2022.04_CompilationMode=wasm_RunKind=micro/System.Threading.Channels.Tests.BoundedChannelPerfTests.TryWriteThenTryRead.html) | +49.45% | 390.568923 | 583.686729 | None |

---
