using HarmonyLib;
using UnityEngine;

namespace TOHE;

//À´Ô´£ºhttps://github.com/tukasa0001/TownOfHost/pull/1265/files
[HarmonyPatch(typeof(OptionsMenuBehaviour), nameof(OptionsMenuBehaviour.Start))]
public static class OptionsMenuBehaviourStartPatch
{
    private static ClientOptionItem UnlockFPS;
    private static ClientOptionItem AutoStart;
    private static ClientOptionItem DisableTOHE;

    public static void Postfix(OptionsMenuBehaviour __instance)
    {
        if (__instance.DisableMouseMovement == null)
        {
            return;
        }

        Main.DisableTOHE.Value = false;
        if (!Main.SetAutoStartToDisable)
        {
            Main.AutoStart.Value = false;
            Main.SetAutoStartToDisable = true;
        }

        if (UnlockFPS == null || UnlockFPS.ToggleButton == null)
        {
            UnlockFPS = ClientOptionItem.Create("UnlockFPS", Main.UnlockFPS, __instance, UnlockFPSButtonToggle);
            static void UnlockFPSButtonToggle()
            {
                Application.targetFrameRate = Main.UnlockFPS.Value ? 165 : 60;
                Logger.SendInGame(string.Format(Translator.GetString("FPSSetTo"), Application.targetFrameRate));
            }
        }
        if (AutoStart == null || AutoStart.ToggleButton == null)
        {
            AutoStart = ClientOptionItem.Create("AutoStart", Main.AutoStart, __instance, AutoStartButtonToggle);
            static void AutoStartButtonToggle()
            {
                if (Main.AutoStart.Value == false && GameStates.IsCountDown)
                {
                    GameStartManager.Instance.ResetStartState();
                    Logger.SendInGame(Translator.GetString("CancelStartCountDown"));
                }
            }
        }
        if (DisableTOHE == null || DisableTOHE.ToggleButton == null)
        {
            DisableTOHE = ClientOptionItem.Create("DisableTOHE", Main.DisableTOHE, __instance, DisableTOHEButtonToggle);
            static void DisableTOHEButtonToggle()
            {
                Harmony.UnpatchAll();
                Main.Instance.Unload();
            }
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