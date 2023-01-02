namespace TownOfHost.Roles;

public class Conjuror: CustomRole
{
    protected override RoleModifier Modify(RoleModifier roleModifier)
    {
        Logger.Warn($"{this.RoleName} Not Implemented Yet", "RoleImplementation");
        return roleModifier;
    }
}