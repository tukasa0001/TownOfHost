using System.Collections.Generic;
using UnityEngine;

using static TownOfHost.Options;

namespace TownOfHost.Roles.AddOns.Crewmate
{
    public static class Workhorse
    {
        private static readonly int Id = 80100;
        public static Color RoleColor = Utils.GetRoleColor(CustomRoles.Workhorse);
        public static List<byte> playerIdList = new();
        private static OptionItem OptionAssignOnlyToCrewmate;
        private static OptionItem OptionNumLongTasks;
        private static OptionItem OptionNumShortTasks;
        public static bool AssignOnlyToCrewmate;
        public static int NumLongTasks;
        public static int NumShortTasks;
        public static void SetupCustomOption()
        {
            SetupRoleOptions(Id, TabGroup.Addons, CustomRoles.Workhorse);
            OptionAssignOnlyToCrewmate = BooleanOptionItem.Create(Id + 10, "AssignOnlyTo%role%", true, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Workhorse]);
            OptionAssignOnlyToCrewmate.ReplacementDictionary = new Dictionary<string, string> { { "%role%", Utils.ColorString(Palette.CrewmateBlue, Utils.GetRoleName(CustomRoles.Crewmate)) } };
            OptionNumLongTasks = IntegerOptionItem.Create(Id + 11, "WorkhorseNumLongTasks", new(0, 5, 1), 1, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Workhorse])
                .SetValueFormat(OptionFormat.Pieces);
            OptionNumShortTasks = IntegerOptionItem.Create(Id + 12, "WorkhorseNumShortTasks", new(0, 5, 1), 1, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Workhorse])
                .SetValueFormat(OptionFormat.Pieces);
        }
        public static void Init()
        {
            playerIdList = new();

            AssignOnlyToCrewmate = OptionAssignOnlyToCrewmate.GetBool();
            NumLongTasks = OptionNumLongTasks.GetInt();
            NumShortTasks = OptionNumShortTasks.GetInt();
        }
        public static void Add(byte playerId)
        {
            playerIdList.Add(playerId);
        }
        public static bool IsEnable => playerIdList.Count > 0;
        public static bool IsThisRole(byte playerId) => playerIdList.Contains(playerId);
        public static (bool, int, int) TaskData => (false, NumLongTasks, NumShortTasks);
        private static bool IsAssignTarget(PlayerControl pc)
        {
            if (!pc.IsAlive() || IsThisRole(pc.PlayerId)) return false;
            var taskState = pc.GetPlayerTaskState();
            if (taskState.CompletedTasksCount + 1 < taskState.AllTasksCount) return false;
            if (AssignOnlyToCrewmate) //クルーメイトのみ
                return pc.Is(CustomRoles.Crewmate);
            return Utils.HasTasks(pc.Data) //タスクがある
                && !OverrideTasksData.AllData.ContainsKey(pc.GetCustomRole()); //タスク上書きオプションが無い
        }
        public static bool OnCompleteTask(PlayerControl pc)
        {
            if (!CustomRoles.Workhorse.IsEnable() || playerIdList.Count >= CustomRoles.Workhorse.GetCount()) return false;
            if (!IsAssignTarget(pc)) return false;

            pc.RpcSetCustomRole(CustomRoles.Workhorse);
            var taskState = pc.GetPlayerTaskState();
            taskState.AllTasksCount += NumLongTasks + NumShortTasks;
            taskState.CompletedTasksCount++; //今回の完了分加算

            if (AmongUsClient.Instance.AmHost)
            {
                Add(pc.PlayerId);
                GameData.Instance.RpcSetTasks(pc.PlayerId, new byte[0]); //タスクを再配布
                pc.SyncSettings();
                Utils.NotifyRoles();
            }

            return true;
        }
    }
}