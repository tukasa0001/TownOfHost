using AmongUs.GameOptions;
using TownOfHost.Roles.Core;

namespace TownOfHost.Roles.Crewmate;
public sealed class Lighter : RoleBase
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(Lighter),
            player => new Lighter(player),
            CustomRoles.Lighter,
            () => RoleTypes.Crewmate,
            CustomRoleTypes.Crewmate,
            20100,
            SetupOptionItem,
            "li",
            "#eee5be"
        );
    public Lighter(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        MaxVision = OptionMaxVision.GetFloat();
        TaskCompletedDisableLightOut = OptionTaskCompletedDisableLightOut.GetBool();
        TaskTrigger = OptionLighterTaskTrigger.GetInt();
        CurrentVision = Main.DefaultCrewmateVision;
        LighterTriggerType = (TriggerType)OptionLighterTriggerType.GetValue();
    }
    /// <summary>最大視野</summary>
    private static OptionItem OptionMaxVision;
    /// <summary>タスク完了時に停電の影響を受けなくする</summary>
    private static OptionItem OptionTaskCompletedDisableLightOut;
    /// <summary>効果発揮のタイプを変更する [タスク進捗率,一定数のタスク達成]</summary>
    private static OptionItem OptionLighterTriggerType;
    /// <summary>能力発動タスク数  TriggerType[一定数のタスク達成]選択時のみ有効</summary>
    private static OptionItem OptionLighterTaskTrigger;
    enum OptionName
    {
        LighterMaxVision,
        LighterTaskCompletedDisableLightOut,
        LighterTriggerType,
        LighterTaskTrigger
    }
    /// <summary>効果を発揮するタイプ</summary>
    public enum TriggerType
    {
        TaskProgressRate,//タスク進捗率
        TaskCount//一定数のタスク達成
    }

    public static TriggerType LighterTriggerType;

    private static float MaxVision;
    private static bool TaskCompletedDisableLightOut;
    private static int TaskTrigger;
    private float CurrentVision;

    private static void SetupOptionItem()
    {
        OptionMaxVision = FloatOptionItem.Create(RoleInfo, 10, OptionName.LighterMaxVision, new(0.0f, 3.0f, 0.1f), 1.0f, false)
            .SetValueFormat(OptionFormat.Multiplier);
        OptionTaskCompletedDisableLightOut = BooleanOptionItem.Create(RoleInfo, 11, OptionName.LighterTaskCompletedDisableLightOut, true, false);
        OptionLighterTriggerType = StringOptionItem.Create(RoleInfo, 12, OptionName.LighterTriggerType, EnumHelper.GetAllNames<TriggerType>(), 0, false);
        OptionLighterTaskTrigger = IntegerOptionItem.Create(RoleInfo, 13, OptionName.LighterTaskTrigger, new(1, 99, 1), 5, false)
            .SetParent(OptionLighterTriggerType);
    }

    public override void ApplyGameOptions(IGameOptions opt)
    {
        if (!Player.IsAlive() || MyTaskState.CompletedTasksCount == 0) return;//死んでる or タスク数0
        //タスクトリガーの場合 トリガータスク数を下回っている or タスク完了していない
        if (LighterTriggerType == TriggerType.TaskCount && !MyTaskState.HasCompletedEnoughCountOfTasks(TaskTrigger)) return;
        Logger.Info("ApplyGameOptions Trigger", "Lighter");
        var crewLightMod = FloatOptionNames.CrewLightMod;
        opt.SetFloat(crewLightMod, CurrentVision);
        if (TaskCompletedDisableLightOut && Utils.IsActive(SystemTypes.Electrical) && MyTaskState.IsTaskFinished)
        {
            opt.SetFloat(crewLightMod, CurrentVision * 5);
        }
    }
    public override bool OnCompleteTask()
    {
        if (!Player.IsAlive() || MyTaskState.CompletedTasksCount == 0) return true;//死んでる or タスク数0
        if (LighterTriggerType == TriggerType.TaskCount && MyTaskState.CompletedTasksCount != TaskTrigger) return true;
        Logger.Info("Ability activation condition", "Lighter");
        if (LighterTriggerType == TriggerType.TaskCount && MyTaskState.CompletedTasksCount == TaskTrigger)
        {
            CurrentVision = MaxVision;
        }
        if (LighterTriggerType == TriggerType.TaskProgressRate)
        {
            //進捗率(%) = 完了タスク数 / 全タスク数   例:1/4 = 0.25=> 0.25*100 =>25%
            int progressRate = MyTaskState.CompletedTasksCount * 100 / MyTaskState.AllTasksCount;
            //視野差 = 最大視野 - デフォルト視野     例:(1.25 - 0.25)/100=> 1.00
            float viewBetween = (MaxVision * 100 - Main.DefaultCrewmateVision * 100) / 100;
            //例:1.00 * 25 / 100 => 0.25(上昇値)
            CurrentVision += viewBetween * progressRate / 100;
            Logger.Info("viewBetween :" + viewBetween.ToString() + "*" + " progressRate:" + progressRate.ToString() + "%", "Lighter");
            Logger.Info("タスク進捗率で視野変更 タスク:" + MyTaskState.CompletedTasksCount + "/" + MyTaskState.AllTasksCount + " セットする視野:" + CurrentVision.ToString(), "Lighter");
        }
        Player.MarkDirtySettings();
        return true;
    }

    ///Lighter以外から視野を変更する場合は以下メソッドを使用すること
    public void AddCurrentVision(float addVision)
    {
        CurrentVision += addVision;
    }
}