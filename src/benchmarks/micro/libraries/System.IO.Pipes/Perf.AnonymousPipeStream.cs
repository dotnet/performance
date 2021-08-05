// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.IO.Pipes.Tests
{
    public class Perf_AnonymousPipeStream : Perf_PipeTest
    {
        protected override ServerClientPair CreateServerClientPair()
        {
            ServerClientPair ret = new ServerClientPair();
            ret.readablePipe = new AnonymousPipeServerStream(PipeDirection.In);
            ret.writeablePipe = new AnonymousPipeClientStream(PipeDirection.Out, ((AnonymousPipeServerStream)ret.readablePipe).ClientSafePipeHandle);
            return ret;
        }
    }
}
