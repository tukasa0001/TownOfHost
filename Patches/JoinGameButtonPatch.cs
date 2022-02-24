using BepInEx;
using BepInEx.Configuration;
using BepInEx.IL2CPP;
using Hazel;
using System;
using HarmonyLib;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnhollowerBaseLib;
using System.Text.RegularExpressions;

namespace TownOfHost
{
    [HarmonyPatch(typeof(JoinGameButton), nameof(JoinGameButton.OnClick))]
    class JoinGameButtonPatch
    {
        public static void Prefix(JoinGameButton __instance)
        {
            if(__instance.GameIdText == null) return;
            if(Regex.IsMatch(GUIUtility.systemCopyBuffer, @"[A-Z]{6}"))
            {
                Logger.info($"{GUIUtility.systemCopyBuffer}");
                __instance.GameIdText.SetText(GUIUtility.systemCopyBuffer);
            }
        }
    }
    [HarmonyPatch(typeof(AccountManager), nameof(AccountManager.CanPlayOnline))]
    class CanPlayOnlinePatch
    {
        public static bool Prefix(AccountManager __instance)
        {
            var leftTime =  main.BanTimestamp.Value+60*60 - (int)((DateTime.UtcNow.Ticks - DateTime.Parse("1970-01-01 00:00:00").Ticks)/10000000);
            if(leftTime > 0 && main.BanTimestamp.Value != -1)
            {
                Logger.info($"BAN解除まで{leftTime}秒");
                return false;
            }
            return true;
        }
    }
}