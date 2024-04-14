using AmongUs.GameOptions;

using TownOfHostForE.Roles.Core;
using TownOfHostForE.Roles.Core.Interfaces;

namespace TownOfHostForE.Roles.Vanilla;

public sealed class Shapeshifter : RoleBase, IImpostor, IKiller, ISidekickable
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.CreateForVanilla(
            typeof(Shapeshifter),
            player => new Shapeshifter(player),
            RoleTypes.Shapeshifter,
            canMakeMadmate: true
        );
    public Shapeshifter(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    { }
}