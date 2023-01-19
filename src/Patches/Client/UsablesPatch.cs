using HarmonyLib;
using UnityEngine;
using AmongUs.GameOptions;
using TownOfHost.Extensions;
using TownOfHost.Roles;

namespace TownOfHost
{
    [HarmonyPatch(typeof(Console), nameof(Console.CanUse))]
    class CanUsePatch
    {
        public static bool Prefix(ref float __result, Console __instance, [HarmonyArgument(0)] GameData.PlayerInfo pc, [HarmonyArgument(1)] out bool canUse, [HarmonyArgument(2)] out bool couldUse)
        {
            canUse = couldUse = false;
            //こいつをfalseでreturnしても、タスク(サボ含む)以外の使用可能な物は使えるまま(ボタンなど)
            return __instance.AllowImpostor || Utils.HasTasks(PlayerControl.LocalPlayer.Data);
        }
    }
    [HarmonyPatch(typeof(EmergencyMinigame), nameof(EmergencyMinigame.Update))]
    class EmergencyMinigamePatch
    {
        public static void Postfix(EmergencyMinigame __instance)
        {
            if (OldOptions.CurrentGameMode == CustomGameMode.HideAndSeek) __instance.Close();
        }
    }
    [HarmonyPatch(typeof(Vent), nameof(Vent.CanUse))]
    class CanUseVentPatch
    {
        public static bool Prefix(Vent __instance, [HarmonyArgument(0)] GameData.PlayerInfo pc,
            [HarmonyArgument(1)] ref bool canUse,
            [HarmonyArgument(2)] ref bool couldUse,
            ref float __result)
        {
            ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

            //#######################################
            //     ==ベント処理==
            //#######################################
            //参考:https://github.com/Eisbison/TheOtherRoles/blob/main/TheOtherRoles/Patches/UsablesPatch.cs

            bool VentForTrigger = false;
            float num = float.MaxValue;

            var usableDistance = __instance.UsableDistance;

            if (pc.IsDead) return false; //死んでる人は強制的にfalseに。
            canUse = couldUse = pc._object.GetCustomRole().CanVent();

            canUse = couldUse = (pc.Object.inVent || canUse) && (pc.Object.CanMove || pc.Object.inVent);

            if (!canUse) { }
            else
            {
                Vector2 truePosition = pc.Object.GetTruePosition();
                Vector3 position = __instance.transform.position;
                num = Vector2.Distance(truePosition, position);
                canUse &= num <= usableDistance &&
                          !PhysicsHelpers.AnythingBetween(truePosition, position, Constants.ShipOnlyMask, false);
            }

            __result = num;
            return false;
            ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        }
    }
}