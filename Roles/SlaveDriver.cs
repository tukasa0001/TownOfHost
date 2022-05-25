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
        static readonly int Id = 2700;

        public static void SetupCustomOption()
        {
            Options.SetupRoleOptions(Id, CustomRoles.SlaveDriver);
        }
        public static void SlaveDriverKillTargetTaskCheck(PlayerControl killer, byte playerId)
        {
            var taskState = PlayerState.taskState?[playerId];
            int TaskHalfValue = taskState.AllTasksCount / 2;
            if (taskState.CompletedTasksCount <= TaskHalfValue)//キル対象の完了タスク数が設定タスク数の半分か、それ以下
            {
                Logger.Info($"SlaveDriver Kill 1", "SlaveDriver");
                Main.AllPlayerKillCooldown[killer.PlayerId] = Options.BHDefaultKillCooldown.GetFloat() * 1.5f;
            }
            if (taskState.CompletedTasksCount > TaskHalfValue)//キル対象の完了タスク数が設定タスク数の半分を超えている
            {
                Logger.Info($"SlaveDriver Kill 2", "SlaveDriver");
                Main.AllPlayerKillCooldown[killer.PlayerId] = Options.BHDefaultKillCooldown.GetFloat() / 1.4f;
            }
            if (taskState.IsTaskFinished)//キル対象がタスクを終えている
            {
                Logger.Info($"SlaveDriver Kill 3", "SlaveDriver");
                Main.AllPlayerKillCooldown[killer.PlayerId] = Options.BHDefaultKillCooldown.GetFloat() / 2f;
            }
            if (taskState.hasTasks == false)//キル対象のタスクがない
                Main.AllPlayerKillCooldown[killer.PlayerId] = Options.BHDefaultKillCooldown.GetFloat();
            killer.CustomSyncSettings();//負荷軽減するため、__instanceだけがCustomSyncSettingsを実行
        }
    }
}