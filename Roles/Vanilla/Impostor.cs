using AmongUs.GameOptions;

using TownOfHost.Roles.Core;
using TownOfHost.Roles.Core.Interfaces;

namespace TownOfHost.Roles.Vanilla;

public sealed class Impostor : RoleBase, IImpostor, IKiller
{
    public static readonly SimpleRoleInfo RoleInfo =
        new(
            typeof(Impostor),
            player => new Impostor(player),
            CustomRoles.Impostor,
            () => RoleTypes.Impostor,
            CustomRoleTypes.Impostor,
            -1,
            null,
            null
        );
    public Impostor(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    { }
}