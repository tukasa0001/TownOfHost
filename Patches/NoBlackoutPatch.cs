using HarmonyLib;

namespace TownOfHost
{
    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.RpcMurderPlayer))]
    class RpcMurderPlayerPatch
    {
        public static void Postfix(PlayerControl __instance, PlayerControl target)
        {
            Logger.Info($"{__instance.GetNameWithRole()}, {target.GetNameWithRole()}", "RpcMurderPlayerPatch");
            target.SetRealKiller(__instance, true);
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