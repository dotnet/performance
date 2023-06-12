using YamlDotNet.Serialization;

namespace GC.Analysis.API
{
    public sealed class GCMethodsData
    {
        [YamlMember(Description = "All GC methods.")]
        public List<string> gc_methods { get; set; }
    }
}
