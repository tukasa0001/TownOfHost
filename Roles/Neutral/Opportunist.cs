using AmongUs.GameOptions;

using TownOfHost.Roles.Core;

namespace TownOfHost.Roles.Neutral;

public sealed class Opportunist : RoleBase
{
    public static SimpleRoleInfo RoleInfo =
        new(
            typeof(Opportunist),
            player => new Opportunist(player),
            CustomRoles.Opportunist,
            () => RoleTypes.Crewmate,
            CustomRoleTypes.Neutral,
            50100,
            null,
            "#00ff00"
        );
    public Opportunist(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    { }
}
