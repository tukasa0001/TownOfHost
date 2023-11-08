using HarmonyLib;
using Hazel;

namespace TownOfHost.Patches.ISystemType;

[HarmonyPatch(typeof(MushroomMixupSabotageSystem), nameof(MushroomMixupSabotageSystem.UpdateSystem))]
public static class MushroomMixupSabotageSystemUpdateSystemPatch
{
    public static void Prefix([HarmonyArgument(1)] MessageReader msgReader, ref byte __state /* amount */)
    {
        byte amount;
        {
            var newReader = MessageReader.Get(msgReader);
            amount = newReader.ReadByte();
            newReader.Recycle();
        }
        __state = amount;
    }
    public static void Postfix(MushroomMixupSabotageSystem __instance, byte __state /* amount */)
    {
        var operation = (MushroomMixupSabotageSystem.Operation)__state;
        if (operation == MushroomMixupSabotageSystem.Operation.TriggerSabotage && Options.SabotageTimeControl.GetBool())
        {
            __instance.currentSecondsUntilHeal = Options.FungleMushroomMixupDuration.GetFloat();
        }
        // Desyncインポスター目線のプレイヤー名の表示/非表示を反映
        Utils.NotifyRoles(ForceLoop: true);
    }
}
[HarmonyPatch(typeof(MushroomMixupSabotageSystem), nameof(MushroomMixupSabotageSystem.Deteriorate))]
public static class MushroomMixupSabotageSystemDeterioratePatch
{
    public static void Prefix(MushroomMixupSabotageSystem __instance, ref bool __state /* 本体処理前のIsActive */)
    {
        __state = __instance.IsActive;
    }
    public static void Postfix(MushroomMixupSabotageSystem __instance, bool __state)
    {
        // 本体処理でIsActiveが変わった場合
        if (__instance.IsActive != __state)
        {
            // Desyncインポスター目線のプレイヤー名の表示/非表示を反映
            Utils.NotifyRoles(ForceLoop: true);
        }
    }
}
