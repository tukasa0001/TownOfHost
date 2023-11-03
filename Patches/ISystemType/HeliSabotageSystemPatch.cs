using HarmonyLib;
using Hazel;
using TownOfHost.Roles.Core;
using TownOfHost.Roles.Core.Interfaces;

namespace TownOfHost.Patches.ISystemType;

[HarmonyPatch(typeof(HeliSabotageSystem), nameof(HeliSabotageSystem.UpdateSystem))]
public static class HeliSabotageSystemUpdateSystemPatch
{
    public static bool Prefix(HeliSabotageSystem __instance, [HarmonyArgument(0)] PlayerControl player, [HarmonyArgument(1)] MessageReader msgReader)
    {
        byte amount;
        {
            var newReader = MessageReader.Get(msgReader);
            amount = newReader.ReadByte();
            newReader.Recycle();
        }

        if (player.GetRoleClass() is ISystemTypeUpdateHook systemTypeUpdateHook && !systemTypeUpdateHook.UpdateHeliSabotageSystem(__instance, amount))
        {
            return false;
        }
        return true;
    }
}

//参考
//https://github.com/Koke1024/Town-Of-Moss/blob/main/TownOfMoss/Patches/MeltDownBoost.cs

[HarmonyPatch(typeof(HeliSabotageSystem), nameof(HeliSabotageSystem.Deteriorate))]
public static class HeliSabotageSystemPatch
{
    public static void Prefix(HeliSabotageSystem __instance)
    {
        if (!__instance.IsActive || !Options.SabotageTimeControl.GetBool())
            return;
        if (AirshipStatus.Instance != null)
            if (__instance.Countdown >= Options.AirshipReactorTimeLimit.GetFloat())
                __instance.Countdown = Options.AirshipReactorTimeLimit.GetFloat();
    }
}
