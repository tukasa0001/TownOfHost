using System;
using System.Collections.Generic;
using System.Linq;
using VentLib.Utilities.Extensions;

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

    public static T Random<T>(this IEnumerable<T> source, Func<T, bool> filter)
    {
        return source.Where(filter).ToList().GetRandom();
    }

    private static Random rng = new Random();

    public static void Shuffle<T>(this IList<T> list)
    {
        int n = list.Count;
        while (n > 1) {
            n--;
            int k = rng.Next(n + 1);
            (list[k], list[n]) = (list[n], list[k]);
        }
    }
}