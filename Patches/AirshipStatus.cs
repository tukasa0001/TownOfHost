using HarmonyLib;

using TownOfHost.Roles.Core;

namespace TownOfHost
{
    //参考元:https://github.com/yukieiji/ExtremeRoles/blob/master/ExtremeRoles/Patches/AirShipStatusPatch.cs
    [HarmonyPatch(typeof(AirshipStatus), nameof(AirshipStatus.PrespawnStep))]
    public static class AirshipStatusPrespawnStepPatch
    {
        public static bool Prefix()
        {
            if (PlayerControl.LocalPlayer.Is(CustomRoles.GM))
            {
                RandomSpawn.hostReady = true;
                RandomSpawn.AirshipSpawn(PlayerControl.LocalPlayer);
                // GMは湧き画面をスキップ
                return false;
            }
            return true;
        }
    }
}