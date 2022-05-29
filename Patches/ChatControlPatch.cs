using HarmonyLib;
using UnityEngine;

namespace TownOfHost
{
    [HarmonyPatch(typeof(ChatController), nameof(ChatController.Awake))]
    class ChatControllerAwakePatch
    {
        public static void Postfix(ChatController __instance)
        {
            __instance.TextArea.AllowPaste = true;
        }
    }
    [HarmonyPatch(typeof(ChatController), nameof(ChatController.Update))]
    class ChatControllerUpdatePatch
    {
        public static void Postfix(ChatController __instance)
        {
            if (!__instance.TextArea.hasFocus) return;
            if ((Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)) && Input.GetKeyDown(KeyCode.C))
                ClipboardHelper.PutClipboardString(__instance.TextArea.text);
            if ((Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)) && Input.GetKeyDown(KeyCode.X))
            {
                ClipboardHelper.PutClipboardString(__instance.TextArea.text);
                __instance.TextArea.SetText("");
            }
        }
    }
}