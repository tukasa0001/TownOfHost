using HarmonyLib;
using UnityEngine;
using System.Text.RegularExpressions;
using UnityEngine.SceneManagement;
using System;

namespace TownOfHost
{
    [HarmonyPatch(typeof(JoinGameButton), nameof(JoinGameButton.OnClick))]
    class JoinGameButtonPatch
    {
        public static void Prefix(JoinGameButton __instance)
        {
            if (__instance.GameIdText == null) return;
            if (Regex.IsMatch(GUIUtility.systemCopyBuffer, @"[A-Z]{6}"))
            {
                Logger.info($"{GUIUtility.systemCopyBuffer}", "ClipBoard");
                __instance.GameIdText.SetText(GUIUtility.systemCopyBuffer);
            }
        }
    }
}