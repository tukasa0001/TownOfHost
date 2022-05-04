using HarmonyLib;
using InnerNet;
using static TownOfHost.Translator;

namespace TownOfHost
{
    [HarmonyPatch(typeof(GameStartManager), nameof(GameStartManager.MakePublic))]
    class MakePublicPatch
    {
        public static bool Prefix(GameStartManager __instance)
        {
            if (ModUpdater.hasUpdate)
            {
                Logger.SendInGame(getString("onSetPublicNoLatest"));
                return false;
            }
            return true;
        }
    }
    [HarmonyPatch(typeof(SplashManager), nameof(SplashManager.Update))]
    class SplashLogoAnimatorPatch
    {
        public static void Prefix(SplashManager __instance)
        {
            if (main.AmDebugger.Value)
            {
                __instance.sceneChanger.AllowFinishLoadingScene();
                __instance.startedSceneLoad = true;
            }
        }
    }
}