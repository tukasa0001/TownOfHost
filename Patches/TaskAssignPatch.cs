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
    [HarmonyPatch(typeof(ShipStatus), nameof(ShipStatus.AddTasksFromList))]
    class AddTasksFromListPatch
    {
        public static void Prefix(ShipStatus __instance,
        [HarmonyArgument(4)] Il2CppSystem.Collections.Generic.List<NormalPlayerTask> unusedTasks)
        {
            List<int> disabledTaskIndex = new List<int>();
            for (var i = 0; i < unusedTasks.Count; i++)
            {
                var task = unusedTasks[i];
                if (task.TaskType == TaskTypes.SubmitScan && main.DisableSwipeCard) disabledTaskIndex.Add(i);//スキャンタスク
                if (task.TaskType == TaskTypes.SwipeCard && main.DisableSubmitScan) disabledTaskIndex.Add(i);//カードタスク
                if (task.TaskType == TaskTypes.UnlockSafe && main.DisableUnlockSafe) disabledTaskIndex.Add(i);//金庫タスク
                if (task.TaskType == TaskTypes.UploadData && main.DisableUploadData) disabledTaskIndex.Add(i);//ダウンロードタスク
            }
            foreach (var i in disabledTaskIndex)
            {
                unusedTasks.RemoveAt(i);
            }
        }
    }
}
