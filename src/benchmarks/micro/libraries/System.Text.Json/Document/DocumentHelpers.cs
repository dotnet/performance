// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.IO;

namespace System.Text.Json.Document.Tests
{
    internal static class DocumentHelpers
    {
        public static byte[] RemoveFormatting(string jsonString)
        {
            // Remove all formatting/indentation
            using (var jsonReader = new JsonTextReader(new StringReader(jsonString)))
            using (var stringWriter = new StringWriter())
            using (var jsonWriter = new JsonTextWriter(stringWriter))
            {
                JToken obj = JToken.ReadFrom(jsonReader);
                obj.WriteTo(jsonWriter);
                jsonString = stringWriter.ToString();
            }

            return Encoding.UTF8.GetBytes(jsonString);
        }
    }
}
