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
            if(!AmongUsClient.Instance.AmHost) return false;
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
                            if(p.myTasks.Count-ct <= main.SnichExposeTaskLeft)
                            {
                                var found = main.AllPlayerCustomRoles.TryGetValue(t.PlayerId, out var role);
                                string RoleName = "STRMISS";
                                if(found) RoleName = main.getRoleName(role);
                                t.RpcSetNamePrivate("<size=1.5>" + RoleName + "</size>\r\n" + t.name + "<color=#90ee90>★</color>" , true, t);
                                p.RpcSetNamePrivate("<color=#90ee90>"+ p.name + "</color>" , false, t);
                            }
                        }
                    }
                }
            }
            return false;
        }
    }
}
