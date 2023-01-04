namespace TownOfHost.Extensions;

public static class Il2CppCollectionExtensions
{
    public static bool TryGet<TKey, TValue>(this Il2CppSystem.Collections.Generic.Dictionary<TKey, TValue> dictionary, TKey key, out TValue? v)
    {
        v = default;
        if (!dictionary.ContainsKey(key)) return false;
        v = dictionary[key];
        return true;
    }
}