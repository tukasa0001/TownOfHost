using HarmonyLib;

namespace TOHE;

[HarmonyPatch(typeof(AmongUsClient), nameof(AmongUsClient.OnDisconnected))]
internal class OnDisconnectedPatch
{
    public static void Postfix(AmongUsClient __instance)
    {
        Main.VisibleTasksCount = false;
    }
}