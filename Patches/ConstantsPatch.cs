using HarmonyLib;

namespace TownOfHost.Patches;

[HarmonyPatch(typeof(Constants), nameof(Constants.GetBroadcastVersion))]
public static class ConstantsGetBroadcastVersionPatch
{
    public static void Postfix(ref int __result)
    {
        if (GameStates.IsLocalGame)
        {
            return;
        }
        __result += 25;
    }
}

// AU side bug?
[HarmonyPatch(typeof(Constants), nameof(Constants.IsVersionModded))]
public static class ConstantsIsVersionModdedPatch
{
    public static bool Prefix(ref bool __result)
    {
        __result = true;
        return false;
    }
}
