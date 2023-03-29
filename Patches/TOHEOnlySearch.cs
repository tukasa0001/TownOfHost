using AmongUs.Data;
using HarmonyLib;

namespace TOHE;

// ²Î¿¼£ºhttps://github.com/ykundesu/SuperNewRoles/blob/master/SuperNewRoles/Patches/SNROnlySearch.cs
public static class TOHEOnlySearch
{
    public const string FilterText = "TOHE";
    [HarmonyPatch(typeof(FilterTagManager), nameof(FilterTagManager.RefreshTags))]
    public static class FilterTagManagerPatch
    {
        public static void Postfix()
        {
            DataManager.Settings.Multiplayer.ValidGameFilterOptions.FilterTags.Add(FilterText);
        }
    }

    [HarmonyPatch(typeof(FilterTagsMenu), nameof(FilterTagsMenu.ChooseOption))]
    public static class FilterTagsMenuChooseOptionPatch
    {
        public static void Postfix(FilterTagsMenu __instance, ChatLanguageButton button, string filter)
        {
            if (__instance.targetOpts.FilterTags.Contains(FilterText))
            {
                if (filter == FilterText)
                {
                    __instance.targetOpts.FilterTags = new();
                    __instance.targetOpts.FilterTags.Add(FilterText);
                    foreach (var btn in __instance.controllerSelectable)
                    {
                        btn.GetComponent<ChatLanguageButton>().SetSelected(false);
                    }
                    button.SetSelected(true);
                }
                else
                {
                    __instance.targetOpts.FilterTags.Remove(FilterText);
                    foreach (var btn in __instance.controllerSelectable)
                    {
                        ChatLanguageButton LangBtn = btn.GetComponent<ChatLanguageButton>();
                        if (LangBtn.Text.text == FilterText)
                            LangBtn.SetSelected(false);
                    }
                }
                __instance.UpdateButtonText();
            }
        }
    }
}