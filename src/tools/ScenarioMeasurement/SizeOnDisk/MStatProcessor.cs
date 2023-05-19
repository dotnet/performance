using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Rocks;

namespace ScenarioMeasurement;

public sealed class MStatProcessor
{
    public sealed record Result(string Name, int Size);

    public List<Result> AssemblyStats { get; private set; }
    public List<Result> BlobStats { get; private set; }

    public void Process(string path)
    {
        var asm = AssemblyDefinition.ReadAssembly(path);
        var globalType = (TypeDefinition)asm.MainModule.LookupToken(0x02000001);

        var version = asm.Name.Version;

        AssemblyStats = GetAssemblyStats(version, globalType);
        BlobStats = GetBlobStats(globalType);
    }

    static List<Result> GetAssemblyStats(Version version, TypeDefinition globalType)
    {
        var types = globalType.Methods.First(x => x.Name == "Types");
        var typesByModules = GetTypes(version, types)
            .GroupBy(x => x.Type.Scope)
            .Select(x => new Result(x.Key.Name, x.Sum(x => x.Size)));

        var methods = globalType.Methods.First(x => x.Name == "Methods");
        var methodsByModules = GetMethods(version, methods)
            .GroupBy(x => x.Method.DeclaringType.Scope)
            .Select(x => new Result(x.Key.Name, x.Sum(x => x.Size + x.GcInfoSize + x.EhInfoSize)));

        return methodsByModules
            .Concat(typesByModules)
            .GroupBy(x => x.Name)
            .Select(x => new Result(x.Key, x.Sum(x => x.Size)))
            .OrderByDescending(x => x.Size)
            .ToList();
    }

    static List<Result> GetBlobStats(TypeDefinition globalType)
    {
        var blobs = globalType.Methods.First(x => x.Name == "Blobs");
        return GetBlobs(blobs)
            .OrderByDescending(x => x.Size)
            .ToList();
    }

    sealed record TypeStats(TypeReference Type, int Size);
    static IEnumerable<TypeStats> GetTypes(Version version, MethodDefinition types)
    {
        var entrySize = version.Major == 1 ? 2 : 3;

        types.Body.SimplifyMacros();
        var il = types.Body.Instructions;
        for (var i = 0; i + entrySize < il.Count; i += entrySize)
        {
            var type = (TypeReference)il[i + 0].Operand;
            var size = (int)il[i + 1].Operand;
            yield return new TypeStats(type, size);
        }
    }

    sealed record MethodStats(MethodReference Method, int Size, int GcInfoSize, int EhInfoSize);
    static IEnumerable<MethodStats> GetMethods(Version version, MethodDefinition methods)
    {
        var entrySize = version.Major == 1 ? 4 : 5;

        methods.Body.SimplifyMacros();
        var il = methods.Body.Instructions;
        for (var i = 0; i + entrySize < il.Count; i += entrySize)
        {
            var method = (MethodReference)il[i + 0].Operand;
            var size = (int)il[i + 1].Operand;
            var gcInfoSize = (int)il[i + 2].Operand;
            var ehInfoSize = (int)il[i + 3].Operand;
            yield return new MethodStats(method, size, gcInfoSize, ehInfoSize);
        }
    }

    static IEnumerable<Result> GetBlobs(MethodDefinition blobs)
    {
        blobs.Body.SimplifyMacros();
        var il = blobs.Body.Instructions;
        for (var i = 0; i + 2 < il.Count; i += 2)
        {
            var name = (string)il[i + 0].Operand;
            var size = (int)il[i + 1].Operand;
            yield return new Result(name, size);
        }
    }
}
