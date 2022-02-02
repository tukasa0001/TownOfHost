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
                string taskText = main.getTaskText(p.Data.Tasks);
                if(p == main.b_target){
                    foreach(var t in PlayerControl.AllPlayerControls){
                        if(t.isBountyHunter()){
                            t.RpcSetNamePrivate($"<color={main.getRoleColorCode(t.getCustomRole())}><size=1.5>{main.getRoleName(t.getCustomRole())}</size>\r\n{t.name}</color>\r\n<size=1.5>{main.b_target.name}<size>" , false, t);
                        }
                    }
                }
                if(main.hasTasks(p.Data))
                {
                    p.RpcSetNamePrivate($"<color={main.getRoleColorCode(p.getCustomRole())}><size=1.5>{main.getRoleName(p.getCustomRole())}</size>\r\n{p.name}</color><color=#ffff00>({taskText})</color>" , true, p);
                    if(p.AllTasksCompleted() && p.isSnitch()){
                        foreach(var t in PlayerControl.AllPlayerControls)
                        {
                            if(t.isImpostor() || t.isShapeshifter() || t.isVampire() || t.isBountyHunter() || t.isWarlock())
                            {
                                t.RpcSetNamePrivate($"<color={main.getRoleColorCode(t.getCustomRole())}>{t.name}</color>" , false, p);
                            }
                        }
                    }
                }else{
                    if(p.isImpostor() || p.isShapeshifter() || p.isVampire() || p.isBountyHunter() || p.isWarlock())
                    {
                        foreach(var t in PlayerControl.AllPlayerControls)
                        {
                            var ct = 0;
                            foreach(var task in t.myTasks) if(task.IsComplete)ct++;
                            if(t.myTasks.Count-ct <= main.SnitchExposeTaskLeft && !t.Data.IsDead && t.isSnitch())
                            {
                                p.RpcSetNamePrivate($"<color={main.getRoleColorCode(p.getCustomRole())}><size=1.5>{main.getRoleName(p.getCustomRole())}</size>\r\n{p.name}</color><color={main.getRoleColorCode(CustomRoles.Snitch)}>★</color>" , true, p);
                                t.RpcSetNamePrivate($"<color={main.getRoleColorCode(CustomRoles.Snitch)}>{t.name}</color>" , false, p);
                            }
                        }
                    }
                }
            }
            return false;
        }
    }
}
