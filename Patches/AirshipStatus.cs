using HarmonyLib;
using UnityEngine;

using TownOfHostForE.Roles.Core;

namespace TownOfHostForE
{
    //参考元:https://github.com/yukieiji/ExtremeRoles/blob/master/ExtremeRoles/Patches/AirShipStatusPatch.cs
    [HarmonyPatch(typeof(AirshipStatus), nameof(AirshipStatus.PrespawnStep))]
    public static class AirshipStatusPrespawnStepPatch
    {
        public static bool Prefix()
        {
            if (PlayerControl.LocalPlayer.Is(CustomRoles.GM))
            {
                RandomSpawn.AirshipSpawn(PlayerControl.LocalPlayer);
                // GMは湧き画面をスキップ
                return false;
            }
            return true;

            //var player = PlayerControl.LocalPlayer;
            ////GMならスポーン画面を開かず、固定値へ
            //if (player.Is(CustomRoles.GM))
            //{
            //    RandomSpawn.TP(player.NetTransform, new Vector3(5.8f, -0.2f, 0.0f));
            //    return false;
            //}
            //else
            //{
            //    return true;
            //}
            //return !PlayerControl.LocalPlayer.Is(CustomRoles.GM); // GMは湧き画面をスキップ
        }
    }
}