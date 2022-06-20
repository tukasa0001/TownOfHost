using UnityEngine;
using System.Collections.Generic;

namespace TownOfHost
{
    public static class SlaveDriver
    {
        static readonly int Id = 2700;
        static List<byte> playerIdList = new();

        static CustomOption SlaveDriverIncreaseKC;
        static CustomOption SlaveDriverDecreaseKC;
        static CustomOption SlaveDriverTaskCompleteDecreaseKC;
        static CustomOption SlaveDriverNoTaskKC;

        public static void SetupCustomOption()
        {
            Options.SetupRoleOptions(Id, CustomRoles.SlaveDriver);
            SlaveDriverIncreaseKC = CustomOption.Create(Id + 10, Color.white, "SlaveDriverIncreaseKC", 1.5f, 0.25f, 5f, 0.25f, Options.CustomRoleSpawnChances[CustomRoles.SlaveDriver]);
            SlaveDriverDecreaseKC = CustomOption.Create(Id + 11, Color.white, "SlaveDriverDecreaseKC", 1.5f, 0.25f, 5f, 0.25f, Options.CustomRoleSpawnChances[CustomRoles.SlaveDriver]);
            SlaveDriverTaskCompleteDecreaseKC = CustomOption.Create(Id + 12, Color.white, "SlaveDriverTaskCompleteDecreaseKC", 2f, 0.25f, 5f, 0.25f, Options.CustomRoleSpawnChances[CustomRoles.SlaveDriver]);
            SlaveDriverNoTaskKC = CustomOption.Create(Id + 13, Color.white, "SlaveDriverNoTaskKC", 30f, 0.25f, 100f, 0.25f, Options.CustomRoleSpawnChances[CustomRoles.SlaveDriver]);
        }
        public static void Init()
        {
            playerIdList = new();
        }
        public static void Add(byte playerId)
        {
            playerIdList.Add(playerId);
        }
        public static bool IsEnable()
        {
            return playerIdList.Count > 0;
        }
        public static void KillTargetTaskCheck(PlayerControl killer, byte playerId)
        {
            var taskState = PlayerState.taskState?[playerId];
            int TaskHalfValue = taskState.AllTasksCount / 2;//TaskHalfValueは設定されたタスク数÷2で出た数値を繰り上げした数値
            if (taskState.CompletedTasksCount <= TaskHalfValue)//キル対象の完了タスク数が設定タスク数の半分か、それ以下
            {
                Logger.Info($"SlaveDriver Kill 1", "SlaveDriver");
                Main.AllPlayerKillCooldown[killer.PlayerId] = Options.DefaultKillCooldown * SlaveDriverIncreaseKC.GetFloat();
            }
            if (taskState.CompletedTasksCount > TaskHalfValue)//キル対象の完了タスク数が設定タスク数の半分を超えている
            {
                Logger.Info($"SlaveDriver Kill 2", "SlaveDriver");
                Main.AllPlayerKillCooldown[killer.PlayerId] = Options.DefaultKillCooldown / SlaveDriverDecreaseKC.GetFloat();
            }
            if (taskState.IsTaskFinished)//キル対象がタスクを終えている
            {
                Logger.Info($"SlaveDriver Kill 3", "SlaveDriver");
                Main.AllPlayerKillCooldown[killer.PlayerId] = Options.DefaultKillCooldown / SlaveDriverTaskCompleteDecreaseKC.GetFloat();
            }
            if (taskState.hasTasks == false)//キル対象のタスクがない
                Main.AllPlayerKillCooldown[killer.PlayerId] = SlaveDriverNoTaskKC.GetFloat();
            killer.CustomSyncSettings();//負荷軽減するため、__instanceだけがCustomSyncSettingsを実行
        }
    }
}