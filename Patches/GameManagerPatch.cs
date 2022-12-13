using HarmonyLib;
using Hazel;

namespace TownOfHost
{
    [HarmonyPatch(typeof(LogicOptions), nameof(LogicOptions.Serialize))]
    class LogicOptionsSerializePatch
    {
        public static bool Prefix(LogicOptions __instance, ref bool __result, MessageWriter writer, bool initialState)
        {
            // 初回以外はブロックし、CustomSyncSettingsでのみ同期する
            if (!initialState)
            {
                __result = false;
                return false;
            }
            else return true;
        }
    }
}