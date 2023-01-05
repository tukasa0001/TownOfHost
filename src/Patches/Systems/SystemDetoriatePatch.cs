using HarmonyLib;
using TownOfHost.ReduxOptions;

namespace TownOfHost.Patches.Systems;
//参考
//https://github.com/Koke1024/Town-Of-Moss/blob/main/TownOfMoss/Patches/MeltDownBoost.cs

[HarmonyPatch(typeof(ReactorSystemType), nameof(ReactorSystemType.Detoriorate))]
public static class ReactorSystemTypePatch
{
    public static void Prefix(ReactorSystemType __instance)
    {
        if (!__instance.IsActive || !StaticOptions.SabotageTimeControl)
            return;
        if (ShipStatus.Instance.Type != ShipStatus.MapType.Pb) return;
        if (__instance.Countdown >= StaticOptions.PolusReactorTimeLimit)
            __instance.Countdown = StaticOptions.PolusReactorTimeLimit;
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