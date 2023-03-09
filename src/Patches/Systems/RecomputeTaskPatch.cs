using System;
using System.Linq;
using HarmonyLib;

namespace TOHTOR.Patches.Systems;

[HarmonyPatch(typeof(GameData), nameof(GameData.RecomputeTaskCounts))]
public class RecomputeTaskPatch
{
    public static bool Prefix(GameData __instance)
    {
        __instance.TotalTasks = 0;
        __instance.CompletedTasks = 0;
        __instance.AllPlayers.ToArray()
            .Where(Utils.HasTasks)
            .SelectMany(p => p?.Tasks?.ToArray() ?? Array.Empty<GameData.TaskInfo>())
            .Do(task =>
            {
                __instance.TotalTasks++;
                if (task.Complete) __instance.CompletedTasks++;
            });

        return false;
    }
}