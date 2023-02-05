using AmongUs.GameOptions;
using VentLib.Options;
using VentLib.Logging;

namespace TownOfHost.Roles;

public class CrewmateGhost : GuardianAngel
{
    protected override RoleModifier Modify(RoleModifier roleModifier)
    {
        base.Modify(roleModifier)
        .VanillaRole(RoleTypes.CrewmateGhost);
        VentLogger.Warn($"{this.RoleName} Not Implemented Yet", "RoleImplementation");
        return roleModifier;
    }

    protected override OptionBuilder RegisterOptions(OptionBuilder optionStream)
    {
        return new OptionBuilder();
    }
}