using AmongUs.GameOptions;

using TownOfHost.Roles.Core;

namespace TownOfHost.Roles.Vanilla;

public sealed class GuardianAngel : RoleBase
{
    public static readonly SimpleRoleInfo RoleInfo =
        new(
            typeof(GuardianAngel),
            player => new GuardianAngel(player),
            CustomRoles.GuardianAngel,
            () => RoleTypes.GuardianAngel,
            CustomRoleTypes.Crewmate,
            -1,
            null,
            null
        );
    public GuardianAngel(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    { }

    public override string GetAbilityButtonText() => StringNames.ProtectAbility.ToString();
}