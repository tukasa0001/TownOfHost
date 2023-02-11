using System.Collections.Generic;
using UnityEngine;
using static TownOfHost.Options;

namespace TownOfHost.Roles.AddOns.Crewmate
{
    public static class Workhorse
    {
        private static readonly int Id = 80100;
        private static Color RoleColor = Utils.GetRoleColor(CustomRoles.Workhorse);
        private static readonly List<CustomRoles> NoAssignList = new()
        {
            CustomRoles.Lighter,
            CustomRoles.TimeManager,
        };
        public static List<byte> playerIdList = new();
        public static OptionItem AssignOnlyToCrewmate;
        public static OptionItem NumLongTasks;
        public static OptionItem NumShortTasks;
        public static void SetupCustomOption()
        {
            SetupRoleOptions(Id, TabGroup.Addons, CustomRoles.Workhorse);
            AssignOnlyToCrewmate = BooleanOptionItem.Create(Id + 10, "AssignOnlyTo%role%", true, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Workhorse]);
            AssignOnlyToCrewmate.ReplacementDictionary = new Dictionary<string, string> { { "%role%", Utils.ColorString(Palette.CrewmateBlue, Utils.GetRoleName(CustomRoles.Crewmate)) } };
            NumLongTasks = IntegerOptionItem.Create(Id + 11, "WorkhorseNumLongTasks", new(0, 5, 1), 1, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Workhorse])
                .SetValueFormat(OptionFormat.Pieces);
            NumShortTasks = IntegerOptionItem.Create(Id + 12, "WorkhorseNumShortTasks", new(0, 5, 1), 1, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Workhorse])
                .SetValueFormat(OptionFormat.Pieces);
        }
        public static void Init()
        {
            playerIdList = new();
        }
        public static void Add(byte playerId)
        {
            playerIdList.Add(playerId);
        }
        public static bool IsEnable => playerIdList.Count > 0;
        public static bool IsThisRole(byte playerId) => playerIdList.Contains(playerId);
        public static (bool, int, int) TaskData => (false, NumLongTasks.GetInt(), NumShortTasks.GetInt());
        private static bool IsAssignTarget(PlayerControl pc)
        {
            if (!pc.IsAlive() || IsThisRole(pc.PlayerId)) return false;
            var taskState = pc.GetPlayerTaskState();
            if (taskState.CompletedTasksCount + 1 < taskState.AllTasksCount) return false;
            if (AssignOnlyToCrewmate.GetBool()) //クルーメイトのみ
                return pc.Is(CustomRoles.Crewmate);
            return Utils.HasTasks(pc.Data) //タスクがある
                && !OverrideTasksData.AllData.ContainsKey(pc.GetCustomRole()) //タスク上書きオプションが無い
                && !NoAssignList.Contains(pc.GetCustomRole()); //タスクトリガーのある役職でない
        }
        public static bool OnCompleteTask(PlayerControl pc)
        {
            if (!CustomRoles.Workhorse.IsEnable() || playerIdList.Count >= CustomRoles.Workhorse.GetCount()) return false;
            if (!IsAssignTarget(pc)) return false;

            pc.RpcSetCustomRole(CustomRoles.Workhorse);
            Add(pc.PlayerId);
            GameData.Instance.RpcSetTasks(pc.PlayerId, new byte[0]); //タスクを再配布
            Main.PlayerStates[pc.PlayerId].InitTask(pc); //TaskStatesをリセット
            pc.SyncSettings();
            Utils.NotifyRoles();

            return true;
        }
        public static (Color, int, int) GetTaskTextData(TaskState taskState)
        {
            var opt = Main.NormalOptions;
            int NumFormerTasks = opt.NumCommonTasks + opt.NumLongTasks + opt.NumShortTasks;
            int NumCompleted = NumFormerTasks + taskState.CompletedTasksCount;
            int NumAllTasks = NumFormerTasks + taskState.AllTasksCount;

            Color color = taskState.IsTaskFinished ? Color.green : RoleColor;
            return (color, NumCompleted, NumAllTasks);
        }
    }
}