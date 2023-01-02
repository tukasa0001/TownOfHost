namespace TownOfHost.Roles;

public class Opportunist: CustomRole
{
    protected override RoleModifier Modify(RoleModifier roleModifier)
    {
        roleModifier.RoleColor("#00ff00");
        Logger.Warn($"{this.RoleName} Not Implemented Yet", "RoleImplementation");
        return roleModifier;
    }
}