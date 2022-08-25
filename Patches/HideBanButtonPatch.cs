/*
* This software is released under the GNU General Public License v3.0.
* Copyright (c) 2022 空き瓶/EmptyBottle
*/

using HarmonyLib;

namespace TownOfHost
{
    [HarmonyPatch(typeof(ChatController), nameof(ChatController.Toggle))]
    class CancelBanMenuStuckPatch
    {
        public static void Prefix(ChatController __instance)
        {
            if (__instance.IsOpen && !__instance.animating) // (IsOpen==true) == 今から閉じないといけない
            {
                // BanButtonを非表示にする
                __instance.BanButton.SetVisible(false);
            }
        }
    }
}