// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RealWorld
{
    /// <summary>
    /// An implementation of ITestOutputHelper that adds one indent level to
    /// the start of each line
    /// </summary>
    public class IndentedTestOutputHelper : ITestOutputHelper, IDisposable
    {
        readonly string _indentText;
        readonly ITestOutputHelper _output;
        readonly string _closingBrace;

        public IndentedTestOutputHelper(string header, ITestOutputHelper innerOutput) :
            this(header, innerOutput, "    ", "{", "}" + Environment.NewLine)
        {
        }

        public IndentedTestOutputHelper(string header, ITestOutputHelper innerOutput, string indentText, string openingBrace, string closingBrace)
        {
            _output = innerOutput;
            _indentText = indentText;
            _closingBrace = closingBrace;
            _output.WriteLine(header);
            _output.WriteLine(openingBrace);
        }

        public void Dispose()
        {
            _output.WriteLine(_closingBrace);
        }

        public void WriteLine(string message)
        {
            _output.WriteLine(_indentText + message);
        }
    }
}
