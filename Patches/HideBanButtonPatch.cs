using HarmonyLib;

namespace TownOfHost
{
    [HarmonyPatch(typeof(ChatController), nameof(ChatController.Toggle))]
    class CancelBanMenuStuckPatch
    {
        public static void Prefix(ChatController __instance)
        {
            if (__instance.IsOpenOrOpening && !__instance.IsAnimating) // (IsOpen==true) == 今から閉じないといけない
            {
                // BanButtonを非表示にする
                __instance.banButton.SetVisible(false);
            }
        }
    }
}