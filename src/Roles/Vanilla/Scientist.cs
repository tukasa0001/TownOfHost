using AmongUs.GameOptions;

namespace TOHTOR.Roles;

public class Scientist: Crewmate
{
    protected override RoleModifier Modify(RoleModifier roleModifier) => roleModifier.VanillaRole(RoleTypes.Scientist);
}