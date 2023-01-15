using System.Collections.Generic;

namespace VentLib.Extensions;

public static class CollectionExtensions
{
    public static string StrJoin<T>(this IEnumerable<T> list)
    {
        return "[" + string.Join(", ", list) + "]";
    }

    public static bool TryGet<TKey, TValue>(this Il2CppSystem.Collections.Generic.Dictionary<TKey, TValue> dictionary, TKey key, out TValue? v)
    {
        v = default;
        if (!dictionary.ContainsKey(key)) return false;
        v = dictionary[key];
        return true;
    }

}