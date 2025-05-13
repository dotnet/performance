using System;
using System.IO;
using System.Linq;
using System.Reflection;
using BenchmarkDotNet.Attributes;

namespace ILLinkBenchmarks;

[BenchmarkCategory("ILLink")]
public partial class BasicBenchmark
{
    MethodInfo _linkerMain;
    string[] _args;
    string _publishOutputFolder;
    string[] _linkerOutArgs => new string[] { "-out", Path.Combine(_publishOutputFolder, "linked") };
    string[] _extraArgs = new string[] {
        "--singlewarn",
        "--trim-mode", "link",
        "--action", "link",
        "--nowarn", "1701;1702;IL2121;1701;1702",
        "--warn", "5",
        "--warnaserror-", "--warnaserror", ";NU1605",
        "--feature", "Microsoft.Extensions.DependencyInjection.VerifyOpenGenericServiceTrimmability", "true",
        "--feature", "System.ComponentModel.TypeConverter.EnableUnsafeBinaryFormatterInDesigntimeLicenseContextSerialization", "false",
        "--feature", "System.Resources.ResourceManager.AllowCustomResourceTypes", "false",
        "--feature", "System.Runtime.InteropServices.BuiltInComInterop.IsSupported", "false",
        "--feature", "System.Runtime.InteropServices.EnableConsumingManagedCodeFromNativeHosting", "false",
        "--feature", "System.Runtime.InteropServices.EnableCppCLIHostActivation", "false",
        "--feature", "System.Runtime.Serialization.EnableUnsafeBinaryFormatterSerialization", "false",
        "--feature", "System.StartupHookProvider.IsSupported", "false",
        "--feature", "System.Threading.Thread.EnableAutoreleasePool", "false",
        "--feature", "System.Text.Json.JsonSerializer.IsReflectionEnabledByDefault", "false",
        "--feature", "System.Text.Encoding.EnableUnsafeUTF7Encoding", "false",
        "-b",
        "--skip-unresolved", "true" };

    [GlobalSetup(Targets = new[] { nameof(LinkHelloWorld) })]
    public void LinkHelloWorldGlobalSetup()
    {
        // Publish the hello world app to link
        string projectFilePath = Environment.GetEnvironmentVariable("ILLINK_SAMPLE_PROJECT");
        string projectName = Path.GetFileNameWithoutExtension(projectFilePath);
        _publishOutputFolder = Utilities.PublishSampleProject(projectFilePath);
        // Gather arguments
        string rootAssembly = Path.Combine(_publishOutputFolder, projectName + ".dll");
        var frameworkFiles = Directory.EnumerateFiles(_publishOutputFolder, "*.dll").Where(a =>
            // Filter references - some .dll's can't be loaded by linker
            FrameworkAssemblies.Contains(Path.GetFileName(a))
        );
        var frameworkArgs = frameworkFiles.SelectMany(fileName => new string[] { "-reference", fileName });
        var assemblyArgs = new string[] { "-a", rootAssembly, "--singlewarn-", projectName };
        _args = assemblyArgs.Concat(frameworkArgs).Concat(_extraArgs).Concat(_linkerOutArgs).ToArray();
        // Use reflection to get the Driver.Main method to run the linker
        Mono.Linker.WarnVersion warnVersion = Mono.Linker.WarnVersion.Latest;
        Type driver = warnVersion.GetType().Assembly.GetType("Mono.Linker.Driver", true, false);
        _linkerMain = driver.GetMethod("Main", BindingFlags.Static | BindingFlags.Public);
        return;
    }

    [GlobalCleanup(Targets = new[] { nameof(LinkHelloWorld) })]
    public void LinkHelloWorldGlobalCleanup()
    {
        Directory.Delete(_publishOutputFolder, recursive: true);
    }

    [Benchmark]
    [BenchmarkCategory("ILLink")]
    public object LinkHelloWorld()
    {
        return _linkerMain.Invoke(null, new object[] { _args });
    }

    private static readonly string[] FrameworkAssemblies = new string[]
    {
        "Microsoft.CSharp.dll",
        "Microsoft.VisualBasic.Core.dll",
        "Microsoft.VisualBasic.dll",
        "Microsoft.Win32.Primitives.dll",
        "Microsoft.Win32.Registry.dll",
        "mscorlib.dll",
        "netstandard.dll",
        "System.AppContext.dll",
        "System.Buffers.dll",
        "System.Collections.Concurrent.dll",
        "System.Collections.dll",
        "System.Collections.Immutable.dll",
        "System.Collections.NonGeneric.dll",
        "System.Collections.Specialized.dll",
        "System.ComponentModel.Annotations.dll",
        "System.ComponentModel.DataAnnotations.dll",
        "System.ComponentModel.dll",
        "System.ComponentModel.EventBasedAsync.dll",
        "System.ComponentModel.Primitives.dll",
        "System.ComponentModel.TypeConverter.dll",
        "System.Configuration.dll",
        "System.Console.dll",
        "System.Core.dll",
        "System.Data.Common.dll",
        "System.Data.DataSetExtensions.dll",
        "System.Data.dll",
        "System.Diagnostics.Contracts.dll",
        "System.Diagnostics.Debug.dll",
        "System.Diagnostics.DiagnosticSource.dll",
        "System.Diagnostics.FileVersionInfo.dll",
        "System.Diagnostics.Process.dll",
        "System.Diagnostics.StackTrace.dll",
        "System.Diagnostics.TextWriterTraceListener.dll",
        "System.Diagnostics.Tools.dll",
        "System.Diagnostics.TraceSource.dll",
        "System.Diagnostics.Tracing.dll",
        "System.dll",
        "System.Drawing.dll",
        "System.Drawing.Primitives.dll",
        "System.Dynamic.Runtime.dll",
        "System.Formats.Asn1.dll",
        "System.Globalization.Calendars.dll",
        "System.Globalization.dll",
        "System.Globalization.Extensions.dll",
        "System.IO.Compression.Brotli.dll",
        "System.IO.Compression.dll",
        "System.IO.Compression.FileSystem.dll",
        "System.IO.Compression.ZipFile.dll",
        "System.IO.dll",
        "System.IO.FileSystem.AccessControl.dll",
        "System.IO.FileSystem.dll",
        "System.IO.FileSystem.DriveInfo.dll",
        "System.IO.FileSystem.Primitives.dll",
        "System.IO.FileSystem.Watcher.dll",
        "System.IO.IsolatedStorage.dll",
        "System.IO.MemoryMappedFiles.dll",
        "System.IO.Pipes.AccessControl.dll",
        "System.IO.Pipes.dll",
        "System.IO.UnmanagedMemoryStream.dll",
        "System.Linq.dll",
        "System.Linq.Expressions.dll",
        "System.Linq.Parallel.dll",
        "System.Linq.Queryable.dll",
        "System.Memory.dll",
        "System.Net.dll",
        "System.Net.Http.dll",
        "System.Net.Http.Json.dll",
        "System.Net.HttpListener.dll",
        "System.Net.Mail.dll",
        "System.Net.NameResolution.dll",
        "System.Net.NetworkInformation.dll",
        "System.Net.Ping.dll",
        "System.Net.Primitives.dll",
        "System.Net.Quic.dll",
        "System.Net.Requests.dll",
        "System.Net.Security.dll",
        "System.Net.ServicePoint.dll",
        "System.Net.Sockets.dll",
        "System.Net.WebClient.dll",
        "System.Net.WebHeaderCollection.dll",
        "System.Net.WebProxy.dll",
        "System.Net.WebSockets.Client.dll",
        "System.Net.WebSockets.dll",
        "System.Numerics.dll",
        "System.Numerics.Vectors.dll",
        "System.ObjectModel.dll",
        "System.Private.CoreLib.dll",
        "System.Private.DataContractSerialization.dll",
        "System.Private.Uri.dll",
        "System.Private.Xml.dll",
        "System.Private.Xml.Linq.dll",
        "System.Reflection.DispatchProxy.dll",
        "System.Reflection.dll",
        "System.Reflection.Emit.dll",
        "System.Reflection.Emit.ILGeneration.dll",
        "System.Reflection.Emit.Lightweight.dll",
        "System.Reflection.Extensions.dll",
        "System.Reflection.Metadata.dll",
        "System.Reflection.Primitives.dll",
        "System.Reflection.TypeExtensions.dll",
        "System.Resources.Reader.dll",
        "System.Resources.ResourceManager.dll",
        "System.Resources.Writer.dll",
        "System.Runtime.CompilerServices.Unsafe.dll",
        "System.Runtime.CompilerServices.VisualC.dll",
        "System.Runtime.dll",
        "System.Runtime.Extensions.dll",
        "System.Runtime.Handles.dll",
        "System.Runtime.InteropServices.dll",
        "System.Runtime.InteropServices.RuntimeInformation.dll",
        "System.Runtime.Intrinsics.dll",
        "System.Runtime.Loader.dll",
        "System.Runtime.Numerics.dll",
        "System.Runtime.Serialization.dll",
        "System.Runtime.Serialization.Formatters.dll",
        "System.Runtime.Serialization.Primitives.dll",
        "System.Runtime.Serialization.Xml.dll",
        "System.Security.AccessControl.dll",
        "System.Security.Claims.dll",
        "System.Security.Cryptography.Algorithms.dll",
        "System.Security.Cryptography.Cng.dll",
        "System.Security.Cryptography.Csp.dll",
        "System.Security.Cryptography.Encoding.dll",
        "System.Security.Cryptography.OpenSsl.dll",
        "System.Security.Cryptography.Primitives.dll",
        "System.Security.Cryptography.X509Certificates.dll",
        "System.Security.dll",
        "System.Security.Principal.dll",
        "System.Security.Principal.Windows.dll",
        "System.Security.SecureString.dll",
        "System.ServiceModel.Web.dll",
        "System.ServiceProcess.dll",
        "System.Text.Encoding.CodePages.dll",
        "System.Text.Encoding.dll",
        "System.Text.Encoding.Extensions.dll",
        "System.Text.Encodings.Web.dll",
        "System.Text.Json.dll",
        "System.Text.RegularExpressions.dll",
        "System.Threading.Channels.dll",
        "System.Threading.dll",
        "System.Threading.Overlapped.dll",
        "System.Threading.Tasks.Dataflow.dll",
        "System.Threading.Tasks.dll",
        "System.Threading.Tasks.Extensions.dll",
        "System.Threading.Tasks.Parallel.dll",
        "System.Threading.Thread.dll",
        "System.Threading.ThreadPool.dll",
        "System.Threading.Timer.dll",
        "System.Transactions.dll",
        "System.Transactions.Local.dll",
        "System.ValueTuple.dll",
        "System.Web.dll",
        "System.Web.HttpUtility.dll",
        "System.Windows.dll",
        "System.Xml.dll",
        "System.Xml.Linq.dll",
        "System.Xml.ReaderWriter.dll",
        "System.Xml.Serialization.dll",
        "System.Xml.XDocument.dll",
        "System.Xml.XmlDocument.dll",
        "System.Xml.XmlSerializer.dll",
        "System.Xml.XPath.dll",
        "System.Xml.XPath.XDocument.dll",
        "WindowsBase.dll"};
}
