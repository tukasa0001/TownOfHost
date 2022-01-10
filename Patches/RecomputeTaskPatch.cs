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
    [HarmonyPatch(typeof(GameData), nameof(GameData.RecomputeTaskCounts))]
    class CustomTaskCountsPatch {
        public static bool Prefix(GameData __instance) {
            __instance.TotalTasks = 0;
            __instance.CompletedTasks = 0;
            foreach(var p in __instance.AllPlayers) {
                var hasTasks = true;
                if(p.Disconnected) hasTasks = false;
                if(p.Role.Role == RoleTypes.Scientist && main.currentScientist == ScientistRole.Jester) hasTasks = false;
                if(p.Role.Role == RoleTypes.Engineer && main.currentEngineer == EngineerRole.Madmate) hasTasks = false;
                if(p.Role.Role == RoleTypes.Engineer && main.currentEngineer == EngineerRole.Terrorist) hasTasks = false;
                if(p.Role.TeamType == RoleTeamTypes.Impostor) hasTasks = false;
                if(p.IsDead && main.IsHideAndSeek) hasTasks = false;
                if(hasTasks) {
                    foreach(var task in p.Tasks) {
                        __instance.TotalTasks++;
                        if(task.Complete) __instance.CompletedTasks++;
                    }
                }
            }
            return false;
        }
    }
}