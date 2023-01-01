using AmongUs.GameOptions;

namespace TownOfHost.Roles;

public class Engineer: Crewmate
{
    protected override RoleModifier Modify(RoleModifier roleModifier) => roleModifier.VanillaRole(RoleTypes.Engineer);
}