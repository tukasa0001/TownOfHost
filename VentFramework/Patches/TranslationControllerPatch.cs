using HarmonyLib;
using VentLib.Localization;
using VentLib.Logging;

namespace VentLib.Patches;

[HarmonyPatch(typeof(TranslationController), nameof(TranslationController.SetLanguage))]
public class LanguageSetPatch
{
    private static void Postfix(TranslationController __instance, [HarmonyArgument(0)] TranslatedImageSet lang)
    {
        VentLogger.Info($"Loaded Language: {lang.languageID}");
        Localizer.CurrentLanguage = lang.languageID.ToString();
    }
}