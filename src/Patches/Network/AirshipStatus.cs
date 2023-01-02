using HarmonyLib;
using TownOfHost.Extensions;
using TownOfHost.Roles;

namespace TownOfHost
{
    //参考元:https://github.com/yukieiji/ExtremeRoles/blob/master/ExtremeRoles/Patches/AirShipStatusPatch.cs
    [HarmonyPatch(typeof(AirshipStatus), nameof(AirshipStatus.PrespawnStep))]
    public static class AirshipStatusPrespawnStepPatch
    {
        public static bool Prefix()
        {
            /*return !PlayerControl.LocalPlayer.Is(GM.Ref<GM>()); // GMは湧き画面をスキップ*/
            return true;
        }
    }
}