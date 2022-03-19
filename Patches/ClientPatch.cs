using HarmonyLib;
using UnityEngine;
using InnerNet;

namespace TownOfHost
{
    [HarmonyPatch(typeof(InnerNetClient), nameof(InnerNetClient.ChangeGamePublic))]
    class ChangeGamePublicPatch
    {
        static int blockCount = 0;
        public static void Prefix(InnerNetClient __instance, [HarmonyArgument(0)] ref bool isPublic)
        {
            if (main.PluginVersionType == VersionTypes.Beta)
            {
                if (isPublic)
                {
                    blockCount++;
                    if (blockCount >= 100 && blockCount % 100 == 0 || Input.GetKey(KeyCode.B))
                    {
                        //100回ごとに特殊処理
                        HudManager.Instance.Chat.AddChat(PlayerControl.LocalPlayer, "ベータ版では公開ルームにできません。\n連打しても無駄です。");
                        DestroyableSingleton<HudManager>.Instance.KillOverlay.ShowKillAnimation(PlayerControl.LocalPlayer.Data, PlayerControl.LocalPlayer.Data);
                    }
                    else
                    {
                        //通常処理
                        HudManager.Instance.Chat.AddChat(PlayerControl.LocalPlayer, "ベータ版では公開ルームにできません。");
                    }

                    if (blockCount >= 10 && blockCount % 10 == 0 && blockCount % 100 != 0 && !HudManager.Instance.Chat.IsOpen)
                    {
                        //10回ごとに強制チャット表示
                        HudManager.Instance.Chat.Toggle();
                    }
                }
                isPublic = false;
            }
        }
    }
}