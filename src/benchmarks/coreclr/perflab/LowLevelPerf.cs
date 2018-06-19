// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using BenchmarkDotNet.Attributes;
using Benchmarks;

namespace PerfLabTests
{
    [BenchmarkCategory(Categories.CoreCLR, Categories.Perflab)]
    public class LowLevelPerf
    {
        public static int InnerIterationCount = 100000; // do not change the value and keep it public static NOT-readonly, ported "as is" from CoreCLR repo
        
    // following fields are static public on purpose! to make sure JIT does not optimize the benchmarks to constants, do NOT change it
        public static Class aClassFiled;
        public static LongHierarchyChildClass aLongHierarchyChildClassField;
        public static SealedClass aSealedClassField;
        public static List<int> iListField;
        public static StructWithInterface aStructWithInterfaceField;
        public static AnInterface aInterfaceField;
        public static AnInterface aInterfaceField1;
        public static string stringField;
        public static MyDelegate aInstanceDelegateField;
        public static MyDelegate aStaticDelegateField;
        public static GenericClass<int> aGenericClassWithIntField;
        public static GenericClass<string> aGenericClassWithStringField;
        public static object aObjectStringField;
        public static object aObjectArrayOfStringField;
        
        [Benchmark]
        public void EmptyStaticFunction()
        {
            for (int i = 0; i < InnerIterationCount; i++)
            {
                Class.EmptyStaticFunction();
                Class.EmptyStaticFunction();
                Class.EmptyStaticFunction();
                Class.EmptyStaticFunction();
                Class.EmptyStaticFunction();
                Class.EmptyStaticFunction();
                Class.EmptyStaticFunction();
                Class.EmptyStaticFunction();
                Class.EmptyStaticFunction();
                Class.EmptyStaticFunction();
            }
        }

        [Benchmark]
        public void EmptyStaticFunction5Arg()
        {
            for (int i = 0; i < InnerIterationCount; i++)
            {
                Class.EmptyStaticFunction5Arg(1, 2, 3, 4, 5);
                Class.EmptyStaticFunction5Arg(1, 2, 3, 4, 5);
                Class.EmptyStaticFunction5Arg(1, 2, 3, 4, 5);
                Class.EmptyStaticFunction5Arg(1, 2, 3, 4, 5);
                Class.EmptyStaticFunction5Arg(1, 2, 3, 4, 5);
                Class.EmptyStaticFunction5Arg(1, 2, 3, 4, 5);
                Class.EmptyStaticFunction5Arg(1, 2, 3, 4, 5);
                Class.EmptyStaticFunction5Arg(1, 2, 3, 4, 5);
                Class.EmptyStaticFunction5Arg(1, 2, 3, 4, 5);
                Class.EmptyStaticFunction5Arg(1, 2, 3, 4, 5);
            }
        }

        [GlobalSetup(Target = nameof(EmptyInstanceFunction))]
        public void SetupEmptyInstanceFunction() => aClassFiled = new Class();

        [Benchmark]
        public void EmptyInstanceFunction()
        {
            Class aClass = aClassFiled;

            for (int i = 0; i < InnerIterationCount; i++)
            {
                aClass.EmptyInstanceFunction();
                aClass.EmptyInstanceFunction();
                aClass.EmptyInstanceFunction();
                aClass.EmptyInstanceFunction();
                aClass.EmptyInstanceFunction();
                aClass.EmptyInstanceFunction();
                aClass.EmptyInstanceFunction();
                aClass.EmptyInstanceFunction();
                aClass.EmptyInstanceFunction();
                aClass.EmptyInstanceFunction();
            }
        }

        [GlobalSetup(Target = nameof(InterfaceInterfaceMethod))]
        public void SetupInterfaceInterfaceMethod() => aClassFiled = new Class();
        
        [Benchmark]
        public void InterfaceInterfaceMethod()
        {
            AnInterface aInterface = aClassFiled;
            
            for (int i = 0; i < InnerIterationCount; i++)
            {
                CallInterfaceMethod(aInterface);
                CallInterfaceMethod(aInterface);
                CallInterfaceMethod(aInterface);
                CallInterfaceMethod(aInterface);
                CallInterfaceMethod(aInterface);
                CallInterfaceMethod(aInterface);
                CallInterfaceMethod(aInterface);
                CallInterfaceMethod(aInterface);
                CallInterfaceMethod(aInterface);
                CallInterfaceMethod(aInterface);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void CallInterfaceMethod(AnInterface aInterface)
        {
            aInterface.InterfaceMethod();
        }

        [GlobalSetup(Target = nameof(InterfaceInterfaceMethodLongHierarchy))]
        public void SetupInterfaceInterfaceMethodLongHierarchy()
        {
            aLongHierarchyChildClassField = new LongHierarchyChildClass();

            //generate all the not-used call site first
            CallInterfaceMethod(new LongHierarchyClass1());
            CallInterfaceMethod(new LongHierarchyClass2());
            CallInterfaceMethod(new LongHierarchyClass3());
            CallInterfaceMethod(new LongHierarchyClass4());
            CallInterfaceMethod(new LongHierarchyClass5());
            CallInterfaceMethod(new LongHierarchyClass6());
            CallInterfaceMethod(new LongHierarchyClass7());
            CallInterfaceMethod(new LongHierarchyClass8());
            CallInterfaceMethod(new LongHierarchyClass9());
            CallInterfaceMethod(new LongHierarchyClass11());
            CallInterfaceMethod(new LongHierarchyClass12());
        }

        [Benchmark]
        public void InterfaceInterfaceMethodLongHierarchy()
        {
            var aInterface = aLongHierarchyChildClassField;

            for (int i = 0; i < InnerIterationCount; i++)
                CallInterfaceMethod(aInterface);
        }

        [GlobalSetup(Target = nameof(InterfaceInterfaceMethodSwitchCallType))]
        public void SetupInterfaceInterfaceMethodSwitchCallType()
        {
            aInterfaceField = new LongHierarchyChildClass();
            aInterfaceField1 = new LongHierarchyClass1();
        }
        
        [Benchmark]
        public void InterfaceInterfaceMethodSwitchCallType()
        {
            AnInterface aInterface = aInterfaceField;
            AnInterface aInterface1 = aInterfaceField1;
            
            for (int i = 0; i < InnerIterationCount; i++)
            {
                CallInterfaceMethod(aInterface);
                CallInterfaceMethod(aInterface1);
            }
        }
        
        [GlobalSetup(Target = nameof(ClassVirtualMethod))]
        public void SetupClassVirtualMethod() => aClassFiled = new Class();

        [Benchmark]
        public int ClassVirtualMethod()
        {
            SuperClass aClass = aClassFiled;

            int x = 0;

            for (int i = 0; i < InnerIterationCount; i++)
                x = aClass.VirtualMethod();
            
            return x;
        }
        
        [GlobalSetup(Target = nameof(SealedClassInterfaceMethod))]
        public void SetupSealedClassInterfaceMethod() => aSealedClassField = new SealedClass();

        [Benchmark]
        public int SealedClassInterfaceMethod()
        {
            SealedClass aSealedClass = aSealedClassField;
            
            int x = 0;
            
            for (int i = 0; i < InnerIterationCount; i++)
                x = aSealedClass.InterfaceMethod();
            
            return x;
        }
        
        [GlobalSetup(Target = nameof(StructWithInterfaceInterfaceMethod))]
        public void SetupStructWithInterfaceInterfaceMethod() => aStructWithInterfaceField = new StructWithInterface();

        [Benchmark]
        public int StructWithInterfaceInterfaceMethod()
        {
            StructWithInterface aStructWithInterface = aStructWithInterfaceField;

            int x = 0;
            
            for (int i = 0; i < InnerIterationCount; i++)
                x = aStructWithInterface.InterfaceMethod();
            
            return x;
        }
        
        [GlobalSetup(Target = nameof(StaticIntPlus))]
        public void SetupStaticIntPlus() => aClassFiled = new Class(); // it's goint to call static ctor

        [Benchmark]
        public void StaticIntPlus()
        {
            for (int i = 0; i < InnerIterationCount; i++)
                Class.aStaticInt += 1; // TODO: reconsider the design, the cost of inner loop iteration is as big as the benchmarked thing
        }
        
        [GlobalSetup(Target = nameof(ObjectStringIsString))]
        public void SetupObjectStringIsString() => aObjectStringField = "aString1";

        [Benchmark]
        public bool ObjectStringIsString()
        {
            bool b = false;

            for (int i = 0; i < InnerIterationCount; i++)
                b = aObjectStringField is String;

            return b;
        }

        [GlobalSetup(Target = nameof(NewDelegateClassEmptyInstanceFn))]
        public void SetupNewDelegateClassEmptyInstanceFn() => aClassFiled = new Class();

        [Benchmark]
        public void NewDelegateClassEmptyInstanceFn()
        {
            Class aClass = aClassFiled;
            MyDelegate aMyDelegate;

            for (int i = 0; i < InnerIterationCount; i++)
                aMyDelegate = new MyDelegate(aClass.EmptyInstanceFunction);
        }

        [Benchmark]
        public void NewDelegateClassEmptyStaticFn()
        {
            MyDelegate aMyDelegate;

            for (int i = 0; i < InnerIterationCount; i++)
                aMyDelegate = new MyDelegate(Class.EmptyStaticFunction);
        }
        
        [GlobalSetup(Target = nameof(InstanceDelegate))]
        public void SetupInstanceDelegate()
        {
            Class aClass = new Class();
            aInstanceDelegateField = new MyDelegate(aClass.EmptyInstanceFunction);
        }

        [Benchmark]
        public void InstanceDelegate()
        {
            MyDelegate aInstanceDelegate = aInstanceDelegateField;

            for (int i = 0; i < InnerIterationCount; i++)
                aInstanceDelegate();
        }
        
        [GlobalSetup(Target = nameof(StaticDelegate))]
        public void SetupStaticDelegate() => aStaticDelegateField = new MyDelegate(Class.EmptyStaticFunction);

        [Benchmark]
        public void StaticDelegate()
        {
            MyDelegate aStaticDelegate = aStaticDelegateField;
            
            for (int i = 0; i < InnerIterationCount; i++)
                aStaticDelegate();
        }

        [GlobalSetup(Target = nameof(MeasureEvents))]
        public void SetupMeasureEvents()
        {
            Class aClass = new Class();
            aClass.AnEvent += new MyDelegate(aClass.EmptyInstanceFunction);

            aClassFiled = aClass;
        }

        [Benchmark]
        public void MeasureEvents()
        {
            Class aClass = aClassFiled;
            
            for (int i = 0; i < InnerIterationCount; i++)
                aClass.MeasureFire100();
        }

        [GlobalSetup(Target = nameof(GenericClassWithIntGenericInstanceField))]
        public void SetupGenericClassWithIntGenericInstanceField() => aGenericClassWithIntField = new GenericClass<int>();

        [Benchmark]
        public void GenericClassWithIntGenericInstanceField()
        {
            for (int i = 0; i < InnerIterationCount; i++)
                aGenericClassWithIntField.aGenericInstanceFieldT = 1;
        }

        [Benchmark]
        public void GenericClassGenericStaticField()
        {
            for (int i = 0; i < InnerIterationCount; i++)
                GenericClass<int>.aGenericStaticFieldT = 1;
        }

        [GlobalSetup(Target = nameof(GenericClassGenericInstanceMethod))]
        public void SetupGenericClassGenericInstanceMethod() => aGenericClassWithIntField = new GenericClass<int>();

        [Benchmark]
        public int GenericClassGenericInstanceMethod()
        {
            GenericClass<int> aGenericClassWithInt = aGenericClassWithIntField;

            int x = 0;

            for (int i = 0; i < InnerIterationCount; i++)
                x = aGenericClassWithInt.ClassGenericInstanceMethod();

            return x;
        }

        [Benchmark]
        public int GenericClassGenericStaticMethod()
        {
            int x = 0;

            for (int i = 0; i < InnerIterationCount; i++)
                x = GenericClass<int>.ClassGenericStaticMethod();

            return x;
        }

        [Benchmark]
        public int GenericGenericMethod()
        {
            int x = 0;

            for (int i = 0; i < InnerIterationCount; i++)
                x = Class.GenericMethod<int>();

            return x;
        }

        [GlobalSetup(Target = nameof(GenericClassWithSTringGenericInstanceMethod))]
        public void SetupGenericClassWithSTringGenericInstanceMethod()
        {
            aGenericClassWithStringField = new GenericClass<string>();
            stringField = "foo";
        }

        [Benchmark]
        public void GenericClassWithSTringGenericInstanceMethod()
        {
            GenericClass<string> aGenericClassWithString = aGenericClassWithStringField;
            string aString = stringField;

            for (int i = 0; i < InnerIterationCount; i++)
                aGenericClassWithString.aGenericInstanceFieldT = aString;
        }

        [GlobalSetup(Target = nameof(ForeachOverList100Elements))]
        public void SetupForeachOverList100Elements() => iListField = Enumerable.Range(0, 100).ToList();
        
        [Benchmark]
        public int ForeachOverList100Elements()
        {
            List<int> iList = iListField;

            int iResult = 0;

            for (int i = 0; i < InnerIterationCount; i++)
                foreach (int j in iList)
                    iResult = j;

            return iResult;
        }
        
        [GlobalSetup(Target = nameof(TypeReflectionObjectGetType))]
        public void SetupTypeReflectionObjectGetType() => stringField = "aString";

        [Benchmark]
        public Type TypeReflectionObjectGetType()
        {
            Type type = null;

            for (int i = 0; i < InnerIterationCount; i++)
                type = stringField.GetType();
            
            return type;
        }

        [GlobalSetup(Target = nameof(TypeReflectionArrayGetType))]
        public void Setup() => aObjectArrayOfStringField = new string[0];

        [Benchmark]
        public Type TypeReflectionArrayGetType()
        {
            Type type = null;

            for (int i = 0; i < InnerIterationCount; i++)
                type = aObjectArrayOfStringField.GetType();

            return type;
        }

        [Benchmark]
        public string IntegerFormatting()
        {
            int number = Int32.MaxValue;
            string result = null;

            for (int i = 0; i < InnerIterationCount; i++)
                result = number.ToString();

            return result;
        }
    }

    #region Support Classes
    // classes and method needed to perform the experiments. 

    public interface AnInterface
    {
        int InterfaceMethod();
    }

    public class SuperClass : AnInterface
    {
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        public virtual int InterfaceMethod() { return 2; }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        public virtual int VirtualMethod()
        {
            return 1;
        }
    }

    public struct ValueType
    {
        public int x;
        public int y;
        public int z;
    }

    public delegate int MyDelegate();

    public struct StructWithInterface : AnInterface
    {
        public int x;
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        public int InterfaceMethod()
        {
            return x++;
        }
    }

    public sealed class SealedClass : SuperClass
    {
        public int aInstanceInt;
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        public override int VirtualMethod()
        {
            return aInstanceInt++;
        }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        public override int InterfaceMethod()
        {
            return aInstanceInt++;
        }
    }

    /// <summary>
    /// A example class.  It inherits, overrides, has intefaces etc.  
    /// It excercises most of the common runtime features 
    /// </summary>
    public class Class : SuperClass
    {
        public event MyDelegate AnEvent;

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        public override int VirtualMethod() { return aInstanceInt++; }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        public override int InterfaceMethod() { return aInstanceInt++; }

        public int aInstanceInt;
        public string aInstanceString;

        public static int aStaticInt;
        public static string aStaticString = "Hello";
        public static ValueType aStaticValueType;

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static int EmptyStaticFunction()
        {
            return aStaticInt++;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static int EmptyStaticFunction5Arg(int arg1, int arg2, int arg3, int arg4, int arg5)
        {
            return aStaticInt++;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public int EmptyInstanceFunction()
        {
            return aInstanceInt++;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static int GenericMethod<T>()
        {
            return aStaticInt++;
        }

        public void MeasureFire100()
        {
            #region callAnEvent
            AnEvent();
            AnEvent();
            AnEvent();
            AnEvent();
            AnEvent();
            AnEvent();
            AnEvent();
            AnEvent();
            AnEvent();
            AnEvent();
            AnEvent();
            AnEvent();
            AnEvent();
            AnEvent();
            AnEvent();
            AnEvent();
            AnEvent();
            AnEvent();
            AnEvent();
            AnEvent();
            AnEvent();
            AnEvent();
            AnEvent();
            AnEvent();
            AnEvent();
            AnEvent();
            AnEvent();
            AnEvent();
            AnEvent();
            AnEvent();
            AnEvent();
            AnEvent();
            AnEvent();
            AnEvent();
            AnEvent();
            AnEvent();
            AnEvent();
            AnEvent();
            AnEvent();
            AnEvent();
            AnEvent();
            AnEvent();
            AnEvent();
            AnEvent();
            AnEvent();
            AnEvent();
            AnEvent();
            AnEvent();
            AnEvent();
            AnEvent();
            AnEvent();
            AnEvent();
            AnEvent();
            AnEvent();
            AnEvent();
            AnEvent();
            AnEvent();
            AnEvent();
            AnEvent();
            AnEvent();
            AnEvent();
            AnEvent();
            AnEvent();
            AnEvent();
            AnEvent();
            AnEvent();
            AnEvent();
            AnEvent();
            AnEvent();
            AnEvent();
            AnEvent();
            AnEvent();
            AnEvent();
            AnEvent();
            AnEvent();
            AnEvent();
            AnEvent();
            AnEvent();
            AnEvent();
            AnEvent();
            AnEvent();
            AnEvent();
            AnEvent();
            AnEvent();
            AnEvent();
            AnEvent();
            AnEvent();
            AnEvent();
            AnEvent();
            AnEvent();
            AnEvent();
            AnEvent();
            AnEvent();
            AnEvent();
            AnEvent();
            AnEvent();
            AnEvent();
            AnEvent();
            AnEvent();
            AnEvent();
            //});
            #endregion
        }
    }

    public class GenericClass<T>
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        public T ClassGenericInstanceMethod()
        {
            tmp++; // need this to not be optimized away
            return aGenericInstanceFieldT;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static T ClassGenericStaticMethod()
        {
            sTmp++; // need this to not be optimized away
            return aGenericStaticFieldT;
        }

        public static int sTmp;
        public int tmp;
        public T aGenericInstanceFieldT;
        public static T aGenericStaticFieldT;
    }

    #region LongHierarchyClass
    public class LongHierarchyClass1 : AnInterface
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        public virtual int InterfaceMethod() { return 2; }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public virtual int VirtualMethod()
        {
            return 1;
        }
    }

    public class LongHierarchyClass2 : LongHierarchyClass1
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        public override int InterfaceMethod() { return 2; }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public override int VirtualMethod()
        {
            return 1;
        }
    }

    public class LongHierarchyClass3 : LongHierarchyClass2
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        public override int InterfaceMethod() { return 2; }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public override int VirtualMethod()
        {
            return 1;
        }
    }

    public class LongHierarchyClass4 : LongHierarchyClass3
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        public override int InterfaceMethod() { return 2; }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public override int VirtualMethod()
        {
            return 1;
        }
    }

    public class LongHierarchyClass5 : LongHierarchyClass4
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        public override int InterfaceMethod() { return 2; }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public override int VirtualMethod()
        {
            return 1;
        }
    }

    public class LongHierarchyClass6 : LongHierarchyClass5
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        public override int InterfaceMethod() { return 2; }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public override int VirtualMethod()
        {
            return 1;
        }
    }

    public class LongHierarchyClass7 : LongHierarchyClass6
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        public override int InterfaceMethod() { return 2; }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public override int VirtualMethod()
        {
            return 1;
        }
    }

    public class LongHierarchyClass8 : LongHierarchyClass7
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        public override int InterfaceMethod() { return 2; }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public override int VirtualMethod()
        {
            return 1;
        }
    }

    public class LongHierarchyClass9 : LongHierarchyClass8
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        public override int InterfaceMethod() { return 2; }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public override int VirtualMethod()
        {
            return 1;
        }
    }

    public class LongHierarchyClass10 : LongHierarchyClass9
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        public override int InterfaceMethod() { return 2; }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public override int VirtualMethod()
        {
            return 1;
        }
    }

    public class LongHierarchyClass11 : LongHierarchyClass10
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        public override int InterfaceMethod() { return 2; }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public override int VirtualMethod()
        {
            return 1;
        }
    }

    public class LongHierarchyClass12 : LongHierarchyClass11
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        public override int InterfaceMethod() { return 2; }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public override int VirtualMethod()
        {
            return 1;
        }
    }

    public class LongHierarchyChildClass : LongHierarchyClass12
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        public override int InterfaceMethod() { return 2; }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public override int VirtualMethod()
        {
            return 1;
        }
    }

    #endregion

    #endregion

}
