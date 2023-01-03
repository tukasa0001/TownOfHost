using UnityEngine;

namespace TownOfHost.Roles;

public class GM : Crewmate
{
    protected override RoleModifier Modify(RoleModifier roleModifier) =>
        base.Modify(roleModifier)
            .RoleColor(new Color(1f, 0.4f, 0.4f));
}