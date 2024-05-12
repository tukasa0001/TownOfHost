using AmongUs.GameOptions;
using UnityEngine;

using TownOfHostForE.Roles.Core;
using System.Linq;
using UnityEngine.Purchasing;
using Mono.Cecil;

namespace TownOfHostForE.Roles.Animals;
public sealed class Dolphin : RoleBase
{
    /// <summary>
    ///  20000:TOH4E役職
    ///   1000:陣営 1:crew 2:imp 3:Third 4:Animals 5:Madmate
    ///    100:役職ID
    /// </summary>
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(Dolphin),
            player => new Dolphin(player),
            CustomRoles.Dolphin,
            () => OptionCanVent.GetBool() ? RoleTypes.Engineer : RoleTypes.Crewmate,
            CustomRoleTypes.Animals,
            24900,
            SetupOptionItem,
            "イルカ",
            "#FF8C00",
            countType:CountTypes.Crew
            //introSound: () => ShipStatus.Instance.CommonTasks.Where(task => task.TaskType == TaskTypes.FixWiring).FirstOrDefault().MinigamePrefab.OpenSound
        );
    public Dolphin(PlayerControl player)
    : base(
        RoleInfo,
        player,
        () => HasTask.ForRecompute
    )
    {
        AbillityCooldown = OptionCooldown.GetInt();
        AbillityLimitCount = OptionAbillityLimitCount.GetInt();
        HasImpostorVision = OptionHasImpostorVision.GetBool();
        Count = 0;
        visualize = false;
    }

    public static OptionItem OptionCanVent;
    private static OptionItem OptionHasImpostorVision;
    private static OptionItem OptionAbillityLimitCount;
    private static OptionItem OptionCooldown;

    //1試合に利用できる能力上限
    private static int AbillityLimitCount = 0;
    //一回撫でてからのクールダウン
    private static int AbillityCooldown = 0;
    //インポスター視界を持つか
    private static bool HasImpostorVision;

    //能力発動回数
    private int Count = 0;

    //パラム
    private static Options.OverrideTasksData Tasks;
    //やじるし視覚化フラグ。
    private bool visualize = false;
    //クールダウンフラグ
    private bool isCool = false;

    enum OptionName
    {
        DolphinLimit,
        DolphinWorkCount,
    }

    public static void SetupOptionItem()
    {
        OptionAbillityLimitCount = IntegerOptionItem.Create(RoleInfo, 10, OptionName.DolphinLimit, new(1, 15, 1), 3, false)
                .SetValueFormat(OptionFormat.Times);
        OptionCanVent = BooleanOptionItem.Create(RoleInfo, 11, GeneralOption.CanVent, true, false);
        OptionHasImpostorVision = BooleanOptionItem.Create(RoleInfo, 12, GeneralOption.ImpostorVision, true, false);
        OptionCooldown = IntegerOptionItem.Create(RoleInfo, 13, GeneralOption.Cooldown, new(5, 30, 1), 10, false)
                .SetValueFormat(OptionFormat.Times);
        // 100-103を使用
        Tasks = Options.OverrideTasksData.Create(RoleInfo, 14);
        Options.SetUpAddOnOptions(RoleInfo.ConfigId + 20, RoleInfo.RoleName, RoleInfo.Tab);
    }
    public override string GetProgressText(bool comms = false) => $"({Count}/{AbillityLimitCount}回)";
    public override void OnTouchPet(PlayerControl player)
    {
        //自身の終わったタスク数が指定未満であるなら能力を発動しない
        //if (MyTaskState.CompletedTasksCount < AbillityWorkCount) return;
        if (!IsTaskFinished) return;
        //発動上限に達していたなら発動できない
        if (Count >= AbillityLimitCount) return;
        if (isCool) return;

        //タスク数問題なし
        SetTarget();

        //表示フラグON
        visualize = true;

        //発動回数を1増やす
        Count++;

        isCool = true;
        new LateTask(() =>
        {
            isCool = false;
        }, AbillityCooldown, "Dolphin Cool");

        Utils.NotifyRoles(SpecifySeer:Player);
    }

    public override void OnStartMeeting()
    {
        visualize = false;
    }

    public override string GetSuffix(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false)
    {
        seen ??= seer;

        if (isForMeeting) return "";
        if (!visualize) return "";

        //return TargetArrow.GetArrowsP(seer, seer.PlayerId);
        return TargetArrow.GetArrowsPM(seer, seer.PlayerId);
    }
    public override void ApplyGameOptions(IGameOptions opt) => opt.SetVision(HasImpostorVision);

    private void SetTarget()
    {
        int counter = 0;
        foreach (var target in Main.AllAlivePlayerControls)
        {
            if (Player.PlayerId == target.PlayerId) continue;
            var cRole = target.GetCustomRole();
            if (cRole.GetCustomRoleTypes() != CustomRoleTypes.Animals) continue;

            TargetArrow.AddMulti(Player.PlayerId, Player.PlayerId, target.transform.position,counter == 0);
            //TargetArrow.Add(Player.PlayerId, Player.PlayerId, target.transform.position);
            counter++;
        }
    }

}
