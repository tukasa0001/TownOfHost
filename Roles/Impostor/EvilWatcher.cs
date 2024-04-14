using AmongUs.GameOptions;

using TownOfHostForE.Roles.Core;
using TownOfHostForE.Roles.Core.Interfaces;

namespace TownOfHostForE.Roles.Impostor;
public sealed class EvilWatcher : RoleBase, IImpostor
{
    public static readonly SimpleRoleInfo RoleInfo =
         SimpleRoleInfo.Create(
            typeof(EvilWatcher),
            player => new EvilWatcher(player),
            CustomRoles.EvilWatcher,
            () => RoleTypes.Impostor,
            CustomRoleTypes.Impostor,
            10000,
            SetupOptionItem,
            "イビルウォッチャー"
        );
    public EvilWatcher(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        KillCooldown = OptionKillCooldown.GetFloat();
    }
    private static OptionItem OptionKillCooldown;
    enum OptionName
    {
        AmbitionerKillCoolDecreaseRate,
    }
    private static float KillCooldown;

    public static void SetupOptionItem()
    {
        OptionKillCooldown = FloatOptionItem.Create(RoleInfo, 10, GeneralOption.KillCooldown, new(0f, 180f, 2.5f), 30f, false)
            .SetValueFormat(OptionFormat.Seconds);
    }
    public override void Add()
    {
        var playerId = Player.PlayerId;
    }
    public float CalculateKillCooldown() => KillCooldown;

    public override void ApplyGameOptions(IGameOptions opt)
    {
        opt.SetBool(BoolOptionNames.AnonymousVotes, false);
    }
}