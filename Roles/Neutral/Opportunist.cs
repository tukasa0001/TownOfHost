using AmongUs.GameOptions;
using Hazel;
using UnityEngine;

using TownOfHostForE.Roles.Core;
using TownOfHostForE.Roles.Core.Interfaces;

namespace TownOfHostForE.Roles.Neutral;

public sealed class Opportunist : RoleBase, IKiller, IAdditionalWinner, ISchrodingerCatOwner
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(Opportunist),
            player => new Opportunist(player),
            CustomRoles.Opportunist,
            () => OptionCanKill.GetBool() ? RoleTypes.Impostor : RoleTypes.Crewmate,
            CustomRoleTypes.Neutral,
            50100,
            SetupOptionItem,
            "オポチュニスト",
            "#00ff00",
            true
        );
    public Opportunist(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        CanKill = OptionCanKill.GetBool();
        ShotLimit = ShotLimitOpt.GetInt();
        KillCooldown = OptionKillCooldown.GetFloat();
        HasImpostorVision = OptionHasImpostorVision.GetBool();
    }
    public static OptionItem OptionCanKill;
    private static OptionItem OptionKillCooldown;
    private static OptionItem ShotLimitOpt;
    private static OptionItem OptionHasImpostorVision;
    enum OptionName
    {
        CanKill,
        SheriffShotLimit,
    }
    public static bool CanKill;
    public int ShotLimit = 0;
    public float KillCooldown = 30;
    private static bool HasImpostorVision;

    public SchrodingerCat.TeamType SchrodingerCatChangeTo => SchrodingerCat.TeamType.Opportunist;
    private static void SetupOptionItem()
    {
        OptionCanKill = BooleanOptionItem.Create(RoleInfo, 10, OptionName.CanKill, false, false);
        OptionKillCooldown = FloatOptionItem.Create(RoleInfo, 11, GeneralOption.KillCooldown, new(0f, 180f, 2.5f), 30f, false, OptionCanKill)
            .SetValueFormat(OptionFormat.Seconds);
        ShotLimitOpt = IntegerOptionItem.Create(RoleInfo, 13, OptionName.SheriffShotLimit, new(1, 15, 1), 15, false, OptionCanKill)
            .SetValueFormat(OptionFormat.Times);
        OptionHasImpostorVision = BooleanOptionItem.Create(RoleInfo, 12, GeneralOption.ImpostorVision, false, false, OptionCanKill);
    }

    public bool CheckWin(ref CustomRoles winnerRole)
    {
        return Player.IsAlive();
    }

    public override void Add()
    {
        if (!CanKill) return;

        var playerId = Player.PlayerId;
        KillCooldown = OptionKillCooldown.GetFloat();

        ShotLimit = ShotLimitOpt.GetInt();
        Logger.Info($"{Utils.GetPlayerById(playerId)?.GetNameWithRole()} : 残り{ShotLimit}発", "Oppo");
    }
    private void SendRPC()
    {
        if (!AmongUsClient.Instance.AmHost) return;
        using var sender = CreateSender(CustomRPC.SetOppoKillerShotLimit);
        sender.Writer.Write(ShotLimit);
    }
    public override void ReceiveRPC(MessageReader reader, CustomRPC rpcType)
    {
        if (rpcType != CustomRPC.SetOppoKillerShotLimit) return;

        ShotLimit = reader.ReadInt32();
    }
    public float CalculateKillCooldown() => KillCooldown;
    public bool CanUseKillButton() => Player.IsAlive() && ShotLimit > 0;
    public bool CanUseImpostorVentButton() => false;
    public bool CanUseSabotageButton() => false;
    public override void ApplyGameOptions(IGameOptions opt) => opt.SetVision(HasImpostorVision);
    //public void OnCheckMurderAsKiller(MurderInfo info)
    public void OnMurderPlayerAsKiller(MurderInfo info)
    {
        if (Is(info.AttemptKiller) && !info.IsSuicide)
        {
            (var killer, var target) = info.AttemptTuple;

            ShotLimit--;
            Logger.Info($"{killer.GetNameWithRole()} : 残り{ShotLimit}発", "OppoKiller");
            SendRPC();
            killer.ResetKillCooldown();
        }
        return;
    }
    public override string GetProgressText(bool comms = false)
    {
        if (!CanKill) return null;
        return Utils.ColorString(CanUseKillButton() ? RoleInfo.RoleColor : Color.gray, $"({ShotLimit})");
    }
}
