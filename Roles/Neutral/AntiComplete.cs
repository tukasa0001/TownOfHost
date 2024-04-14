using System.Collections.Generic;
using AmongUs.GameOptions;

using UnityEngine;
using TownOfHostForE.Modules;
using TownOfHostForE.Roles.Core;
using static TownOfHostForE.Utils;

namespace TownOfHostForE.Roles.Neutral;

public sealed class AntiComplete : RoleBase
{
    public static readonly SimpleRoleInfo RoleInfo =
         SimpleRoleInfo.Create(
            typeof(AntiComplete),
            player => new AntiComplete(player),
            CustomRoles.AntiComplete,
            () => RoleTypes.Crewmate,
            CustomRoleTypes.Neutral,
            60000,
            SetupOptionItem,
            "アンチコンプリート",
            "#ec62a5"
        );
    public AntiComplete(PlayerControl player)
    : base(
        RoleInfo,
        player,
        () => KnowOption ? HasTask.ForRecompute : HasTask.False
    )
    {
        StartGuardCount = OptionGuardCount.GetInt();
        KnowOption = OptionKnowOption.GetBool();
        KnowNotask = OptionKnowNotask.GetBool();
        KnowCompTask = OptionKnowCompTask.GetBool();
        AddGuardCount = OptionKnowCompTask.GetInt();
    }

    private static OptionItem OptionGuardCount;
    private static OptionItem OptionKnowOption;
    private static OptionItem OptionKnowNotask;
    private static OptionItem OptionKnowCompTask;
    private static OptionItem OptionAddGuardCount;
    private static Options.OverrideTasksData Tasks;
    private enum OptionName
    {
        AntiCompGuardCount,
        AntiCompKnowOption,
        AntiCompKnowNotask,
        AntiCompKnowCompTask,
        AntiCompAddGuardCount,
    }
    private static int StartGuardCount;
    private static bool KnowOption;
    private static bool KnowNotask;
    private static bool KnowCompTask;
    private static int AddGuardCount;

    int GuardCount;

    private static void SetupOptionItem()
    {
        OptionGuardCount = IntegerOptionItem.Create(RoleInfo, 10, OptionName.AntiCompGuardCount, new(0, 20, 1), 2, false)
                .SetValueFormat(OptionFormat.Times);
        OptionKnowOption = BooleanOptionItem.Create(RoleInfo, 11, OptionName.AntiCompKnowOption, false, false);
        OptionKnowNotask = BooleanOptionItem.Create(RoleInfo, 12, OptionName.AntiCompKnowNotask, true, false, OptionKnowOption);
        OptionKnowCompTask = BooleanOptionItem.Create(RoleInfo, 13, OptionName.AntiCompKnowCompTask, false, false, OptionKnowOption);
        OptionAddGuardCount = IntegerOptionItem.Create(RoleInfo, 14, OptionName.AntiCompAddGuardCount, new(0, 10, 1), 0, false, OptionKnowOption)
                .SetValueFormat(OptionFormat.Seconds);
        // 20-23を使用
        Tasks = Options.OverrideTasksData.Create(RoleInfo, 20, OptionKnowOption);
    }
    public override void Add()
    {
        GuardCount = StartGuardCount;
    }

    public override bool OnCheckMurderAsTarget(MurderInfo info)
    {
        (var killer, var target) = info.AttemptTuple;
        // 直接キル出来る役職チェック
        if (killer.GetCustomRole().IsDirectKillRole()) return true;
        if (GuardCount <= 0) return true;//普通にキル

        killer.RpcProtectedMurderPlayer(target);
        target.RpcProtectedMurderPlayer(target);
        info.CanKill = false;

        GuardCount--;
        Logger.Info($"{target.GetNameWithRole()} : ガード残り{GuardCount}回", "AntiComp");
        return true;
    }
    public override bool OnCompleteTask()
    {
        if (IsTaskFinished && Player.IsAlive())
            GuardCount += AddGuardCount;
        return true;
    }

    public override string GetMark(PlayerControl seer, PlayerControl seen, bool isForMeeting = false)
    {
        //seenが省略の場合seer
        seen ??= seer;

        if (seer.Is(CustomRoles.AntiComplete) && KnowOption && seer.GetPlayerTaskState().IsTaskFinished && seer != seen)
        {
            if (KnowCompTask && seen.GetPlayerTaskState().IsTaskFinished && !isForMeeting)
                return ColorString(RoleInfo.RoleColor, "◎");

            if (KnowNotask && !seen.GetPlayerTaskState().hasTasks)
                return ColorString(RoleInfo.RoleColor, "×");
        }
        return string.Empty;
    }

    public override string GetProgressText(bool comms = false) => ColorString(GuardCount > 0 ? RoleInfo.RoleColor : Color.gray, $"({GuardCount})");

    public override (byte? votedForId, int? numVotes, bool doVote) ModifyVote(byte voterId, byte sourceVotedForId, bool isIntentional)
    {
        var (votedForId, numVotes, doVote) = base.ModifyVote(voterId, sourceVotedForId, isIntentional);
        var baseVote = (votedForId, numVotes, doVote);
        if (voterId != Player.PlayerId              // 投票者がアンチコンプ自身でない
            || sourceVotedForId == Player.PlayerId  // 投票先がアンチコンプ自身(自投票)
            || sourceVotedForId >= 253              // 投票先がスキップ253/無投票254
            || !Player.IsAlive())                   // アンチコンプ自身が死亡
        {
            return baseVote;                        // 変更なしで返す
        }
        
        // 今までに行われた投票をすべて削除し，特定の投票先に1票投じられた状態で会議を強制終了
        // 投票者 = アンチコンプ自身  exiled = 投票先  アンチコンプ処理
        MeetingVoteManager.Instance.ClearAndExile(Player.PlayerId, sourceVotedForId, true);

        // 投票先のタスクステートを取得
        var taskState = PlayerState.GetByPlayerId(sourceVotedForId).GetTaskState();
        if (taskState.IsTaskFinished)   // 投票先がタスク完了している
            MyState.DeathReason = CustomDeathReason.Win;
        else                            // タスク未完了/タスク未所持
            MyState.DeathReason = CustomDeathReason.Suicide;

        // 変更後の投票先 = 元の投票先  変更後の票数 = 1  投票カウント = しない
        return (votedForId, numVotes, false);
    }

    public override void OnExileWrapUp(GameData.PlayerInfo exiled, ref bool DecidedWinner)
    {
        if (!AmongUsClient.Instance.AmHost || Player.PlayerId != exiled.PlayerId) return;
        if (MyState.DeathReason != CustomDeathReason.Win) return;

        CustomWinnerHolder.ResetAndSetWinner(CustomWinner.AntiComplete);
        CustomWinnerHolder.WinnerIds.Add(exiled.PlayerId);
        DecidedWinner = true;
    }
}
