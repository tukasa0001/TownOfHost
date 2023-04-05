using HarmonyLib;

namespace TownOfHost
{
    [HarmonyPatch(typeof(OptionsMenuBehaviour), nameof(OptionsMenuBehaviour.Start))]
    public static class OptionsMenuBehaviourStartPatch
    {
        private static ClientOptionItem ForceJapanese;
        private static ClientOptionItem JapaneseRoleName;

        public static void Postfix(OptionsMenuBehaviour __instance)
        {
            if (__instance.DisableMouseMovement == null)
            {
                return;
            }

            if (ForceJapanese == null || ForceJapanese.ToggleButton == null)
            {
                ForceJapanese = ClientOptionItem.Create(
                    Translator.GetString("ForceJapanese"),
                    "ForceJapanese",
                    Main.ForceJapanese,
                    __instance);
            }
            if (JapaneseRoleName == null || JapaneseRoleName.ToggleButton == null)
            {
                JapaneseRoleName = ClientOptionItem.Create(
                    Translator.GetString("JapaneseRoleName"),
                    "JapaneseRoleName",
                    Main.JapaneseRoleName,
                    __instance);
            }
        }
    }

    [HarmonyPatch(typeof(OptionsMenuBehaviour), nameof(OptionsMenuBehaviour.Close))]
    public static class OptionsMenuBehaviourClosePatch
    {
        public static void Postfix()
        {
            if (ClientOptionItem.CustomBackground != null)
            {
                ClientOptionItem.CustomBackground.gameObject.SetActive(false);
            }
        }
    }
}
