using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using AmongUs.GameOptions;

using TownOfHost.Roles.Core;

namespace TownOfHost.Roles.Crewmate;
public class Snitch : RoleBase
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(Snitch),
            pc => new Snitch(pc),
            CustomRoles.Snitch,
            () => RoleTypes.Crewmate,
            CustomRoleTypes.Crewmate,
            20500,
            SetupOptionItem,
            "sn",
            "#b8fb4f"
        );
    public Snitch(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        EnableTargetArrow = OptionEnableTargetArrow.GetBool();
        CanGetColoredArrow = OptionCanGetColoredArrow.GetBool();
        CanFindNeutralKiller = OptionCanFindNeutralKiller.GetBool();
        RemainingTasksToBeFound = OptionRemainingTasks.GetInt();

        //他視点用のMarkメソッド登録
        CustomRoleManager.MarkOthers.Add(GetMarkOthers);
    }
    public override void OnDestroy()
    {
        TargetList.Clear();
        TargetColorlist.Clear();
        ExposedList.Clear();
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

    private bool IsExposed = false;
    private bool IsComplete = false;

    //複数Snitchで共有するためstatic
    private static HashSet<byte> TargetList = new();
    private static Dictionary<byte, Color> TargetColorlist = new();
    private static HashSet<byte> ExposedList = new();

    private static void SetupOptionItem()
    {
        OptionEnableTargetArrow = BooleanOptionItem.Create(RoleInfo, 10, OptionName.SnitchEnableTargetArrow, false, false);
        OptionCanGetColoredArrow = BooleanOptionItem.Create(RoleInfo, 11, OptionName.SnitchCanGetArrowColor, false, false);
        OptionCanFindNeutralKiller = BooleanOptionItem.Create(RoleInfo, 12, OptionName.SnitchCanFindNeutralKiller, false, false);
        OptionRemainingTasks = IntegerOptionItem.Create(RoleInfo, 13, OptionName.SnitchRemainingTaskFound, new(0, 10, 1), 1, false);
        Options.OverrideTasksData.Create(RoleInfo, 20);
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
            || (CanFindNeutralKiller && target.IsNeutralKiller());
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

        //キラーじゃなければ無し
        if (!IsSnitchTarget(seer)) return "";
        //タスクが進んでいなければ無し
        if (ExposedList.Count == 0) return "";

        if (seer.PlayerId == seen.PlayerId)
        {
            //キラー自身がseenのとき
            var mark = "★";
            if (!isForMeeting && EnableTargetArrow)
            {
                mark += TargetArrow.GetArrows(seer, ExposedList.ToArray());
            }
            return Utils.ColorString(RoleInfo.RoleColor, mark);
        }
        else if (seen.GetRoleClass() is Snitch snitch && snitch.IsExposed)
        {
            //seenがタスク終わりそうなスニッチの時
            return Utils.ColorString(RoleInfo.RoleColor, "★");

        }
        //その他seenなら無し
        return "";
    }

    /// <summary>
    /// スニッチからキラーへの矢印
    /// </summary>
    /// <param name="seer">スニッチの場合有効</param>
    /// <param name="seen">スニッチの場合有効</param>
    /// <returns></returns>
    public override string GetSuffix(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false)
    {
        //矢印表示する必要がなければ無し
        if (!EnableTargetArrow || isForMeeting) return "";

        //seenが省略の場合seer
        seen ??= seer;

        //ともにスニッチでなければ無し
        if (!Is(seer) && !Is(seen)) return "";
        //タスク終わってなければ無し
        if (!IsComplete) return "";

        var arrows = "";
        foreach (var targetId in TargetList)
        {
            var arrow = TargetArrow.GetArrows(seer, targetId);
            arrows += CanGetColoredArrow ? Utils.ColorString(TargetColorlist[targetId], arrow) : arrow;
        }
        return arrows;
    }
    /// <summary>
    /// タスクの進行状況の管理
    /// </summary>
    public override bool OnCompleteTask()
    {
        var update = false;
        if (TargetList.Count == 0)
        {
            //TargetListが未作成ならここで作る
            foreach (var target in Main.AllAlivePlayerControls)
            {
                if (!IsSnitchTarget(target)) continue;

                var targetId = target.PlayerId;
                TargetList.Add(targetId);
                TargetColorlist.Add(targetId, target.GetRoleColor());
            }
        }
        if (!IsExposed && MyTaskState.RemainingTasksCount <= RemainingTasksToBeFound)
        {
            IsExposed = true;
            ExposedList.Add(Player.PlayerId);
            if (EnableTargetArrow)
            {
                foreach (var targetId in TargetList)
                {
                    TargetArrow.Add(targetId, Player.PlayerId);
                }
            }
            update = true;
        }

        if (!IsComplete && IsTaskFinished)
        {
            IsComplete = true;
            foreach (var targetId in TargetList)
            {
                NameColorManager.Add(Player.PlayerId, targetId);

                if (EnableTargetArrow)
                    TargetArrow.Add(Player.PlayerId, targetId);
            }
            update = true;
        }
        if (update) Utils.NotifyRoles();
        return true;
    }
}
