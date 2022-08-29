/*
* This software is released under the GNU General Public License v3.0.
* Copyright (c) 2022 空き瓶/EmptyBottle
*/

using HarmonyLib;

namespace TownOfHost.Patches
{
    [HarmonyPatch(typeof(ChatBubble), nameof(ChatBubble.SetRight))]
    class ChatBubbleSetRightPatch
    {
        public static void Postfix(ChatBubble __instance)
        {
            if (Main.isChatCommand) __instance.SetLeft();
        }
    }
    [HarmonyPatch(typeof(ChatBubble), nameof(ChatBubble.SetName))]
    class ChatBubbleSetNamePatch
    {
        public static void Postfix(ChatBubble __instance)
        {
            if (GameStates.IsInGame && __instance.playerInfo.PlayerId == PlayerControl.LocalPlayer.PlayerId)
                __instance.NameText.color = PlayerControl.LocalPlayer.GetRoleColor();
        }
    }
}