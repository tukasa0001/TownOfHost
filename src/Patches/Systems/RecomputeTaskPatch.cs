using System.Linq;
using HarmonyLib;

namespace TownOfHost.Patches.Systems;

[HarmonyPatch(typeof(GameData), nameof(GameData.RecomputeTaskCounts))]
public class RecomputeTaskPatch
{
    public static bool Prefix(GameData __instance)
    {
        __instance.TotalTasks = 0;
        __instance.CompletedTasks = 0;
        __instance.AllPlayers.ToArray()
            .Where(p => p != null && Utils.HasTasks(p))
            .SelectMany(p => p.Tasks.ToArray())
            .Do(task =>
            {
                __instance.TotalTasks++;
                if (task.Complete) __instance.CompletedTasks++;
            });

        return false;
    }
}