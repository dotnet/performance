// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using BenchmarkDotNet.Attributes;
using MicroBenchmarks;

namespace System.Reflection
{
    [BenchmarkCategory(Categories.Runtime, Categories.Reflection)]
    public class Invoke
    {
        private static MyClass s_MyClass = new MyClass();
        private static object[] s_args = new object[] { 42, "Hello", default(MyBlittableStruct), s_MyClass };
        private static int s_dummy;
        private static MethodInfo s_method;
        private static MethodInfo s_method_int_string_struct_class;
        private static MethodInfo s_method_byref_int_string_struct_class;
        private static MethodInfo s_method_nullableInt;
        private static ConstructorInfo s_ctor_int_string_struct_class;
        private static ConstructorInfo s_ctor_NoParams;
        private static PropertyInfo s_property_int;
        private static PropertyInfo s_property_class;
        private static FieldInfo s_field_int;
        private static FieldInfo s_field_class;

        [GlobalSetup]
        public void Setup()
        {
            s_method = typeof(MyClass).
                GetMethod(nameof(MyClass.MyMethod), BindingFlags.Public | BindingFlags.Instance)!;

            s_method_int_string_struct_class = typeof(Invoke).
                GetMethod(nameof(Method_int_string_struct_class), BindingFlags.Public | BindingFlags.Static)!;

            s_method_byref_int_string_struct_class = typeof(Invoke).
                GetMethod(nameof(Method_byref_int_string_struct_class), BindingFlags.Public | BindingFlags.Static)!;

            s_method_nullableInt = typeof(Invoke).
                GetMethod(nameof(Method_nullableInt), BindingFlags.Public | BindingFlags.Static)!;

            s_ctor_int_string_struct_class = typeof(MyClass).
                GetConstructor(new Type[] { typeof(int), typeof(string), typeof(MyBlittableStruct), typeof(MyClass) })!;

            s_ctor_NoParams = typeof(MyClass).
                GetConstructor(Array.Empty<Type>())!;

            s_property_int = typeof(MyClass).
                GetProperty(nameof(MyClass.I));

            s_property_class = typeof(MyClass).
                GetProperty(nameof(MyClass.O));

            s_field_int = typeof(MyClass).
                GetField(nameof(MyClass.i));

            s_field_class = typeof(MyClass).
                GetField(nameof(MyClass.o));
        }

        public static void Method_int_string_struct_class(int i, string s, MyBlittableStruct myStruct, MyClass myClass)
        {
            s_dummy++;
        }

        public static void Method_byref_int_string_struct_class(ref int i, ref string s, ref MyBlittableStruct myStruct, ref MyClass myClass)
        {
            s_dummy++;
        }

        public static void Method_nullableInt(int? i)
        {
            s_dummy++;
        }

        [Benchmark]
        public void Method0_NoParms()
        {
            s_method.Invoke(s_MyClass, null);
        }

        [Benchmark]
        // Include the array allocation and population for a typical scenario.
        public void StaticMethod4_arrayNotCached_int_string_struct_class()
        {
            object[] args = new object[] { 42, "Hello", default(MyBlittableStruct), s_MyClass };
            s_method_int_string_struct_class.Invoke(null, args);
        }

        [Benchmark]
        public void StaticMethod4_int_string_struct_class()
        {
            s_method_int_string_struct_class.Invoke(null, s_args);
        }

        [Benchmark]
        public void StaticMethod4_ByRefParams_int_string_struct_class()
        {
            s_method_byref_int_string_struct_class.Invoke(null, s_args);
        }

        [Benchmark]
        public void Ctor0_NoParams()
        {
            s_ctor_NoParams.Invoke(null);
        }

        [Benchmark]
        public void Ctor0_ActivatorCreateInstance_NoParams()
        {
            Activator.CreateInstance(typeof(MyClass));
        }

        [Benchmark]
        public void Ctor4_int_string_struct_class()
        {
            s_ctor_int_string_struct_class.Invoke(s_args);
        }

        [Benchmark]
        public void Ctor4_ActivatorCreateInstance()
        {
            Activator.CreateInstance(typeof(MyClass), s_args);
        }

        [Benchmark]
        public void Property_Get_int()
        {
            s_property_int.GetValue(s_MyClass);
        }

        [Benchmark]
        public void Property_Get_class()
        {
            s_property_class.GetValue(s_MyClass);
        }

        [Benchmark]
        public void Property_Set_int()
        {
            s_property_int.SetValue(s_MyClass, 42);
        }

        [Benchmark]
        public void Property_Set_class()
        {
            s_property_class.SetValue(s_MyClass, null);
        }

        [Benchmark]
        public void Field_Get_int()
        {
            s_field_int.GetValue(s_MyClass);
        }

        [Benchmark]
        public void Field_Set_int()
        {
            s_field_int.SetValue(s_MyClass, 42);
        }

        public struct MyBlittableStruct
        {
            public int i;
            public bool b;
        }

        public class MyClass
        {
            public MyClass() { }
            public MyClass(int i, string s, MyBlittableStruct myStruct, MyClass myClass) { }

            public void MyMethod() { }

            public int i = 0;
            public bool b = false;
            public string s = null;
            public object o = null;

            public int I { get; set; } = 0;
            public object O { get; set; } = null;
        }
    }
}
