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
        private const int Iterations = 1_000; // Reduce the randomness of these short-lived calls.

        private static MyClass s_MyClass = new MyClass();
        private static object[] s_args4 = new object[] { 42, "Hello", default(MyBlittableStruct), s_MyClass };
        private static object[] s_args5 = new object[] { 42, "Hello", default(MyBlittableStruct), s_MyClass, true };
        private static int s_dummy;
        private static MethodInfo s_method;
        private static MethodInfo s_method_int_string_struct_class;
        private static MethodInfo s_method_int_string_struct_class_bool;
        private static MethodInfo s_method_byref_int_string_struct_class;
        private static MethodInfo s_method_byref_int_string_struct_class_bool;
        private static MethodInfo s_method_nullableInt;
        private static ConstructorInfo s_ctor_int_string_struct_class;
        private static ConstructorInfo s_ctor_NoParams;
        private static PropertyInfo s_property_int;
        private static PropertyInfo s_property_class;
        private static FieldInfo s_field_int;
        private static FieldInfo s_field_class;
        private static FieldInfo s_field_struct;
        private static FieldInfo s_staticField_int;
        private static FieldInfo s_staticField_class;
        private static FieldInfo s_staticField_struct;
        public static int s_int;
        public static object s_class;
        public static MyBlittableStruct s_struct;

        [GlobalSetup]
        public void Setup()
        {
            s_method = typeof(MyClass).
                GetMethod(nameof(MyClass.MyMethod), BindingFlags.Public | BindingFlags.Instance)!;

            s_method_int_string_struct_class = typeof(Invoke).
                GetMethod(nameof(Method_int_string_struct_class), BindingFlags.Public | BindingFlags.Static)!;

            s_method_int_string_struct_class_bool = typeof(Invoke).
                GetMethod(nameof(Method_int_string_struct_class_bool), BindingFlags.Public | BindingFlags.Static)!;

            s_method_byref_int_string_struct_class = typeof(Invoke).
                GetMethod(nameof(Method_byref_int_string_struct_class), BindingFlags.Public | BindingFlags.Static)!;

            s_method_byref_int_string_struct_class_bool = typeof(Invoke).
                GetMethod(nameof(Method_byref_int_string_struct_class_bool), BindingFlags.Public | BindingFlags.Static)!;

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

            s_staticField_int = typeof(MyClass).
                GetField(nameof(MyClass.s_i));

            s_field_class = typeof(MyClass).
                GetField(nameof(MyClass.o));

            s_staticField_class = typeof(MyClass).
                GetField(nameof(MyClass.s_o));

            s_field_struct = typeof(MyClass).
                GetField(nameof(MyClass.blittableStruct));

            s_staticField_struct = typeof(MyClass).
                GetField(nameof(MyClass.s_blittableStruct));
        }

        public static void Method_int_string_struct_class(int i, string s, MyBlittableStruct myStruct, MyClass myClass)
        {
            s_dummy++;
        }

        public static void Method_int_string_struct_class_bool(int i, string s, MyBlittableStruct myStruct, MyClass myClass, bool b)
        {
            s_dummy++;
        }

        public static void Method_byref_int_string_struct_class(ref int i, ref string s, ref MyBlittableStruct myStruct, ref MyClass myClass)
        {
            s_dummy++;
        }

        public static void Method_byref_int_string_struct_class_bool(ref int i, ref string s, ref MyBlittableStruct myStruct, ref MyClass myClass, ref bool b)
        {
            s_dummy++;
        }

        public static void Method_nullableInt(int? i)
        {
            s_dummy++;
        }

        [Benchmark(OperationsPerInvoke = Iterations)]
        public void Method0_NoParms()
        {
            for (int i = 0; i < Iterations; i++)
            {
                s_method.Invoke(s_MyClass, null);
            }
        }

        [Benchmark(OperationsPerInvoke = Iterations)]
        // Include the array allocation and population for a typical scenario.
        public void StaticMethod4_arrayNotCached_int_string_struct_class()
        {
            for (int i = 0; i < Iterations; i++)
            {
                object[] args = new object[] { 42, "Hello", default(MyBlittableStruct), s_MyClass };
                s_method_int_string_struct_class.Invoke(null, args);
            }
        }

        [Benchmark(OperationsPerInvoke = Iterations)]
        // Include the array allocation and population for a typical scenario.
        // Starting with 5 parameters, stack allocations are replaced with heap allocations.
        public void StaticMethod5_arrayNotCached_int_string_struct_class_bool()
        {
            for (int i = 0; i < Iterations; i++)
            {
                object[] args = new object[] { 42, "Hello", default(MyBlittableStruct), s_MyClass, true };
                s_method_int_string_struct_class_bool.Invoke(null, args);
            }
        }

        [Benchmark(OperationsPerInvoke = Iterations)]
        public void StaticMethod4_int_string_struct_class()
        {
            for (int i = 0; i < Iterations; i++)
            {
                s_method_int_string_struct_class.Invoke(null, s_args4);
            }
        }

        [Benchmark(OperationsPerInvoke = Iterations)]
        public void StaticMethod4_ByRefParams_int_string_struct_class()
        {
            for (int i = 0; i < Iterations; i++)
            {
                s_method_byref_int_string_struct_class.Invoke(null, s_args4);
            }
        }

        [Benchmark(OperationsPerInvoke = Iterations)]
        // Starting with 5 parameters, stack allocations are replaced with heap allocations.
        public void StaticMethod5_ByRefParams_int_string_struct_class_bool()
        {
            for (int i = 0; i < Iterations; i++)
            {
                s_method_byref_int_string_struct_class_bool.Invoke(null, s_args5);
            }
        }

        [Benchmark(OperationsPerInvoke = Iterations)]
        public void Ctor0_NoParams()
        {
            for (int i = 0; i < Iterations; i++)
            {
                s_ctor_NoParams.Invoke(null);
            }
        }

        [Benchmark(OperationsPerInvoke = Iterations)]
        public void Ctor0_ActivatorCreateInstance_NoParams()
        {
            for (int i = 0; i < Iterations; i++)
            {
                Activator.CreateInstance(typeof(MyClass));
            }
        }

        [Benchmark(OperationsPerInvoke = Iterations)]
        public void Ctor4_int_string_struct_class()
        {
            for (int i = 0; i < Iterations; i++)
            {
                s_ctor_int_string_struct_class.Invoke(s_args4);
            }
        }

        [Benchmark(OperationsPerInvoke = Iterations)]
        public void Ctor4_ActivatorCreateInstance()
        {
            for (int i = 0; i < Iterations; i++)
            {
                Activator.CreateInstance(typeof(MyClass), s_args4);
            }
        }

        [Benchmark(OperationsPerInvoke = Iterations)]
        public void Property_Get_int()
        {
            for (int i = 0; i < Iterations; i++)
            {
                s_int = (int)s_property_int.GetValue(s_MyClass);
            }
        }

        [Benchmark(OperationsPerInvoke = Iterations)]
        public void Property_Get_class()
        {
            for (int i = 0; i < Iterations; i++)
            {
                s_class = s_property_class.GetValue(s_MyClass);
            }
        }

        [Benchmark(OperationsPerInvoke = Iterations)]
        public void Property_Set_int()
        {
            for (int i = 0; i < 1000; i++)
            {
                s_property_int.SetValue(s_MyClass, 42);
            }
        }

        [Benchmark(OperationsPerInvoke = Iterations)]
        public void Property_Set_class()
        {
            for (int i = 0; i < Iterations; i++)
            {
                s_property_class.SetValue(s_MyClass, null);
            }
        }

        [Benchmark(OperationsPerInvoke = Iterations)]
        public void Field_Get_int()
        {
            for (int i = 0; i < Iterations; i++)
            {
                s_int = (int)s_field_int.GetValue(s_MyClass);
            }
        }

        [Benchmark(OperationsPerInvoke = Iterations)]
        public void Field_GetStatic_int()
        {
            for (int i = 0; i < Iterations; i++)
            {
                s_int = (int)s_staticField_int.GetValue(null);
            }
        }

        [Benchmark(OperationsPerInvoke = Iterations)]
        public void Field_Get_class()
        {
            for (int i = 0; i < Iterations; i++)
            {
                s_class = s_field_class.GetValue(s_MyClass);
            }
        }


        [Benchmark(OperationsPerInvoke = Iterations)]
        public void Field_GetStatic_class()
        {
            for (int i = 0; i < Iterations; i++)
            {
                s_class = s_staticField_class.GetValue(null);
            }
        }

        [Benchmark(OperationsPerInvoke = Iterations)]
        public void Field_Get_struct()
        {
            for (int i = 0; i < Iterations; i++)
            {
                s_struct = (MyBlittableStruct)s_field_struct.GetValue(s_MyClass);
            }
        }

        [Benchmark(OperationsPerInvoke = Iterations)]
        public void Field_GetStatic_struct()
        {
            for (int i = 0; i < Iterations; i++)
            {
                s_struct = (MyBlittableStruct)s_staticField_struct.GetValue(null);
            }
        }

        [Benchmark(OperationsPerInvoke = Iterations)]
        public void Field_Set_int()
        {
            for (int i = 0; i < Iterations; i++)
            {
                s_field_int.SetValue(s_MyClass, 42);
            }
        }

        [Benchmark(OperationsPerInvoke = Iterations)]
        public void Field_SetStatic_int()
        {
            for (int i = 0; i < Iterations; i++)
            {
                s_staticField_int.SetValue(null, 42);
            }
        }

        [Benchmark(OperationsPerInvoke = Iterations)]
        public void Field_Set_class()
        {
            for (int i = 0; i < Iterations; i++)
            {
                s_field_class.SetValue(s_MyClass, 42);
            }
        }

        [Benchmark(OperationsPerInvoke = Iterations)]
        public void Field_SetStatic_class()
        {
            for (int i = 0; i < Iterations; i++)
            {
                s_staticField_class.SetValue(null, 42);
            }
        }

        [Benchmark(OperationsPerInvoke = Iterations)]
        public void Field_Set_struct()
        {
            for (int i = 0; i < Iterations; i++)
            {
                s_field_struct.SetValue(s_MyClass, default(MyBlittableStruct));
            }
        }

        [Benchmark(OperationsPerInvoke = Iterations)]
        public void Field_SetStatic_struct()
        {
            for (int i = 0; i < Iterations; i++)
            {
                s_staticField_struct.SetValue(null, default(MyBlittableStruct));
            }
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
            public static int s_i = 0;
            public bool b = false;
            public string s = null;
            public object o = null;
            public static object s_o = null;
            public MyBlittableStruct blittableStruct = default;
            public static MyBlittableStruct s_blittableStruct = default;

            public int I { get; set; } = 0;
            public object O { get; set; } = null;
        }
    }
}
