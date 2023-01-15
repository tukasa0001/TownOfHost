using AmongUs.GameOptions;
using TownOfHost.Options;

namespace TownOfHost.Roles;

public class GuardianAngel: CustomRole
{
    protected override SmartOptionBuilder RegisterOptions(SmartOptionBuilder optionStream)
    {
        return new SmartOptionBuilder();
    }

    protected override RoleModifier Modify(RoleModifier roleModifier) => roleModifier.VanillaRole(RoleTypes.GuardianAngel);
}