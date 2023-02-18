using System;
using HarmonyLib;

namespace TownOfHost.Patches;

[HarmonyPatch(typeof(HashRandom))]
class HashRandomPatch
{
    [HarmonyPatch(nameof(HashRandom.FastNext)), HarmonyPrefix]
    static bool FastNext([HarmonyArgument(0)] int maxInt, ref int __result)
    {
        if (IRandom.Instance.GetType() == typeof(HashRandomWrapper)) return true;

        __result = IRandom.Instance.Next(maxInt);
        Logger.Info("らんだむ", "FastNext");

        return false;
    }
    [HarmonyPatch(nameof(HashRandom.Next), new Type[] { typeof(int) }), HarmonyPrefix]
    static bool MaxNext([HarmonyArgument(0)] int maxInt, ref int __result)
    {
        if (IRandom.Instance.GetType() == typeof(HashRandomWrapper)) return true;

        __result = IRandom.Instance.Next(maxInt);
        Logger.Info("らんだむ", "MaxNext");

        return false;
    }
    [HarmonyPatch(nameof(HashRandom.Next), new Type[] { typeof(int), typeof(int) }), HarmonyPrefix]
    static bool MinMaxNext([HarmonyArgument(0)] int minInt, [HarmonyArgument(1)] int maxInt, ref int __result)
    {
        if (IRandom.Instance.GetType() == typeof(HashRandomWrapper)) return true;

        __result = IRandom.Instance.Next(minInt, maxInt);
        Logger.Info("らんだむ", "MinMaxNext");

        return false;
    }
}