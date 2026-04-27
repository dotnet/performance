// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using BenchmarkDotNet.Attributes;
using MicroBenchmarks;

namespace System.Reflection
{
    [BenchmarkCategory(Categories.Runtime, Categories.Reflection)]
    public class Attributes
    {
        private static readonly MethodInfo AttributedOverride = typeof(AttributedClass).GetMethod(nameof(AttributedClass.FooAttributed));
        private static readonly MethodInfo NonAttributedOverride = typeof(NonAttributedClass).GetMethod(nameof(NonAttributedClass.FooAttributed));
        private static readonly MethodInfo AttributedBase = typeof(BaseClass).GetMethod(nameof(BaseClass.FooAttributed));
        private static readonly MethodInfo NonAttributedBase = typeof(BaseClass).GetMethod(nameof(BaseClass.FooUnattributed));

        [My("Test")]
        [Serializable]
        public class AttributedClass : BaseClass
        {
            [My("FooAttributed")]
            public override string FooAttributed() => null;
        }

        public class NonAttributedClass : BaseClass
        {
            public override string FooUnattributed() => null;
        }

        public class BaseClass
        {
            [My("Foo")]
            public virtual string FooAttributed() => null;
            public virtual string FooUnattributed() => null;
        }

        [AttributeUsage(AttributeTargets.All, AllowMultiple = false)]
        public class MyAttribute : Attribute
        {
            private readonly string _name;
            public MyAttribute(string name) => _name = name;
        }

        // GetCustomAttributes

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
        [MemoryRandomization]
        public object[] GetCustomAttributesMethodBaseMissInherit() => NonAttributedBase.GetCustomAttributes(typeof(MyAttribute), true);

        [Benchmark(Description = "GetCustomAttributes - Method Base: Hit (no inherit)")]
        public object[] GetCustomAttributesMethodBaseHit() => AttributedBase.GetCustomAttributes(typeof(MyAttribute), false);

        [Benchmark(Description = "GetCustomAttributes - Method Base: Miss (no inherit)")]
        [MemoryRandomization]
        public object[] GetCustomAttributesMethodBaseMiss() => NonAttributedBase.GetCustomAttributes(typeof(MyAttribute), false);

        // IsDefined

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