using BenchmarkDotNet.Attributes;

namespace Attributes
{
    public partial class AttributeTests
    {
        [Benchmark(Description = "IsDefined - Class: Hit (inherit)")]
        public bool IsDefinedClassHitInherit() => typeof(AttributedClass).IsDefined(typeof(MyAttribute), true);
        [Benchmark(Description = "IsDefined - Class: Miss (inherit)")]
        public bool IsDefinedClassMissInherit() => typeof(NonAttributedClass).IsDefined(typeof(MyAttribute), true);

        [Benchmark(Description = "IsDefined - Class: Hit (no inherit)")]
        public bool IsDefinedClassHit() => typeof(AttributedClass).IsDefined(typeof(MyAttribute), false);
        [Benchmark(Description = "IsDefined - Class: Miss (no inherit)")]
        public bool IsDefinedClassMiss() => typeof(NonAttributedClass).IsDefined(typeof(MyAttribute), false);

        [Benchmark(Description = "IsDefined - Method Override: Hit (inherit)")]
        public bool IsDefinedMethodOverrideHitInherit() => AttributedOverride.IsDefined(typeof(MyAttribute), true);
        [Benchmark(Description = "IsDefined - Method Override: Miss (inherit)")]
        public bool IsDefinedMethodOverrideMissInherit() => NonAttributedOverride.IsDefined(typeof(MyAttribute), true);

        [Benchmark(Description = "IsDefined - Method Override: Hit (no inherit)")]
        public bool IsDefinedMethodOverrideHit() => AttributedOverride.IsDefined(typeof(MyAttribute), false);
        [Benchmark(Description = "IsDefined - Method Override: Miss (no inherit)")]
        public bool IsDefinedMethodOverrideMiss() => NonAttributedOverride.IsDefined(typeof(MyAttribute), false);

        [Benchmark(Description = "IsDefined - Method Base: Hit (inherit)")]
        public bool IsDefinedMethodBaseHitInherit() => AttributedBase.IsDefined(typeof(MyAttribute), true);
        [Benchmark(Description = "IsDefined - Method Base: Miss (inherit)")]
        public bool IsDefinedMethodBaseMissInherit() => NonAttributedBase.IsDefined(typeof(MyAttribute), true);

        [Benchmark(Description = "IsDefined - Method Base: Hit (no inherit)")]
        public bool IsDefinedMethodBaseHit() => AttributedBase.IsDefined(typeof(MyAttribute), false);
        [Benchmark(Description = "IsDefined - Method Base: Miss (no inherit)")]
        public bool IsDefinedMethodBaseMiss() => NonAttributedBase.IsDefined(typeof(MyAttribute), false);
    }
}
