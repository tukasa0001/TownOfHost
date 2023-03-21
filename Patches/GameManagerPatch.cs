using HarmonyLib;
using Hazel;

namespace TOHE;

[HarmonyPatch(typeof(GameManager), nameof(GameManager.Serialize))]
class GameManagerSerializeFix
{
    public static bool Prefix(GameManager __instance, [HarmonyArgument(0)] MessageWriter writer, [HarmonyArgument(1)] bool initialState, ref bool __result)
    {
        bool flag = false;
        for (int index = 0; index < __instance.LogicComponents.Count; ++index)
        {
            GameLogicComponent logicComponent = __instance.LogicComponents[index];
            if (initialState || logicComponent.IsDirty)
            {
                flag = true;
                writer.StartMessage((byte)index);
                var hasBody = logicComponent.Serialize(writer, initialState);
                if (hasBody) writer.EndMessage();
                else writer.CancelMessage();
                logicComponent.ClearDirtyFlag();
            }
        }
        __instance.ClearDirtyBits();
        __result = flag;
        return false;
    }
}
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