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
            int TaskHalfValue = taskState.AllTasksCount / 2;


            if (taskState.CompletedTasksCount <= TaskHalfValue)//キル対象の完了タスク数が設定タスク数の半分か、それ以下
            {
                Main.AllPlayerKillCooldown[__instance.PlayerId] = Options.BHDefaultKillCooldown.GetFloat() * 1.5f;
            }
            if (taskState.CompletedTasksCount > TaskHalfValue)//キル対象の完了タスク数が設定タスク数の半分を超えている
            {
                Main.AllPlayerKillCooldown[__instance.PlayerId] = Options.BHDefaultKillCooldown.GetFloat() / 1.4f;
            }
            if (taskState.IsTaskFinished)//キル対象がタスクを終えている
            {
                Main.AllPlayerKillCooldown[__instance.PlayerId] = Options.BHDefaultKillCooldown.GetFloat() / 1.9f;
            }
        }
    }
}