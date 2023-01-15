using System.Collections.Generic;
using HarmonyLib;
using Sentry.Unity.NativeUtils;
using UnityEngine;

namespace TownOfHost
{
    public static class Snitch
    {
        private static readonly int Id = 20500;
        private static List<byte> playerIdList = new();
        private static Color RoleColor = Utils.GetRoleColor(CustomRoles.Snitch);

        private static OptionItem OptionEnableTargetArrow;
        private static OptionItem OptionCanGetColoredArrow;
        private static OptionItem OptionCanFindNeutralKiller;
        private static OptionItem OptionRemainingTasks;

        private static bool EnableTargetArrow;
        private static bool CanGetColoredArrow;
        private static bool CanFindNeutralKiller;
        private static int RemainingTasksToBeFound;
        public static void SetupCustomOption()
        {
            Options.SetupRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.Snitch);
            OptionEnableTargetArrow = BooleanOptionItem.Create(Id + 10, "SnitchEnableTargetArrow", false, TabGroup.CrewmateRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Snitch]);
            OptionCanGetColoredArrow = BooleanOptionItem.Create(Id + 11, "SnitchCanGetArrowColor", false, TabGroup.CrewmateRoles, false).SetParent(OptionEnableTargetArrow);
            OptionCanFindNeutralKiller = BooleanOptionItem.Create(Id + 12, "SnitchCanFindNeutralKiller", false, TabGroup.CrewmateRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Snitch]);
            OptionRemainingTasks = IntegerOptionItem.Create(Id + 13, "SnitchRemainingTaskFound", new(0, 10, 1), 1, TabGroup.CrewmateRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Snitch]);
            Options.OverrideTasksData.Create(Id + 20, TabGroup.CrewmateRoles, CustomRoles.Snitch);
        }
        public static void Init()
        {
            playerIdList = new();
            EnableTargetArrow = OptionEnableTargetArrow.GetBool();
            CanGetColoredArrow = OptionCanGetColoredArrow.GetBool();
            CanFindNeutralKiller = OptionCanFindNeutralKiller.GetBool();
            RemainingTasksToBeFound = OptionRemainingTasks.GetInt();
        }

        public static void Add(byte playerId) => playerIdList.Add(playerId);
        public static bool IsEnable => playerIdList.Count > 0;
        public static bool IsThisRole(byte playerId) => playerIdList.Contains(playerId);
        private static bool DoExpose(PlayerControl pc)
            => IsThisRole(pc.PlayerId) && pc.IsAlive()
            && pc.GetPlayerTaskState().RemainingTasksCount <= RemainingTasksToBeFound;
        private static bool IsSnitchTarget(PlayerControl target) => IsEnable && (target.Is(RoleType.Impostor) || (target.IsNeutralKiller() && CanFindNeutralKiller));
        public static void CheckTask(PlayerControl snitch)
        {
            if (DoExpose(snitch))
            {
                foreach (var target in Main.AllAlivePlayerControls)
                {
                    if (IsSnitchTarget(target))
                    {
                        TargetArrow.Add(target.PlayerId, snitch.PlayerId, ArrowType.SnitchWarning, false);
                    }
                }
            }
            if (!snitch.GetPlayerTaskState().IsTaskFinished) return;
            foreach (var target in Main.AllAlivePlayerControls)
            {
                if (!IsSnitchTarget(target)) continue;
                TargetArrow.Add(snitch.PlayerId, target.PlayerId, ArrowType.Target, CanGetColoredArrow);
                NameColorManager.Instance.RpcAdd(snitch.PlayerId, target.PlayerId, target.GetRoleColorCode());
            }
        }

        /// <summary>
        /// タスクが進んだスニッチに警告マーク
        /// </summary>
        /// <param name="seer">キラーの場合有効</param>
        /// <param name="target">スニッチの場合有効</param>
        /// <returns></returns>
        public static string GetWarningMark(PlayerControl seer, PlayerControl target)
            => IsSnitchTarget(seer) && DoExpose(target) ? Utils.ColorString(RoleColor, "★") : "";

        /// <summary>
        /// キラーからスニッチに対する矢印
        /// </summary>
        /// <param name="seer">キラーの場合有効</param>
        /// <param name="target">スニッチの場合有効</param>
        /// <returns></returns>
        public static string GetWarningArrow(PlayerControl seer, PlayerControl target = null)
        {
            if (target != null && seer.PlayerId != target.PlayerId) return "";
            if (!IsSnitchTarget(seer)) return "";
            string arrows = TargetArrow.GetArrows(seer, ArrowType.SnitchWarning);
            if (arrows.Length == 0)
            {
                return "";
            }
            if (!EnableTargetArrow) arrows = "";
            return Utils.ColorString(RoleColor, "★" + arrows);
        }
        /// <summary>
        /// スニッチからキラーへの矢印
        /// </summary>
        /// <param name="seer">スニッチの場合有効</param>
        /// <param name="target">スニッチの場合有効</param>
        /// <returns></returns>
        public static string GetSnitchArrow(PlayerControl seer, PlayerControl target = null)
        {
            if (!IsThisRole(seer.PlayerId)) return "";
            if (target != null && seer.PlayerId != target.PlayerId) return "";
            if (!EnableTargetArrow) return "";
            return TargetArrow.GetArrows(seer);
        }
        public static void OnCompleteTask(PlayerControl __instance)
        {
            if (!IsThisRole(__instance.PlayerId)) return;
            CheckTask(__instance);
        }
    }
}