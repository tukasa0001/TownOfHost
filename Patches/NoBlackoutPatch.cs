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

    [HarmonyPatch(typeof(LogicGameFlow), nameof(LogicGameFlow.IsGameOverDueToDeath))]
    class DontBlackoutPatch
    {
        public static void Postfix(ref bool __result)
        {
            __result = false;
        }
    }
}