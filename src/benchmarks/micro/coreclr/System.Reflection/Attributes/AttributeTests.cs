using BenchmarkDotNet.Attributes;
using MicroBenchmarks;
using System;
using System.Reflection;

namespace Attributes
{
    [BenchmarkCategory(Categories.CoreCLR, Categories.Reflection)]
    public partial class AttributeTests
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
    }
}