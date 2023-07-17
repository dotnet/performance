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
        [MemoryRandomization]
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

        private static void ReadJson400B(JsonElement elem)
        {
            for (int i = 0; i < elem.GetArrayLength(); i++)
            {
                elem[i].GetProperty("_id").GetString();
                elem[i].GetProperty("index").GetInt32();
                elem[i].GetProperty("isActive").GetBoolean();
                elem[i].GetProperty("balance").GetString();
                elem[i].GetProperty("picture").GetString();
                elem[i].GetProperty("age").GetInt32();
                elem[i].GetProperty("email").GetString();
                elem[i].GetProperty("phone").GetString();
                elem[i].GetProperty("address").GetString();
                elem[i].GetProperty("registered").GetString();
                elem[i].GetProperty("latitude").GetDouble();
                elem[i].GetProperty("longitude").GetDouble();
            }
        }

        private static void ReadJsonBasic(JsonElement elem)
        {
            elem.GetProperty("age").GetInt32();
            elem.GetProperty("first").GetString();
            elem.GetProperty("last").GetString();
            
            JsonElement phoneNumbers = elem.GetProperty("phoneNumbers");
            for (int i = 0; i < phoneNumbers.GetArrayLength(); i++)
            {
                phoneNumbers[i].GetString();
            }

            JsonElement address = elem.GetProperty("address");
            address.GetProperty("street").GetString();
            address.GetProperty("city").GetString();
            address.GetProperty("zip").GetInt32();
        }

        private static void ReadJson400KB(JsonElement elem)
        {
            for (int i = 0; i < elem.GetArrayLength(); i++)
            {
                elem[i].GetProperty("_id").GetString();
                elem[i].GetProperty("index").GetInt32();
                elem[i].GetProperty("guid").GetString();
                elem[i].GetProperty("isActive").GetBoolean();
                elem[i].GetProperty("balance").GetString();
                elem[i].GetProperty("picture").GetString();
                elem[i].GetProperty("age").GetInt32();
                elem[i].GetProperty("eyeColor").GetString();
                elem[i].GetProperty("name").GetString();
                elem[i].GetProperty("gender").GetString();
                elem[i].GetProperty("company").GetString();
                elem[i].GetProperty("email").GetString();
                elem[i].GetProperty("phone").GetString();
                elem[i].GetProperty("address").GetString();
                elem[i].GetProperty("about").GetString();
                elem[i].GetProperty("registered").GetString();
                elem[i].GetProperty("latitude").GetDouble();
                elem[i].GetProperty("longitude").GetDouble();

                JsonElement tagsObject = elem[i].GetProperty("tags");
                for (int j = 0; j < tagsObject.GetArrayLength(); j++)
                {
                    tagsObject[j].GetString();
                }

                JsonElement friendsObject = elem[i].GetProperty("friends");
                for (int j = 0; j < friendsObject.GetArrayLength(); j++)
                {
                    friendsObject[j].GetProperty("id").GetInt32();
                    friendsObject[j].GetProperty("name").GetString();
                }

                elem[i].GetProperty("greeting").GetString();
                elem[i].GetProperty("favoriteFruit").GetString();
            }
        }
    }
}
