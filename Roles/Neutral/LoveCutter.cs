using System.Linq;
using AmongUs.GameOptions;
using Hazel;

using TownOfHostForE.Roles.Core;
using static UnityEngine.GraphicsBuffer;

namespace TownOfHostForE.Roles.Neutral;

public sealed class LoveCutter : RoleBase
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(LoveCutter),
            player => new LoveCutter(player),
            CustomRoles.LoveCutter,
            () => RoleTypes.Engineer,
            CustomRoleTypes.Neutral,
            60300,
            SetupOptionItem,
            "ラブカッター",
            "#c71585"
        );
    public LoveCutter(PlayerControl player)
    : base(
        RoleInfo,
        player,
        () => KnowKiller ? HasTask.ForRecompute : HasTask.False
    )
    {
        VictoryCutCount = OptionVictoryCutCount.GetInt();
        KnowKiller = OptionKnowKiller.GetBool();

        KilledCount = 0;
    }
    private static OptionItem OptionVictoryCutCount;
    private static OptionItem OptionKnowKiller;
    private static Options.OverrideTasksData Tasks;
    private enum OptionName
    {
        LoveCutterVictoryCutCount,
        LoveCutterKnowKiller
    }
    private static int VictoryCutCount;
    private static bool KnowKiller;

    int KilledCount = 0;

    private static void SetupOptionItem()
    {
        OptionVictoryCutCount = IntegerOptionItem.Create(RoleInfo, 10, OptionName.LoveCutterVictoryCutCount, new(1, 20, 1), 2, false)
            .SetValueFormat(OptionFormat.Times);
        OptionKnowKiller = BooleanOptionItem.Create(RoleInfo, 11, OptionName.LoveCutterKnowKiller, false, false);
        // 20-23を使用
        Tasks = Options.OverrideTasksData.Create(RoleInfo, 20, OptionKnowKiller);
    }

    public override bool OnCheckMurderAsTarget(MurderInfo info)
    {
        (var killer, var target) = info.AttemptTuple;
        // 直接キル出来る役職チェック
        if (killer.GetCustomRole().IsDirectKillRole()) return true;

        killer.RpcProtectedMurderPlayer(target);
        target.RpcProtectedMurderPlayer(target);
        KilledCount++;
        SendRPC();
        Logger.Info($"{target.GetNameWithRole()} : {KilledCount}回目", "LoveCutter");
        NameColorManager.Add(killer.PlayerId, target.PlayerId, RoleInfo.RoleColorCode);
        Utils.NotifyRoles(SpecifySeer: target);

        if (KilledCount >= VictoryCutCount) Win();
        info.CanKill = false;
        return true;
    }
    public override bool OnCompleteTask()
    {
        if (!KnowKiller || !IsTaskFinished || !Player.IsAlive()) return true;

        foreach (var killer in Main.AllAlivePlayerControls
            .Where(pc => pc.Is(CustomRoleTypes.Impostor) || pc.IsCrewKiller() || pc.IsNeutralKiller() || pc.IsAnimalsKiller()))
        {
            NameColorManager.Add(Player.PlayerId, killer.PlayerId);
        }
        Utils.NotifyRoles(SpecifySeer: Player);

        return true;
    }


    public void Win()
    {
        if (!AmongUsClient.Instance.AmHost) return;
        foreach (var pc in Main.AllAlivePlayerControls)
        {
            var playerState = PlayerState.GetByPlayerId(pc.PlayerId);
            if (pc == Player)
            {
                playerState.DeathReason = CustomDeathReason.Kill;
                continue;
            }
            pc.SetRealKiller(Player);
            pc.RpcMurderPlayer(pc);
            playerState.DeathReason = CustomDeathReason.Bombed;
            playerState.SetDead();
        }
        CustomWinnerHolder.ResetAndSetWinner(CustomWinner.LoveCutter);
        CustomWinnerHolder.WinnerIds.Add(Player.PlayerId);
    }

    public override string GetProgressText(bool comms = false)
    => Utils.ColorString(RoleInfo.RoleColor,$"({KilledCount}/{VictoryCutCount})");

    private void SendRPC()
    {
        if (!AmongUsClient.Instance.AmHost) return;
        using var sender = CreateSender(CustomRPC.LoveCuttorSync);
        sender.Writer.Write(KilledCount);
    }
    public override void ReceiveRPC(MessageReader reader, CustomRPC rpcType)
    {
        if (rpcType != CustomRPC.LoveCuttorSync) return;

        KilledCount = reader.ReadInt32();
    }
}
