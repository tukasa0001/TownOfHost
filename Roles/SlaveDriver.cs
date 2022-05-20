using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HarmonyLib;
using Hazel;

namespace TownOfHost
{
    public static class SlaveDriver
    {
        static int Id = 2700;

        public static void SetupCustomOption()
        {
            Options.SetupRoleOptions(Id, CustomRoles.SlaveDriver);
        }
        public static void SlaveDriverKillTargetTaskCheck(PlayerControl __instance, byte playerId, PlayerControl target)
        {
            var taskState = PlayerState.taskState?[playerId];

            float targetPlayerTaskGage = taskState.CompletedTasksCount / taskState.AllTasksCount;
            int totalTaskNum = GameData.Instance.TotalTasks;
            int compTaskNum = GameData.Instance.CompletedTasks;
            float totalTaskGauge = compTaskNum / totalTaskNum;
            float diff = totalTaskGauge - targetPlayerTaskGage;


            if ((0.5f < diff))
            {
                Main.AllPlayerKillCooldown[__instance.PlayerId] *= 2;
            }
            if (taskState.IsTaskFinished)
            {
                Main.AllPlayerKillCooldown[__instance.PlayerId] /= 3;
            }
            if (taskState.hasTasks == false)
            {
                Main.AllPlayerKillCooldown[__instance.PlayerId] *= 3;
            }
        }
    }
}