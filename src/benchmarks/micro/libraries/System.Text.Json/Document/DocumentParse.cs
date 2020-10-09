// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using BenchmarkDotNet.Attributes;
using MicroBenchmarks;
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
        public bool IsDataIndented;

        [Params(false, true)]
        public bool TestRandomAccess;

        private byte[] _dataUtf8;

        [GlobalSetup]
        public void Setup()
        {
            string jsonString = JsonStrings.ResourceManager.GetString(TestCase.ToString());

            // Remove all formatting/indentation
            if (!IsDataIndented)
            {
                _dataUtf8 = DocumentHelpers.RemoveFormatting(jsonString);
            }
            else
            {
                _dataUtf8 = Encoding.UTF8.GetBytes(jsonString);
            }
        }

        [Benchmark]
        public void Parse()
        {
            using (JsonDocument document = JsonDocument.Parse(_dataUtf8))
            {
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
            }
        }

        private static string ReadHelloWorld(JsonElement elem)
        {
            string message = elem.GetProperty("message").GetString();
            return message;
        }

        private static int ReadJson400B(JsonElement elem)
        {
            int result = 0;

            for (int i = 0; i < elem.GetArrayLength(); i++)
            {
                result += elem[i].GetProperty("_id").GetString().Length;
                result += elem[i].GetProperty("index").GetInt32();
                result += elem[i].GetProperty("isActive").GetBoolean() ? 1 : 0;
                result += elem[i].GetProperty("balance").GetString().Length;
                result += elem[i].GetProperty("picture").GetString().Length;
                result += elem[i].GetProperty("age").GetInt32();
                result += elem[i].GetProperty("email").GetString().Length;
                result += elem[i].GetProperty("phone").GetString().Length;
                result += elem[i].GetProperty("address").GetString().Length;
                result += elem[i].GetProperty("registered").GetString().Length;
                result += (int)elem[i].GetProperty("latitude").GetDouble();
                result += (int)elem[i].GetProperty("longitude").GetDouble();
            }

            return result;
        }

        private static int ReadJsonBasic(JsonElement elem)
        {
            int result = 0;

            result += elem.GetProperty("age").GetInt32();
            result += elem.GetProperty("first").GetString().Length;
            result += elem.GetProperty("last").GetString().Length;
            
			JsonElement phoneNumbers = elem.GetProperty("phoneNumbers");
            for (int i = 0; i < phoneNumbers.GetArrayLength(); i++)
            {
                result += phoneNumbers[i].GetString().Length;
            }

            JsonElement address = elem.GetProperty("address");
            result += address.GetProperty("street").GetString().Length;
            result += address.GetProperty("city").GetString().Length;
            result += address.GetProperty("zip").GetInt32();

            return result;
        }

        private static int ReadJson400KB(JsonElement elem)
        {
            int result = 0;

            for (int i = 0; i < elem.GetArrayLength(); i++)
            {
                result += elem[i].GetProperty("_id").GetString().Length;
                result += elem[i].GetProperty("index").GetInt32();
                result += elem[i].GetProperty("guid").GetString().Length;
                result += elem[i].GetProperty("isActive").GetBoolean() ? 1 : 0;
                result += elem[i].GetProperty("balance").GetString().Length;
                result += elem[i].GetProperty("picture").GetString().Length;
                result += elem[i].GetProperty("age").GetInt32();
                result += elem[i].GetProperty("eyeColor").GetString().Length;
                result += elem[i].GetProperty("name").GetString().Length;
                result += elem[i].GetProperty("gender").GetString().Length;
                result += elem[i].GetProperty("company").GetString().Length;
                result += elem[i].GetProperty("email").GetString().Length;
                result += elem[i].GetProperty("phone").GetString().Length;
                result += elem[i].GetProperty("address").GetString().Length;
                result += elem[i].GetProperty("about").GetString().Length;
                result += elem[i].GetProperty("registered").GetString().Length;
                result += (int)elem[i].GetProperty("latitude").GetDouble();
                result += (int)elem[i].GetProperty("longitude").GetDouble();

                JsonElement tagsObject = elem[i].GetProperty("tags");
                for (int j = 0; j < tagsObject.GetArrayLength(); j++)
                {
                    result += tagsObject[j].GetString().Length;
                }

                JsonElement friendsObject = elem[i].GetProperty("friends");
                for (int j = 0; j < friendsObject.GetArrayLength(); j++)
                {
                    result += friendsObject[j].GetProperty("id").GetInt32();
                    result += friendsObject[j].GetProperty("name").GetString().Length;
                }

                result += elem[i].GetProperty("greeting").GetString().Length;
                result += elem[i].GetProperty("favoriteFruit").GetString().Length;
            }

            return result;
        }
    }
}
