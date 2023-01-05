using AmongUs.GameOptions;
using TownOfHost.ReduxOptions;

namespace TownOfHost.Roles;

public class ImpostorGhost : GuardianAngel
{
    protected override RoleModifier Modify(RoleModifier roleModifier)
    {
        base.Modify(roleModifier)
         .VanillaRole(RoleTypes.CrewmateGhost);
        Logger.Warn($"{this.RoleName} Not Implemented Yet", "RoleImplementation");
        return roleModifier;
    }

    protected override SmartOptionBuilder RegisterOptions(SmartOptionBuilder optionStream)
    {
        return new SmartOptionBuilder();
    }
}