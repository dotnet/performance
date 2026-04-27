// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;

namespace SixLabors.ImageSharp.Tests
{
    public static partial class TestEnvironment
    {
        private const string InputImagesRelativePath = @"Images\Input";

        private static readonly FileInfo TestAssemblyFile =
            new FileInfo(typeof(TestEnvironment).GetTypeInfo().Assembly.Location);

        private static readonly string TestAssemblyDirectory = Path.GetDirectoryName(TestAssemblyFile.FullName);

        private static string GetFullPath(string relativePath) =>
            Path.Combine(TestAssemblyDirectory, relativePath)
            .Replace('\\', Path.DirectorySeparatorChar);

        /// <summary>
        /// Gets the correct full path to the Input Images directory.
        /// </summary>
        internal static string InputImagesDirectoryFullPath => GetFullPath(InputImagesRelativePath);
    }
}
