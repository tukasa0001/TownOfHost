using AmongUs.GameOptions;
using TownOfHost.Options;
using VentLib.Logging;

namespace TownOfHost.Roles;

public class ImpostorGhost : GuardianAngel
{
    protected override RoleModifier Modify(RoleModifier roleModifier)
    {
        base.Modify(roleModifier)
         .VanillaRole(RoleTypes.CrewmateGhost);
        VentLogger.Warn($"{this.RoleName} Not Implemented Yet", "RoleImplementation");
        return roleModifier;
    }

    protected override SmartOptionBuilder RegisterOptions(SmartOptionBuilder optionStream)
    {
        return new SmartOptionBuilder();
    }
}