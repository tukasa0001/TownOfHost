/*
* This software is released under the GNU General Public License v3.0.
* Copyright (c) 2022 空き瓶/EmptyBottle
*/

using HarmonyLib;

namespace TownOfHost
{
    [HarmonyPatch(typeof(AmongUsClient), nameof(AmongUsClient.OnDisconnected))]
    class OnDisconnectedPatch
    {
        public static void Postfix(AmongUsClient __instance)
        {
            Main.VisibleTasksCount = false;
        }
    }
}