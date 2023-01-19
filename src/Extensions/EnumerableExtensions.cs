using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace TownOfHost.Extensions;

public static class EnumerableExtensions
{
    public static List<T> ToList<T>(this Il2CppSystem.Collections.Generic.HashSet<T> hashSet)
    {
        List<T> list = new();
        foreach (T item in hashSet)
            list.Add(item);
        return list;
    }
}