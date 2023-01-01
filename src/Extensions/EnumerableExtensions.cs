using System;
using System.Collections.Generic;

namespace TownOfHost.Extensions;

public static class EnumerableExtensions
{
    public static T PopRandom<T>(this List<T> list)
    {
        return list.Pop(new Random().Next(list.Count));
    }

    public static T GetRandom<T>(this List<T> list) => list[new Random().Next(list.Count)];

    public static T Pop<T>(this List<T> list, int index)
    {
        T value = list[index];
        list.RemoveAt(index);
        return value;
    }

    public static String PrettyString<T>(this IEnumerable<T> list)
    {
        return "[" + System.String.Join(", ", list) + "]";
    }


    public static List<T> ToList<T>(this Il2CppSystem.Collections.Generic.HashSet<T> hashSet)
    {
        List<T> list = new();
        foreach (T item in hashSet)
            list.Add(item);
        return list;
    }
}