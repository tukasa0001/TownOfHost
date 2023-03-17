using AmongUs.GameOptions;

namespace TOHTOR.Roles.RoleGroups.Vanilla;

public class GuardianAngel: CustomRole
{
    public override bool CanBeKilled() => false;

    protected override RoleModifier Modify(RoleModifier roleModifier) => roleModifier.VanillaRole(RoleTypes.GuardianAngel);
}