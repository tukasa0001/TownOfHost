using AmongUs.GameOptions;

namespace TOHTOR.Roles;

public class GuardianAngel: CustomRole
{
    protected override RoleModifier Modify(RoleModifier roleModifier) => roleModifier.VanillaRole(RoleTypes.GuardianAngel);
}