// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using BenchmarkDotNet.Attributes;
using MicroBenchmarks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.IO;
using System.Text.Json.Tests;

namespace System.Text.Json.Document.Tests
{
    [BenchmarkCategory(Categories.Libraries, Categories.JSON)]
    public class Perf_DocumentParse
    {
        public enum TestCaseType
        {
            HelloWorld,
            BasicJson,
            Json400B,
            Json400KB
        }

        [ParamsAllValues]
        public TestCaseType TestCase;

        [Params(true, false)]
        public bool IsDataCompact;

        [Params(false, true)]
        public bool TestRandomAccess;

        private byte[] _dataUtf8;

        [GlobalSetup]
        public void Setup()
        {
            string jsonString = JsonStrings.ResourceManager.GetString(TestCase.ToString());

            // Remove all formatting/indentation
            if (IsDataCompact)
            {
                using (var jsonReader = new JsonTextReader(new StringReader(jsonString)))
                using (var stringWriter = new StringWriter())
                using (var jsonWriter = new JsonTextWriter(stringWriter))
                {
                    JToken obj = JToken.ReadFrom(jsonReader);
                    obj.WriteTo(jsonWriter);
                    jsonString = stringWriter.ToString();
                }
            }

            _dataUtf8 = Encoding.UTF8.GetBytes(jsonString);
        }

        [Benchmark]
        public void Parse()
        {
            JsonDocument document = JsonDocument.Parse(_dataUtf8);
            if (TestRandomAccess)
            {
                if (TestCase == TestCaseType.HelloWorld)
                {
                    ReadHelloWorld(document.RootElement);
                }
                else if (TestCase == TestCaseType.Json400B)
                {
                    ReadJson400B(document.RootElement);
                }
                else if (TestCase == TestCaseType.BasicJson)
                {
                    ReadJsonBasic(document.RootElement);
                }
                else if (TestCase == TestCaseType.Json400KB)
                {
                    ReadJson400KB(document.RootElement);
                }
            }
            document.Dispose();
        }

        private static string ReadHelloWorld(JsonElement elem)
        {
            string message = elem.GetProperty("message").GetString();
            return message;
        }

        private static string ReadJson400B(JsonElement elem)
        {
            var sb = new StringBuilder();
            for (int i = 0; i < elem.GetArrayLength(); i++)
            {
                sb.Append(elem[i].GetProperty("_id").GetString());
                sb.Append(elem[i].GetProperty("index").GetInt32());
                sb.Append(elem[i].GetProperty("isActive").GetBoolean());
                sb.Append(elem[i].GetProperty("balance").GetString());
                sb.Append(elem[i].GetProperty("picture").GetString());
                sb.Append(elem[i].GetProperty("age").GetInt32());
                sb.Append(elem[i].GetProperty("email").GetString());
                sb.Append(elem[i].GetProperty("phone").GetString());
                sb.Append(elem[i].GetProperty("address").GetString());
                sb.Append(elem[i].GetProperty("registered").GetString());
                sb.Append(elem[i].GetProperty("latitude").GetDouble());
                sb.Append(elem[i].GetProperty("longitude").GetDouble());
            }
            return sb.ToString();
        }

        private static string ReadJsonBasic(JsonElement elem)
        {
            var sb = new StringBuilder();
            sb.Append(elem.GetProperty("age").GetInt32());
            sb.Append(elem.GetProperty("first").GetString());
            sb.Append(elem.GetProperty("last").GetString());
            
			JsonElement phoneNumbers = elem.GetProperty("phoneNumbers");
            for (int i = 0; i < phoneNumbers.GetArrayLength(); i++)
            {
                sb.Append(phoneNumbers[i].GetString());
            }

            JsonElement address = elem.GetProperty("address");
            sb.Append(address.GetProperty("street").GetString());
            sb.Append(address.GetProperty("city").GetString());
            sb.Append(address.GetProperty("zip").GetInt32());
            return sb.ToString();
        }

        private static string ReadJson400KB(JsonElement elem)
        {
            var sb = new StringBuilder();
            for (int i = 0; i < elem.GetArrayLength(); i++)
            {
                sb.Append(elem[i].GetProperty("_id").GetString());
                sb.Append(elem[i].GetProperty("index").GetInt32());
                sb.Append(elem[i].GetProperty("guid").GetString());
                sb.Append(elem[i].GetProperty("isActive").GetBoolean());
                sb.Append(elem[i].GetProperty("balance").GetString());
                sb.Append(elem[i].GetProperty("picture").GetString());
                sb.Append(elem[i].GetProperty("age").GetInt32());
                sb.Append(elem[i].GetProperty("eyeColor").GetString());
                sb.Append(elem[i].GetProperty("name").GetString());
                sb.Append(elem[i].GetProperty("gender").GetString());
                sb.Append(elem[i].GetProperty("company").GetString());
                sb.Append(elem[i].GetProperty("email").GetString());
                sb.Append(elem[i].GetProperty("phone").GetString());
                sb.Append(elem[i].GetProperty("address").GetString());
                sb.Append(elem[i].GetProperty("about").GetString());
                sb.Append(elem[i].GetProperty("registered").GetString());
                sb.Append(elem[i].GetProperty("latitude").GetDouble());
                sb.Append(elem[i].GetProperty("longitude").GetDouble());

                JsonElement tagsObject = elem[i].GetProperty("tags");
                for (int j = 0; j < tagsObject.GetArrayLength(); j++)
                {
                    sb.Append(tagsObject[j].GetString());
                }

                JsonElement friendsObject = elem[i].GetProperty("friends");
                for (int j = 0; j < friendsObject.GetArrayLength(); j++)
                {
                    sb.Append(friendsObject[j].GetProperty("id").GetInt32());
                    sb.Append(friendsObject[j].GetProperty("name").GetString());
                }
                sb.Append(elem[i].GetProperty("greeting").GetString());
                sb.Append(elem[i].GetProperty("favoriteFruit").GetString());
            }
            return sb.ToString();
        }
    }
}
