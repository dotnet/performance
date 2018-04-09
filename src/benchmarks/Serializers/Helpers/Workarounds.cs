#if SGEN
// the new SGEN tool fails to load some of the dependencies, so we need to replace the problematic dependencies for this particular build configuration
using System;
using System.Collections.Generic;
using System.Text;

namespace Benchmarks.Serializers.Helpers
{
    public class ProtoContractAttribute : Attribute
    {
        public ProtoContractAttribute() { }
    }

    public class ProtoMemberAttribute : Attribute
    {
        public ProtoMemberAttribute(int tag) { }
    }

    public class ZeroFormattableAttribute : Attribute
    {
        public ZeroFormattableAttribute() { }
    }

    public class MessagePackObjectAttribute : Attribute
    {
        public MessagePackObjectAttribute() { }
    }

    public class IndexAttribute : Attribute
    {
        public IndexAttribute(int index) { }
    }

    public class KeyAttribute : Attribute
    {
        public KeyAttribute(int key) { }
    }

    public class IgnoreFormatAttribute : Attribute
    {
        public IgnoreFormatAttribute() { }
    }

    public class IgnoreMemberAttribute : Attribute
    {
        public IgnoreMemberAttribute() { }
    }
}
#endif