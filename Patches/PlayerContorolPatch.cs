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

namespace TownOfHost {
    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.MurderPlayer))]
    class PlayerControlPatch {
        public static void Postfix(PlayerControl __instance, [HarmonyArgument(0)]PlayerControl target) {
            //When Bait is killed
            if(target.Data.Role.Role == RoleTypes.Scientist && main.currentScientist == ScientistRole.Bait) {
                __instance.CmdReportDeadBody(target.Data);
            }
        }
    }
}