using AmongUs.GameOptions;

using TownOfHost.Roles.Core;
using TownOfHost.Roles.Core.Interfaces;

namespace TownOfHost.Roles.Vanilla;

public sealed class Shapeshifter : RoleBase, IImpostor, IKiller, ISidekickable
{
    public static readonly SimpleRoleInfo RoleInfo =
        new(
            typeof(Shapeshifter),
            player => new Shapeshifter(player),
            CustomRoles.Shapeshifter,
            () => RoleTypes.Shapeshifter,
            CustomRoleTypes.Impostor,
            -1,
            null,
            null,
            canMakeMadmate: () => true
        );
    public Shapeshifter(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    { }

    public override string GetAbilityButtonText() => StringNames.ShapeshiftAbility.ToString();
}