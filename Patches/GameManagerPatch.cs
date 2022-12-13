using HarmonyLib;

namespace TownOfHost
{
    [HarmonyPatch(typeof(LogicOptions), nameof(LogicOptions.Serialize))]
    class LogicOptionsSerializePatch
    {
        public static bool Prefix(LogicOptions __instance, ref bool __result)
        {
            // ゲーム開始後はブロックし、CustomSyncSettingsでのみ同期する
            if (GameManager.Instance.GameHasStarted)
            {
                __result = false;
                return false;
            }
            else return true;
        }
    }
}