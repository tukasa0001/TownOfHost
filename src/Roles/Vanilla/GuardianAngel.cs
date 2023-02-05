using AmongUs.GameOptions;

namespace TownOfHost.Roles;

public class GuardianAngel: CustomRole
{
    protected override RoleModifier Modify(RoleModifier roleModifier) => roleModifier.VanillaRole(RoleTypes.GuardianAngel);
}