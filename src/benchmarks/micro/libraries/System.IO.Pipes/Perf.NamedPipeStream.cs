// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using BenchmarkDotNet.Attributes;
using System.Threading.Tasks;

namespace System.IO.Pipes.Tests
{
    public class Perf_NamedPipeStream : Perf_PipeTest
    {
        [Params(PipeOptions.None, PipeOptions.Asynchronous)]
        public PipeOptions Options { get; set; }

        protected override ServerClientPair CreateServerClientPair()
        {
            ServerClientPair ret = new ServerClientPair();
            string pipeName = GetUniquePipeName();
            var readablePipe = new NamedPipeServerStream(pipeName, PipeDirection.InOut, 1, PipeTransmissionMode.Byte, Options);
            var writeablePipe = new NamedPipeClientStream(".", pipeName, PipeDirection.InOut, Options);

            Task clientConnect = writeablePipe.ConnectAsync();
            readablePipe.WaitForConnection();
            clientConnect.Wait();

            ret.readablePipe = readablePipe;
            ret.writeablePipe = writeablePipe;
            return ret;
        }
    }
}
