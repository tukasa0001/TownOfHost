using AmongUs.GameOptions;
using TownOfHost.Roles.Core;

namespace TownOfHost.Roles.Vanilla;

public sealed class Scientist : RoleBase
{
    public static readonly SimpleRoleInfo RoleInfo =
        new(
            typeof(Scientist),
            player => new Scientist(player),
            CustomRoles.Scientist,
            () => RoleTypes.Scientist,
            CustomRoleTypes.Crewmate,
            -1,
            null,
            null,
            "#8cffff"
        );
    public Scientist(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    { }

    public override string GetAbilityButtonText() => StringNames.VitalsAbility.ToString();
}