using HarmonyLib;

namespace TownOfHost.Patches;

[HarmonyPatch(typeof(Constants), nameof(Constants.GetBroadcastVersion))]
public static class GetBroadcastVersionPatch
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
