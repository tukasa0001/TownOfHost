using HarmonyLib;

namespace TownOfHost
{
    [HarmonyPatch(typeof(Console), nameof(Console.CanUse))]
    class CanUsePatch
    {
        public static bool Prefix(ref float __result, Console __instance, [HarmonyArgument(0)] GameData.PlayerInfo pc, [HarmonyArgument(1)] out bool canUse, [HarmonyArgument(2)] out bool couldUse)
        {
            canUse = couldUse = false;
            //こいつをfalseでreturnしても、タスク(サボ含む)以外の使用可能な物は使えるまま(ボタンなど)
            if (__instance.AllowImpostor) return true;
            if (!Utils.hasTasks(PlayerControl.LocalPlayer.Data, false))
            {
                return false;
            }
            return true;
        }
    }
    [HarmonyPatch(typeof(EmergencyMinigame), nameof(EmergencyMinigame.Update))]
    class EmergencyMinigamePatch
    {
        public static void Postfix(EmergencyMinigame __instance)
        {
            if (Options.IsHideAndSeek) __instance.Close();
        }
    }
    [HarmonyPatch(typeof(Vent), nameof(Vent.CanUse))]
    class CanUseVentPatch
    {
        public static void Postfix([HarmonyArgument(0)] GameData.PlayerInfo pc,
            [HarmonyArgument(1)] ref bool canUse,
            [HarmonyArgument(2)] ref bool couldUse)
        {
            if (pc.Object.isSheriff())
                canUse = couldUse = false;
        }
    }
}
