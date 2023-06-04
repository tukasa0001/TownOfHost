using AmongUs.GameOptions;

using TownOfHost.Roles.Core;
using TownOfHost.Roles.Core.Interfaces;

namespace TownOfHost.Roles.Vanilla;

public sealed class Impostor : RoleBase, IImpostor
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.CreateForVanilla(
            typeof(Impostor),
            player => new Impostor(player),
            RoleTypes.Impostor
        );
    public Impostor(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    { }
}