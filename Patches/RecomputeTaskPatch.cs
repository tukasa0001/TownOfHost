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

namespace TownOfHost
{
    [HarmonyPatch(typeof(GameData), nameof(GameData.RecomputeTaskCounts))]
    class CustomTaskCountsPatch
    {
        public static bool Prefix(GameData __instance)
        {
            __instance.TotalTasks = 0;
            __instance.CompletedTasks = 0;
            foreach (var p in __instance.AllPlayers)
            {
                var hasTasks = main.hasTasks(p);
                if (hasTasks)
                {
                    foreach (var task in p.Tasks)
                    {
                        __instance.TotalTasks++;
                        if (task.Complete) __instance.CompletedTasks++;
                    }
                }
            }
            foreach(PlayerControl p in PlayerControl.AllPlayerControls)
            {
                if(main.isSnitch(p)){
                    foreach(var t in PlayerControl.AllPlayerControls)
                    {
                        if(t.Data.Role.IsImpostor)
                        {
                            if(p.AllTasksCompleted()) t.RpcSetNamePrivate("<color=#ff0000>"+ t.name + "</color>" , false, p);
                            var ct = 0;
                            foreach(var task in p.myTasks)
                            {
                                if(task.IsComplete)ct++;
                            }
                            if(p.myTasks.Count-ct <= main.SnichExposeTaskLeft)p.RpcSetNamePrivate("<color=#90ee90>"+ p.name + "</color>" , false, t);
                        }
                    }
                }
            }
            return false;
        }
    }
}
