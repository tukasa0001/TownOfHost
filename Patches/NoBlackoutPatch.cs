using HarmonyLib;

namespace TOHE;

[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.RpcMurderPlayer))]
internal class RpcMurderPlayerPatch
{
    public static void Postfix(PlayerControl __instance)
    {
        Utils.NotifyRoles();
    }
}

[HarmonyPatch(typeof(LogicGameFlowNormal), nameof(LogicGameFlowNormal.IsGameOverDueToDeath))]
internal class DontBlackoutPatch
{
    public static void Postfix(ref bool __result)
    {
        __result = false;
    }
}