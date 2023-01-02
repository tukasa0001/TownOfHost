using UnityEngine;

namespace TownOfHost.Roles;

public class Survivor: CustomRole
{
    protected override RoleModifier Modify(RoleModifier roleModifier)
    {
        roleModifier.RoleColor(new Color(1f, 0.9f, 0.3f));
        Logger.Warn($"{this.RoleName} Not Implemented Yet", "RoleImplementation");
        return roleModifier;
    }
}