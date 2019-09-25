// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.IO;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace CompilerBenchmarks
{
    internal sealed class AdditionalTextFile : AdditionalText
    {
        private readonly SourceText _text;

        public AdditionalTextFile(string path)
        {
            Path = path;
            _text = SourceText.From(File.ReadAllText(path));
        }

        public override string Path { get; }

        public override SourceText GetText(CancellationToken cancellationToken = default) => _text;
    }
}
