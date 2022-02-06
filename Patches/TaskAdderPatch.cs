using System.Diagnostics;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.IL2CPP;
using System;
using HarmonyLib;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnhollowerBaseLib;
using TownOfHost;
using Hazel;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using InnerNet;

namespace TownOfHost {
    [HarmonyPatch(typeof(TaskAdderGame), nameof(TaskAdderGame.ShowFolder))]
    class ShowFolderPatch {
        public static void Postfix(TaskAdderGame __instance) {
            if(__instance.Hierarchy.Count == 1) {
                TaskAddButton button = UnityEngine.Object.Instantiate<TaskAddButton>(__instance.RoleButton);
                button.Text.text = "EMPTY BOTTLE";
                float xCursor = 0.0f;
                float yCursor = 15f;
                float maxHeight = 15f;
                __instance.AddFileAsChild(__instance.Root, button, ref xCursor, ref yCursor, ref maxHeight);
            }
        }
    }
}