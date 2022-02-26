using System;
using System.Collections.Generic;
using System.Text;
using HarmonyLib;

namespace TownOfHost.Patches
{
    [HarmonyPatch(typeof(ChatBubble), nameof(ChatBubble.SetRight))]
    class ChatBubblePatch
    {
        public static void Postfix(ChatBubble __instance)
        {
           if(main.isChatCommand) __instance.SetLeft();
        }
    }
}
