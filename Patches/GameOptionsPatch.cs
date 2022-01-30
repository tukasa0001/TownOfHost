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
using TownOfHost;
using Il2CppSystem;

namespace TownOfHost
{
    [HarmonyPatch(typeof(RoleOptionSetting), nameof(RoleOptionSetting.UpdateValuesAndText))]
    class ChanceChangePatch {
        public static void Postfix(RoleOptionSetting __instance) {
            bool forced = false;
            if(__instance.Role.Role == RoleTypes.Engineer) {
                if(main.RoleCounts[CustomRoles.Madmate] > 0) forced = true;
                if(main.RoleCounts[CustomRoles.Terrorist] > 0) forced = true;
            }
            if(__instance.Role.Role == RoleTypes.Shapeshifter) {
                if(main.RoleCounts[CustomRoles.Sidekick] > 0) forced = true;
            }

            if(forced) {
                ((TMPro.TMP_Text)__instance.ChanceText).text = "Always";
            }
        }
    }
}