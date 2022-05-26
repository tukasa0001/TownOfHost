
using HarmonyLib;

namespace TownOfHost.Patches
{
    [HarmonyPatch(typeof(ChatBubble), nameof(ChatBubble.SetRight))]
    class ChatBubblePatch
    {
        public static void Postfix(ChatBubble __instance)
        {
            if (Main.isChatCommand) __instance.SetLeft();
        }
    }
}