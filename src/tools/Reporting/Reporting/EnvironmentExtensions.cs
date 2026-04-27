// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Reporting;

public static class EnvironmentExtensions
{
    public static bool IsLabEnvironment(this IEnvironment environment)
        => environment.GetEnvironmentVariable("PERFLAB_INLAB")?.Equals("1") ?? false;
}
