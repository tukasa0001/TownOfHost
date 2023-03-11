using System.Collections.Generic;
using System.Linq;
using AmongUs.GameOptions;
using TownOfHost.Roles.Impostor;
using UnityEngine;

using TownOfHost.Roles.Core;
using static TownOfHost.Options;

namespace TownOfHost.Roles.Crewmate
{
    public class Snitch : RoleBase
    {
        public static readonly SimpleRoleInfo RoleInfo =
            new(
                typeof(Snitch),
                pc => new Snitch(pc),
                CustomRoles.Snitch,
                RoleTypes.Crewmate,
                CustomRoleTypes.Crewmate,
                20500,
                SetupOptionItem
            );
        public Snitch(PlayerControl player)
        : base(
            RoleInfo,
            player,
            true
        )
        {
            EnableTargetArrow = OptionEnableTargetArrow.GetBool();
            CanGetColoredArrow = OptionCanGetColoredArrow.GetBool();
            CanFindNeutralKiller = OptionCanFindNeutralKiller.GetBool();
            RemainingTasksToBeFound = OptionRemainingTasks.GetInt();

            //他視点用のMarkメソッド登録
            CustomRoleManager.MarkOthers.Add(GetMarkOthers);

        }

        private static OptionItem OptionEnableTargetArrow;
        private static OptionItem OptionCanGetColoredArrow;
        private static OptionItem OptionCanFindNeutralKiller;
        private static OptionItem OptionRemainingTasks;
        enum OptionName
        {
            SnitchEnableTargetArrow,
            SnitchCanGetArrowColor,
            SnitchCanFindNeutralKiller,
            SnitchRemainingTaskFound,
        }

        private static bool EnableTargetArrow;
        private static bool CanGetColoredArrow;
        private static bool CanFindNeutralKiller;
        private static int RemainingTasksToBeFound;

        private bool IsExposed;
        private bool IsComplete;

        private HashSet<byte> TargetList = new();
        private Dictionary<byte, Color> TargetColorlist = new();

        private static void SetupOptionItem()
        {
            var id = RoleInfo.ConfigId;
            var tab = RoleInfo.Tab;
            var parent = RoleInfo.RoleOption;
            OptionEnableTargetArrow = BooleanOptionItem.Create(id + 10, OptionName.SnitchEnableTargetArrow, false, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Snitch]);
            OptionCanGetColoredArrow = BooleanOptionItem.Create(id + 11, OptionName.SnitchCanGetArrowColor, false, TabGroup.CrewmateRoles, false).SetParent(OptionEnableTargetArrow);
            OptionCanFindNeutralKiller = BooleanOptionItem.Create(id + 12, OptionName.SnitchEnableTargetArrow, false, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Snitch]);
            OptionRemainingTasks = IntegerOptionItem.Create(id + 13, OptionName.SnitchRemainingTaskFound, new(0, 10, 1), 1, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Snitch]);
            OverrideTasksData.Create(id + 20, TabGroup.CrewmateRoles, CustomRoles.Snitch);
        }

        public override void Add()
        {
            TargetList.Clear();
            TargetColorlist.Clear();

            IsExposed = false;
            IsComplete = false;
        }
        /// <summary>
        /// スニッチのターゲットであるかの判定
        /// Others系でも使うためstatic実装
        /// </summary>
        /// <param name="target">判定対象</param>
        /// <returns></returns>
        private static bool IsSnitchTarget(PlayerControl target)
        {
            return target.Is(CustomRoleTypes.Impostor)
                || (target.IsNeutralKiller() && CanFindNeutralKiller);
        }

        /// <summary>
        /// タスクが終わりそうかの判定
        /// Others系でも使うためstatic実装
        /// </summary>
        /// <param name="pc">スニッチである場合判定</param>
        /// <returns>タスク終わりそうなスニッチであればtrue</returns>
        private static bool GetExpose(PlayerControl pc)
        {
            if (pc.GetRoleClass() is Snitch snitch)
            {
                return snitch.IsExposed;
            }
            return false;
        }

        /// <summary>
        /// キラーから見たスニッチ警告マーク
        /// キラーにはタスクが進んだスニッチを発見した警告マーク
        /// スニッチにはキラーに発見された警告マーク
        /// キラーが対象なためstatic実装
        /// </summary>
        /// <param name="seer">キラーの場合有効</param>
        /// <param name="seen">キラー自身またはスニッチの場合有効</param>
        /// <returns></returns>
        public static string GetMarkOthers(PlayerControl seer, PlayerControl seen, bool isForMeeting = false)
        {
            //seenが省略の場合seer
            seen ??= seer;

            //seerがキラーでなければ関係なし
            if (!IsSnitchTarget(seer)) return "";

            string mark = null;
            if (seer.PlayerId == seen.PlayerId)
            {
                //キラー自身がseenのとき
                if (!isForMeeting && EnableTargetArrow)
                {
                    var exposedSnitch = Main.AllAlivePlayerControls
                        .Where(p => p.Is(CustomRoles.Snitch) && GetExpose(p))
                        .Select(p => p.PlayerId)
                        .ToArray();
                    if (exposedSnitch.Length > 0)
                    {
                        mark = "★" + TargetArrow.GetArrows(seer, exposedSnitch);
                    }
                }
            }
            else
            {
                var seenRole = seen.GetRoleClass();
                if (seenRole is Snitch snitch)
                {
                    if (snitch.IsExposed) mark = "★";
                }
            }
            return mark == null ? "" : Utils.ColorString(Utils.GetRoleColor(CustomRoles.Snitch), mark);
        }

        /// <summary>
        /// スニッチからキラーへの矢印
        /// </summary>
        /// <param name="seer">スニッチの場合有効</param>
        /// <param name="seen">スニッチの場合有効</param>
        /// <returns></returns>
        public override string GetSuffix(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false)
        {
            //seenが省略の場合seer
            seen ??= seer;
            if (!Is(seer) && !Is(seen)) return "";
            if (!IsComplete) return "";
            if (!EnableTargetArrow || isForMeeting) return "";

            var arrows = "";
            foreach (var targetId in TargetList)
            {
                var arrow = TargetArrow.GetArrows(seer, targetId);
                arrows += CanGetColoredArrow ? Utils.ColorString(TargetColorlist[targetId], arrow) : arrow;
            }
            return arrows;
        }
        public override void OnCompleteTask()
        {
            var update = false;
            var snitchTask = Player.GetPlayerTaskState();
            if (!IsExposed && snitchTask.RemainingTasksCount <= RemainingTasksToBeFound)
            {
                foreach (var target in Main.AllAlivePlayerControls)
                {
                    if (!IsSnitchTarget(target)) continue;

                    TargetArrow.Add(target.PlayerId, Player.PlayerId);
                }
                IsExposed = true;
                update = true;
            }

            if (!IsComplete && snitchTask.IsTaskFinished)
            {
                foreach (var target in Main.AllAlivePlayerControls)
                {
                    if (!IsSnitchTarget(target)) continue;

                    var targetId = target.PlayerId;
                    NameColorManager.Add(Player.PlayerId, targetId);

                    if (!EnableTargetArrow) continue;

                    TargetArrow.Add(Player.PlayerId, targetId);

                    TargetList.Add(targetId);

                    if (CanGetColoredArrow)
                        TargetColorlist.Add(targetId, target.GetRoleColor());
                }
                IsComplete = true;
                update = true;
            }
            if (update) Utils.NotifyRoles();
        }
    }
}
