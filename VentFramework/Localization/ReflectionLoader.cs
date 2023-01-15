using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using MonoMod.RuntimeDetour;
using static VentLib.Localization.LocalizedAttribute;

namespace VentLib.Localization;

public class ReflectionLoader
{
    private static Dictionary<string, string> lookupCache = new ();


    public static void RegisterClass(Type cls, string? parentGroup = null, string? group = null)
    {
        LocalizedAttribute? parentAttribute = cls.GetCustomAttribute<LocalizedAttribute>();
        if (parentAttribute != null)
        {
            parentAttribute.Source = cls;
            Attributes.Add(parentAttribute, new ReflectionObject(cls, ReflectionType.Class));
            group ??= parentAttribute.Group;
        }

        List<FieldInfo> staticFields = cls.GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic).ToList();
        FieldInfo? overrideField = staticFields.FirstOrDefault(f => f.GetCustomAttribute<SubgroupProvider>() != null);
        if (overrideField != null) group = (string?)overrideField.GetValue(null);

        List<PropertyInfo> properties = cls.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).ToList();
        PropertyInfo? overrideProperty = properties.FirstOrDefault(f => f.GetCustomAttribute<SubgroupProvider>() != null);
        if (overrideProperty != null)
        {
            Hook _ = new(overrideProperty.GetGetMethod(true), new Func<Func<object, string>, object, string>((getter, self) => PropertyInfoHook(getter, self, parentAttribute)));
        }

        staticFields.Do(f => RegisterField(f, ReflectionType.StaticField, parentAttribute));
        cls.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).Do(f => RegisterField(f, ReflectionType.InstanceField, parentAttribute));
        cls.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).Do(f => RegisterProperty(f, parentAttribute, cls.Assembly));
        cls.GetNestedTypes().Do(clz => RegisterClass(clz, group));
    }

    public static void RegisterField(FieldInfo field, ReflectionType reflectionType, LocalizedAttribute? parent)
    {
        LocalizedAttribute? attribute = field.GetCustomAttribute<LocalizedAttribute>();
        if (attribute == null) return;
        if (parent != null)
            attribute.GroupSupplier = parent.GetPath;

        attribute.Source = field;
        Attributes.Add(attribute, new ReflectionObject(field, reflectionType));
    }

    public static void RegisterProperty(PropertyInfo property, LocalizedAttribute? parent, Assembly assembly)
    {
        LocalizedAttribute? attribute = property.GetCustomAttribute<LocalizedAttribute>();
        if (attribute == null) return;

        string assemblyName = Assembly.GetCallingAssembly().GetName().Name!;
        Hook _ = new(property.GetGetMethod(true), new Func<Func<object, string>, object, string>((getter, self) => PropertyModifyHook(getter, self, attribute, assemblyName)));
        if (parent != null)
            attribute.GroupSupplier = parent.GetPath;

        attribute.Source = property;
        Attributes.Add(attribute, new ReflectionObject(property, ReflectionType.Property));
    }

    public static string PropertyInfoHook(Func<object, string> getter, object self, LocalizedAttribute? parent)
    {
        string value = getter(self);
        if (parent == null) return value;
        parent.Subgroup = value;
        return value;
    }

    public static string PropertyModifyHook(Func<object, string> getter, object self, LocalizedAttribute target, string assemblyName)
    {
        // DO NOT REMOVE BELOW. THIS IS REQUIRED TO POSSIBLY INVOKE ANY REQS
        getter(self);
        return Localizer.Get(target.GetPath(), assemblyName);
    }
}