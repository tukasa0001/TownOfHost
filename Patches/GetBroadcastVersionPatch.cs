using HarmonyLib;

namespace TownOfHostForE.Patches;

[HarmonyPatch(typeof(Constants), nameof(Constants.GetBroadcastVersion))]
public static class GetBroadcastVersionPatch
{
    public static void Postfix(ref int __result)
    {
        if (AmongUsClient.Instance.NetworkMode is NetworkModes.LocalGame or NetworkModes.FreePlay) return;
        __result += 25;
    }
}
[HarmonyPatch(typeof(Constants), nameof(Constants.IsVersionModded))]
public static class IsVersionModdedPatch
{
    public static bool Prefix(ref bool __result)
    {
        __result = true;
        return false;
    }
}