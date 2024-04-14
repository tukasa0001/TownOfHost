using AmongUs.GameOptions;

using TownOfHostForE.Roles.Core;
using TownOfHostForE.Roles.Core.Interfaces;

namespace TownOfHostForE.Roles.Madmate;
public sealed class MadSheriff : RoleBase, IKiller
{
    public static readonly SimpleRoleInfo RoleInfo =
         SimpleRoleInfo.Create(
            typeof(MadSheriff),
            player => new MadSheriff(player),
            CustomRoles.MadSheriff,
            () => RoleTypes.Impostor,
            CustomRoleTypes.Madmate,
            5600,
            SetupOptionItem,
            "マッドシェリフ"
        );
    public MadSheriff(PlayerControl player)
    : base(
        RoleInfo,
        player,
        () => HasTask.False
    )
    {
        KillCooldown = OptionKillCooldown.GetFloat();
        MisfireKillsTarget = OptionMisfireKillsTarget.GetBool();
        CanVent = OptionCanVent.GetBool();
    }

    private static OptionItem OptionKillCooldown;
    private static OptionItem OptionMisfireKillsTarget;
    private static OptionItem OptionCanVent;

    enum OptionName
    {
        SheriffMisfireKillsTarget,
    }
    private static float KillCooldown;
    private static bool MisfireKillsTarget;
    public static bool CanVent;

    private static void SetupOptionItem()
    {
        OptionKillCooldown = FloatOptionItem.Create(RoleInfo, 10, GeneralOption.KillCooldown, new(0f, 180f, 2.5f), 30f, false)
            .SetValueFormat(OptionFormat.Seconds);
        OptionMisfireKillsTarget = BooleanOptionItem.Create(RoleInfo, 11, OptionName.SheriffMisfireKillsTarget, false, false);
        OptionCanVent = BooleanOptionItem.Create(RoleInfo, 12, GeneralOption.CanVent, false, false);
        Options.SetUpAddOnOptions(RoleInfo.ConfigId + 20, RoleInfo.RoleName, RoleInfo.Tab);
    }
    public override void Add()
    {
        var playerId = Player.PlayerId;
    }
    public float CalculateKillCooldown() => CanUseKillButton() ? KillCooldown : 0f;
    public bool CanUseKillButton() => Player.IsAlive();
    public bool CanUseImpostorVentButton() => true;
    public bool CanUseSabotageButton() => false;
    public override void ApplyGameOptions(IGameOptions opt)
    {
        opt.SetVision(Options.AddOnRoleOptions[(CustomRoles.MadSheriff, CustomRoles.AddLight)].GetBool());
    }
    public void OnCheckMurderAsKiller(MurderInfo info)
    {
        if (!Is(info.AttemptKiller) || info.IsSuicide) return;
        (var killer, var target) = info.AttemptTuple;

        PlayerState.GetByPlayerId(killer.PlayerId).DeathReason = CustomDeathReason.Misfire;
        killer.RpcMurderPlayer(killer);

        if (!MisfireKillsTarget) info.DoKill = false;
    }
}