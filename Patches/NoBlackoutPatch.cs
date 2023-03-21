using HarmonyLib;

namespace TOHE;

[HarmonyPatch(typeof(LogicGameFlowNormal), nameof(LogicGameFlowNormal.IsGameOverDueToDeath))]
internal class DontBlackoutPatch
{
    public static void Postfix(ref bool __result)
    {
        __result = false;
    }
}