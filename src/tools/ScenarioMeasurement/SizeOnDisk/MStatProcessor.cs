using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Rocks;

namespace ScenarioMeasurement
{
    public sealed class MStatProcessor
    {
        public sealed record Result(string Name, int Size);

        public List<Result> AssemblyStats { get; private set; }
        public List<Result> BlobStats { get; private set; }

        public void Process(string path)
        {
            var asm = AssemblyDefinition.ReadAssembly(path);
            var globalType = (TypeDefinition)asm.MainModule.LookupToken(0x02000001);

            AssemblyStats = GetAssemblyStats(globalType);
            BlobStats = GetBlobStats(globalType);
        }

        static List<Result> GetAssemblyStats(TypeDefinition globalType)
        {
            var types = globalType.Methods.First(x => x.Name == "Types");
            var typesByModules = GetTypes(types)
                .GroupBy(x => x.Type.Scope)
                .Select(x => new Result(x.Key.Name, x.Sum(x => x.Size)));

            var methods = globalType.Methods.First(x => x.Name == "Methods");
            var methodsByModules = GetMethods(methods)
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
        static IEnumerable<TypeStats> GetTypes(MethodDefinition types)
        {
            types.Body.SimplifyMacros();
            var il = types.Body.Instructions;
            for (int i = 0; i + 2 < il.Count; i += 2)
            {
                var type = (TypeReference)il[i + 0].Operand;
                var size = (int)il[i + 1].Operand;
                yield return new TypeStats(type, size);
            }
        }

        sealed record MethodStats(MethodReference Method, int Size, int GcInfoSize, int EhInfoSize);
        static IEnumerable<MethodStats> GetMethods(MethodDefinition methods)
        {
            methods.Body.SimplifyMacros();
            var il = methods.Body.Instructions;
            for (int i = 0; i + 4 < il.Count; i += 4)
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
            for (int i = 0; i + 2 < il.Count; i += 2)
            {
                var name = (string)il[i + 0].Operand;
                var size = (int)il[i + 1].Operand;
                yield return new Result(name, size);
            }
        }
    }
}
