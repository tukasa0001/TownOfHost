using System;
using HarmonyLib;
using UnityEngine;

namespace TownOfHost
{
    [HarmonyPatch]
    public class MainMenuManagerPatch
    {
        public static GameObject template;
        [HarmonyPatch(typeof(MainMenuManager), nameof(MainMenuManager.Start)), HarmonyPrefix]
        public static void Start_Prefix(MainMenuManager __instance)
        {
            if (template == null) template = GameObject.Find("/MainUI/ExitGameButton");
            if (template == null) return;

        }
    }
}