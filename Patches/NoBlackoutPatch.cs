using HarmonyLib;

namespace TownOfHost
{
    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.RpcMurderPlayer))]
    class RpcMurderPlayerPatch
    {
        public static void Postfix(PlayerControl __instance)
        {
            Utils.NotifyRoles();
            Main.BlockKilling[__instance.PlayerId] = false;
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