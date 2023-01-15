using HarmonyLib;
using TownOfHost.GUI;
using TownOfHost.Managers;
using UnityEngine;

namespace TownOfHost.Patches
{
    [HarmonyPatch(typeof(ChatBubble), nameof(ChatBubble.SetRight))]
    class ChatBubbleSetRightPatch
    {
        public static void Postfix(ChatBubble __instance)
        {
            if (TOHPlugin.isChatCommand) __instance.SetLeft();
        }
    }
    [HarmonyPatch(typeof(ChatBubble), nameof(ChatBubble.SetName))]
    class ChatBubbleSetNamePatch
    {
        public static void Prefix(ChatBubble __instance)
        {
            PlayerControl relatedPlayer = __instance.playerInfo.Object;
            if (relatedPlayer == null) return;
            DynamicName name = relatedPlayer.GetDynamicName();
            relatedPlayer.RpcSetName(name.RawName);
            __instance.NameText.color = Color.white;
        }

        /*public static void Postfix(ChatBubble __instance)
        {
            PlayerControl relatedPlayer = __instance.playerInfo.Object;
            if (relatedPlayer == null) return; ;
        }*/
    }
}