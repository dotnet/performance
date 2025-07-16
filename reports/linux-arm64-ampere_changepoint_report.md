# Changepoint Impact Report - linux-arm64-ampere

Generated on: 2025-07-16 14:29:28

Total changepoints: 41
Total tests: 3271

## 1. 0fe82fbd8e - Reduce spin-waiting in the thread pool on Arm processors (#115402)

**Date:** 2025-05-12 14:13:35
**Commit:** [0fe82fbd8e](https://github.com/dotnet/runtime/commit/0fe82fbd8e107a0b7c2a79f458229aea5632c999)
**Affected Tests:** 50

| Test Name | Link | Change | Before | After | Other Changepoints |
|-----------|------|--------|--------|-------|--------------------|
| System.IO.Tests.Perf_File.WriteAllTextAsync(size: 10000) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.IO.Tests.Perf_File.WriteAllTextAsync%28size%3A%2010000%29.html) | +41.66% | 88893.208479 | 124973.000719 | None |
| System.Net.Security.Tests.SslStreamTests.LargeReadWriteAsync | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.Net.Security.Tests.SslStreamTests.LargeReadWriteAsync.html) | +40.83% | 60786.478609 | 70470.586767 | None |
| System.IO.Tests.Perf_FileStream.CopyToFileAsync(fileSize: 1024, options: Asynchronous) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.IO.Tests.Perf_FileStream.CopyToFileAsync%28fileSize%3A%201024%2C%20options%3A%20Asynchronous%29.html) | +40.76% | 104920.958591 | 142453.893624 | None |
| System.Net.Http.Tests.SocketsHttpHandlerPerfTest.Get(ssl: True, chunkedResponse: False, responseLength: 1) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.Net.Http.Tests.SocketsHttpHandlerPerfTest.Get%28ssl%3A%20True%2C%20chunkedResponse%3A%20False%2C%20responseLength%3A%201%29.html) | +33.27% | 97403.087889 | 127993.393756 | None |
| System.IO.Tests.Perf_FileStream.CopyToFileAsync(fileSize: 1024, options: None) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.IO.Tests.Perf_FileStream.CopyToFileAsync%28fileSize%3A%201024%2C%20options%3A%20None%29.html) | +32.64% | 97433.925761 | 138129.986629 | None |
| System.Net.Http.Tests.SocketsHttpHandlerPerfTest.Get_EnumerateHeaders_Validated(ssl: False, chunkedResponse: False, responseLength: 100000) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.Net.Http.Tests.SocketsHttpHandlerPerfTest.Get_EnumerateHeaders_Validated%28ssl%3A%20False%2C%20chunkedResponse%3A%20False%2C%20responseLength%3A%20100000%29.html) | +31.84% | 107495.527930 | 139435.516946 | None |
| BenchmarksGame.Fasta_1.RunBench | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/BenchmarksGame.Fasta_1.RunBench.html) | +31.78% | 392484.662448 | 518185.284832 | None |
| System.Net.Sockets.Tests.SocketSendReceivePerfTest.ConnectAcceptAsync | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.Net.Sockets.Tests.SocketSendReceivePerfTest.ConnectAcceptAsync.html) | +31.62% | 85781.423491 | 113295.579798 | [39](#39-8115429a72---jit-disallow-forward-substitution-of-async-calls-115936) |
| System.Buffers.Tests.RentReturnArrayPoolTests<Object>.MultipleSerial(RentalSize: 4096, ManipulateArray: False, Async: True, UseSharedPool: False) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.Buffers.Tests.RentReturnArrayPoolTests%28Object%29.MultipleSerial%28RentalSize%3A%204096%2C%20ManipulateArray%3A%20False%2C%20Async%3A%20True%2C%20UseSharedPool%3A%20False%29.html) | +29.84% | 1152.714171 | 1549.599862 | None |
| System.Net.Http.Tests.SocketsHttpHandlerPerfTest.Get_EnumerateHeaders_Validated(ssl: False, chunkedResponse: True, responseLength: 100000) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.Net.Http.Tests.SocketsHttpHandlerPerfTest.Get_EnumerateHeaders_Validated%28ssl%3A%20False%2C%20chunkedResponse%3A%20True%2C%20responseLength%3A%20100000%29.html) | +29.77% | 137679.596213 | 193741.999269 | None |
| System.IO.Pipes.Tests.Perf_NamedPipeStream.ReadWriteAsync(size: 1000000, Options: None) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.IO.Pipes.Tests.Perf_NamedPipeStream.ReadWriteAsync%28size%3A%201000000%2C%20Options%3A%20None%29.html) | +29.40% | 223113.187661 | 291831.602694 | None |
| System.IO.Pipes.Tests.Perf_AnonymousPipeStream.ReadWriteAsync(size: 1000000) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.IO.Pipes.Tests.Perf_AnonymousPipeStream.ReadWriteAsync%28size%3A%201000000%29.html) | +28.89% | 553537.140696 | 657708.068393 | None |
| System.Net.Http.Tests.SocketsHttpHandlerPerfTest.Get(ssl: False, chunkedResponse: False, responseLength: 100000) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.Net.Http.Tests.SocketsHttpHandlerPerfTest.Get%28ssl%3A%20False%2C%20chunkedResponse%3A%20False%2C%20responseLength%3A%20100000%29.html) | +28.53% | 101280.916664 | 133986.683634 | None |
| System.Net.Http.Tests.SocketsHttpHandlerPerfTest.Get(ssl: True, chunkedResponse: True, responseLength: 1) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.Net.Http.Tests.SocketsHttpHandlerPerfTest.Get%28ssl%3A%20True%2C%20chunkedResponse%3A%20True%2C%20responseLength%3A%201%29.html) | +28.23% | 130329.604482 | 156470.066785 | None |
| System.Net.Http.Tests.SocketsHttpHandlerPerfTest.Get_EnumerateHeaders_Unvalidated(ssl: False, chunkedResponse: False, responseLength: 100000) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.Net.Http.Tests.SocketsHttpHandlerPerfTest.Get_EnumerateHeaders_Unvalidated%28ssl%3A%20False%2C%20chunkedResponse%3A%20False%2C%20responseLength%3A%20100000%29.html) | +28.18% | 104090.807753 | 137475.948729 | None |
| System.IO.Tests.Perf_File.WriteAllTextAsync(size: 100000) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.IO.Tests.Perf_File.WriteAllTextAsync%28size%3A%20100000%29.html) | +27.99% | 219429.261136 | 295580.123659 | None |
| System.Net.WebSockets.Tests.SocketSendReceivePerfTest.ReceiveSend | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.Net.WebSockets.Tests.SocketSendReceivePerfTest.ReceiveSend.html) | +27.60% | 36140.937132 | 47409.087203 | None |
| System.IO.Pipes.Tests.Perf_NamedPipeStream.ReadWriteAsync(size: 1000000, Options: Asynchronous) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.IO.Pipes.Tests.Perf_NamedPipeStream.ReadWriteAsync%28size%3A%201000000%2C%20Options%3A%20Asynchronous%29.html) | +27.10% | 219145.277821 | 289039.970661 | None |
| System.Net.Http.Tests.SocketsHttpHandlerPerfTest.Get_EnumerateHeaders_Unvalidated(ssl: False, chunkedResponse: False, responseLength: 1) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.Net.Http.Tests.SocketsHttpHandlerPerfTest.Get_EnumerateHeaders_Unvalidated%28ssl%3A%20False%2C%20chunkedResponse%3A%20False%2C%20responseLength%3A%201%29.html) | +26.92% | 67798.803909 | 83436.418624 | None |
| System.Buffers.Tests.RentReturnArrayPoolTests<Object>.ProducerConsumer(RentalSize: 4096, ManipulateArray: True, Async: False, UseSharedPool: False) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.Buffers.Tests.RentReturnArrayPoolTests%28Object%29.ProducerConsumer%28RentalSize%3A%204096%2C%20ManipulateArray%3A%20True%2C%20Async%3A%20False%2C%20UseSharedPool%3A%20False%29.html) | +26.58% | 2217.028398 | 2893.332228 | None |
| System.Net.Http.Tests.SocketsHttpHandlerPerfTest.Get_EnumerateHeaders_Unvalidated(ssl: True, chunkedResponse: True, responseLength: 1) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.Net.Http.Tests.SocketsHttpHandlerPerfTest.Get_EnumerateHeaders_Unvalidated%28ssl%3A%20True%2C%20chunkedResponse%3A%20True%2C%20responseLength%3A%201%29.html) | +26.12% | 102223.788620 | 133080.248529 | None |
| System.Net.Http.Tests.SocketsHttpHandlerPerfTest.Get_EnumerateHeaders_Validated(ssl: False, chunkedResponse: True, responseLength: 1) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.Net.Http.Tests.SocketsHttpHandlerPerfTest.Get_EnumerateHeaders_Validated%28ssl%3A%20False%2C%20chunkedResponse%3A%20True%2C%20responseLength%3A%201%29.html) | +25.82% | 66355.682974 | 83189.406509 | None |
| System.IO.Tests.Perf_File.ReadAllLinesAsync | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.IO.Tests.Perf_File.ReadAllLinesAsync.html) | +25.81% | 51276.233747 | 63398.510432 | None |
| System.IO.Tests.Perf_FileStream.ReadAsync_NoBuffering(fileSize: 1048576, userBufferSize: 16384, options: Asynchronous) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.IO.Tests.Perf_FileStream.ReadAsync_NoBuffering%28fileSize%3A%201048576%2C%20userBufferSize%3A%2016384%2C%20options%3A%20Asynchronous%29.html) | +25.41% | 241485.464877 | 303118.429771 | None |
| System.Net.Http.Tests.SocketsHttpHandlerPerfTest.Get_EnumerateHeaders_Validated(ssl: False, chunkedResponse: False, responseLength: 1) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.Net.Http.Tests.SocketsHttpHandlerPerfTest.Get_EnumerateHeaders_Validated%28ssl%3A%20False%2C%20chunkedResponse%3A%20False%2C%20responseLength%3A%201%29.html) | +25.17% | 65651.586816 | 78553.823912 | None |
| System.Net.Http.Tests.SocketsHttpHandlerPerfTest.Get_EnumerateHeaders_Unvalidated(ssl: False, chunkedResponse: True, responseLength: 1) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.Net.Http.Tests.SocketsHttpHandlerPerfTest.Get_EnumerateHeaders_Unvalidated%28ssl%3A%20False%2C%20chunkedResponse%3A%20True%2C%20responseLength%3A%201%29.html) | +25.10% | 74104.288263 | 95273.553324 | None |
| System.Buffers.Tests.RentReturnArrayPoolTests<Byte>.MultipleSerial(RentalSize: 4096, ManipulateArray: False, Async: True, UseSharedPool: False) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.Buffers.Tests.RentReturnArrayPoolTests%28Byte%29.MultipleSerial%28RentalSize%3A%204096%2C%20ManipulateArray%3A%20False%2C%20Async%3A%20True%2C%20UseSharedPool%3A%20False%29.html) | +24.34% | 1142.536373 | 1473.563668 | None |
| System.Net.Http.Tests.SocketsHttpHandlerPerfTest.Get(ssl: False, chunkedResponse: True, responseLength: 1) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.Net.Http.Tests.SocketsHttpHandlerPerfTest.Get%28ssl%3A%20False%2C%20chunkedResponse%3A%20True%2C%20responseLength%3A%201%29.html) | +23.52% | 69223.313510 | 81800.602135 | None |
| System.Net.Http.Tests.SocketsHttpHandlerPerfTest.Get(ssl: False, chunkedResponse: False, responseLength: 1) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.Net.Http.Tests.SocketsHttpHandlerPerfTest.Get%28ssl%3A%20False%2C%20chunkedResponse%3A%20False%2C%20responseLength%3A%201%29.html) | +23.38% | 65104.099129 | 83926.493706 | None |
| System.Net.Http.Tests.SocketsHttpHandlerPerfTest.Get_EnumerateHeaders_Validated(ssl: True, chunkedResponse: False, responseLength: 1) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.Net.Http.Tests.SocketsHttpHandlerPerfTest.Get_EnumerateHeaders_Validated%28ssl%3A%20True%2C%20chunkedResponse%3A%20False%2C%20responseLength%3A%201%29.html) | +22.54% | 126747.725497 | 153853.482188 | None |
| System.IO.Tests.Perf_File.WriteAllBytesAsync(size: 16384) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.IO.Tests.Perf_File.WriteAllBytesAsync%28size%3A%2016384%29.html) | +21.90% | 92436.981303 | 107029.379164 | None |
| System.Formats.Tar.Tests.Perf_TarFile.ExtractToDirectory_Path_Async | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.Formats.Tar.Tests.Perf_TarFile.ExtractToDirectory_Path_Async.html) | +21.65% | 126487.124247 | 142742.012685 | None |
| System.Formats.Tar.Tests.Perf_TarFile.ExtractToDirectory_Stream_Async | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.Formats.Tar.Tests.Perf_TarFile.ExtractToDirectory_Stream_Async.html) | +21.57% | 138145.106134 | 157297.349570 | None |
| System.Net.Http.Tests.SocketsHttpHandlerPerfTest.Get_EnumerateHeaders_Unvalidated(ssl: True, chunkedResponse: False, responseLength: 100000) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.Net.Http.Tests.SocketsHttpHandlerPerfTest.Get_EnumerateHeaders_Unvalidated%28ssl%3A%20True%2C%20chunkedResponse%3A%20False%2C%20responseLength%3A%20100000%29.html) | +20.49% | 8306859.652679 | 13467445.639286 | None |
| System.Formats.Tar.Tests.Perf_TarFile.CreateFromDirectory_Stream_Async | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.Formats.Tar.Tests.Perf_TarFile.CreateFromDirectory_Stream_Async.html) | +20.16% | 133865.345171 | 169410.533662 | None |
| System.Buffers.Tests.RentReturnArrayPoolTests<Object>.ProducerConsumer(RentalSize: 4096, ManipulateArray: False, Async: True, UseSharedPool: True) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.Buffers.Tests.RentReturnArrayPoolTests%28Object%29.ProducerConsumer%28RentalSize%3A%204096%2C%20ManipulateArray%3A%20False%2C%20Async%3A%20True%2C%20UseSharedPool%3A%20True%29.html) | +20.15% | 2250.389435 | 2853.580717 | None |
| System.IO.Tests.Perf_RandomAccess.ReadScatterAsync(fileSize: 1048576, buffersSize: 16384, options: None) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.IO.Tests.Perf_RandomAccess.ReadScatterAsync%28fileSize%3A%201048576%2C%20buffersSize%3A%2016384%2C%20options%3A%20None%29.html) | +19.53% | 333914.046242 | 380585.919076 | None |
| System.Net.Http.Tests.SocketsHttpHandlerPerfTest.Get_EnumerateHeaders_Validated(ssl: True, chunkedResponse: True, responseLength: 1) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.Net.Http.Tests.SocketsHttpHandlerPerfTest.Get_EnumerateHeaders_Validated%28ssl%3A%20True%2C%20chunkedResponse%3A%20True%2C%20responseLength%3A%201%29.html) | +19.37% | 104075.032070 | 130787.768781 | None |
| System.IO.Tests.Perf_File.WriteAllBytesAsync(size: 4096) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.IO.Tests.Perf_File.WriteAllBytesAsync%28size%3A%204096%29.html) | +19.10% | 72212.640213 | 83423.229443 | None |
| System.IO.Tests.Perf_File.WriteAllTextAsync(size: 100) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.IO.Tests.Perf_File.WriteAllTextAsync%28size%3A%20100%29.html) | +18.70% | 75528.575376 | 88714.711007 | None |
| System.IO.Tests.Perf_File.AppendAllTextAsync(size: 100) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.IO.Tests.Perf_File.AppendAllTextAsync%28size%3A%20100%29.html) | +18.14% | 15208.924872 | 17324.975336 | None |
| System.IO.Tests.Perf_RandomAccess.ReadScatterAsync(fileSize: 1048576, buffersSize: 16384, options: Asynchronous) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.IO.Tests.Perf_RandomAccess.ReadScatterAsync%28fileSize%3A%201048576%2C%20buffersSize%3A%2016384%2C%20options%3A%20Asynchronous%29.html) | +16.07% | 318342.402519 | 370544.040028 | None |
| System.IO.Tests.Perf_File.AppendAllLinesAsync | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.IO.Tests.Perf_File.AppendAllLinesAsync.html) | +13.46% | 23292.728755 | 26688.970620 | None |
| System.IO.Tests.Perf_File.WriteAllBytesAsync(size: 512) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.IO.Tests.Perf_File.WriteAllBytesAsync%28size%3A%20512%29.html) | +11.42% | 71381.918426 | 82311.585540 | None |
| CscBench.CompileTest | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/CscBench.CompileTest.html) | +10.50% | 499018370.446428 | 536667261.500000 | None |
| System.Net.Security.Tests.SslStreamTests.ReadWriteAsync | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.Net.Security.Tests.SslStreamTests.ReadWriteAsync.html) | +8.83% | 34693.305333 | 42181.063943 | None |
| System.Net.Security.Tests.SslStreamTests.DefaultMutualHandshakeContextIPv6Async | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.Net.Security.Tests.SslStreamTests.DefaultMutualHandshakeContextIPv6Async.html) | +7.32% | 3868096.907087 | 4099399.708984 | [24](#24-13fef94591---support-getting-thread-id-in-gc-108207) |
| System.Net.Security.Tests.SslStreamTests.DefaultMutualHandshakeContextIPv4Async | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.Net.Security.Tests.SslStreamTests.DefaultMutualHandshakeContextIPv4Async.html) | +6.48% | 3801710.661281 | 4084877.318878 | [24](#24-13fef94591---support-getting-thread-id-in-gc-108207) |
| System.IO.Tests.Perf_FileStream.CopyToFileAsync(fileSize: 104857600, options: Asynchronous) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.IO.Tests.Perf_FileStream.CopyToFileAsync%28fileSize%3A%20104857600%2C%20options%3A%20Asynchronous%29.html) | +6.20% | 144165523.089286 | 153659835.696429 | None |
| System.Net.Http.Tests.SocketsHttpHandlerPerfTest.Get_EnumerateHeaders_Unvalidated(ssl: True, chunkedResponse: False, responseLength: 1) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.Net.Http.Tests.SocketsHttpHandlerPerfTest.Get_EnumerateHeaders_Unvalidated%28ssl%3A%20True%2C%20chunkedResponse%3A%20False%2C%20responseLength%3A%201%29.html) | +5.07% | 102799.814843 | 129871.046260 | None |

---

## 2. ffcd1c5442 - Trust single-edge synthetic profile (#116054)

**Date:** 2025-05-28 16:16:24
**Commit:** [ffcd1c5442](https://github.com/dotnet/runtime/commit/ffcd1c5442a0c6e5317efa46d6ce381003397476)
**Affected Tests:** 49

| Test Name | Link | Change | Before | After | Other Changepoints |
|-----------|------|--------|--------|-------|--------------------|
| System.Tests.Perf_Int32.ParseHex(value: "80000000") | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.Tests.Perf_Int32.ParseHex%28value%3A%20%2280000000%22%29.html) | +161.05% | 20.493603 | 53.429202 | None |
| System.Tests.Perf_Int32.ParseHex(value: "7FFFFFFF") | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.Tests.Perf_Int32.ParseHex%28value%3A%20%227FFFFFFF%22%29.html) | +161.02% | 20.461906 | 53.368506 | None |
| System.Memory.Span<Int32>.Reverse(Size: 512) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.Memory.Span%28Int32%29.Reverse%28Size%3A%20512%29.html) | +121.12% | 68.388976 | 151.311849 | None |
| System.Memory.Span<Char>.Reverse(Size: 512) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.Memory.Span%28Char%29.Reverse%28Size%3A%20512%29.html) | +79.50% | 41.281426 | 74.142616 | None |
| System.Text.Encodings.Web.Tests.Perf_Encoders.EncodeUtf16(arguments: Url,&lorem ipsum=dolor sit amet,16) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.Text.Encodings.Web.Tests.Perf_Encoders.EncodeUtf16%28arguments%3A%20Url%2C%26lorem%20ipsum%3Ddolor%20sit%20amet%2C16%29.html) | +78.11% | 85.841962 | 152.381635 | None |
| System.Text.Encodings.Web.Tests.Perf_Encoders.EncodeUtf16(arguments: JavaScript,&Hello+<World>!,16) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.Text.Encodings.Web.Tests.Perf_Encoders.EncodeUtf16%28arguments%3A%20JavaScript%2C%26Hello%2B%28World%29%21%2C16%29.html) | +74.25% | 72.616224 | 126.150858 | None |
| System.Collections.Concurrent.IsEmpty<String>.Queue(Size: 0) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.Collections.Concurrent.IsEmpty%28String%29.Queue%28Size%3A%200%29.html) | +51.70% | 8.961095 | 13.591754 | None |
| System.Text.Json.Tests.Perf_Basic.WriteBasicUtf8(Formatted: False, SkipValidation: False, DataSize: 100000) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.Text.Json.Tests.Perf_Basic.WriteBasicUtf8%28Formatted%3A%20False%2C%20SkipValidation%3A%20False%2C%20DataSize%3A%20100000%29.html) | +44.48% | 1967788.783324 | 2845587.121766 | [5](#5-34545d790e---jit-dont-mark-callees-noinline-for-non-fatal-observations-with-pgo-114821) |
| System.Collections.ContainsTrueComparer<Int32>.HashSet(Size: 512) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.Collections.ContainsTrueComparer%28Int32%29.HashSet%28Size%3A%20512%29.html) | +43.78% | 5171.150888 | 7283.701569 | None |
| System.Text.Encodings.Web.Tests.Perf_Encoders.EncodeUtf16(arguments: UnsafeRelaxed,hello "there",16) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.Text.Encodings.Web.Tests.Perf_Encoders.EncodeUtf16%28arguments%3A%20UnsafeRelaxed%2Chello%20%22there%22%2C16%29.html) | +38.12% | 41.774188 | 57.118293 | [15](#15-0fa747abd5---replace-optimizedinboxtextencoder-vectorization-with-searchvalues-114494) |
| System.Text.Encodings.Web.Tests.Perf_Encoders.EncodeUtf16(arguments: Url,�2020,16) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.Text.Encodings.Web.Tests.Perf_Encoders.EncodeUtf16%28arguments%3A%20Url%2C%EF%BF%BD2020%2C16%29.html) | +36.06% | 48.200426 | 65.625433 | [15](#15-0fa747abd5---replace-optimizedinboxtextencoder-vectorization-with-searchvalues-114494) |
| System.Text.Json.Tests.Perf_Basic.WriteBasicUtf8(Formatted: False, SkipValidation: True, DataSize: 100000) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.Text.Json.Tests.Perf_Basic.WriteBasicUtf8%28Formatted%3A%20False%2C%20SkipValidation%3A%20True%2C%20DataSize%3A%20100000%29.html) | +35.60% | 1856784.735147 | 2511933.996164 | [5](#5-34545d790e---jit-dont-mark-callees-noinline-for-non-fatal-observations-with-pgo-114821) |
| System.Collections.Concurrent.IsEmpty<String>.Queue(Size: 512) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.Collections.Concurrent.IsEmpty%28String%29.Queue%28Size%3A%20512%29.html) | +34.86% | 8.829297 | 11.886044 | None |
| System.Text.Encodings.Web.Tests.Perf_Encoders.EncodeUtf16(arguments: Url,&lorem ipsum=dolor sit amet,512) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.Text.Encodings.Web.Tests.Perf_Encoders.EncodeUtf16%28arguments%3A%20Url%2C%26lorem%20ipsum%3Ddolor%20sit%20amet%2C512%29.html) | +33.14% | 189.284811 | 250.609875 | None |
| System.Memory.ReadOnlySequence.Slice_StartPosition_And_EndPosition(Segment: Multiple) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.Memory.ReadOnlySequence.Slice_StartPosition_And_EndPosition%28Segment%3A%20Multiple%29.html) | +29.93% | 11.431075 | 15.311321 | None |
| System.Text.Encodings.Web.Tests.Perf_Encoders.EncodeUtf16(arguments: JavaScript,&Hello+<World>!,512) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.Text.Encodings.Web.Tests.Perf_Encoders.EncodeUtf16%28arguments%3A%20JavaScript%2C%26Hello%2B%28World%29%21%2C512%29.html) | +29.13% | 175.427144 | 226.881080 | None |
| System.Threading.Tests.Perf_SemaphoreSlim.ReleaseWait | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.Threading.Tests.Perf_SemaphoreSlim.ReleaseWait.html) | +25.03% | 57.781287 | 72.742664 | None |
| System.Collections.ContainsFalse<Int32>.ICollection(Size: 512) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.Collections.ContainsFalse%28Int32%29.ICollection%28Size%3A%20512%29.html) | +24.75% | 53851.318274 | 67049.399042 | [3](#3-41be5e229b---jit-graph-based-loop-inversion-116017) |
| System.Collections.ContainsFalse<Int32>.List(Size: 512) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.Collections.ContainsFalse%28Int32%29.List%28Size%3A%20512%29.html) | +24.23% | 53876.621334 | 66774.560338 | [3](#3-41be5e229b---jit-graph-based-loop-inversion-116017) |
| System.Memory.Span<Int32>.StartsWith(Size: 4) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.Memory.Span%28Int32%29.StartsWith%28Size%3A%204%29.html) | +23.49% | 4.097634 | 4.955961 | None |
| System.Memory.Span<Int32>.SequenceEqual(Size: 4) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.Memory.Span%28Int32%29.SequenceEqual%28Size%3A%204%29.html) | +22.17% | 3.860062 | 4.337260 | None |
| System.Tests.Perf_Enum.InterpolateIntoString(value: 32) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.Tests.Perf_Enum.InterpolateIntoString%28value%3A%2032%29.html) | +21.86% | 142.380814 | 172.877631 | None |
| System.Collections.CtorFromCollectionNonGeneric<String>.Hashtable(Size: 512) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.Collections.CtorFromCollectionNonGeneric%28String%29.Hashtable%28Size%3A%20512%29.html) | +21.16% | 34607.126240 | 42233.948304 | None |
| System.IO.Hashing.Tests.Crc64_AppendPerf.Append(BufferSize: 16) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.IO.Hashing.Tests.Crc64_AppendPerf.Append%28BufferSize%3A%2016%29.html) | +19.16% | 12.736726 | 15.165888 | None |
| System.Tests.Perf_Enum.GetValuesAsUnderlyingType_Generic | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.Tests.Perf_Enum.GetValuesAsUnderlyingType_Generic.html) | +18.62% | 26.598461 | 31.064422 | [11](#11-14cd365a64---jit-dont-try-to-create-fallthrough-from-try-region-into-cloned-finally-109788) |
| System.Tests.Perf_Decimal.Mod | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.Tests.Perf_Decimal.Mod.html) | +17.47% | 11.246185 | 12.957709 | [7](#7-2a2b7dc72b---jit-fix-profile-maintenance-in-optsetblockweights-funclet-creation-111736), [8](#8-e2ad5fcc17---arm64-implement-region-write-barriers-111636) |
| System.Tests.Perf_Version.Parse3 | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.Tests.Perf_Version.Parse3.html) | +17.12% | 50.756169 | 58.539354 | None |
| System.Text.Encodings.Web.Tests.Perf_Encoders.EncodeUtf8(arguments: UnsafeRelaxed,hello "there",16) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.Text.Encodings.Web.Tests.Perf_Encoders.EncodeUtf8%28arguments%3A%20UnsafeRelaxed%2Chello%20%22there%22%2C16%29.html) | +17.01% | 33.562457 | 39.164661 | [15](#15-0fa747abd5---replace-optimizedinboxtextencoder-vectorization-with-searchvalues-114494) |
| System.Collections.CtorFromCollection<Int32>.ConcurrentBag(Size: 512) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.Collections.CtorFromCollection%28Int32%29.ConcurrentBag%28Size%3A%20512%29.html) | +15.97% | 10621.840335 | 12412.708145 | None |
| System.Tests.Perf_Version.Parse4 | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.Tests.Perf_Version.Parse4.html) | +15.77% | 65.032537 | 74.496944 | None |
| System.Tests.Perf_Version.TryParse4 | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.Tests.Perf_Version.TryParse4.html) | +14.86% | 61.689763 | 70.715003 | None |
| System.Linq.Tests.Perf_Enumerable.Intersect(input: IEnumerable) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.Linq.Tests.Perf_Enumerable.Intersect%28input%3A%20IEnumerable%29.html) | +13.85% | 3898.405617 | 4443.535125 | None |
| System.Buffers.Tests.SearchValuesByteTests.IndexOfAnyExcept(Values: "abcdefABCDEF0123456789Ü") | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.Buffers.Tests.SearchValuesByteTests.IndexOfAnyExcept%28Values%3A%20%22abcdefABCDEF0123456789%C3%9C%22%29.html) | +13.78% | 26.250423 | 29.973038 | None |
| System.Linq.Tests.Perf_Enumerable.ToDictionary(input: IEnumerable) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.Linq.Tests.Perf_Enumerable.ToDictionary%28input%3A%20IEnumerable%29.html) | +13.47% | 2394.239315 | 2637.260596 | None |
| System.Tests.Perf_Version.TryParse3 | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.Tests.Perf_Version.TryParse3.html) | +13.33% | 48.742521 | 55.537563 | None |
| System.Tests.Perf_Version.TryParse2 | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.Tests.Perf_Version.TryParse2.html) | +12.97% | 35.000673 | 39.564198 | None |
| System.Tests.Perf_Version.Parse2 | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.Tests.Perf_Version.Parse2.html) | +11.99% | 36.873300 | 42.510045 | None |
| System.Text.Json.Serialization.Tests.WriteJson<ImmutableSortedDictionary<String, String>>.SerializeToWriter(Mode: SourceGen) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.Text.Json.Serialization.Tests.WriteJson%28ImmutableSortedDictionary%28String%2C%20String%29%29.SerializeToWriter%28Mode%3A%20SourceGen%29.html) | +11.44% | 9147.970689 | 10097.218159 | None |
| System.Text.Encodings.Web.Tests.Perf_Encoders.EncodeUtf16(arguments: UnsafeRelaxed,hello "there",512) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.Text.Encodings.Web.Tests.Perf_Encoders.EncodeUtf16%28arguments%3A%20UnsafeRelaxed%2Chello%20%22there%22%2C512%29.html) | +10.77% | 143.039916 | 158.452140 | None |
| System.IO.Tests.Perf_File.AppendAllTextAsync(size: 10000) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.IO.Tests.Perf_File.AppendAllTextAsync%28size%3A%2010000%29.html) | +10.10% | 28736.234200 | 33442.079444 | [18](#18-254b55a49e---enable-loop-cloning-for-spans-113575) |
| System.IO.Tests.Perf_Path.GetFileName | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.IO.Tests.Perf_Path.GetFileName.html) | +10.10% | 31.290566 | 35.318632 | None |
| System.Linq.Tests.Perf_Enumerable.Except(input: IEnumerable) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.Linq.Tests.Perf_Enumerable.Except%28input%3A%20IEnumerable%29.html) | +9.41% | 3368.199963 | 3663.109643 | None |
| System.Buffers.Text.Tests.Base64Tests.Base64EncodeDestinationTooSmall(NumberOfBytes: 1000) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.Buffers.Text.Tests.Base64Tests.Base64EncodeDestinationTooSmall%28NumberOfBytes%3A%201000%29.html) | +8.40% | 178.239783 | 193.264313 | None |
| System.Text.Encodings.Web.Tests.Perf_Encoders.EncodeUtf8(arguments: Url,&lorem ipsum=dolor sit amet,16) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.Text.Encodings.Web.Tests.Perf_Encoders.EncodeUtf8%28arguments%3A%20Url%2C%26lorem%20ipsum%3Ddolor%20sit%20amet%2C16%29.html) | +8.20% | 65.967826 | 70.876888 | [15](#15-0fa747abd5---replace-optimizedinboxtextencoder-vectorization-with-searchvalues-114494) |
| System.Text.Encodings.Web.Tests.Perf_Encoders.EncodeUtf16(arguments: Url,�2020,512) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.Text.Encodings.Web.Tests.Perf_Encoders.EncodeUtf16%28arguments%3A%20Url%2C%EF%BF%BD2020%2C512%29.html) | +7.58% | 151.544996 | 163.472073 | [15](#15-0fa747abd5---replace-optimizedinboxtextencoder-vectorization-with-searchvalues-114494) |
| System.Text.Encodings.Web.Tests.Perf_Encoders.EncodeUtf8(arguments: UnsafeRelaxed,hello "there",512) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.Text.Encodings.Web.Tests.Perf_Encoders.EncodeUtf8%28arguments%3A%20UnsafeRelaxed%2Chello%20%22there%22%2C512%29.html) | +6.35% | 94.733892 | 101.305540 | [15](#15-0fa747abd5---replace-optimizedinboxtextencoder-vectorization-with-searchvalues-114494) |
| System.Tests.Perf_Enum.InterpolateIntoSpan_Flags(value: 32) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.Tests.Perf_Enum.InterpolateIntoSpan_Flags%28value%3A%2032%29.html) | +6.16% | 112.080035 | 118.861415 | None |
| System.Tests.Perf_Enum.InterpolateIntoSpan_NonFlags(value: 42) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.Tests.Perf_Enum.InterpolateIntoSpan_NonFlags%28value%3A%2042%29.html) | +6.04% | 120.816726 | 127.596505 | None |
| System.Buffers.Text.Tests.Utf8ParserTests.TryParseDecimal(value: 123456.789) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.Buffers.Text.Tests.Utf8ParserTests.TryParseDecimal%28value%3A%20123456.789%29.html) | +5.25% | N/A | N/A | None |

---

## 3. 41be5e229b - JIT: Graph-based loop inversion (#116017)

**Date:** 2025-06-04 14:39:50
**Commit:** [41be5e229b](https://github.com/dotnet/runtime/commit/41be5e229b30fc3e7aaed9361b9db4487c5bb7f8)
**Affected Tests:** 36

| Test Name | Link | Change | Before | After | Other Changepoints |
|-----------|------|--------|--------|-------|--------------------|
| Burgers.Test0 | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/Burgers.Test0.html) | +654.97% | 331324004.160714 | 2475280704.857143 | None |
| Benchstone.BenchF.Simpsn.Test | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/Benchstone.BenchF.Simpsn.Test.html) | +78.96% | 179804915.339286 | 342799731.089286 | None |
| Benchstone.BenchI.AddArray2.Test | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/Benchstone.BenchI.AddArray2.Test.html) | +60.04% | 9152356.182540 | 14619914.208333 | None |
| System.Collections.ContainsFalse<Int32>.Queue(Size: 512) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.Collections.ContainsFalse%28Int32%29.Queue%28Size%3A%20512%29.html) | +25.69% | 54594.806595 | 68455.714938 | None |
| System.Collections.ContainsFalse<Int32>.ICollection(Size: 512) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.Collections.ContainsFalse%28Int32%29.ICollection%28Size%3A%20512%29.html) | +24.75% | 53851.318274 | 67049.399042 | [2](#2-ffcd1c5442---trust-single-edge-synthetic-profile-116054) |
| System.Collections.ContainsFalse<Int32>.List(Size: 512) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.Collections.ContainsFalse%28Int32%29.List%28Size%3A%20512%29.html) | +24.23% | 53876.621334 | 66774.560338 | [2](#2-ffcd1c5442---trust-single-edge-synthetic-profile-116054) |
| System.Collections.ContainsFalse<Int32>.Array(Size: 512) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.Collections.ContainsFalse%28Int32%29.Array%28Size%3A%20512%29.html) | +21.98% | 54623.899931 | 66691.759413 | None |
| System.Collections.ContainsFalse<Int32>.ImmutableArray(Size: 512) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.Collections.ContainsFalse%28Int32%29.ImmutableArray%28Size%3A%20512%29.html) | +19.48% | 55536.810682 | 66478.176967 | None |
| System.Collections.ContainsFalse<Int32>.Span(Size: 512) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.Collections.ContainsFalse%28Int32%29.Span%28Size%3A%20512%29.html) | +18.82% | 55938.400185 | 66353.253250 | None |
| System.Collections.ContainsTrue<Int32>.Stack(Size: 512) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.Collections.ContainsTrue%28Int32%29.Stack%28Size%3A%20512%29.html) | +18.79% | 32508.175120 | 37665.216544 | None |
| SciMark2.kernel.benchSparseMult | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/SciMark2.kernel.benchSparseMult.html) | +18.24% | 919930046.910714 | 1087784852.250000 | [13](#13-1c10ceecbf---jit-add-3-opt-implementation-for-improving-upon-rpo-based-block-layout-103450) |
| JetStream.TimeSeriesSegmentation.MaximizeSchwarzCriterion | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/JetStream.TimeSeriesSegmentation.MaximizeSchwarzCriterion.html) | +16.01% | 77465356.964286 | 90120601.505952 | None |
| Benchstone.BenchI.Array2.Test | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/Benchstone.BenchI.Array2.Test.html) | +15.43% | 1348032152.321428 | 1555925549.160714 | [13](#13-1c10ceecbf---jit-add-3-opt-implementation-for-improving-upon-rpo-based-block-layout-103450), [27](#27-d410898949---jit-skip-fgcomputemissingblockweights-when-we-have-profile-data-111873) |
| ByteMark.BenchNeuralJagged | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/ByteMark.BenchNeuralJagged.html) | +12.85% | 937422862.767857 | 1054728157.821429 | None |
| System.Numerics.Tests.Perf_BigInteger.ModPow(arguments: 16384,16384,64 bits) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.Numerics.Tests.Perf_BigInteger.ModPow%28arguments%3A%2016384%2C16384%2C64%20bits%29.html) | +12.73% | 1950801.181641 | 2184383.142578 | [12](#12-ddf8075a2f---jit-visit-blocks-in-rpo-during-lsra-107927) |
| System.Collections.Perf_SingleCharFrozenDictionary.TryGetValue_False_FrozenDictionary(Count: 10000) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.Collections.Perf_SingleCharFrozenDictionary.TryGetValue_False_FrozenDictionary%28Count%3A%2010000%29.html) | +10.72% | N/A | N/A | None |
| BenchmarksGame.ReverseComplement_1.RunBench | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/BenchmarksGame.ReverseComplement_1.RunBench.html) | +10.42% | 692168.626068 | 773129.819495 | None |
| System.Collections.Tests.Perf_Dictionary.Clone(Items: 3000) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.Collections.Tests.Perf_Dictionary.Clone%28Items%3A%203000%29.html) | +9.68% | 13096.742466 | 14309.082178 | None |
| System.Collections.CtorFromCollection<Int32>.Dictionary(Size: 512) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.Collections.CtorFromCollection%28Int32%29.Dictionary%28Size%3A%20512%29.html) | +9.62% | 2383.274697 | 2639.814366 | None |
| System.Collections.ContainsTrue<Int32>.Span(Size: 512) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.Collections.ContainsTrue%28Int32%29.Span%28Size%3A%20512%29.html) | +8.34% | N/A | N/A | None |
| System.Tests.Perf_Int32.TryFormat(value: 4) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.Tests.Perf_Int32.TryFormat%28value%3A%204%29.html) | +8.22% | 4.964687 | 5.255960 | [7](#7-2a2b7dc72b---jit-fix-profile-maintenance-in-optsetblockweights-funclet-creation-111736) |
| System.Text.Json.Tests.Utf8JsonReaderCommentsTests.Utf8JsonReaderCommentParsing(CommentHandling: Skip, SegmentSize: 0, TestCase: LongMultiLine) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.Text.Json.Tests.Utf8JsonReaderCommentsTests.Utf8JsonReaderCommentParsing%28CommentHandling%3A%20Skip%2C%20SegmentSize%3A%200%2C%20TestCase%3A%20LongMultiLine%29.html) | +8.05% | 4254.722756 | 4602.573816 | [5](#5-34545d790e---jit-dont-mark-callees-noinline-for-non-fatal-observations-with-pgo-114821), [17](#17-1af7c2370b---add-a-number-of-additional-apis-to-the-various-simd-accelerated-vector-types-111179) |
| System.Collections.Perf_Frozen<Int16>.TryGetValue_True(Count: 4) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.Collections.Perf_Frozen%28Int16%29.TryGetValue_True%28Count%3A%204%29.html) | +7.77% | 19.116617 | 21.387935 | None |
| System.IO.Tests.Perf_StreamWriter.WriteCharArray(writeLength: 2) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.IO.Tests.Perf_StreamWriter.WriteCharArray%28writeLength%3A%202%29.html) | +7.37% | N/A | N/A | None |
| ByteMark.BenchNumericSortJagged | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/ByteMark.BenchNumericSortJagged.html) | +7.17% | 1165604040.696428 | 1270208078.000000 | None |
| System.Tests.Perf_Boolean.ToString(value: False) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.Tests.Perf_Boolean.ToString%28value%3A%20False%29.html) | +6.99% | 4.275444 | 4.840564 | None |
| PerfLabTests.GetMember.GetMethod2 | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/PerfLabTests.GetMember.GetMethod2.html) | +6.88% | 167115.265036 | 179357.152262 | None |
| Span.Sorting.BubbleSortArray(Size: 512) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/Span.Sorting.BubbleSortArray%28Size%3A%20512%29.html) | +6.78% | 229633.784375 | 245627.347659 | None |
| System.Tests.Perf_Boolean.ToString(value: True) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.Tests.Perf_Boolean.ToString%28value%3A%20True%29.html) | +6.46% | 4.295366 | 4.963439 | None |
| System.Collections.Perf_Frozen<Int16>.TryGetValue_True(Count: 64) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.Collections.Perf_Frozen%28Int16%29.TryGetValue_True%28Count%3A%2064%29.html) | +5.99% | 380.729280 | 402.831421 | None |
| PerfLabTests.GetMember.GetMethod20 | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/PerfLabTests.GetMember.GetMethod20.html) | +5.93% | N/A | N/A | None |
| System.Collections.Perf_Frozen<Int16>.TryGetValue_True(Count: 512) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.Collections.Perf_Frozen%28Int16%29.TryGetValue_True%28Count%3A%20512%29.html) | +5.73% | 3010.753143 | 3188.206634 | None |
| System.Text.Json.Tests.Utf8JsonReaderCommentsTests.Utf8JsonReaderCommentParsing(CommentHandling: Allow, SegmentSize: 0, TestCase: LongMultiLine) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.Text.Json.Tests.Utf8JsonReaderCommentsTests.Utf8JsonReaderCommentParsing%28CommentHandling%3A%20Allow%2C%20SegmentSize%3A%200%2C%20TestCase%3A%20LongMultiLine%29.html) | +5.67% | 4263.284606 | 4522.115674 | [17](#17-1af7c2370b---add-a-number-of-additional-apis-to-the-various-simd-accelerated-vector-types-111179) |
| System.Text.Perf_Utf8Encoding.GetByteCount(Input: EnglishAllAscii) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.Text.Perf_Utf8Encoding.GetByteCount%28Input%3A%20EnglishAllAscii%29.html) | +5.64% | 8487.613072 | 8969.671549 | None |
| Burgers.Test2 | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/Burgers.Test2.html) | +5.37% | 277044815.785714 | 291030700.678571 | [28](#28-ea43e17c95---jit-run-profile-repair-after-frontend-phases-111915) |
| System.Collections.ContainsKeyTrue<Int32, Int32>.FrozenDictionary(Size: 512) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.Collections.ContainsKeyTrue%28Int32%2C%20Int32%29.FrozenDictionary%28Size%3A%20512%29.html) | +5.23% | 2487.526510 | 2612.996482 | None |

---

## 4. b146d7512c - JIT: Move loop inversion to after loop recognition (#115850)

**Date:** 2025-06-14 17:22:46
**Commit:** [b146d7512c](https://github.com/dotnet/runtime/commit/b146d7512ce67051e127ab48dc2d4f65d30e818f)
**Affected Tests:** 36

| Test Name | Link | Change | Before | After | Other Changepoints |
|-----------|------|--------|--------|-------|--------------------|
| System.Tests.Perf_Byte.TryParse(value: "255") | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.Tests.Perf_Byte.TryParse%28value%3A%20%22255%22%29.html) | +57.05% | 13.704667 | 21.515341 | None |
| System.Tests.Perf_UInt64.TryParse(value: "12345") | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.Tests.Perf_UInt64.TryParse%28value%3A%20%2212345%22%29.html) | +49.80% | 15.464010 | 23.171181 | None |
| System.Tests.Perf_SByte.TryParse(value: "-128") | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.Tests.Perf_SByte.TryParse%28value%3A%20%22-128%22%29.html) | +48.99% | 15.147520 | 22.715115 | None |
| System.Tests.Perf_Int16.TryParse(value: "-32768") | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.Tests.Perf_Int16.TryParse%28value%3A%20%22-32768%22%29.html) | +45.55% | 17.412819 | 25.311962 | None |
| System.Tests.Perf_Int32.ParseSpan(value: "12345") | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.Tests.Perf_Int32.ParseSpan%28value%3A%20%2212345%22%29.html) | +45.19% | 14.910517 | 21.492033 | None |
| System.Tests.Perf_Int16.TryParse(value: "32767") | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.Tests.Perf_Int16.TryParse%28value%3A%20%2232767%22%29.html) | +45.19% | 17.198045 | 25.001586 | None |
| System.Tests.Perf_Int32.Parse(value: "12345") | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.Tests.Perf_Int32.Parse%28value%3A%20%2212345%22%29.html) | +45.16% | 14.971739 | 21.703707 | None |
| System.Tests.Perf_UInt32.ParseSpan(value: "12345") | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.Tests.Perf_UInt32.ParseSpan%28value%3A%20%2212345%22%29.html) | +44.93% | 15.035263 | 21.795727 | None |
| System.Tests.Perf_UInt64.ParseSpan(value: "12345") | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.Tests.Perf_UInt64.ParseSpan%28value%3A%20%2212345%22%29.html) | +44.84% | 15.343733 | 22.191164 | None |
| System.Tests.Perf_UInt32.Parse(value: "12345") | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.Tests.Perf_UInt32.Parse%28value%3A%20%2212345%22%29.html) | +44.15% | 15.029788 | 21.685986 | None |
| System.Tests.Perf_UInt16.Parse(value: "65535") | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.Tests.Perf_UInt16.Parse%28value%3A%20%2265535%22%29.html) | +43.60% | 16.185018 | 23.186593 | None |
| System.Tests.Perf_UInt16.Parse(value: "12345") | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.Tests.Perf_UInt16.Parse%28value%3A%20%2212345%22%29.html) | +41.82% | 16.144404 | 23.056438 | None |
| System.Tests.Perf_UInt16.TryParse(value: "65535") | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.Tests.Perf_UInt16.TryParse%28value%3A%20%2265535%22%29.html) | +41.64% | 16.245913 | 22.971181 | None |
| System.Tests.Perf_Int16.Parse(value: "-32768") | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.Tests.Perf_Int16.Parse%28value%3A%20%22-32768%22%29.html) | +41.46% | 17.325951 | 24.222764 | None |
| System.Tests.Perf_Int16.Parse(value: "32767") | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.Tests.Perf_Int16.Parse%28value%3A%20%2232767%22%29.html) | +39.29% | 17.179523 | 23.943490 | None |
| System.Tests.Perf_UInt32.Parse(value: "4294967295") | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.Tests.Perf_UInt32.Parse%28value%3A%20%224294967295%22%29.html) | +34.12% | 21.308499 | 28.402339 | None |
| System.Tests.Perf_UInt32.ParseSpan(value: "4294967295") | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.Tests.Perf_UInt32.ParseSpan%28value%3A%20%224294967295%22%29.html) | +33.79% | 21.163862 | 28.317937 | None |
| System.Tests.Perf_Int32.Parse(value: "2147483647") | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.Tests.Perf_Int32.Parse%28value%3A%20%222147483647%22%29.html) | +33.43% | 21.543521 | 28.727164 | None |
| System.Tests.Perf_Int32.Parse(value: "-2147483648") | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.Tests.Perf_Int32.Parse%28value%3A%20%22-2147483648%22%29.html) | +32.88% | 21.620610 | 28.824759 | None |
| System.Tests.Perf_UInt32.TryParse(value: "4294967295") | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.Tests.Perf_UInt32.TryParse%28value%3A%20%224294967295%22%29.html) | +32.81% | 21.148439 | 28.013722 | None |
| System.Tests.Perf_Int32.TryParseSpan(value: "2147483647") | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.Tests.Perf_Int32.TryParseSpan%28value%3A%20%222147483647%22%29.html) | +32.71% | 21.576257 | 28.290276 | None |
| System.Tests.Perf_Int32.TryParse(value: "2147483647") | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.Tests.Perf_Int32.TryParse%28value%3A%20%222147483647%22%29.html) | +31.43% | 21.600017 | 28.387948 | None |
| System.Tests.Perf_Int32.TryParseSpan(value: "-2147483648") | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.Tests.Perf_Int32.TryParseSpan%28value%3A%20%22-2147483648%22%29.html) | +30.91% | 21.740919 | 28.487668 | None |
| System.Tests.Perf_Int32.TryParse(value: "-2147483648") | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.Tests.Perf_Int32.TryParse%28value%3A%20%22-2147483648%22%29.html) | +30.87% | 21.760052 | 28.434334 | None |
| System.Tests.Perf_Int64.TryParse(value: "9223372036854775807") | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.Tests.Perf_Int64.TryParse%28value%3A%20%229223372036854775807%22%29.html) | +19.53% | 36.434277 | 43.498243 | None |
| System.Tests.Perf_Int64.ParseSpan(value: "9223372036854775807") | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.Tests.Perf_Int64.ParseSpan%28value%3A%20%229223372036854775807%22%29.html) | +19.18% | 36.582366 | 43.538841 | None |
| System.Tests.Perf_Int64.TryParseSpan(value: "9223372036854775807") | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.Tests.Perf_Int64.TryParseSpan%28value%3A%20%229223372036854775807%22%29.html) | +18.92% | 36.558177 | 43.514782 | None |
| System.Tests.Perf_Int64.Parse(value: "9223372036854775807") | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.Tests.Perf_Int64.Parse%28value%3A%20%229223372036854775807%22%29.html) | +18.85% | 36.730237 | 43.657066 | None |
| System.Tests.Perf_Int64.TryParse(value: "-9223372036854775808") | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.Tests.Perf_Int64.TryParse%28value%3A%20%22-9223372036854775808%22%29.html) | +18.49% | 36.907621 | 43.739154 | None |
| System.Tests.Perf_Int64.ParseSpan(value: "-9223372036854775808") | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.Tests.Perf_Int64.ParseSpan%28value%3A%20%22-9223372036854775808%22%29.html) | +18.30% | 36.968665 | 43.770247 | None |
| System.Tests.Perf_Int64.TryParseSpan(value: "-9223372036854775808") | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.Tests.Perf_Int64.TryParseSpan%28value%3A%20%22-9223372036854775808%22%29.html) | +18.14% | 36.958694 | 43.729003 | None |
| System.Tests.Perf_Int64.Parse(value: "-9223372036854775808") | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.Tests.Perf_Int64.Parse%28value%3A%20%22-9223372036854775808%22%29.html) | +18.02% | 37.074221 | 43.772702 | None |
| Benchmark.GetChildKeysTests.AddChainedConfigurationNoDelimiter | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/Benchmark.GetChildKeysTests.AddChainedConfigurationNoDelimiter.html) | +16.79% | 592239.658316 | 684986.842369 | None |
| System.IO.Tests.Perf_FileInfo.ctor_str | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.IO.Tests.Perf_FileInfo.ctor_str.html) | +16.06% | 139.465925 | 164.718486 | None |
| System.Collections.ContainsKeyTrue<String, String>.SortedList(Size: 512) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.Collections.ContainsKeyTrue%28String%2C%20String%29.SortedList%28Size%3A%20512%29.html) | +14.23% | 231979.593288 | 265362.325448 | None |
| System.Tests.Perf_Array.ArrayAssign2D | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.Tests.Perf_Array.ArrayAssign2D.html) | +12.55% | 744475.137117 | 812150.889321 | [6](#6-f9b2a6d4b3---jit-optimize-for-cost-instead-of-score-in-3-opt-layout-109741) |

---

## 5. 34545d790e - JIT: don't mark callees noinline for non-fatal observations with pgo (#114821)

**Date:** 2025-04-21 02:03:19
**Commit:** [34545d790e](https://github.com/dotnet/runtime/commit/34545d790e0f92be34b13f0d41b7df93f04bbe02)
**Affected Tests:** 25

| Test Name | Link | Change | Before | After | Other Changepoints |
|-----------|------|--------|--------|-------|--------------------|
| System.Text.Json.Tests.Perf_Basic.WriteBasicUtf8(Formatted: False, SkipValidation: False, DataSize: 100000) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.Text.Json.Tests.Perf_Basic.WriteBasicUtf8%28Formatted%3A%20False%2C%20SkipValidation%3A%20False%2C%20DataSize%3A%20100000%29.html) | +44.48% | 1967788.783324 | 2845587.121766 | [2](#2-ffcd1c5442---trust-single-edge-synthetic-profile-116054) |
| System.Buffers.Tests.RentReturnArrayPoolTests<Byte>.MultipleSerial(RentalSize: 4096, ManipulateArray: False, Async: False, UseSharedPool: False) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.Buffers.Tests.RentReturnArrayPoolTests%28Byte%29.MultipleSerial%28RentalSize%3A%204096%2C%20ManipulateArray%3A%20False%2C%20Async%3A%20False%2C%20UseSharedPool%3A%20False%29.html) | +42.85% | 346.376216 | 497.890873 | [10](#10-34f1db49db---jit-use-root-compiler-instance-for-sufficient-pgo-observation-115119) |
| System.Buffers.Tests.RentReturnArrayPoolTests<Object>.MultipleSerial(RentalSize: 4096, ManipulateArray: False, Async: False, UseSharedPool: False) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.Buffers.Tests.RentReturnArrayPoolTests%28Object%29.MultipleSerial%28RentalSize%3A%204096%2C%20ManipulateArray%3A%20False%2C%20Async%3A%20False%2C%20UseSharedPool%3A%20False%29.html) | +41.05% | 345.916598 | 489.710332 | [10](#10-34f1db49db---jit-use-root-compiler-instance-for-sufficient-pgo-observation-115119) |
| System.Text.Json.Tests.Perf_Basic.WriteBasicUtf8(Formatted: False, SkipValidation: True, DataSize: 100000) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.Text.Json.Tests.Perf_Basic.WriteBasicUtf8%28Formatted%3A%20False%2C%20SkipValidation%3A%20True%2C%20DataSize%3A%20100000%29.html) | +35.60% | 1856784.735147 | 2511933.996164 | [2](#2-ffcd1c5442---trust-single-edge-synthetic-profile-116054) |
| System.Buffers.Tests.RentReturnArrayPoolTests<Byte>.SingleSerial(RentalSize: 4096, ManipulateArray: False, Async: False, UseSharedPool: False) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.Buffers.Tests.RentReturnArrayPoolTests%28Byte%29.SingleSerial%28RentalSize%3A%204096%2C%20ManipulateArray%3A%20False%2C%20Async%3A%20False%2C%20UseSharedPool%3A%20False%29.html) | +29.45% | 40.025178 | 51.021273 | [10](#10-34f1db49db---jit-use-root-compiler-instance-for-sufficient-pgo-observation-115119) |
| System.Buffers.Tests.RentReturnArrayPoolTests<Object>.SingleSerial(RentalSize: 4096, ManipulateArray: False, Async: False, UseSharedPool: False) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.Buffers.Tests.RentReturnArrayPoolTests%28Object%29.SingleSerial%28RentalSize%3A%204096%2C%20ManipulateArray%3A%20False%2C%20Async%3A%20False%2C%20UseSharedPool%3A%20False%29.html) | +21.19% | 42.108170 | 50.853902 | [10](#10-34f1db49db---jit-use-root-compiler-instance-for-sufficient-pgo-observation-115119) |
| System.Buffers.Tests.RentReturnArrayPoolTests<Object>.SingleParallel(RentalSize: 4096, ManipulateArray: False, Async: True, UseSharedPool: False) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.Buffers.Tests.RentReturnArrayPoolTests%28Object%29.SingleParallel%28RentalSize%3A%204096%2C%20ManipulateArray%3A%20False%2C%20Async%3A%20True%2C%20UseSharedPool%3A%20False%29.html) | +18.45% | 56122.175109 | 65117.824271 | None |
| System.Collections.CreateAddAndClear<String>.Stack(Size: 512) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.Collections.CreateAddAndClear%28String%29.Stack%28Size%3A%20512%29.html) | +15.66% | 4202.479875 | 4553.577818 | [10](#10-34f1db49db---jit-use-root-compiler-instance-for-sufficient-pgo-observation-115119) |
| System.Collections.AddGivenSize<String>.Stack(Size: 512) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.Collections.AddGivenSize%28String%29.Stack%28Size%3A%20512%29.html) | +15.63% | 3348.170226 | 3769.311446 | [10](#10-34f1db49db---jit-use-root-compiler-instance-for-sufficient-pgo-observation-115119) |
| System.Buffers.Tests.RentReturnArrayPoolTests<Byte>.SingleParallel(RentalSize: 4096, ManipulateArray: False, Async: True, UseSharedPool: False) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.Buffers.Tests.RentReturnArrayPoolTests%28Byte%29.SingleParallel%28RentalSize%3A%204096%2C%20ManipulateArray%3A%20False%2C%20Async%3A%20True%2C%20UseSharedPool%3A%20False%29.html) | +14.70% | 53856.921021 | 63888.733894 | None |
| System.Numerics.Tensors.Tests.Perf_FloatingPointTensorPrimitives<Double>.AtanPi(BufferLength: 3079) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.Numerics.Tensors.Tests.Perf_FloatingPointTensorPrimitives%28Double%29.AtanPi%28BufferLength%3A%203079%29.html) | +14.55% | 33770.755883 | 38138.319134 | None |
| System.Collections.CreateAddAndClear<String>.List(Size: 512) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.Collections.CreateAddAndClear%28String%29.List%28Size%3A%20512%29.html) | +13.96% | 4224.144574 | 4649.796259 | None |
| System.ConsoleTests.Perf_Console.OpenStandardInput | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.ConsoleTests.Perf_Console.OpenStandardInput.html) | +11.88% | N/A | N/A | None |
| System.Buffers.Tests.RentReturnArrayPoolTests<Byte>.SingleParallel(RentalSize: 4096, ManipulateArray: True, Async: False, UseSharedPool: False) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.Buffers.Tests.RentReturnArrayPoolTests%28Byte%29.SingleParallel%28RentalSize%3A%204096%2C%20ManipulateArray%3A%20True%2C%20Async%3A%20False%2C%20UseSharedPool%3A%20False%29.html) | +11.49% | 57480.733135 | 65137.628673 | None |
| System.Buffers.Tests.RentReturnArrayPoolTests<Byte>.SingleParallel(RentalSize: 4096, ManipulateArray: True, Async: True, UseSharedPool: False) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.Buffers.Tests.RentReturnArrayPoolTests%28Byte%29.SingleParallel%28RentalSize%3A%204096%2C%20ManipulateArray%3A%20True%2C%20Async%3A%20True%2C%20UseSharedPool%3A%20False%29.html) | +9.76% | 56571.530920 | 63665.928609 | None |
| System.Buffers.Tests.RentReturnArrayPoolTests<Object>.SingleParallel(RentalSize: 4096, ManipulateArray: True, Async: True, UseSharedPool: False) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.Buffers.Tests.RentReturnArrayPoolTests%28Object%29.SingleParallel%28RentalSize%3A%204096%2C%20ManipulateArray%3A%20True%2C%20Async%3A%20True%2C%20UseSharedPool%3A%20False%29.html) | +9.31% | 58428.183137 | 64112.926849 | None |
| System.Collections.CreateAddAndRemove<String>.Stack(Size: 512) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.Collections.CreateAddAndRemove%28String%29.Stack%28Size%3A%20512%29.html) | +8.55% | 5281.234977 | 5573.343010 | None |
| System.Text.Json.Tests.Utf8JsonReaderCommentsTests.Utf8JsonReaderCommentParsing(CommentHandling: Skip, SegmentSize: 0, TestCase: LongMultiLine) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.Text.Json.Tests.Utf8JsonReaderCommentsTests.Utf8JsonReaderCommentParsing%28CommentHandling%3A%20Skip%2C%20SegmentSize%3A%200%2C%20TestCase%3A%20LongMultiLine%29.html) | +8.05% | 4254.722756 | 4602.573816 | [3](#3-41be5e229b---jit-graph-based-loop-inversion-116017), [17](#17-1af7c2370b---add-a-number-of-additional-apis-to-the-various-simd-accelerated-vector-types-111179) |
| System.ConsoleTests.Perf_Console.OpenStandardOutput | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.ConsoleTests.Perf_Console.OpenStandardOutput.html) | +7.68% | N/A | N/A | None |
| System.Collections.CreateAddAndRemove<String>.Queue(Size: 512) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.Collections.CreateAddAndRemove%28String%29.Queue%28Size%3A%20512%29.html) | +7.06% | N/A | N/A | None |
| System.Collections.CreateAddAndClear<String>.Queue(Size: 512) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.Collections.CreateAddAndClear%28String%29.Queue%28Size%3A%20512%29.html) | +6.92% | N/A | N/A | None |
| System.Buffers.Tests.RentReturnArrayPoolTests<Byte>.SingleParallel(RentalSize: 4096, ManipulateArray: False, Async: False, UseSharedPool: False) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.Buffers.Tests.RentReturnArrayPoolTests%28Byte%29.SingleParallel%28RentalSize%3A%204096%2C%20ManipulateArray%3A%20False%2C%20Async%3A%20False%2C%20UseSharedPool%3A%20False%29.html) | +6.62% | 8486.788592 | 9357.151452 | None |
| System.Globalization.Tests.StringSearch.IndexOf_Word_NotFound(Options: (en-US, IgnoreCase, False)) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.Globalization.Tests.StringSearch.IndexOf_Word_NotFound%28Options%3A%20%28en-US%2C%20IgnoreCase%2C%20False%29%29.html) | +6.54% | 772.896845 | 823.863583 | [13](#13-1c10ceecbf---jit-add-3-opt-implementation-for-improving-upon-rpo-based-block-layout-103450) |
| System.Collections.AddGivenSize<String>.Queue(Size: 512) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.Collections.AddGivenSize%28String%29.Queue%28Size%3A%20512%29.html) | +6.53% | 3660.966616 | 3998.553438 | None |
| System.Collections.Perf_LengthBucketsFrozenDictionary.ToFrozenDictionary(Count: 1000, ItemsPerBucket: 5) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.Collections.Perf_LengthBucketsFrozenDictionary.ToFrozenDictionary%28Count%3A%201000%2C%20ItemsPerBucket%3A%205%29.html) | +5.52% | N/A | N/A | None |

---

## 6. f9b2a6d4b3 - JIT: Optimize for cost instead of score in 3-opt layout (#109741)

**Date:** 2024-11-13 16:58:22
**Commit:** [f9b2a6d4b3](https://github.com/dotnet/runtime/commit/f9b2a6d4b3f0ea910e8f3b881cd88603eedfe8a3)
**Affected Tests:** 23

| Test Name | Link | Change | Before | After | Other Changepoints |
|-----------|------|--------|--------|-------|--------------------|
| System.IO.Tests.BinaryWriterTests.DefaultCtor | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.IO.Tests.BinaryWriterTests.DefaultCtor.html) | +16.78% | 19.620213 | 21.552331 | None |
| System.Threading.Tests.Perf_Timer.ShortScheduleAndDisposeWithFiringTimers | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.Threading.Tests.Perf_Timer.ShortScheduleAndDisposeWithFiringTimers.html) | +15.85% | 205.182322 | 240.806630 | None |
| System.Formats.Tar.Tests.Perf_TarWriter.V7TarEntry_WriteEntry_Async | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.Formats.Tar.Tests.Perf_TarWriter.V7TarEntry_WriteEntry_Async.html) | +15.45% | 461.615185 | 540.869176 | None |
| System.Collections.CreateAddAndClear<Int32>.ICollection(Size: 512) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.Collections.CreateAddAndClear%28Int32%29.ICollection%28Size%3A%20512%29.html) | +14.85% | 1560.807508 | 1738.341560 | [21](#21-f72179a0f6---jit-remove-fallthrough-checks-in-compilertrylowerswitchtobittest-108106) |
| System.Numerics.Tests.Perf_BigInteger.ToStringD(numberString: 123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678... | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.Numerics.Tests.Perf_BigInteger.ToStringD%28numberString%3A%20123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678.html) | +13.85% | 7167250.090861 | 8191053.367647 | [18](#18-254b55a49e---enable-loop-cloning-for-spans-113575) |
| System.Numerics.Tests.Perf_BigInteger.ToStringD(numberString: 123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678... | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.Numerics.Tests.Perf_BigInteger.ToStringD%28numberString%3A%20123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678.html) | +13.78% | 7161780.735294 | 8176144.062500 | [18](#18-254b55a49e---enable-loop-cloning-for-spans-113575) |
| System.Numerics.Tests.Perf_BigInteger.ToStringD(numberString: -2147483648) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.Numerics.Tests.Perf_BigInteger.ToStringD%28numberString%3A%20-2147483648%29.html) | +12.90% | 62.815645 | 70.845750 | None |
| System.Tests.Perf_Array.ArrayAssign2D | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.Tests.Perf_Array.ArrayAssign2D.html) | +12.55% | 744475.137117 | 812150.889321 | [4](#4-b146d7512c---jit-move-loop-inversion-to-after-loop-recognition-115850) |
| System.Collections.CreateAddAndClear<String>.SortedList(Size: 512) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.Collections.CreateAddAndClear%28String%29.SortedList%28Size%3A%20512%29.html) | +12.35% | 309608.980328 | 349132.590534 | None |
| System.Collections.CreateAddAndRemove<String>.SortedList(Size: 512) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.Collections.CreateAddAndRemove%28String%29.SortedList%28Size%3A%20512%29.html) | +12.15% | 548852.718917 | 614600.950432 | [20](#20-39a31f082e---virtual-stub-indirect-call-profiling-116453) |
| System.Collections.AddGivenSize<String>.SortedList(Size: 512) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.Collections.AddGivenSize%28String%29.SortedList%28Size%3A%20512%29.html) | +12.14% | 310155.522775 | 343758.040632 | [20](#20-39a31f082e---virtual-stub-indirect-call-profiling-116453) |
| System.Collections.TryGetValueTrue<String, String>.SortedList(Size: 512) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.Collections.TryGetValueTrue%28String%2C%20String%29.SortedList%28Size%3A%20512%29.html) | +12.10% | 229642.072388 | 266117.228549 | [20](#20-39a31f082e---virtual-stub-indirect-call-profiling-116453) |
| System.IO.Tests.Perf_FileStream.ReadAsync(fileSize: 1024, userBufferSize: 1024, options: None) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.IO.Tests.Perf_FileStream.ReadAsync%28fileSize%3A%201024%2C%20userBufferSize%3A%201024%2C%20options%3A%20None%29.html) | +9.73% | 14079.498605 | 15077.393813 | [19](#19-d7347b5bbb---some-sve-fixes-for-code-generation-113860) |
| System.Collections.CreateAddAndClear<Int32>.LinkedList(Size: 512) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.Collections.CreateAddAndClear%28Int32%29.LinkedList%28Size%3A%20512%29.html) | +7.86% | 13182.044032 | 14174.976373 | None |
| System.Perf_Convert.ToBase64String(formattingOptions: None) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.Perf_Convert.ToBase64String%28formattingOptions%3A%20None%29.html) | +7.71% | 505.710700 | 537.668787 | None |
| System.Collections.CtorDefaultSize<Int32>.SortedSet | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.Collections.CtorDefaultSize%28Int32%29.SortedSet.html) | +7.66% | 15.996000 | 16.882656 | None |
| System.Tests.Perf_GC<Char>.AllocateArray(length: 1000, pinned: False) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.Tests.Perf_GC%28Char%29.AllocateArray%28length%3A%201000%2C%20pinned%3A%20False%29.html) | +7.27% | 138.765453 | 152.691031 | None |
| System.Tests.Perf_GC<Byte>.AllocateArray(length: 1000, pinned: True) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.Tests.Perf_GC%28Byte%29.AllocateArray%28length%3A%201000%2C%20pinned%3A%20True%29.html) | +6.29% | 552.495413 | 582.472168 | None |
| System.Threading.Tests.Perf_Timer.ScheduleManyThenDisposeMany | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.Threading.Tests.Perf_Timer.ScheduleManyThenDisposeMany.html) | +6.09% | 428162056.732143 | 451771010.017857 | None |
| System.IO.MemoryMappedFiles.Tests.Perf_MemoryMappedFile.CreateFromFile(capacity: 10000) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.IO.MemoryMappedFiles.Tests.Perf_MemoryMappedFile.CreateFromFile%28capacity%3A%2010000%29.html) | +6.05% | 17556.730494 | 19547.499225 | None |
| Microsoft.Extensions.Configuration.ConfigurationBinderBenchmarks.Get(ConfigurationProvidersCount: 16, KeysCountPerProvider: 40) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/Microsoft.Extensions.Configuration.ConfigurationBinderBenchmarks.Get%28ConfigurationProvidersCount%3A%2016%2C%20KeysCountPerProvider%3A%2040%29.html) | +5.99% | 5937232.494898 | 6338370.420918 | None |
| System.ConsoleTests.Perf_Console.OpenStandardError | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.ConsoleTests.Perf_Console.OpenStandardError.html) | +5.86% | 715.000319 | 760.917789 | None |
| System.Tests.Perf_GC<Byte>.AllocateUninitializedArray(length: 1000, pinned: True) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.Tests.Perf_GC%28Byte%29.AllocateUninitializedArray%28length%3A%201000%2C%20pinned%3A%20True%29.html) | +5.42% | 548.950346 | 577.768813 | [9](#9-a484803332---dont-optimize-prologsepilogues-in-optimizepostindexed-114843) |

---

## 7. 2a2b7dc72b - JIT: Fix profile maintenance in `optSetBlockWeights`, funclet creation (#111736)

**Date:** 2025-01-23 19:40:52
**Commit:** [2a2b7dc72b](https://github.com/dotnet/runtime/commit/2a2b7dc72b5642dd24ca37623327e765a9730dd7)
**Affected Tests:** 17

| Test Name | Link | Change | Before | After | Other Changepoints |
|-----------|------|--------|--------|-------|--------------------|
| System.Memory.Span<Byte>.Reverse(Size: 512) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.Memory.Span%28Byte%29.Reverse%28Size%3A%20512%29.html) | +46.48% | 19.128117 | 27.936702 | None |
| System.Numerics.Tests.Perf_Vector4.ClampBenchmark | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.Numerics.Tests.Perf_Vector4.ClampBenchmark.html) | +20.12% | N/A | N/A | None |
| Benchstone.BenchI.BubbleSort.Test | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/Benchstone.BenchI.BubbleSort.Test.html) | +18.27% | 11607.312464 | 13133.369099 | [32](#32-5cb6a06da6---jit-add-simple-late-layout-pass-107483) |
| System.Tests.Perf_Decimal.Mod | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.Tests.Perf_Decimal.Mod.html) | +17.47% | 11.246185 | 12.957709 | [2](#2-ffcd1c5442---trust-single-edge-synthetic-profile-116054), [8](#8-e2ad5fcc17---arm64-implement-region-write-barriers-111636) |
| Microsoft.Extensions.Primitives.StringSegmentBenchmark.Equals_Object_Valid | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/Microsoft.Extensions.Primitives.StringSegmentBenchmark.Equals_Object_Valid.html) | +12.21% | 9.248650 | 10.374644 | None |
| System.Buffers.Text.Tests.Base64Tests.ConvertToBase64CharArray(NumberOfBytes: 1000) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.Buffers.Text.Tests.Base64Tests.ConvertToBase64CharArray%28NumberOfBytes%3A%201000%29.html) | +11.63% | 263.802434 | 295.922009 | [41](#41-6278b81081---main-update-dependencies-from-dncenginternaldotnet-optimization-116426) |
| System.Memory.Span<Byte>.IndexOfAnyThreeValues(Size: 33) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.Memory.Span%28Byte%29.IndexOfAnyThreeValues%28Size%3A%2033%29.html) | +9.69% | 8.603975 | 9.403029 | None |
| System.Memory.Span<Byte>.IndexOfAnyTwoValues(Size: 33) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.Memory.Span%28Byte%29.IndexOfAnyTwoValues%28Size%3A%2033%29.html) | +9.62% | 7.476444 | 8.070462 | None |
| System.Numerics.Tensors.Tests.Perf_NumberTensorPrimitives<Single>.Max(BufferLength: 3079) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.Numerics.Tensors.Tests.Perf_NumberTensorPrimitives%28Single%29.Max%28BufferLength%3A%203079%29.html) | +8.81% | 1064.530335 | 966.974327 | None |
| System.Numerics.Tests.Constructor.SpanCastBenchmark_UInt16 | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.Numerics.Tests.Constructor.SpanCastBenchmark_UInt16.html) | +8.70% | 2.731565 | 2.255927 | None |
| System.Numerics.Tensors.Tests.Perf_FloatingPointTensorPrimitives<Single>.Sin(BufferLength: 128) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.Numerics.Tensors.Tests.Perf_FloatingPointTensorPrimitives%28Single%29.Sin%28BufferLength%3A%20128%29.html) | +8.42% | 582.203838 | 530.079008 | None |
| System.Tests.Perf_Int32.TryFormat(value: 4) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.Tests.Perf_Int32.TryFormat%28value%3A%204%29.html) | +8.22% | 4.964687 | 5.255960 | [3](#3-41be5e229b---jit-graph-based-loop-inversion-116017) |
| System.Numerics.Tensors.Tests.Perf_NumberTensorPrimitives<Single>.Max(BufferLength: 128) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.Numerics.Tensors.Tests.Perf_NumberTensorPrimitives%28Single%29.Max%28BufferLength%3A%20128%29.html) | +7.62% | 47.079156 | 43.483630 | None |
| System.Numerics.Tensors.Tests.Perf_NumberTensorPrimitives<Single>.IndexOfMax(BufferLength: 3079) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.Numerics.Tensors.Tests.Perf_NumberTensorPrimitives%28Single%29.IndexOfMax%28BufferLength%3A%203079%29.html) | +7.54% | 2077.856504 | 1908.883502 | None |
| System.Memory.Span<Byte>.IndexOfAnyThreeValues(Size: 512) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.Memory.Span%28Byte%29.IndexOfAnyThreeValues%28Size%3A%20512%29.html) | +5.72% | 28.531705 | 30.201932 | None |
| System.Numerics.Tensors.Tests.Perf_NumberTensorPrimitives<Single>.IndexOfMax(BufferLength: 128) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.Numerics.Tensors.Tests.Perf_NumberTensorPrimitives%28Single%29.IndexOfMax%28BufferLength%3A%20128%29.html) | +5.43% | 85.719570 | 90.614991 | None |
| System.Buffers.Tests.RentReturnArrayPoolTests<Object>.ProducerConsumer(RentalSize: 4096, ManipulateArray: False, Async: True, UseSharedPool: False) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.Buffers.Tests.RentReturnArrayPoolTests%28Object%29.ProducerConsumer%28RentalSize%3A%204096%2C%20ManipulateArray%3A%20False%2C%20Async%3A%20True%2C%20UseSharedPool%3A%20False%29.html) | +5.07% | N/A | N/A | None |

---

## 8. e2ad5fcc17 - Arm64: Implement region write barriers (#111636)

**Date:** 2025-05-17 02:08:12
**Commit:** [e2ad5fcc17](https://github.com/dotnet/runtime/commit/e2ad5fcc1702105d9cb9c32802181976df1b97ba)
**Affected Tests:** 14

| Test Name | Link | Change | Before | After | Other Changepoints |
|-----------|------|--------|--------|-------|--------------------|
| PerfLabTests.CastingPerf.ObjObjIsFoo | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/PerfLabTests.CastingPerf.ObjObjIsFoo.html) | +44.72% | 271844.904018 | 253793.924951 | None |
| XmlDocumentTests.XmlNodeListTests.Perf_XmlNodeList.Enumerator | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/XmlDocumentTests.XmlNodeListTests.Perf_XmlNodeList.Enumerator.html) | +22.43% | 133.600614 | 141.463806 | None |
| System.Tests.Perf_Decimal.Mod | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.Tests.Perf_Decimal.Mod.html) | +17.47% | 11.246185 | 12.957709 | [2](#2-ffcd1c5442---trust-single-edge-synthetic-profile-116054), [7](#7-2a2b7dc72b---jit-fix-profile-maintenance-in-optsetblockweights-funclet-creation-111736) |
| PerfLabTests.CastingPerf2.CastingPerf.FooObjIsDescendant | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/PerfLabTests.CastingPerf2.CastingPerf.FooObjIsDescendant.html) | +15.95% | 296926.161734 | 317855.456059 | None |
| PerfLabTests.CastingPerf.FooObjIsFoo | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/PerfLabTests.CastingPerf.FooObjIsFoo.html) | +14.69% | 297106.328394 | 267625.344136 | None |
| System.Tests.Perf_Enum.GetName_Generic_Flags | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.Tests.Perf_Enum.GetName_Generic_Flags.html) | +14.12% | 11.697073 | 13.359317 | None |
| PerfLabTests.CastingPerf.ObjScalarValueType | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/PerfLabTests.CastingPerf.ObjScalarValueType.html) | +6.94% | 296716.053438 | 317937.216359 | None |
| PerfLabTests.CastingPerf.FooObjIsFoo2 | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/PerfLabTests.CastingPerf.FooObjIsFoo2.html) | +6.90% | N/A | N/A | None |
| PerfLabTests.CastingPerf.ObjObjrefValueType | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/PerfLabTests.CastingPerf.ObjObjrefValueType.html) | +6.84% | N/A | N/A | None |
| PerfLabTests.CastingPerf.ObjFooIsObj | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/PerfLabTests.CastingPerf.ObjFooIsObj.html) | +6.70% | N/A | N/A | None |
| PerfLabTests.CastingPerf2.CastingPerf.IFooFooIsIFoo | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/PerfLabTests.CastingPerf2.CastingPerf.IFooFooIsIFoo.html) | +6.64% | 544032.334546 | 602292.932354 | None |
| PerfLabTests.CastingPerf.IFooFooIsIFoo | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/PerfLabTests.CastingPerf.IFooFooIsIFoo.html) | +6.61% | 297298.143794 | 317835.980071 | None |
| System.Collections.CtorFromCollection<String>.Dictionary(Size: 512) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.Collections.CtorFromCollection%28String%29.Dictionary%28Size%3A%20512%29.html) | +5.44% | N/A | N/A | None |
| PerfLabTests.CastingPerf.ScalarValueTypeObj | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/PerfLabTests.CastingPerf.ScalarValueTypeObj.html) | +5.14% | 284743.090848 | 351383.105564 | None |

---

## 9. a484803332 - don't optimize prologs/epilogues in OptimizePostIndexed (#114843)

**Date:** 2025-04-20 22:05:57
**Commit:** [a484803332](https://github.com/dotnet/runtime/commit/a4848033324be5a3e0f7f0a901e222cdcadce07b)
**Affected Tests:** 12

| Test Name | Link | Change | Before | After | Other Changepoints |
|-----------|------|--------|--------|-------|--------------------|
| System.MathBenchmarks.Single.Log10P1 | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.MathBenchmarks.Single.Log10P1.html) | +11.35% | N/A | N/A | None |
| System.Collections.CreateAddAndClear<String>.Array(Size: 512) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.Collections.CreateAddAndClear%28String%29.Array%28Size%3A%20512%29.html) | +11.27% | 3526.147718 | 3981.978381 | None |
| System.MathBenchmarks.Double.AsinPi | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.MathBenchmarks.Double.AsinPi.html) | +7.42% | N/A | N/A | None |
| System.IO.Tests.Perf_FileStream.CopyToFile(fileSize: 1024, options: None) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.IO.Tests.Perf_FileStream.CopyToFile%28fileSize%3A%201024%2C%20options%3A%20None%29.html) | +6.83% | 66444.908615 | 71292.679604 | None |
| Benchstone.BenchF.Trap.Test | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/Benchstone.BenchF.Trap.Test.html) | +6.55% | N/A | N/A | None |
| System.Buffers.Tests.RentReturnArrayPoolTests<Object>.SingleParallel(RentalSize: 4096, ManipulateArray: True, Async: False, UseSharedPool: False) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.Buffers.Tests.RentReturnArrayPoolTests%28Object%29.SingleParallel%28RentalSize%3A%204096%2C%20ManipulateArray%3A%20True%2C%20Async%3A%20False%2C%20UseSharedPool%3A%20False%29.html) | +6.23% | 57378.856646 | 64044.739097 | None |
| System.IO.Tests.Perf_Path.GetRandomFileName | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.IO.Tests.Perf_Path.GetRandomFileName.html) | +6.16% | 647.969893 | 691.920146 | None |
| System.Numerics.Tensors.Tests.Perf_FloatingPointTensorPrimitives<Double>.AtanPi(BufferLength: 128) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.Numerics.Tensors.Tests.Perf_FloatingPointTensorPrimitives%28Double%29.AtanPi%28BufferLength%3A%20128%29.html) | +5.48% | N/A | N/A | None |
| System.MathBenchmarks.Double.Sin | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.MathBenchmarks.Double.Sin.html) | +5.47% | N/A | N/A | None |
| System.Tests.Perf_GC<Byte>.AllocateUninitializedArray(length: 1000, pinned: True) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.Tests.Perf_GC%28Byte%29.AllocateUninitializedArray%28length%3A%201000%2C%20pinned%3A%20True%29.html) | +5.42% | 548.950346 | 577.768813 | [6](#6-f9b2a6d4b3---jit-optimize-for-cost-instead-of-score-in-3-opt-layout-109741) |
| System.IO.Tests.Perf_File.ReadAllBytes(size: 4096) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.IO.Tests.Perf_File.ReadAllBytes%28size%3A%204096%29.html) | +5.36% | N/A | N/A | None |
| System.MathBenchmarks.Double.Asin | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.MathBenchmarks.Double.Asin.html) | +5.10% | N/A | N/A | None |

---

## 10. 34f1db49db - JIT: use root compiler instance for sufficient PGO observation (#115119)

**Date:** 2025-05-19 14:21:16
**Commit:** [34f1db49db](https://github.com/dotnet/runtime/commit/34f1db49dbf702697483ee2809d493f5ef441768)
**Affected Tests:** 11

| Test Name | Link | Change | Before | After | Other Changepoints |
|-----------|------|--------|--------|-------|--------------------|
| System.Globalization.Tests.StringEquality.Compare_DifferentFirstChar(Count: 1024, Options: (en-US, OrdinalIgnoreCase)) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.Globalization.Tests.StringEquality.Compare_DifferentFirstChar%28Count%3A%201024%2C%20Options%3A%20%28en-US%2C%20OrdinalIgnoreCase%29%29.html) | +56.36% | 11.041748 | 17.239539 | None |
| System.Globalization.Tests.StringEquality.Compare_DifferentFirstChar(Count: 1024, Options: (en-US, Ordinal)) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.Globalization.Tests.StringEquality.Compare_DifferentFirstChar%28Count%3A%201024%2C%20Options%3A%20%28en-US%2C%20Ordinal%29%29.html) | +56.00% | 12.479778 | 19.395112 | None |
| System.Buffers.Tests.NonStandardArrayPoolTests<Byte>.RentNoReturn(RentalSize: 64, UseSharedPool: False) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.Buffers.Tests.NonStandardArrayPoolTests%28Byte%29.RentNoReturn%28RentalSize%3A%2064%2C%20UseSharedPool%3A%20False%29.html) | +47.36% | 40.222699 | 59.122232 | None |
| System.Buffers.Tests.RentReturnArrayPoolTests<Byte>.MultipleSerial(RentalSize: 4096, ManipulateArray: False, Async: False, UseSharedPool: False) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.Buffers.Tests.RentReturnArrayPoolTests%28Byte%29.MultipleSerial%28RentalSize%3A%204096%2C%20ManipulateArray%3A%20False%2C%20Async%3A%20False%2C%20UseSharedPool%3A%20False%29.html) | +42.85% | 346.376216 | 497.890873 | [5](#5-34545d790e---jit-dont-mark-callees-noinline-for-non-fatal-observations-with-pgo-114821) |
| System.Buffers.Tests.RentReturnArrayPoolTests<Object>.MultipleSerial(RentalSize: 4096, ManipulateArray: False, Async: False, UseSharedPool: False) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.Buffers.Tests.RentReturnArrayPoolTests%28Object%29.MultipleSerial%28RentalSize%3A%204096%2C%20ManipulateArray%3A%20False%2C%20Async%3A%20False%2C%20UseSharedPool%3A%20False%29.html) | +41.05% | 345.916598 | 489.710332 | [5](#5-34545d790e---jit-dont-mark-callees-noinline-for-non-fatal-observations-with-pgo-114821) |
| System.Buffers.Tests.RentReturnArrayPoolTests<Byte>.SingleSerial(RentalSize: 4096, ManipulateArray: False, Async: False, UseSharedPool: False) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.Buffers.Tests.RentReturnArrayPoolTests%28Byte%29.SingleSerial%28RentalSize%3A%204096%2C%20ManipulateArray%3A%20False%2C%20Async%3A%20False%2C%20UseSharedPool%3A%20False%29.html) | +29.45% | 40.025178 | 51.021273 | [5](#5-34545d790e---jit-dont-mark-callees-noinline-for-non-fatal-observations-with-pgo-114821) |
| System.Globalization.Tests.StringEquality.Compare_Same_Upper(Count: 1024, Options: (en-US, Ordinal)) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.Globalization.Tests.StringEquality.Compare_Same_Upper%28Count%3A%201024%2C%20Options%3A%20%28en-US%2C%20Ordinal%29%29.html) | +22.52% | 23.432793 | 28.788922 | None |
| System.Buffers.Tests.NonStandardArrayPoolTests<Object>.RentNoReturn(RentalSize: 64, UseSharedPool: False) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.Buffers.Tests.NonStandardArrayPoolTests%28Object%29.RentNoReturn%28RentalSize%3A%2064%2C%20UseSharedPool%3A%20False%29.html) | +22.30% | 67.475759 | 83.694014 | None |
| System.Buffers.Tests.RentReturnArrayPoolTests<Object>.SingleSerial(RentalSize: 4096, ManipulateArray: False, Async: False, UseSharedPool: False) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.Buffers.Tests.RentReturnArrayPoolTests%28Object%29.SingleSerial%28RentalSize%3A%204096%2C%20ManipulateArray%3A%20False%2C%20Async%3A%20False%2C%20UseSharedPool%3A%20False%29.html) | +21.19% | 42.108170 | 50.853902 | [5](#5-34545d790e---jit-dont-mark-callees-noinline-for-non-fatal-observations-with-pgo-114821) |
| System.Collections.CreateAddAndClear<String>.Stack(Size: 512) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.Collections.CreateAddAndClear%28String%29.Stack%28Size%3A%20512%29.html) | +15.66% | 4202.479875 | 4553.577818 | [5](#5-34545d790e---jit-dont-mark-callees-noinline-for-non-fatal-observations-with-pgo-114821) |
| System.Collections.AddGivenSize<String>.Stack(Size: 512) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.Collections.AddGivenSize%28String%29.Stack%28Size%3A%20512%29.html) | +15.63% | 3348.170226 | 3769.311446 | [5](#5-34545d790e---jit-dont-mark-callees-noinline-for-non-fatal-observations-with-pgo-114821) |

---

## 11. 14cd365a64 - JIT: Don't try to create fallthrough from try region into cloned finally (#109788)

**Date:** 2024-11-13 20:48:19
**Commit:** [14cd365a64](https://github.com/dotnet/runtime/commit/14cd365a64c00724d8029e32f5fe35e733a81c30)
**Affected Tests:** 10

| Test Name | Link | Change | Before | After | Other Changepoints |
|-----------|------|--------|--------|-------|--------------------|
| System.Buffers.Tests.RentReturnArrayPoolTests<Byte>.ProducerConsumer(RentalSize: 4096, ManipulateArray: False, Async: True, UseSharedPool: True) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.Buffers.Tests.RentReturnArrayPoolTests%28Byte%29.ProducerConsumer%28RentalSize%3A%204096%2C%20ManipulateArray%3A%20False%2C%20Async%3A%20True%2C%20UseSharedPool%3A%20True%29.html) | +22.95% | 2324.769134 | 2872.567647 | None |
| System.Formats.Tar.Tests.Perf_TarWriter.UstarTarEntry_WriteEntry_Async | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.Formats.Tar.Tests.Perf_TarWriter.UstarTarEntry_WriteEntry_Async.html) | +20.31% | 473.538648 | 574.918280 | None |
| System.Buffers.Tests.RentReturnArrayPoolTests<Object>.MultipleSerial(RentalSize: 4096, ManipulateArray: False, Async: True, UseSharedPool: True) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.Buffers.Tests.RentReturnArrayPoolTests%28Object%29.MultipleSerial%28RentalSize%3A%204096%2C%20ManipulateArray%3A%20False%2C%20Async%3A%20True%2C%20UseSharedPool%3A%20True%29.html) | +19.07% | 1276.099769 | 1479.692416 | None |
| Microsoft.Extensions.Primitives.StringSegmentBenchmark.SubString | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/Microsoft.Extensions.Primitives.StringSegmentBenchmark.SubString.html) | +18.85% | 12.197624 | 14.971465 | None |
| System.Tests.Perf_Enum.GetValuesAsUnderlyingType_Generic | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.Tests.Perf_Enum.GetValuesAsUnderlyingType_Generic.html) | +18.62% | 26.598461 | 31.064422 | [2](#2-ffcd1c5442---trust-single-edge-synthetic-profile-116054) |
| System.Threading.Tests.Perf_Timer.LongScheduleAndDispose | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.Threading.Tests.Perf_Timer.LongScheduleAndDispose.html) | +16.31% | 181.207184 | 213.850559 | None |
| System.Tests.Perf_GC<Byte>.AllocateArray(length: 1000, pinned: False) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.Tests.Perf_GC%28Byte%29.AllocateArray%28length%3A%201000%2C%20pinned%3A%20False%29.html) | +16.04% | 89.654079 | 105.816205 | None |
| System.Text.Json.Tests.Perf_Ctor.Ctor(Formatted: False, SkipValidation: False) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.Text.Json.Tests.Perf_Ctor.Ctor%28Formatted%3A%20False%2C%20SkipValidation%3A%20False%29.html) | +15.86% | 23.230044 | 25.662952 | None |
| System.Threading.Tests.Perf_Timer.ShortScheduleAndDispose | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.Threading.Tests.Perf_Timer.ShortScheduleAndDispose.html) | +15.31% | 183.989703 | 214.182754 | None |
| BenchmarksGame.RegexRedux_1.RunBench | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/BenchmarksGame.RegexRedux_1.RunBench.html) | +14.84% | 45722125.216327 | 49464225.085714 | None |

---

## 12. ddf8075a2f - JIT: Visit blocks in RPO during LSRA (#107927)

**Date:** 2024-09-20 18:38:45
**Commit:** [ddf8075a2f](https://github.com/dotnet/runtime/commit/ddf8075a2fa3044554ded41c375a82a318ae01eb)
**Affected Tests:** 8

| Test Name | Link | Change | Before | After | Other Changepoints |
|-----------|------|--------|--------|-------|--------------------|
| System.Memory.Span<Byte>.Clear(Size: 33) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.Memory.Span%28Byte%29.Clear%28Size%3A%2033%29.html) | +59.22% | 4.994657 | 8.123683 | None |
| System.Linq.Tests.Perf_OrderBy.OrderByCustomComparer(NumberOfPeople: 512) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.Linq.Tests.Perf_OrderBy.OrderByCustomComparer%28NumberOfPeople%3A%20512%29.html) | +54.31% | 49119.133473 | 76057.154972 | None |
| System.Linq.Tests.Perf_Enumerable.OrderByThenBy(input: IEnumerable) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.Linq.Tests.Perf_Enumerable.OrderByThenBy%28input%3A%20IEnumerable%29.html) | +23.17% | 2731.267285 | 3406.939099 | None |
| System.Text.RegularExpressions.Tests.Perf_Regex_Industry_Leipzig.Count(Pattern: ".{0,2}(Tom\|Sawyer\|Huckleberry\|Finn)", Options: NonBacktracking) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.Text.RegularExpressions.Tests.Perf_Regex_Industry_Leipzig.Count%28Pattern%3A%20%22.%7B0%2C2%7D%28Tom%7CSawyer%7CHuckleberry%7CFinn%29%22%2C%20Options%3A%20NonBacktracking%29.html) | +12.77% | 76390647.886905 | 86314129.922619 | None |
| System.Numerics.Tests.Perf_BigInteger.ModPow(arguments: 16384,16384,64 bits) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.Numerics.Tests.Perf_BigInteger.ModPow%28arguments%3A%2016384%2C16384%2C64%20bits%29.html) | +12.73% | 1950801.181641 | 2184383.142578 | [3](#3-41be5e229b---jit-graph-based-loop-inversion-116017) |
| System.Text.RegularExpressions.Tests.Perf_Regex_Industry_Leipzig.Count(Pattern: ".{2,4}(Tom\|Sawyer\|Huckleberry\|Finn)", Options: NonBacktracking) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.Text.RegularExpressions.Tests.Perf_Regex_Industry_Leipzig.Count%28Pattern%3A%20%22.%7B2%2C4%7D%28Tom%7CSawyer%7CHuckleberry%7CFinn%29%22%2C%20Options%3A%20NonBacktracking%29.html) | +12.26% | 76417703.482143 | 85994423.309524 | None |
| System.Numerics.Tests.Perf_BigInteger.ModPow(arguments: 1024,1024,64 bits) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.Numerics.Tests.Perf_BigInteger.ModPow%28arguments%3A%201024%2C1024%2C64%20bits%29.html) | +7.03% | 95817.971292 | 102569.741452 | [36](#36-37b1764e19---optimize-bigintegerdivide-96895) |
| System.Text.RegularExpressions.Tests.Perf_Regex_Industry_RustLang_Sherlock.Count(Pattern: "\\w+\\s+Holmes\\s+\\w+", Options: NonBacktracking) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.Text.RegularExpressions.Tests.Perf_Regex_Industry_RustLang_Sherlock.Count%28Pattern%3A%20%22%5C%5Cw%2B%5C%5Cs%2BHolmes%5C%5Cs%2B%5C%5Cw%2B%22%2C%20Options%3A%20NonBacktracking%29.html) | +5.47% | 2935038.941964 | 3102438.758929 | None |

---

## 13. 1c10ceecbf - JIT: Add 3-opt implementation for improving upon RPO-based block layout (#103450)

**Date:** 2024-11-04 18:18:38
**Commit:** [1c10ceecbf](https://github.com/dotnet/runtime/commit/1c10ceecbf5356c33c67f6325072d753707f854e)
**Affected Tests:** 7

| Test Name | Link | Change | Before | After | Other Changepoints |
|-----------|------|--------|--------|-------|--------------------|
| Benchstone.BenchI.IniArray.Test | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/Benchstone.BenchI.IniArray.Test.html) | +20.59% | 114074829.562500 | 137377821.633929 | [26](#26-6d12a304b3---jit-do-greedy-4-opt-for-backward-jumps-in-3-opt-layout-110277), [27](#27-d410898949---jit-skip-fgcomputemissingblockweights-when-we-have-profile-data-111873) |
| SciMark2.kernel.benchSparseMult | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/SciMark2.kernel.benchSparseMult.html) | +18.24% | 919930046.910714 | 1087784852.250000 | [3](#3-41be5e229b---jit-graph-based-loop-inversion-116017) |
| Benchstone.BenchI.Array2.Test | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/Benchstone.BenchI.Array2.Test.html) | +15.43% | 1348032152.321428 | 1555925549.160714 | [3](#3-41be5e229b---jit-graph-based-loop-inversion-116017), [27](#27-d410898949---jit-skip-fgcomputemissingblockweights-when-we-have-profile-data-111873) |
| Span.IndexerBench.CoveredIndex2(length: 1024) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/Span.IndexerBench.CoveredIndex2%28length%3A%201024%29.html) | +14.11% | 1208.548206 | 1380.742593 | None |
| System.Buffers.Tests.RentReturnArrayPoolTests<Byte>.ProducerConsumer(RentalSize: 4096, ManipulateArray: True, Async: False, UseSharedPool: True) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.Buffers.Tests.RentReturnArrayPoolTests%28Byte%29.ProducerConsumer%28RentalSize%3A%204096%2C%20ManipulateArray%3A%20True%2C%20Async%3A%20False%2C%20UseSharedPool%3A%20True%29.html) | +12.15% | 2871.611261 | 3637.544543 | None |
| System.Globalization.Tests.StringSearch.IndexOf_Word_NotFound(Options: (en-US, IgnoreCase, False)) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.Globalization.Tests.StringSearch.IndexOf_Word_NotFound%28Options%3A%20%28en-US%2C%20IgnoreCase%2C%20False%29%29.html) | +6.54% | 772.896845 | 823.863583 | [5](#5-34545d790e---jit-dont-mark-callees-noinline-for-non-fatal-observations-with-pgo-114821) |
| System.Collections.IterateForEach<String>.ImmutableStack(Size: 512) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.Collections.IterateForEach%28String%29.ImmutableStack%28Size%3A%20512%29.html) | +5.43% | N/A | N/A | None |

---

## 14. 5f652c2887 - JIT: Limit 3-opt to 1000 swaps per run (#112259)

**Date:** 2025-02-07 20:46:26
**Commit:** [5f652c2887](https://github.com/dotnet/runtime/commit/5f652c2887c0ae0b96a0a9e3602a72e50713b520)
**Affected Tests:** 7

| Test Name | Link | Change | Before | After | Other Changepoints |
|-----------|------|--------|--------|-------|--------------------|
| System.IO.Tests.StreamReaderReadToEndTests.ReadToEndAsync(LineLengthRange: [ 129, 1024]) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.IO.Tests.StreamReaderReadToEndTests.ReadToEndAsync%28LineLengthRange%3A%20%5B%20129%2C%201024%5D%29.html) | +8.25% | 64913134.226190 | 69688607.571429 | None |
| System.IO.Tests.StreamReaderReadToEndTests.ReadToEnd(LineLengthRange: [ 129, 1024]) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.IO.Tests.StreamReaderReadToEndTests.ReadToEnd%28LineLengthRange%3A%20%5B%20129%2C%201024%5D%29.html) | +7.07% | 62608014.952381 | 69595054.156463 | None |
| System.IO.Tests.StreamReaderReadToEndTests.ReadToEnd(LineLengthRange: [   9,   32]) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.IO.Tests.StreamReaderReadToEndTests.ReadToEnd%28LineLengthRange%3A%20%5B%20%20%209%2C%20%20%2032%5D%29.html) | +7.02% | 63912872.452381 | 68970724.087302 | None |
| System.IO.Tests.StreamReaderReadToEndTests.ReadToEnd(LineLengthRange: [   1,    1]) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.IO.Tests.StreamReaderReadToEndTests.ReadToEnd%28LineLengthRange%3A%20%5B%20%20%201%2C%20%20%20%201%5D%29.html) | +6.98% | 64476802.071429 | 68089184.825397 | None |
| System.IO.Tests.StreamReaderReadToEndTests.ReadToEndAsync(LineLengthRange: [   0, 1024]) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.IO.Tests.StreamReaderReadToEndTests.ReadToEndAsync%28LineLengthRange%3A%20%5B%20%20%200%2C%201024%5D%29.html) | +6.55% | N/A | N/A | None |
| System.IO.Tests.StreamReaderReadToEndTests.ReadToEndAsync(LineLengthRange: [1025, 2048]) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.IO.Tests.StreamReaderReadToEndTests.ReadToEndAsync%28LineLengthRange%3A%20%5B1025%2C%202048%5D%29.html) | +5.38% | N/A | N/A | None |
| System.IO.Tests.StreamReaderReadToEndTests.ReadToEndAsync(LineLengthRange: [   9,   32]) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.IO.Tests.StreamReaderReadToEndTests.ReadToEndAsync%28LineLengthRange%3A%20%5B%20%20%209%2C%20%20%2032%5D%29.html) | +5.26% | 64779972.684524 | 69001161.952381 | None |

---

## 15. 0fa747abd5 - Replace OptimizedInboxTextEncoder vectorization with SearchValues (#114494)

**Date:** 2025-04-16 21:36:38
**Commit:** [0fa747abd5](https://github.com/dotnet/runtime/commit/0fa747abd5224373adbbece9a5ddc0325e373d7a)
**Affected Tests:** 7

| Test Name | Link | Change | Before | After | Other Changepoints |
|-----------|------|--------|--------|-------|--------------------|
| System.Text.Encodings.Web.Tests.Perf_Encoders.EncodeUtf16(arguments: UnsafeRelaxed,hello "there",16) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.Text.Encodings.Web.Tests.Perf_Encoders.EncodeUtf16%28arguments%3A%20UnsafeRelaxed%2Chello%20%22there%22%2C16%29.html) | +38.12% | 41.774188 | 57.118293 | [2](#2-ffcd1c5442---trust-single-edge-synthetic-profile-116054) |
| System.Text.Encodings.Web.Tests.Perf_Encoders.EncodeUtf16(arguments: Url,�2020,16) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.Text.Encodings.Web.Tests.Perf_Encoders.EncodeUtf16%28arguments%3A%20Url%2C%EF%BF%BD2020%2C16%29.html) | +36.06% | 48.200426 | 65.625433 | [2](#2-ffcd1c5442---trust-single-edge-synthetic-profile-116054) |
| System.Text.Encodings.Web.Tests.Perf_Encoders.EncodeUtf8(arguments: UnsafeRelaxed,hello "there",16) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.Text.Encodings.Web.Tests.Perf_Encoders.EncodeUtf8%28arguments%3A%20UnsafeRelaxed%2Chello%20%22there%22%2C16%29.html) | +17.01% | 33.562457 | 39.164661 | [2](#2-ffcd1c5442---trust-single-edge-synthetic-profile-116054) |
| System.IO.Tests.Perf_File.ReadAllBytes(size: 512) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.IO.Tests.Perf_File.ReadAllBytes%28size%3A%20512%29.html) | +14.61% | N/A | N/A | None |
| System.Text.Encodings.Web.Tests.Perf_Encoders.EncodeUtf8(arguments: Url,&lorem ipsum=dolor sit amet,16) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.Text.Encodings.Web.Tests.Perf_Encoders.EncodeUtf8%28arguments%3A%20Url%2C%26lorem%20ipsum%3Ddolor%20sit%20amet%2C16%29.html) | +8.20% | 65.967826 | 70.876888 | [2](#2-ffcd1c5442---trust-single-edge-synthetic-profile-116054) |
| System.Text.Encodings.Web.Tests.Perf_Encoders.EncodeUtf16(arguments: Url,�2020,512) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.Text.Encodings.Web.Tests.Perf_Encoders.EncodeUtf16%28arguments%3A%20Url%2C%EF%BF%BD2020%2C512%29.html) | +7.58% | 151.544996 | 163.472073 | [2](#2-ffcd1c5442---trust-single-edge-synthetic-profile-116054) |
| System.Text.Encodings.Web.Tests.Perf_Encoders.EncodeUtf8(arguments: UnsafeRelaxed,hello "there",512) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.Text.Encodings.Web.Tests.Perf_Encoders.EncodeUtf8%28arguments%3A%20UnsafeRelaxed%2Chello%20%22there%22%2C512%29.html) | +6.35% | 94.733892 | 101.305540 | [2](#2-ffcd1c5442---trust-single-edge-synthetic-profile-116054) |

---

## 16. a950953d00 - Remove bounds checks for Log2 function in FormattingHelpers.CountDigits (#113790)

**Date:** 2025-05-30 15:09:59
**Commit:** [a950953d00](https://github.com/dotnet/runtime/commit/a950953d0019b2df11d3bdc3f93bbad272438640)
**Affected Tests:** 5

| Test Name | Link | Change | Before | After | Other Changepoints |
|-----------|------|--------|--------|-------|--------------------|
| System.Tests.Perf_Int64.ToString(value: 9223372036854775807) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.Tests.Perf_Int64.ToString%28value%3A%209223372036854775807%29.html) | +25.28% | 41.278166 | 51.962026 | None |
| System.Tests.Perf_Int64.ToString(value: 12345) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.Tests.Perf_Int64.ToString%28value%3A%2012345%29.html) | +24.08% | 18.516576 | 24.021955 | None |
| System.Buffers.Tests.RentReturnArrayPoolTests<Byte>.ProducerConsumer(RentalSize: 4096, ManipulateArray: True, Async: True, UseSharedPool: True) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.Buffers.Tests.RentReturnArrayPoolTests%28Byte%29.ProducerConsumer%28RentalSize%3A%204096%2C%20ManipulateArray%3A%20True%2C%20Async%3A%20True%2C%20UseSharedPool%3A%20True%29.html) | +23.04% | 2855.101663 | 3898.764552 | None |
| System.Tests.Perf_Version.TryFormat4 | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.Tests.Perf_Version.TryFormat4.html) | +7.20% | 26.650957 | 28.468325 | None |
| System.Tests.Perf_Version.TryFormat3 | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.Tests.Perf_Version.TryFormat3.html) | +6.33% | 21.140380 | 22.466892 | None |

---

## 17. 1af7c2370b - Add a number of additional APIs to the various SIMD accelerated vector types (#111179)

**Date:** 2025-01-16 16:30:16
**Commit:** [1af7c2370b](https://github.com/dotnet/runtime/commit/1af7c2370bce80cba73d442d69f4a2f1b02dcbef)
**Affected Tests:** 4

| Test Name | Link | Change | Before | After | Other Changepoints |
|-----------|------|--------|--------|-------|--------------------|
| System.MathBenchmarks.Double.ILogB | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.MathBenchmarks.Double.ILogB.html) | +10.67% | 6703.835831 | 7421.691683 | None |
| System.Net.Sockets.Tests.SocketSendReceivePerfTest.ReceiveFromAsyncThenSendToAsync_SocketAddress | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.Net.Sockets.Tests.SocketSendReceivePerfTest.ReceiveFromAsyncThenSendToAsync_SocketAddress.html) | +8.86% | 335335715.928571 | 390568944.535714 | None |
| System.Text.Json.Tests.Utf8JsonReaderCommentsTests.Utf8JsonReaderCommentParsing(CommentHandling: Skip, SegmentSize: 0, TestCase: LongMultiLine) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.Text.Json.Tests.Utf8JsonReaderCommentsTests.Utf8JsonReaderCommentParsing%28CommentHandling%3A%20Skip%2C%20SegmentSize%3A%200%2C%20TestCase%3A%20LongMultiLine%29.html) | +8.05% | 4254.722756 | 4602.573816 | [3](#3-41be5e229b---jit-graph-based-loop-inversion-116017), [5](#5-34545d790e---jit-dont-mark-callees-noinline-for-non-fatal-observations-with-pgo-114821) |
| System.Text.Json.Tests.Utf8JsonReaderCommentsTests.Utf8JsonReaderCommentParsing(CommentHandling: Allow, SegmentSize: 0, TestCase: LongMultiLine) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.Text.Json.Tests.Utf8JsonReaderCommentsTests.Utf8JsonReaderCommentParsing%28CommentHandling%3A%20Allow%2C%20SegmentSize%3A%200%2C%20TestCase%3A%20LongMultiLine%29.html) | +5.67% | 4263.284606 | 4522.115674 | [3](#3-41be5e229b---jit-graph-based-loop-inversion-116017) |

---

## 18. 254b55a49e - Enable Loop Cloning for Spans (#113575)

**Date:** 2025-03-20 01:07:06
**Commit:** [254b55a49e](https://github.com/dotnet/runtime/commit/254b55a49e04ef6c63b68174c0f11d96223136fb)
**Affected Tests:** 4

| Test Name | Link | Change | Before | After | Other Changepoints |
|-----------|------|--------|--------|-------|--------------------|
| System.Numerics.Tests.Perf_BigInteger.ToStringD(numberString: 123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678... | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.Numerics.Tests.Perf_BigInteger.ToStringD%28numberString%3A%20123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678.html) | +13.85% | 7167250.090861 | 8191053.367647 | [6](#6-f9b2a6d4b3---jit-optimize-for-cost-instead-of-score-in-3-opt-layout-109741) |
| System.Numerics.Tests.Perf_BigInteger.ToStringD(numberString: 123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678... | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.Numerics.Tests.Perf_BigInteger.ToStringD%28numberString%3A%20123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678.html) | +13.78% | 7161780.735294 | 8176144.062500 | [6](#6-f9b2a6d4b3---jit-optimize-for-cost-instead-of-score-in-3-opt-layout-109741) |
| System.IO.Tests.Perf_File.AppendAllTextAsync(size: 10000) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.IO.Tests.Perf_File.AppendAllTextAsync%28size%3A%2010000%29.html) | +10.10% | 28736.234200 | 33442.079444 | [2](#2-ffcd1c5442---trust-single-edge-synthetic-profile-116054) |
| System.Net.Sockets.Tests.SocketSendReceivePerfTest.ReceiveAsyncThenSendAsync_Task | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.Net.Sockets.Tests.SocketSendReceivePerfTest.ReceiveAsyncThenSendAsync_Task.html) | +9.66% | 215260277.017857 | 260566867.732143 | None |

---

## 19. d7347b5bbb - Some SVE fixes for code generation (#113860)

**Date:** 2025-03-25 21:48:59
**Commit:** [d7347b5bbb](https://github.com/dotnet/runtime/commit/d7347b5bbb5f174d6261952205661eda752c84b7)
**Affected Tests:** 4

| Test Name | Link | Change | Before | After | Other Changepoints |
|-----------|------|--------|--------|-------|--------------------|
| System.Net.Security.Tests.SslStreamTests.ConcurrentReadWriteLargeBuffer | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.Net.Security.Tests.SslStreamTests.ConcurrentReadWriteLargeBuffer.html) | +17.95% | 52172.529125 | 58552.282941 | None |
| System.IO.MemoryMappedFiles.Tests.Perf_MemoryMappedFile.CreateFromFile_Read(capacity: 10000) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.IO.MemoryMappedFiles.Tests.Perf_MemoryMappedFile.CreateFromFile_Read%28capacity%3A%2010000%29.html) | +17.28% | 10258.931205 | 10968.123156 | None |
| System.IO.Tests.Perf_FileStream.ReadAsync(fileSize: 1024, userBufferSize: 1024, options: None) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.IO.Tests.Perf_FileStream.ReadAsync%28fileSize%3A%201024%2C%20userBufferSize%3A%201024%2C%20options%3A%20None%29.html) | +9.73% | 14079.498605 | 15077.393813 | [6](#6-f9b2a6d4b3---jit-optimize-for-cost-instead-of-score-in-3-opt-layout-109741) |
| System.IO.Tests.Perf_FileStream.ReadAsync(fileSize: 1024, userBufferSize: 1024, options: Asynchronous) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.IO.Tests.Perf_FileStream.ReadAsync%28fileSize%3A%201024%2C%20userBufferSize%3A%201024%2C%20options%3A%20Asynchronous%29.html) | +5.47% | 14156.508695 | 15368.972434 | None |

---

## 20. 39a31f082e - Virtual stub indirect call profiling (#116453)

**Date:** 2025-06-17 00:35:31
**Commit:** [39a31f082e](https://github.com/dotnet/runtime/commit/39a31f082e77fb8893016c30c0858f0e5f8c89ea)
**Affected Tests:** 4

| Test Name | Link | Change | Before | After | Other Changepoints |
|-----------|------|--------|--------|-------|--------------------|
| System.Collections.CreateAddAndRemove<String>.SortedList(Size: 512) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.Collections.CreateAddAndRemove%28String%29.SortedList%28Size%3A%20512%29.html) | +12.15% | 548852.718917 | 614600.950432 | [6](#6-f9b2a6d4b3---jit-optimize-for-cost-instead-of-score-in-3-opt-layout-109741) |
| System.Collections.AddGivenSize<String>.SortedList(Size: 512) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.Collections.AddGivenSize%28String%29.SortedList%28Size%3A%20512%29.html) | +12.14% | 310155.522775 | 343758.040632 | [6](#6-f9b2a6d4b3---jit-optimize-for-cost-instead-of-score-in-3-opt-layout-109741) |
| System.Collections.TryGetValueTrue<String, String>.SortedList(Size: 512) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.Collections.TryGetValueTrue%28String%2C%20String%29.SortedList%28Size%3A%20512%29.html) | +12.10% | 229642.072388 | 266117.228549 | [6](#6-f9b2a6d4b3---jit-optimize-for-cost-instead-of-score-in-3-opt-layout-109741) |
| System.Collections.ContainsTrueComparer<Int32>.SortedSet(Size: 512) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.Collections.ContainsTrueComparer%28Int32%29.SortedSet%28Size%3A%20512%29.html) | +5.45% | 17375.825415 | 18256.130257 | None |

---

## 21. f72179a0f6 - JIT: Remove fallthrough checks in `Compiler::TryLowerSwitchToBitTest` (#108106)

**Date:** 2024-10-16 01:10:22
**Commit:** [f72179a0f6](https://github.com/dotnet/runtime/commit/f72179a0f60c2fc81dee944b8f081a020cd5f8ea)
**Affected Tests:** 3

| Test Name | Link | Change | Before | After | Other Changepoints |
|-----------|------|--------|--------|-------|--------------------|
| System.IO.Tests.Perf_FileStream.WriteAsync(fileSize: 1048576, userBufferSize: 512, options: Asynchronous) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.IO.Tests.Perf_FileStream.WriteAsync%28fileSize%3A%201048576%2C%20userBufferSize%3A%20512%2C%20options%3A%20Asynchronous%29.html) | +17.81% | 2741737.462927 | 3481398.846856 | None |
| System.Buffers.Tests.RentReturnArrayPoolTests<Object>.SingleParallel(RentalSize: 4096, ManipulateArray: True, Async: True, UseSharedPool: True) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.Buffers.Tests.RentReturnArrayPoolTests%28Object%29.SingleParallel%28RentalSize%3A%204096%2C%20ManipulateArray%3A%20True%2C%20Async%3A%20True%2C%20UseSharedPool%3A%20True%29.html) | +15.09% | 8873.537647 | 9774.856882 | None |
| System.Collections.CreateAddAndClear<Int32>.ICollection(Size: 512) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.Collections.CreateAddAndClear%28Int32%29.ICollection%28Size%3A%20512%29.html) | +14.85% | 1560.807508 | 1738.341560 | [6](#6-f9b2a6d4b3---jit-optimize-for-cost-instead-of-score-in-3-opt-layout-109741) |

---

## 22. c653208332 - JIT: Switch config values to UTF8 (#109418)

**Date:** 2024-11-14 13:00:16
**Commit:** [c653208332](https://github.com/dotnet/runtime/commit/c65320833210f3df7412e2fcd11d6751fa374adc)
**Affected Tests:** 3

| Test Name | Link | Change | Before | After | Other Changepoints |
|-----------|------|--------|--------|-------|--------------------|
| Microsoft.Extensions.Configuration.ConfigurationBinderBenchmarks.Get(ConfigurationProvidersCount: 16, KeysCountPerProvider: 20) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/Microsoft.Extensions.Configuration.ConfigurationBinderBenchmarks.Get%28ConfigurationProvidersCount%3A%2016%2C%20KeysCountPerProvider%3A%2020%29.html) | +7.18% | 2652458.018047 | 2851250.973784 | None |
| System.Text.Json.Node.Tests.Perf_Create.Create_JsonNumber | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.Text.Json.Node.Tests.Perf_Create.Create_JsonNumber.html) | +6.95% | 813.652575 | 854.431759 | None |
| System.IO.Tests.Perf_File.ReadAllLines | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.IO.Tests.Perf_File.ReadAllLines.html) | +6.42% | 15081.732786 | 16304.919006 | None |

---

## 23. 1f06729327 - Revert "Reenable SslStreamTlsResumeTests.ClientDisableTlsResume_Succeeds (#11…" (#113730)

**Date:** 2025-03-21 07:26:52
**Commit:** [1f06729327](https://github.com/dotnet/runtime/commit/1f067293272ab6d42e329782798bc2f7398d1907)
**Affected Tests:** 3

| Test Name | Link | Change | Before | After | Other Changepoints |
|-----------|------|--------|--------|-------|--------------------|
| System.Net.Security.Tests.SslStreamTests.WriteReadAsync | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.Net.Security.Tests.SslStreamTests.WriteReadAsync.html) | +16.99% | 17339.249806 | 24202.047351 | None |
| System.Net.Security.Tests.SslStreamTests.ConcurrentReadWrite | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.Net.Security.Tests.SslStreamTests.ConcurrentReadWrite.html) | +14.15% | 38515.591876 | 44897.060959 | None |
| System.Net.Sockets.Tests.SocketSendReceivePerfTest.ReceiveFromAsyncThenSendToAsync_Task | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.Net.Sockets.Tests.SocketSendReceivePerfTest.ReceiveFromAsyncThenSendToAsync_Task.html) | +10.40% | 374490735.061224 | 412256547.571428 | None |

---

## 24. 13fef94591 - Support getting thread id in GC (#108207)

**Date:** 2024-09-25 12:38:33
**Commit:** [13fef94591](https://github.com/dotnet/runtime/commit/13fef94591f1d5d6f00204e4d920f34bd15fbb47)
**Affected Tests:** 2

| Test Name | Link | Change | Before | After | Other Changepoints |
|-----------|------|--------|--------|-------|--------------------|
| System.Net.Security.Tests.SslStreamTests.DefaultMutualHandshakeContextIPv6Async | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.Net.Security.Tests.SslStreamTests.DefaultMutualHandshakeContextIPv6Async.html) | +7.32% | 3868096.907087 | 4099399.708984 | [1](#1-0fe82fbd8e---reduce-spin-waiting-in-the-thread-pool-on-arm-processors-115402) |
| System.Net.Security.Tests.SslStreamTests.DefaultMutualHandshakeContextIPv4Async | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.Net.Security.Tests.SslStreamTests.DefaultMutualHandshakeContextIPv4Async.html) | +6.48% | 3801710.661281 | 4084877.318878 | [1](#1-0fe82fbd8e---reduce-spin-waiting-in-the-thread-pool-on-arm-processors-115402) |

---

## 25. 54b86f1843 - Remove the rest of the SimdAsHWIntrinsic support (#106594)

**Date:** 2024-10-31 19:46:24
**Commit:** [54b86f1843](https://github.com/dotnet/runtime/commit/54b86f18439397f51fbf4b14f6127a337446f3cf)
**Affected Tests:** 2

| Test Name | Link | Change | Before | After | Other Changepoints |
|-----------|------|--------|--------|-------|--------------------|
| System.Numerics.Tests.Perf_VectorOf<Single>.SquareRootBenchmark | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.Numerics.Tests.Perf_VectorOf%28Single%29.SquareRootBenchmark.html) | +372.72% | 2.010714 | 10.251370 | None |
| System.Numerics.Tests.Perf_VectorOf<Double>.SquareRootBenchmark | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.Numerics.Tests.Perf_VectorOf%28Double%29.SquareRootBenchmark.html) | +27.30% | 2.141966 | 3.761575 | None |

---

## 26. 6d12a304b3 - JIT: Do greedy 4-opt for backward jumps in 3-opt layout (#110277)

**Date:** 2024-12-03 21:25:35
**Commit:** [6d12a304b3](https://github.com/dotnet/runtime/commit/6d12a304b3068f8a9308a1aec4f3b95dd636a693)
**Affected Tests:** 2

| Test Name | Link | Change | Before | After | Other Changepoints |
|-----------|------|--------|--------|-------|--------------------|
| Benchstone.BenchI.IniArray.Test | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/Benchstone.BenchI.IniArray.Test.html) | +20.59% | 114074829.562500 | 137377821.633929 | [13](#13-1c10ceecbf---jit-add-3-opt-implementation-for-improving-upon-rpo-based-block-layout-103450), [27](#27-d410898949---jit-skip-fgcomputemissingblockweights-when-we-have-profile-data-111873) |
| System.Tests.Perf_Char.Char_ToUpperInvariant(input: "Hello World!") | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.Tests.Perf_Char.Char_ToUpperInvariant%28input%3A%20%22Hello%20World%21%22%29.html) | +15.67% | 15.078298 | 17.071104 | None |

---

## 27. d410898949 - JIT: Skip `fgComputeMissingBlockWeights` when we have profile data (#111873)

**Date:** 2025-01-29 16:10:41
**Commit:** [d410898949](https://github.com/dotnet/runtime/commit/d410898949f19681587bb308e6c50190350f3d81)
**Affected Tests:** 2

| Test Name | Link | Change | Before | After | Other Changepoints |
|-----------|------|--------|--------|-------|--------------------|
| Benchstone.BenchI.IniArray.Test | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/Benchstone.BenchI.IniArray.Test.html) | +20.59% | 114074829.562500 | 137377821.633929 | [13](#13-1c10ceecbf---jit-add-3-opt-implementation-for-improving-upon-rpo-based-block-layout-103450), [26](#26-6d12a304b3---jit-do-greedy-4-opt-for-backward-jumps-in-3-opt-layout-110277) |
| Benchstone.BenchI.Array2.Test | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/Benchstone.BenchI.Array2.Test.html) | +15.43% | 1348032152.321428 | 1555925549.160714 | [3](#3-41be5e229b---jit-graph-based-loop-inversion-116017), [13](#13-1c10ceecbf---jit-add-3-opt-implementation-for-improving-upon-rpo-based-block-layout-103450) |

---

## 28. ea43e17c95 - JIT: Run profile repair after frontend phases (#111915)

**Date:** 2025-02-21 16:40:21
**Commit:** [ea43e17c95](https://github.com/dotnet/runtime/commit/ea43e17c953a1230667c684a9f57d241e8a95171)
**Affected Tests:** 2

| Test Name | Link | Change | Before | After | Other Changepoints |
|-----------|------|--------|--------|-------|--------------------|
| Microsoft.Extensions.Primitives.Performance.StringValuesBenchmark.Indexer_FirstElement_String | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/Microsoft.Extensions.Primitives.Performance.StringValuesBenchmark.Indexer_FirstElement_String.html) | +7.95% | 4.662951 | 4.997623 | None |
| Burgers.Test2 | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/Burgers.Test2.html) | +5.37% | 277044815.785714 | 291030700.678571 | [3](#3-41be5e229b---jit-graph-based-loop-inversion-116017) |

---

## 29. 0ac2caf41a - Tar: Adjust the way we write GNU longlink and longpath metadata (#114940)

**Date:** 2025-04-24 15:49:11
**Commit:** [0ac2caf41a](https://github.com/dotnet/runtime/commit/0ac2caf41a88c56a287ab790e92eaf3ccf846fc8)
**Affected Tests:** 2

| Test Name | Link | Change | Before | After | Other Changepoints |
|-----------|------|--------|--------|-------|--------------------|
| System.Formats.Tar.Tests.Perf_TarWriter.UstarTarEntry_WriteEntry | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.Formats.Tar.Tests.Perf_TarWriter.UstarTarEntry_WriteEntry.html) | +14.19% | 321.106357 | 366.327641 | None |
| System.Formats.Tar.Tests.Perf_TarWriter.V7TarEntry_WriteEntry | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.Formats.Tar.Tests.Perf_TarWriter.V7TarEntry_WriteEntry.html) | +8.85% | 304.118608 | 331.962722 | None |

---

## 30. fd8933aac2 - Share implementation of ComWrappers between CoreCLR and NativeAOT (#113907)

**Date:** 2025-05-10 17:16:10
**Commit:** [fd8933aac2](https://github.com/dotnet/runtime/commit/fd8933aac237d2f3103de071ec4bc1547bfef16c)
**Affected Tests:** 2

| Test Name | Link | Change | Before | After | Other Changepoints |
|-----------|------|--------|--------|-------|--------------------|
| Interop.ComWrappersTests.ParallelRCWLookUp | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/Interop.ComWrappersTests.ParallelRCWLookUp.html) | +136.03% | 1022084625.482143 | 2076028982.839285 | None |
| System.IO.Tests.Perf_File.Exists | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.IO.Tests.Perf_File.Exists.html) | +6.67% | N/A | N/A | None |

---

## 31. e0e9f15d06 - Implement various convenience methods for System.Numerics types (#115457)

**Date:** 2025-05-12 19:51:25
**Commit:** [e0e9f15d06](https://github.com/dotnet/runtime/commit/e0e9f15d06b775325c874674bfca51d18c8f5075)
**Affected Tests:** 2

| Test Name | Link | Change | Before | After | Other Changepoints |
|-----------|------|--------|--------|-------|--------------------|
| System.Numerics.Tests.Perf_Matrix3x2.InvertBenchmark | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.Numerics.Tests.Perf_Matrix3x2.InvertBenchmark.html) | +52.90% | 3.860899 | 5.471292 | None |
| System.Numerics.Tests.Perf_Matrix3x2.GetDeterminantBenchmark | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.Numerics.Tests.Perf_Matrix3x2.GetDeterminantBenchmark.html) | +35.07% | 3.548779 | 4.548978 | None |

---

## 32. 5cb6a06da6 - JIT: Add simple late layout pass (#107483)

**Date:** 2024-09-10 02:38:23
**Commit:** [5cb6a06da6](https://github.com/dotnet/runtime/commit/5cb6a06da634ee4be4f426711e9c5f66535a78c8)
**Affected Tests:** 1

| Test Name | Link | Change | Before | After | Other Changepoints |
|-----------|------|--------|--------|-------|--------------------|
| Benchstone.BenchI.BubbleSort.Test | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/Benchstone.BenchI.BubbleSort.Test.html) | +18.27% | 11607.312464 | 13133.369099 | [7](#7-2a2b7dc72b---jit-fix-profile-maintenance-in-optsetblockweights-funclet-creation-111736) |

---

## 33. f4289173ad - Update field references in property accessors (#108413)

**Date:** 2024-10-03 20:13:08
**Commit:** [f4289173ad](https://github.com/dotnet/runtime/commit/f4289173ad518aacc98ba315491d5cd78ecc3b13)
**Affected Tests:** 1

| Test Name | Link | Change | Before | After | Other Changepoints |
|-----------|------|--------|--------|-------|--------------------|
| System.Reflection.Invoke.Field_SetStatic_struct | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.Reflection.Invoke.Field_SetStatic_struct.html) | +16.77% | 58.879207 | 68.374136 | None |

---

## 34. 9c1f53e39f - JIT: Put all CSEs into SSA (#106637)

**Date:** 2024-11-07 10:17:50
**Commit:** [9c1f53e39f](https://github.com/dotnet/runtime/commit/9c1f53e39f48b09be71097f1b7a47e45331e4906)
**Affected Tests:** 1

| Test Name | Link | Change | Before | After | Other Changepoints |
|-----------|------|--------|--------|-------|--------------------|
| System.Buffers.Tests.RentReturnArrayPoolTests<Byte>.ProducerConsumer(RentalSize: 4096, ManipulateArray: True, Async: False, UseSharedPool: False) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.Buffers.Tests.RentReturnArrayPoolTests%28Byte%29.ProducerConsumer%28RentalSize%3A%204096%2C%20ManipulateArray%3A%20True%2C%20Async%3A%20False%2C%20UseSharedPool%3A%20False%29.html) | +55.73% | 2206.295943 | 3108.850392 | None |

---

## 35. 72a243b43f - JIT: fix loop cloning when loop's last block is within a hander (#111432)

**Date:** 2025-01-15 21:30:47
**Commit:** [72a243b43f](https://github.com/dotnet/runtime/commit/72a243b43fae9b11933113a454533e68678e5bad)
**Affected Tests:** 1

| Test Name | Link | Change | Before | After | Other Changepoints |
|-----------|------|--------|--------|-------|--------------------|
| System.IO.Tests.Perf_File.ReadAllBytesAsync(size: 512) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.IO.Tests.Perf_File.ReadAllBytesAsync%28size%3A%20512%29.html) | +8.45% | 12244.885496 | 12877.704953 | None |

---

## 36. 37b1764e19 - Optimize BigInteger.Divide (#96895)

**Date:** 2025-02-04 19:13:15
**Commit:** [37b1764e19](https://github.com/dotnet/runtime/commit/37b1764e19aceaa545d8433c490b850538b8905a)
**Affected Tests:** 1

| Test Name | Link | Change | Before | After | Other Changepoints |
|-----------|------|--------|--------|-------|--------------------|
| System.Numerics.Tests.Perf_BigInteger.ModPow(arguments: 1024,1024,64 bits) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.Numerics.Tests.Perf_BigInteger.ModPow%28arguments%3A%201024%2C1024%2C64%20bits%29.html) | +7.03% | 95817.971292 | 102569.741452 | [12](#12-ddf8075a2f---jit-visit-blocks-in-rpo-during-lsra-107927) |

---

## 37. 995b6de753 - JIT: Don't use `Compiler::compFloatingPointUsed` to check if FP kills are needed (#112668)

**Date:** 2025-02-19 12:45:56
**Commit:** [995b6de753](https://github.com/dotnet/runtime/commit/995b6de7533b0baae6f93b003e6d9560d448eb30)
**Affected Tests:** 1

| Test Name | Link | Change | Before | After | Other Changepoints |
|-----------|------|--------|--------|-------|--------------------|
| System.Threading.Tests.Perf_Volatile.Read_double | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.Threading.Tests.Perf_Volatile.Read_double.html) | +107.17% | 2.150956 | 4.180414 | None |

---

## 38. 3c8bae3ff0 - JIT: also run local assertion prop in postorder during morph (#115626)

**Date:** 2025-05-16 22:16:17
**Commit:** [3c8bae3ff0](https://github.com/dotnet/runtime/commit/3c8bae3ff0906f590c6eec61eb114eac205ac2cc)
**Affected Tests:** 1

| Test Name | Link | Change | Before | After | Other Changepoints |
|-----------|------|--------|--------|-------|--------------------|
| System.Collections.Perf_LengthBucketsFrozenDictionary.TryGetValue_False_FrozenDictionary(Count: 1000, ItemsPerBucket: 1) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.Collections.Perf_LengthBucketsFrozenDictionary.TryGetValue_False_FrozenDictionary%28Count%3A%201000%2C%20ItemsPerBucket%3A%201%29.html) | +5.27% | N/A | N/A | None |

---

## 39. 8115429a72 - JIT: Disallow forward substitution of async calls (#115936)

**Date:** 2025-05-25 13:31:12
**Commit:** [8115429a72](https://github.com/dotnet/runtime/commit/8115429a72fa599584135f217f3a34bd4aef8cf4)
**Affected Tests:** 1

| Test Name | Link | Change | Before | After | Other Changepoints |
|-----------|------|--------|--------|-------|--------------------|
| System.Net.Sockets.Tests.SocketSendReceivePerfTest.ConnectAcceptAsync | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.Net.Sockets.Tests.SocketSendReceivePerfTest.ConnectAcceptAsync.html) | +31.62% | 85781.423491 | 113295.579798 | [1](#1-0fe82fbd8e---reduce-spin-waiting-in-the-thread-pool-on-arm-processors-115402) |

---

## 40. 2d5a2ee095 - Remove canceled AsyncOperations from channel queues (#116021)

**Date:** 2025-06-02 15:59:11
**Commit:** [2d5a2ee095](https://github.com/dotnet/runtime/commit/2d5a2ee09518e3afad75ea9bc40df0a548bcfa36)
**Affected Tests:** 1

| Test Name | Link | Change | Before | After | Other Changepoints |
|-----------|------|--------|--------|-------|--------------------|
| System.Threading.Channels.Tests.BoundedChannelPerfTests.TryWriteThenTryRead | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.Threading.Channels.Tests.BoundedChannelPerfTests.TryWriteThenTryRead.html) | +24.09% | 63.379614 | 78.056613 | None |

---

## 41. 6278b81081 - [main] Update dependencies from dnceng/internal/dotnet-optimization (#116426)

**Date:** 2025-07-09 12:44:57
**Commit:** [6278b81081](https://github.com/dotnet/runtime/commit/6278b81081f1f66dea78686f5198922da9011645)
**Affected Tests:** 1

| Test Name | Link | Change | Before | After | Other Changepoints |
|-----------|------|--------|--------|-------|--------------------|
| System.Buffers.Text.Tests.Base64Tests.ConvertToBase64CharArray(NumberOfBytes: 1000) | [Link](https://pvscmdupload.z22.web.core.windows.net/reports/allTestHistory/refs/heads/main_arm64_ubuntu%2022.04/System.Buffers.Text.Tests.Base64Tests.ConvertToBase64CharArray%28NumberOfBytes%3A%201000%29.html) | +11.63% | 263.802434 | 295.922009 | [7](#7-2a2b7dc72b---jit-fix-profile-maintenance-in-optsetblockweights-funclet-creation-111736) |

---

## Legend

- **Change %** Positive = regression (slower), Negative = improvement (faster)
- **Other Changepoints** Links to other changepoints affecting the same test
