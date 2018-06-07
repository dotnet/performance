// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using Benchmarks;

namespace PerfLabTests
{
    [BenchmarkCategory(Categories.CoreCLR, Categories.Perflab)]
    public class LowLevelPerf
    {
        Class aClassFiled;
        LongHierarchyChildClass aLongHierarchyChildClassField;
        SealedClass aSealedClassField;
        List<int> iListField;
        StructWithInterface aStructWithInterfaceField;
        AnInterface aInterfaceField;
        AnInterface aInterfaceField1;

        [Benchmark(OperationsPerInvoke = 10)]
        public void EmptyStaticFunction()
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

        [Benchmark(OperationsPerInvoke = 10)]
        public void EmptyStaticFunction5Arg()
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

        [GlobalSetup(Target = nameof(EmptyInstanceFunction))]
        public void SetupEmptyInstanceFunction() => aClassFiled = new Class();

        [Benchmark(OperationsPerInvoke = 10)]
        public void EmptyInstanceFunction()
        {
            Class aClass = aClassFiled;

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

        [GlobalSetup(Target = nameof(InterfaceInterfaceMethod))]
        public void SetupInterfaceInterfaceMethod() => aClassFiled = new Class();
        
        [Benchmark(OperationsPerInvoke = 10)]
        public void InterfaceInterfaceMethod()
        {
            AnInterface aInterface = aClassFiled;
            
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

        [Benchmark(OperationsPerInvoke = 10)]
        public void InterfaceInterfaceMethodLongHierarchy()
        {
            var aInterface = aLongHierarchyChildClassField;

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

        [GlobalSetup(Target = nameof(InterfaceInterfaceMethodSwitchCallType))]
        public void SetupInterfaceInterfaceMethodSwitchCallType()
        {
            aInterfaceField = new LongHierarchyChildClass();
            aInterfaceField1 = new LongHierarchyClass1();
        }
        
        [Benchmark(OperationsPerInvoke = 10)]
        public void InterfaceInterfaceMethodSwitchCallType()
        {
            AnInterface aInterface = aInterfaceField;
            AnInterface aInterface1 = aInterfaceField1;
            
            CallInterfaceMethod(aInterface); CallInterfaceMethod(aInterface1);
            CallInterfaceMethod(aInterface); CallInterfaceMethod(aInterface1);
            CallInterfaceMethod(aInterface); CallInterfaceMethod(aInterface1);
            CallInterfaceMethod(aInterface); CallInterfaceMethod(aInterface1);
            CallInterfaceMethod(aInterface); CallInterfaceMethod(aInterface1);
            CallInterfaceMethod(aInterface); CallInterfaceMethod(aInterface1);
            CallInterfaceMethod(aInterface); CallInterfaceMethod(aInterface1);
            CallInterfaceMethod(aInterface); CallInterfaceMethod(aInterface1);
            CallInterfaceMethod(aInterface); CallInterfaceMethod(aInterface1);
            CallInterfaceMethod(aInterface); CallInterfaceMethod(aInterface1);
        }
        
        [GlobalSetup(Target = nameof(ClassVirtualMethod))]
        public void SetupClassVirtualMethod() => aClassFiled = new Class();

        [Benchmark(OperationsPerInvoke = 10)]
        public int ClassVirtualMethod()
        {
            SuperClass aClass = aClassFiled;

            int x = 0;
            
            x = aClass.VirtualMethod();
            x = aClass.VirtualMethod();
            x = aClass.VirtualMethod();
            x = aClass.VirtualMethod();
            x = aClass.VirtualMethod();
            x = aClass.VirtualMethod();
            x = aClass.VirtualMethod();
            x = aClass.VirtualMethod();
            x = aClass.VirtualMethod();
            x = aClass.VirtualMethod();
            
            return x;
        }
        
        [GlobalSetup(Target = nameof(SealedClassInterfaceMethod))]
        public void SetupSealedClassInterfaceMethod() => aSealedClassField = new SealedClass();

        [Benchmark(OperationsPerInvoke = 10)]
        public int SealedClassInterfaceMethod()
        {
            SealedClass aSealedClass = aSealedClassField;
            
            int x = 0;
            
            x = aSealedClass.InterfaceMethod();
            x = aSealedClass.InterfaceMethod();
            x = aSealedClass.InterfaceMethod();
            x = aSealedClass.InterfaceMethod();
            x = aSealedClass.InterfaceMethod();
            x = aSealedClass.InterfaceMethod();
            x = aSealedClass.InterfaceMethod();
            x = aSealedClass.InterfaceMethod();
            x = aSealedClass.InterfaceMethod();
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
            
            x = aStructWithInterface.InterfaceMethod();
            x = aStructWithInterface.InterfaceMethod();
            x = aStructWithInterface.InterfaceMethod();
            x = aStructWithInterface.InterfaceMethod();
            x = aStructWithInterface.InterfaceMethod();
            x = aStructWithInterface.InterfaceMethod();
            x = aStructWithInterface.InterfaceMethod();
            x = aStructWithInterface.InterfaceMethod();
            x = aStructWithInterface.InterfaceMethod();
            x = aStructWithInterface.InterfaceMethod();
            
            return x;
        }
        
        [GlobalSetup(Target = nameof(StaticIntPlus))]
        public void SetupStaticIntPlus() => aClassFiled = new Class(); // it's goint to call static ctor

        [Benchmark]
        public void StaticIntPlus() => Class.aStaticInt += 1;

        [Benchmark]
        [Arguments("aString1")]
        public bool ObjectStringIsString(object aObjectString) => aObjectString is String;

        [GlobalSetup(Target = nameof(NewDelegateClassEmptyInstanceFn))]
        public void SetupNewDelegateClassEmptyInstanceFn() => aClassFiled = new Class();
        
        [Benchmark]
        public MyDelegate NewDelegateClassEmptyInstanceFn() => new MyDelegate(aClassFiled.EmptyInstanceFunction);

        [Benchmark]
        public MyDelegate NewDelegateClassEmptyStaticFn() => new MyDelegate(Class.EmptyStaticFunction);

        public IEnumerable<object> GetInstanceDelegateArguments()
        {
            Class aClass = new Class();
            MyDelegate aInstanceDelegate = new MyDelegate(aClass.EmptyInstanceFunction);

            yield return aInstanceDelegate;
        }

        [Benchmark]
        [ArgumentsSource(nameof(GetInstanceDelegateArguments))]
        public int InstanceDelegate(MyDelegate aInstanceDelegate) => aInstanceDelegate();
        
        public IEnumerable<object> GetStaticDelegateArguments()
        {
            yield return new MyDelegate(Class.EmptyStaticFunction);
        }
        
        [Benchmark]
        [ArgumentsSource(nameof(GetStaticDelegateArguments))]
        public void StaticDelegate(MyDelegate aStaticDelegate) => aStaticDelegate();

        public IEnumerable<object> GetMeasureEventsArguments()
        {
            Class aClass = new Class();
            aClass.AnEvent += new MyDelegate(aClass.EmptyInstanceFunction);

            yield return aClass;
        }

        [Benchmark]
        [ArgumentsSource(nameof(GetMeasureEventsArguments))]
        public void MeasureEvents(Class aClass) => aClass.MeasureFire100();

        public IEnumerable<object> GetGenericClassWithIntGenericInstanceFieldArguments()
        {
            yield return new GenericClass<int>();
        }
        
        [Benchmark]
        [ArgumentsSource(nameof(GetGenericClassWithIntGenericInstanceFieldArguments))]
        public void GenericClassWithIntGenericInstanceField(GenericClass<int> aGenericClassWithInt) 
            => aGenericClassWithInt.aGenericInstanceFieldT = 1;

        [Benchmark]
        public void GenericClassGenericStaticField() => GenericClass<int>.aGenericStaticFieldT = 1;

        public IEnumerable<object> GetGenericClassGenericInstanceMethodArguments()
        {
            yield return new GenericClass<int>();
        }

        [Benchmark]
        [ArgumentsSource(nameof(GetGenericClassGenericInstanceMethodArguments))]
        public int GenericClassGenericInstanceMethod(GenericClass<int> aGenericClassWithInt)
            => aGenericClassWithInt.ClassGenericInstanceMethod();

        [Benchmark]
        public int GenericClassGenericStaticMethod() => GenericClass<int>.ClassGenericStaticMethod();

        [Benchmark]
        public int GenericGenericMethod() => Class.GenericMethod<int>();

        public IEnumerable<object[]> GetGenericClassWithSTringGenericInstanceMethodArguments()
        {
            GenericClass<string> aGenericClassWithString = new GenericClass<string>();
            string aString = "foo";
            
            yield return new object[2] { aGenericClassWithString, aString };
        }

        [Benchmark]
        [ArgumentsSource(nameof(GetGenericClassWithSTringGenericInstanceMethodArguments))]
        public void GenericClassWithSTringGenericInstanceMethod(GenericClass<string> aGenericClassWithString, string aString)
            => aGenericClassWithString.aGenericInstanceFieldT = aString;

        [GlobalSetup(Target = nameof(ForeachOverList100Elements))]
        public void SetupForeachOverList100Elements() => iListField = Enumerable.Range(0, 100).ToList();
        
        [Benchmark]
        public int ForeachOverList100Elements()
        {
            List<int> iList = iListField;

            int iResult = 0;
            
            foreach (int j in iList)
                iResult = j;

            return iResult;
        }

        [Benchmark]
        [Arguments("aString")]
        public Type TypeReflectionObjectGetType(object anObject) => anObject.GetType();

        [Benchmark]
        [Arguments(new object[1] { new string[0] })] // arguments accept "params object[]", when we pass just a string[] it's recognized as an array of params
        public Type TypeReflectionArrayGetType(object anArray) => anArray.GetType();

        [Benchmark]
        [Arguments(Int32.MaxValue)]
        public string IntegerFormatting(int number) => number.ToString();
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
