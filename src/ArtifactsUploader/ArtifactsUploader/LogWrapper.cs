// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Text;
using Serilog;

namespace ArtifactsUploader
{
    public class LogWrapper : TextWriter
    {
        public LogWrapper(ILogger log) => Log = log;

        public override Encoding Encoding => Encoding.UTF8;

        private ILogger Log { get; }

        public override void Write(string value) => Log.Error(value); // this logger will be used only for parsing errors
    }
}