// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using BenchmarkDotNet.Attributes;
using MicroBenchmarks;
using System.Text.Json.Document.Tests;
using System.Text.Json.Tests;

namespace System.Text.Json.Nodes.Tests;

[BenchmarkCategory(Categories.Libraries, Categories.JSON)]
public class Perf_NodeParse
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
        JsonNode node = JsonNode.Parse(_dataUtf8);
        if (TestRandomAccess)
        {
            if (TestCase == TestCaseType.HelloWorld)
            {
                ReadHelloWorld(node);
            }
            else if (TestCase == TestCaseType.Json400B)
            {
                ReadJson400B(node);
            }
            else if (TestCase == TestCaseType.BasicJson)
            {
                ReadJsonBasic(node);
            }
            else if (TestCase == TestCaseType.Json400KB)
            {
                ReadJson400KB(node);
            }
        }
    }

    private static string ReadHelloWorld(JsonNode node)
    {
        string message = node["message"].GetValue<string>();
        return message;
    }

    private static void ReadJson400B(JsonNode node)
    {
        JsonArray nodeArr = node.AsArray();
        for (int i = 0; i < nodeArr.Count; i++)
        {
            nodeArr[i]["_id"].GetValue<string>();
            nodeArr[i]["index"].GetValue<int>();
            nodeArr[i]["isActive"].GetValue<bool>();
            nodeArr[i]["balance"].GetValue<string> ();
            nodeArr[i]["picture"].GetValue<string>();
            nodeArr[i]["age"].GetValue<int>();
            nodeArr[i]["email"].GetValue<string>();
            nodeArr[i]["phone"].GetValue<string>();
            nodeArr[i]["address"].GetValue<string>();
            nodeArr[i]["registered"].GetValue<string>();
            nodeArr[i]["latitude"].GetValue<double>();
            nodeArr[i]["longitude"].GetValue<double>();
        }
    }

    private static void ReadJsonBasic(JsonNode node)
    {
        node["age"].GetValue<int>();
        node["first"].GetValue<string>();
        node["last"].GetValue<string>();

        JsonArray phoneNumbers = node["phoneNumbers"].AsArray();
        for (int i = 0; i < phoneNumbers.Count; i++)
        {
            phoneNumbers[i].GetValue<string>();
        }

        JsonNode address = node["address"];
        address["street"].GetValue<string>();
        address["city"].GetValue<string>();
        address["zip"].GetValue<int>();
    }

    private static void ReadJson400KB(JsonNode node)
    {
        JsonArray nodeArr = node.AsArray();
        for (int i = 0; i < nodeArr.Count; i++)
        {
            nodeArr[i]["_id"].GetValue<string>();
            nodeArr[i]["index"].GetValue<int>();
            nodeArr[i]["guid"].GetValue<string>();
            nodeArr[i]["isActive"].GetValue<bool>();
            nodeArr[i]["balance"].GetValue<string>();
            nodeArr[i]["picture"].GetValue<string>();
            nodeArr[i]["age"].GetValue<int>();
            nodeArr[i]["eyeColor"].GetValue<string>();
            nodeArr[i]["name"].GetValue<string>();
            nodeArr[i]["gender"].GetValue<string>();
            nodeArr[i]["company"].GetValue<string>();
            nodeArr[i]["email"].GetValue<string>();
            nodeArr[i]["phone"].GetValue<string>();
            nodeArr[i]["address"].GetValue<string>();
            nodeArr[i]["about"].GetValue<string>();
            nodeArr[i]["registered"].GetValue<string>();
            nodeArr[i]["latitude"].GetValue<double>();
            nodeArr[i]["longitude"].GetValue<double>();

            JsonArray tagsObject = nodeArr[i]["tags"].AsArray();
            for (int j = 0; j < tagsObject.Count; j++)
            {
                tagsObject[j].GetValue<string>();
            }

            JsonArray friendsObject = node[i]["friends"].AsArray();
            for (int j = 0; j < friendsObject.Count; j++)
            {
                friendsObject[j]["id"].GetValue<int>();
                friendsObject[j]["name"].GetValue<string>();
            }

            node[i]["greeting"].GetValue<string>();
            node[i]["favoriteFruit"].GetValue<string>();
        }
    }
}
