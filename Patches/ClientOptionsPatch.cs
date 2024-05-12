using HarmonyLib;

using TownOfHostForE.Modules.ClientOptions;

namespace TownOfHostForE
{
    [HarmonyPatch(typeof(OptionsMenuBehaviour), nameof(OptionsMenuBehaviour.Start))]
    public static class OptionsMenuBehaviourStartPatch
    {
        private static ClientActionItem ForceJapanese;
        private static ClientActionItem JapaneseRoleName;
        private static ClientActionItem UnloadMod;
        private static ClientActionItem DumpLog;
        private static ClientOptionItem EnableCustomSoundEffect;
        private static ClientOptionItem EnableCustomBGMEffect;

        public static void Postfix(OptionsMenuBehaviour __instance)
        {
            if (__instance.DisableMouseMovement == null)
            {
                return;
            }

            if (ForceJapanese == null || ForceJapanese.ToggleButton == null)
            {
                ForceJapanese = ClientOptionItem.Create("ForceJapanese", Main.ForceJapanese, __instance);
            }
            if (JapaneseRoleName == null || JapaneseRoleName.ToggleButton == null)
            {
                JapaneseRoleName = ClientOptionItem.Create("JapaneseRoleName", Main.JapaneseRoleName, __instance);
            }
            if (UnloadMod == null || UnloadMod.ToggleButton == null)
            {
                UnloadMod = ClientActionItem.Create("UnloadMod", ModUnloaderScreen.Show, __instance);
            }
            if (DumpLog == null || DumpLog.ToggleButton == null)
            {
                DumpLog = ClientActionItem.Create("DumpLog", Utils.DumpLog, __instance);
            }

            //if (EnableCustomBGMEffect == null || EnableCustomBGMEffect.ToggleButton == null)
            //{
            //    EnableCustomBGMEffect = ClientOptionItem.Create("EnableCustomBGMEffect", Main.EnableCustomBGMEffect, __instance);
            //}

            if (EnableCustomSoundEffect == null || EnableCustomSoundEffect.ToggleButton == null)
            {
                EnableCustomSoundEffect = ClientOptionItem.Create("EnableCustomSoundEffect", Main.EnableCustomSoundEffect, __instance);
            }
            if (ModUnloaderScreen.Popup == null)
            {
                ModUnloaderScreen.Init(__instance);
            }
        }
    }

    [HarmonyPatch(typeof(OptionsMenuBehaviour), nameof(OptionsMenuBehaviour.Close))]
    public static class OptionsMenuBehaviourClosePatch
    {
        public static void Postfix()
        {
            if (ClientActionItem.CustomBackground != null)
            {
                ClientActionItem.CustomBackground.gameObject.SetActive(false);
            }
            ModUnloaderScreen.Hide();
        }
    }
}
