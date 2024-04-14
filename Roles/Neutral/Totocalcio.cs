using AmongUs.GameOptions;
using Hazel;
using System.Linq;

using TownOfHostForE.Roles.Core;
using TownOfHostForE.Roles.Core.Interfaces;
namespace TownOfHostForE.Roles.Neutral;

public sealed class Totocalcio : RoleBase, IKiller, IAdditionalWinner
{
    public static readonly SimpleRoleInfo RoleInfo =
         SimpleRoleInfo.Create(
            typeof(Totocalcio),
            player => new Totocalcio(player),
            CustomRoles.Totocalcio,
            () => RoleTypes.Impostor,
            CustomRoleTypes.Neutral,
            60600,
            SetupOptionItem,
            "トトカルチョ",
            "#00ff00",
            true
        );
    public Totocalcio(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        InitialCoolDown = OptionInitialCoolDown.GetFloat();
        FinalCoolDown = OptionFinalCoolDown.GetFloat();
        BetChangeCount = OptionBetChangeCount.GetInt();

        AllPlayer = Main.AllPlayerControls.Count();
        if (AllPlayer > 3) Coolrate = (FinalCoolDown - InitialCoolDown) / (AllPlayer - 3);
        else Coolrate = (FinalCoolDown - InitialCoolDown) / AllPlayer;
    }
    public static PlayerControl BetTarget;
    public static int BetTargetCount;

    private static OptionItem OptionInitialCoolDown;
    private static OptionItem OptionFinalCoolDown;
    private static OptionItem OptionBetChangeCount;
    enum OptionName
    {
        TotocalcioInitialCoolDown,
        TotocalcioBetChangeCount,
        TotocalcioFinalCoolDown,
    }
    private static float InitialCoolDown;
    private static float FinalCoolDown;
    private static int BetChangeCount;

    private static float Coolrate;
    private static int AllPlayer;

    private static void SetupOptionItem()
    {
        OptionInitialCoolDown = FloatOptionItem.Create(RoleInfo, 10, OptionName.TotocalcioInitialCoolDown, new(0f, 180f, 2.5f), 30f, false)
            .SetValueFormat(OptionFormat.Seconds);
        OptionBetChangeCount = IntegerOptionItem.Create(RoleInfo, 11, OptionName.TotocalcioBetChangeCount, new(0, 10, 1), 0, false)
            .SetValueFormat(OptionFormat.Times);
        OptionFinalCoolDown = FloatOptionItem.Create(RoleInfo, 12, OptionName.TotocalcioFinalCoolDown, new(0f, 180f, 2.5f), 60f, false)
            .SetValueFormat(OptionFormat.Seconds);
    }
    public bool CheckWin(ref CustomRoles winnerRole)
    {
        //賭けしてないなら負けよ
        if (BetTarget == null) return false;
        var targetRole = BetTarget.GetCustomRole();
        bool winTeam = false;
        foreach (var role in CustomWinnerHolder.WinnerRoles)
        {
            if(role == targetRole) { winTeam = true; break; }
        }
        bool winIds = CustomWinnerHolder.WinnerIds.Contains(BetTarget.PlayerId);
        return Player.IsAlive() && (winIds || winTeam);
    }
    public override void Add()
    {
        var playerId = Player.PlayerId;
        BetTarget = null;
        BetTargetCount = BetChangeCount + 1;
    }
    public float CalculateKillCooldown()
    {
        float plusCool = Coolrate * (AllPlayer - Main.AllAlivePlayerControls.Count());
        return CanUseKillButton() ? InitialCoolDown + plusCool : 300f;
    }
    public bool CanUseKillButton() => Player.IsAlive() && BetTargetCount > 0;
    public bool CanUseImpostorVentButton() => false;
    public bool CanUseSabotageButton() => false;
    public override void ApplyGameOptions(IGameOptions opt) => opt.SetVision(false);
    public void OnCheckMurderAsKiller(MurderInfo info)
    {
        if (Is(info.AttemptKiller) && !info.IsSuicide)
        {
            (var killer, var target) = info.AttemptTuple;

            BetTarget = target;
            BetTargetCount--;
            SendRPC(target.PlayerId);
            killer.RpcProtectedMurderPlayer(target);
            info.DoKill = false;
            Logger.Info($"{killer.GetNameWithRole()} : {target.GetRealName()}に賭けた", "Totocalcio");

            Utils.NotifyRoles();
        }
    }

    public bool OverrideKillButtonText(out string text)
    {
        text = Translator.GetString("TotocalcioButtonText");
        return true;
    }
    public override string GetMark(PlayerControl seer, PlayerControl seen, bool isForMeeting = false)
    {
        //seenが省略の場合seer
        seen ??= seer;
        if (BetTarget == seen) return Utils.ColorString(RoleInfo.RoleColor, "▲");

        return string.Empty;
    }

    private void SendRPC(byte targetId)
    {
        if (!AmongUsClient.Instance.AmHost) return;
        using var sender = CreateSender(CustomRPC.SetTotocalcioTarget);
        sender.Writer.Write(targetId);
        sender.Writer.Write(BetTargetCount);
    }
    public override void ReceiveRPC(MessageReader reader, CustomRPC rpcType)
    {
        if (rpcType != CustomRPC.SetTotocalcioTarget) return;

        var targetId = reader.ReadByte();
        BetTarget = Utils.GetPlayerById(targetId);
        BetTargetCount = reader.ReadInt32();
    }
}
