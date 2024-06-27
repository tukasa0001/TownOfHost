using System;
using System.Collections.Generic;
using UnityEngine;

using TownOfHost.Attributes;
using TownOfHost.Roles.Core;
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
            SetupRoleOptions(Id, TabGroup.Addons, CustomRoles.Workhorse, RoleColor);
            OptionAssignOnlyToCrewmate = BooleanOptionItem.Create(Id + 10, "AssignOnlyTo%role%", true, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Workhorse]);
            OptionAssignOnlyToCrewmate.ReplacementDictionary = new Dictionary<string, string> { { "%role%", Utils.ColorString(Palette.CrewmateBlue, Utils.GetRoleName(CustomRoles.Crewmate)) } };
            OptionNumLongTasks = IntegerOptionItem.Create(Id + 11, "WorkhorseNumLongTasks", new(0, 5, 1), 1, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Workhorse])
                .SetValueFormat(OptionFormat.Pieces);
            OptionNumShortTasks = IntegerOptionItem.Create(Id + 12, "WorkhorseNumShortTasks", new(0, 5, 1), 1, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Workhorse])
                .SetValueFormat(OptionFormat.Pieces);
        }
        [GameModuleInitializer]
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
            if (taskState.CompletedTasksCount < taskState.AllTasksCount) return false;
            if (!Utils.HasTasks(pc.Data)) return false;
            if (AssignOnlyToCrewmate) //クルーメイトのみ
                return pc.Is(CustomRoles.Crewmate);
            return !OverrideTasksData.AllData.ContainsKey(pc.GetCustomRole()); //タスク上書きオプションが無い
        }
        public static bool OnCompleteTask(PlayerControl pc)
        {
            if (!CustomRoles.Workhorse.IsPresent() || playerIdList.Count >= CustomRoles.Workhorse.GetRealCount()) return true;
            if (!IsAssignTarget(pc)) return true;

            pc.RpcSetCustomRole(CustomRoles.Workhorse);
            var taskState = pc.GetPlayerTaskState();
            taskState.AllTasksCount += NumLongTasks + NumShortTasks;

            if (AmongUsClient.Instance.AmHost)
            {
                Add(pc.PlayerId);
                pc.Data.RpcSetTasks(Array.Empty<byte>()); //タスクを再配布
                pc.SyncSettings();
                Utils.NotifyRoles();
            }

            return false;
        }
    }
}