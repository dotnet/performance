// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Serilog;
using Serilog.Core;

namespace ArtifactsUploader
{
    public class Program
    {
        public static int Main(string[] args)
        {
            using (var log = CreateLogger())
            {
                var (isSuccess, commandLineOptions) = CommandLineOptions.Parse(args, log);

                if (!isSuccess)
                {
                    return -1;
                }

                return 0;
            }
        }

        private static Logger CreateLogger()
            => new LoggerConfiguration()
                .WriteTo.File("log.txt", rollingInterval: RollingInterval.Day)
                .WriteTo.Console()
                .CreateLogger();
    }
}