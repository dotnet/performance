using BenchmarkDotNet.Attributes;

namespace Attributes
{
    public partial class AttributeTests
    {
        [Benchmark(Description = "GetCustomAttributes - Class: Hit (inherit)")]
        public object[] GetCustomAttributesClassHitInherit() => typeof(AttributedClass).GetCustomAttributes(typeof(MyAttribute), true);
        [Benchmark(Description = "GetCustomAttributes - Class: Miss (inherit)")]
        public object[] GetCustomAttributesClassMissInherit() => typeof(NonAttributedClass).GetCustomAttributes(typeof(MyAttribute), true);

        [Benchmark(Description = "GetCustomAttributes - Class: Hit (no inherit)")]
        public object[] GetCustomAttributesClassHit() => typeof(AttributedClass).GetCustomAttributes(typeof(MyAttribute), false);
        [Benchmark(Description = "GetCustomAttributes - Class: Miss (no inherit)")]
        public object[] GetCustomAttributesClassMiss() => typeof(NonAttributedClass).GetCustomAttributes(typeof(MyAttribute), false);

        [Benchmark(Description = "GetCustomAttributes - Method Override: Hit (inherit)")]
        public object[] GetCustomAttributesMethodOverrideHitInherit() => AttributedOverride.GetCustomAttributes(typeof(MyAttribute), true);
        [Benchmark(Description = "GetCustomAttributes - Method Override: Miss (inherit)")]
        public object[] GetCustomAttributesMethodOverrideMissInherit() => NonAttributedOverride.GetCustomAttributes(typeof(MyAttribute), true);

        [Benchmark(Description = "GetCustomAttributes - Method Override: Hit (no inherit)")]
        public object[] GetCustomAttributesMethodOverrideHit() => AttributedOverride.GetCustomAttributes(typeof(MyAttribute), false);
        [Benchmark(Description = "GetCustomAttributes - Method Override: Miss (no inherit)")]
        public object[] GetCustomAttributesMethodOverrideMiss() => NonAttributedOverride.GetCustomAttributes(typeof(MyAttribute), false);

        [Benchmark(Description = "GetCustomAttributes - Method Base: Hit (inherit)")]
        public object[] GetCustomAttributesMethodBaseHitInherit() => AttributedBase.GetCustomAttributes(typeof(MyAttribute), true);
        [Benchmark(Description = "GetCustomAttributes - Method Base: Miss (inherit)")]
        public object[] GetCustomAttributesMethodBaseMissInherit() => NonAttributedBase.GetCustomAttributes(typeof(MyAttribute), true);

        [Benchmark(Description = "GetCustomAttributes - Method Base: Hit (no inherit)")]
        public object[] GetCustomAttributesMethodBaseHit() => AttributedBase.GetCustomAttributes(typeof(MyAttribute), false);
        [Benchmark(Description = "GetCustomAttributes - Method Base: Miss (no inherit)")]
        public object[] GetCustomAttributesMethodBaseMiss() => NonAttributedBase.GetCustomAttributes(typeof(MyAttribute), false);
    }
}
