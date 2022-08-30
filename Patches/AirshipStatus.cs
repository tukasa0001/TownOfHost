/*
* AirshipStatus.cs created on Tue Aug 30 2022
* This software is released under the GNU General Public License v3.0.
* Copyright (c) 2022 空き瓶/EmptyBottle
*/

using HarmonyLib;

namespace TownOfHost
{
    //参考元:https://github.com/yukieiji/ExtremeRoles/blob/master/ExtremeRoles/Patches/AirShipStatusPatch.cs
    [HarmonyPatch(typeof(AirshipStatus), nameof(AirshipStatus.PrespawnStep))]
    public static class AirshipStatusPrespawnStepPatch
    {
        public static bool Prefix()
        {
            return !PlayerControl.LocalPlayer.Is(CustomRoles.GM); // GMは湧き画面をスキップ
        }
    }
}