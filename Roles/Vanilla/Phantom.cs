using AmongUs.GameOptions;
using TownOfHost.Roles.Core;
using TownOfHost.Roles.Core.Interfaces;

namespace TownOfHost.Roles.Vanilla;

public sealed class Phantom : RoleBase, IImpostor
{
    public Phantom(PlayerControl player) : base(RoleInfo, player) { }
    public static readonly SimpleRoleInfo RoleInfo = SimpleRoleInfo.CreateForVanilla(typeof(Phantom), player => new Phantom(player), RoleTypes.Phantom);
}
