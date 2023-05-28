using AmongUs.GameOptions;

using TownOfHost.Roles.Core;

namespace TownOfHost.Roles.Vanilla;

public sealed class Engineer : RoleBase
{
    public static readonly SimpleRoleInfo RoleInfo =
        new(
            typeof(Engineer),
            player => new Engineer(player),
            CustomRoles.Engineer,
            () => RoleTypes.Engineer,
            CustomRoleTypes.Crewmate,
            -1,
            null,
            null,
            "#8cffff"
        );
    public Engineer(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    { }

    public override string GetAbilityButtonText() => StringNames.VentAbility.ToString();
}