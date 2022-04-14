using HarmonyLib;
using UnityEngine;

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
            if (Options.CurrentGameMode == CustomGameMode.HideAndSeek) __instance.Close();
        }
    }
    [HarmonyPatch(typeof(Vent), nameof(Vent.CanUse))]
    class CanUseVentPatch
    {
        public static void Postfix(Vent __instance, [HarmonyArgument(0)] GameData.PlayerInfo pc,
            [HarmonyArgument(1)] ref bool canUse,
            [HarmonyArgument(2)] ref bool couldUse,
            ref float __result)
        {
            if (pc.Object.isSheriff() || (pc.Object.isArsonist() && main.DousedPlayerCount[pc.Object.PlayerId] != 0) || pc.Object.Data.IsDead)
                canUse = couldUse = false;
            if (pc.Object.isArsonist() && main.DousedPlayerCount[pc.Object.PlayerId] == 0)
                canUse = couldUse = true;

            ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

            //#######################################
            //     ==ベントに入れるようにする処理==
            //#######################################
            //参考:https://github.com/Koke1024/Town-Of-Moss/blob/main/TownOfMoss/Patches/Vent.cs

            float num = float.MaxValue;
            var ventilationSystem = ShipStatus.Instance.Systems[SystemTypes.Ventilation].Cast<VentilationSystem>();
            if (canUse && !pc.Object.getCustomRole().isImpostor())
            {
                DestroyableSingleton<HudManager>.Instance.ImpostorVentButton.ToggleVisible(true && !pc.Object.Data.IsDead);
                Vector3 center = pc.Object.Collider.bounds.center;
                Vector3 position = __instance.transform.position;
                num = Vector2.Distance((Vector2)center, (Vector2)position);
                var usableDistance = pc._object.inVent ? 0.35 : (double)__instance.UsableDistance;
                canUse = ((canUse ? 1 : 0) & ((double)num > usableDistance ? 0 : (!PhysicsHelpers.AnythingBetween(pc.Object.Collider, (Vector2)center, (Vector2)position, Constants.ShipOnlyMask, false) ? 1 : 0))) != 0;
            }
            __result = num;
            ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        }
    }
}
