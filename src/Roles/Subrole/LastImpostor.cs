using VentLib.Logging;

namespace TOHTOR.Roles.Subrole;

public class LastImpostor : Subrole
{
    protected override RoleModifier Modify(RoleModifier roleModifier)
    {
        base.Modify(roleModifier);
        VentLogger.Warn($"{this.RoleName} Not Implemented Yet", "RoleImplementation");
        return roleModifier;
    }
}