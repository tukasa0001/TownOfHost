using AmongUs.GameOptions;
using TOHTOR.Roles.Internals;

namespace TOHTOR.Roles.RoleGroups.Vanilla;

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