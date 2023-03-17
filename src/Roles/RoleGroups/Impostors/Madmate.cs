
using VentLib.Logging;

namespace TOHTOR.Roles.RoleGroups.Impostors;

public class Madmate: Vanilla.Impostor
{
    protected override RoleModifier Modify(RoleModifier roleModifier)
    {
        base.Modify(roleModifier);
        VentLogger.Warn($"{this.RoleName} Not Implemented Yet", "RoleImplementation");
        return roleModifier;
    }
}