using HarmonyLib;

namespace TownOfHostForE
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
                if (p == null) continue;
                var hasTasks = Utils.HasTasks(p) && PlayerState.GetByPlayerId(p.PlayerId).GetTaskState().AllTasksCount > 0;
                if (hasTasks)
                {
                    // if (p.Tasks == null)
                    // {
                    //     Logger.warn("警告:" + p.PlayerName + "のタスクがnullです");
                    //     continue;//これより下を実行しない
                    // }
                    foreach (var task in p.Tasks)
                    {
                        __instance.TotalTasks++;
                        if (task.Complete) __instance.CompletedTasks++;
                    }
                }
            }

            return false;
        }
    }
    [HarmonyPatch(typeof(GameData), nameof(GameData.CompleteTask))]
    class CompleteTaskPatch
    {
        public static bool Prefix(PlayerControl pc, uint taskId)
        {
            GameData.TaskInfo taskById = pc.Data.FindTaskById(taskId);
            if (taskById != null)
            {
                if (!taskById.Complete)
                {
                    taskById.Complete = true;
                    if (Utils.HasTasks(pc.Data))
                        ++GameData.Instance.CompletedTasks;
                    Logger.Info($"{pc?.name} {Utils.HasTasks(pc?.Data)} TotalTaskCounts = {GameData.Instance.CompletedTasks}/{GameData.Instance.TotalTasks}", "CompleteTaskPatch.Prefix");
                }
            }

            return false;
        }
    }
}