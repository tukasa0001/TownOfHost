using AmongUs.GameOptions;

namespace TownOfHost.Roles;

public class Engineer: Crewmate
{
    protected float VentCooldown;
    protected float VentDuration;


    protected override RoleModifier Modify(RoleModifier roleModifier) =>
        base.Modify(roleModifier)
            .VanillaRole(RoleTypes.Engineer)
            .OptionOverride(Override.EngVentCooldown, VentCooldown)
            .OptionOverride(Override.EngVentDuration, VentDuration);
}