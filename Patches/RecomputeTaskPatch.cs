using HarmonyLib;

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
                if(p == null) continue;
                var hasTasks = Utils.hasTasks(p);
                if (hasTasks)
                {
//                    if(p.Tasks == null) {
//                        Logger.warn("警告:" + p.PlayerName + "のタスクがnullです");
//                        continue;//これより下を実行しない
//                    }
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
    class CompleteTaskPatch {
        public static void Postfix(GameData __instance) {
            if(!AmongUsClient.Instance.AmHost) return;

            Utils.NotifyRoles();
        }
    }
}
