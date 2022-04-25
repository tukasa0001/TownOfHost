using HarmonyLib;
using InnerNet;

namespace TownOfHost {
    [HarmonyPatch(typeof(InnerNetClient), nameof(InnerNetClient.ChangeGamePublic))]
    class ChangeGamePublicPatch {
        public static void Prefix(InnerNetClient __instance, [HarmonyArgument(0)] ref bool isPublic) {
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