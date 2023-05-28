using AmongUs.GameOptions;

using TownOfHost.Roles.Core;

namespace TownOfHost.Roles.Vanilla;

public sealed class Crewmate : RoleBase
{
    public static readonly SimpleRoleInfo RoleInfo =
        new(
            typeof(Crewmate),
            player => new Crewmate(player),
            CustomRoles.Crewmate,
            () => RoleTypes.Crewmate,
            CustomRoleTypes.Crewmate,
            -1,
            null,
            null
        );
    public Crewmate(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    { }
}