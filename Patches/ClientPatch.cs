using HarmonyLib;
using UnityEngine;
using System.Globalization;
using static TownOfHost.Translator;

namespace TownOfHost
{
    [HarmonyPatch(typeof(GameStartManager), nameof(GameStartManager.MakePublic))]
    class MakePublicPatch
    {
        public static bool Prefix(GameStartManager __instance)
        {
            // 定数設定による公開ルームブロック
            if (!Main.AllowPublicRoom)
            {
                var message = GetString("DisabledByProgram");
                Logger.Info(message, "MakePublicPatch");
                Logger.SendInGame(message);
                return false;
            }
            // 名前確認による公開ルームブロック
            bool NameIncludeTOH = SaveManager.PlayerName.ToUpper().Contains("TOH");
            if (ModUpdater.isBroken || ModUpdater.hasUpdate || !NameIncludeTOH)
            {
                var message = GetString("NameIncludeTOH");
                if (ModUpdater.isBroken) message = GetString("ModBrokenMessage");
                if (ModUpdater.hasUpdate) message = GetString("CanNotJoinPublicRoomNoLatest");
                Logger.Info(message, "MakePublicPatch");
                Logger.SendInGame(message);
                return false;
            }
            return true;
        }
    }
    [HarmonyPatch(typeof(MMOnlineManager), nameof(MMOnlineManager.Start))]
    class MMOnlineManagerStartPatch
    {
        public static void Postfix(MMOnlineManager __instance)
        {
            if (!(ModUpdater.hasUpdate || ModUpdater.isBroken)) return;
            var obj = GameObject.Find("FindGameButton");
            if (obj)
            {
                obj?.SetActive(false);
                var parentObj = obj.transform.parent.gameObject;
                var textObj = Object.Instantiate<TMPro.TextMeshPro>(obj.transform.FindChild("Text_TMP").GetComponent<TMPro.TextMeshPro>());
                textObj.transform.position = new Vector3(1f, -0.3f, 0);
                textObj.name = "CanNotJoinPublic";
                var message = ModUpdater.isBroken ? $"<size=2>{Helpers.ColorString(Color.red, GetString("ModBrokenMessage"))}</size>"
                    : $"<size=2>{Helpers.ColorString(Color.red, GetString("CanNotJoinPublicRoomNoLatest"))}</size>";
                new LateTask(() => { textObj.text = message; }, 0.01f, "CanNotJoinPublic");
            }
        }
    }
    [HarmonyPatch(typeof(SplashManager), nameof(SplashManager.Update))]
    class SplashLogoAnimatorPatch
    {
        public static void Prefix(SplashManager __instance)
        {
            if (Main.AmDebugger.Value)
            {
                __instance.sceneChanger.AllowFinishLoadingScene();
                __instance.startedSceneLoad = true;
            }
        }
    }
    [HarmonyPatch(typeof(EOSManager), nameof(EOSManager.IsAllowedOnline))]
    class RunLoginPatch
    {
        public static void Prefix(ref bool canOnline)
        {
            if (ThisAssembly.Git.Branch != "main" && CultureInfo.CurrentCulture.Name != "ja-JP") canOnline = false;
        }
    }
}