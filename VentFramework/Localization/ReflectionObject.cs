using System;
using System.Reflection;
using TownOfHost.Roles;

namespace VentLib.Localization;

public class ReflectionObject
{
    public object Object;
    public ReflectionType ReflectionType;

    public ReflectionObject(object obj, ReflectionType reflectionType)
    {
        this.Object = obj;
        this.ReflectionType = reflectionType;
    }

    public void SetValue(object value)
    {
        switch (ReflectionType)
        {
            case ReflectionType.Class:
                break;
            case ReflectionType.Method:
                break;
            case ReflectionType.StaticField:
                FieldInfo field = (FieldInfo)Object;
                field.SetValue(typeof(AbstractBaseRole), value);
                break;
            case ReflectionType.Property:
            case ReflectionType.InstanceField:
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

    }

}

public enum ReflectionType
{
    Class,
    Method,
    StaticField,
    InstanceField,
    Property
}