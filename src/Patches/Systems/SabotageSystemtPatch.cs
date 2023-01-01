using HarmonyLib;

namespace TownOfHost
{
    //参考
    //https://github.com/Koke1024/Town-Of-Moss/blob/main/TownOfMoss/Patches/MeltDownBoost.cs

    [HarmonyPatch(typeof(ReactorSystemType), nameof(ReactorSystemType.Detoriorate))]
    public static class ReactorSystemTypePatch
    {
        public static void Prefix(ReactorSystemType __instance)
        {
            if (!__instance.IsActive || !Options.SabotageTimeControl.GetBool())
                return;
            if (ShipStatus.Instance.Type == ShipStatus.MapType.Pb)
            {
                if (__instance.Countdown >= Options.PolusReactorTimeLimit.GetFloat())
                    __instance.Countdown = Options.PolusReactorTimeLimit.GetFloat();
                return;
            }
            return;
        }
    }
    [HarmonyPatch(typeof(HeliSabotageSystem), nameof(HeliSabotageSystem.Detoriorate))]
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
}