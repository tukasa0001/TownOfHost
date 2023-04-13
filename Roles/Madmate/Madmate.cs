using AmongUs.GameOptions;

using TownOfHost.Roles.Core;

namespace TownOfHost.Roles.Madmate;
public sealed class Madmate : RoleBase
{
    public static SimpleRoleInfo RoleInfo =
        new(
            typeof(Madmate),
            player => new Madmate(player),
            CustomRoles.Madmate,
            () => RoleTypes.Engineer,
            CustomRoleTypes.Madmate,
            10000,
            null
        );
    public Madmate(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    { }
}