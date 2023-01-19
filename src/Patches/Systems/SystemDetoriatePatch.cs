using HarmonyLib;
using TownOfHost.Extensions;
using TownOfHost.Options;

namespace TownOfHost.Patches.Systems;
//参考
//https://github.com/Koke1024/Town-Of-Moss/blob/main/TownOfMoss/Patches/MeltDownBoost.cs

[HarmonyPatch(typeof(ReactorSystemType), nameof(ReactorSystemType.Detoriorate))]
public static class ReactorSystemTypePatch
{
    public static void Prefix(ReactorSystemType __instance)
    {
        if (SabotagePatch.CurrentSabotage is SabotageType.Reactor)
            SabotagePatch.SabotageCountdown = __instance.Countdown;
        if (!__instance.IsActive || !StaticOptions.SabotageTimeControl)
            return;
        if (ShipStatus.Instance.Type != ShipStatus.MapType.Pb) return;
        if (__instance.Countdown >= StaticOptions.PolusReactorTimeLimit)
            __instance.Countdown = StaticOptions.PolusReactorTimeLimit;
    }
}

[HarmonyPatch(typeof(LifeSuppSystemType), nameof(LifeSuppSystemType.Detoriorate))]
public static class LifeSupportSystemPatch
{
    public static void Prefix(LifeSuppSystemType __instance)
    {
        if (SabotagePatch.CurrentSabotage is SabotageType.Oxygen)
            SabotagePatch.SabotageCountdown = __instance.Countdown;
    }
}

[HarmonyPatch(typeof(HeliSabotageSystem), nameof(HeliSabotageSystem.Detoriorate))]
public static class HeliSabotageSystemPatch
{
    public static void Prefix(HeliSabotageSystem __instance)
    {
        if (!__instance.IsActive || !StaticOptions.SabotageTimeControl)
            return;
        if (AirshipStatus.Instance == null) return;
        if (__instance.Countdown >= StaticOptions.AirshipReactorTimeLimit)
            __instance.Countdown = StaticOptions.AirshipReactorTimeLimit;
    }
}