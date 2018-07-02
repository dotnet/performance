// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
#define PERFORMANCE_TESTS

namespace System.ComponentModel.Tests
{
    public struct SomeValueType
    {
        public int a;
    }

    public enum SomeEnum
    {
        Add,
        Sub,
        Mul
    }

    [TypeConverter("System.ComponentModel.Tests.BaseClassConverter, Benchmarks, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null")]
    public class BaseClass
    {
        public BaseClass()
        {
            BaseProperty = 1;
        }
        public override bool Equals(object other)
        {
            BaseClass otherBaseClass = other as BaseClass;
            if (otherBaseClass == null)
            {
                return false;
            }
            if (otherBaseClass.BaseProperty == BaseProperty)
            {
                return true;
            }
            return base.Equals(other);
        }
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
        public int BaseProperty;
    }

    public class BaseClassConverter : TypeConverter
    {
        public BaseClassConverter(string someString) { throw new InvalidOperationException("This constructor should not be invoked by TypeDescriptor.GetConverter."); }
        public BaseClassConverter() { }
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            if (sourceType == typeof(int))
            {
                return true;
            }
            return base.CanConvertFrom(context, sourceType);
        }
        public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
        {
            if (destinationType == typeof(int))
            {
                return true;
            }
            return base.CanConvertTo(context, destinationType);
        }
        public override object ConvertFrom(ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value)
        {
            if (value is int)
            {
                BaseClass baseClass = new BaseClass();
                baseClass.BaseProperty = (int)value;
                return baseClass;
            }
            return base.ConvertFrom(context, culture, value);
        }
        public override object ConvertTo(ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value, Type destinationType)
        {
            if (destinationType == typeof(int))
            {
                BaseClass baseClass = value as BaseClass;
                if (baseClass != null)
                {
                    return baseClass.BaseProperty;
                }
            }
            return base.ConvertTo(context, culture, value, destinationType);
        }
    }

    [TypeConverter("System.ComponentModel.Tests.DerivedClassConverter")]
    public class DerivedClass : BaseClass
    {
        public DerivedClass()
            : base()
        {
            DerivedProperty = 2;
        }
        public DerivedClass(int i)
            : base()
        {
            DerivedProperty = i;
        }
        public override bool Equals(object other)
        {
            DerivedClass otherDerivedClass = other as DerivedClass;
            if (otherDerivedClass == null)
            {
                return false;
            }
            if (otherDerivedClass.DerivedProperty != DerivedProperty)
            {
                return false;
            }
            return base.Equals(other);
        }
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
        public int DerivedProperty;
    }

    public class DerivedClassConverter : TypeConverter
    {
        public DerivedClassConverter() { }

        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            if (sourceType == typeof(int))
            {
                return true;
            }
            return base.CanConvertFrom(context, sourceType);
        }
        public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
        {
            if (destinationType == typeof(int))
            {
                return true;
            }
            return base.CanConvertTo(context, destinationType);
        }
        public override object ConvertFrom(ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value)
        {
            if (value is int)
            {
                DerivedClass derived = new DerivedClass();
                derived.BaseProperty = (int)value;
                derived.DerivedProperty = (int)value;
                return derived;
            }
            return base.ConvertFrom(context, culture, value);
        }
        public override object ConvertTo(ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value, Type destinationType)
        {
            if (destinationType == typeof(int))
            {
                DerivedClass derived = value as DerivedClass;
                if (derived != null)
                {
                    return derived.BaseProperty + derived.DerivedProperty;
                }
            }
            return base.ConvertTo(context, culture, value, destinationType);
        }
    }

    [TypeConverter(typeof(IBaseConverter))]
    public interface IBase
    {
        int InterfaceProperty { get; set; }
    }

    public interface IDerived : IBase
    {
        int DerivedInterfaceProperty { get; set; }
    }

    public class ClassIDerived : IDerived
    {
        public ClassIDerived()
        {
            InterfaceProperty = 20;
            DerivedInterfaceProperty = InterfaceProperty / 2;
        }
        public int InterfaceProperty { get; set; }
        public int DerivedInterfaceProperty { get; set; }
    }

    public class IBaseConverter : TypeConverter
    {
        public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
        {
            if (destinationType == typeof(string) || destinationType == typeof(int))
            {
                return true;
            }
            return base.CanConvertTo(context, destinationType);
        }

        public override object ConvertTo(ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value, Type destinationType)
        {
            if (destinationType == typeof(string))
            {
                IBase baseInterface = (IBase)value;
                return "InterfaceProperty = " + baseInterface.InterfaceProperty.ToString();
            }
            if (destinationType == typeof(int))
            {
                IBase baseInterface = (IBase)value;
                return baseInterface.InterfaceProperty;
            }
            return base.ConvertTo(context, culture, value, destinationType);
        }
    }

    // TypeDescriptor should default to the TypeConverter in this case.
    public class ClassWithNoConverter
    {
    }
}
