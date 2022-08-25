/*
* This software is released under the GNU General Public License v3.0.
* Copyright (c) 2022 空き瓶/EmptyBottle
*/

using HarmonyLib;

namespace TownOfHost
{
    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.RpcMurderPlayer))]
    class RpcMurderPlayerPatch
    {
        public static void Postfix(PlayerControl __instance)
        {
            Utils.NotifyRoles();
        }
    }

    [HarmonyPatch(typeof(ShipStatus), nameof(ShipStatus.IsGameOverDueToDeath))]
    class DontBlackoutPatch
    {
        public static void Postfix(ShipStatus __instance, ref bool __result)
        {
            __result = false;
        }
    }
}