using TOHTOR.Roles.RoleGroups.Vanilla;
using UnityEngine;

namespace TOHTOR.Roles.Extra;

public class GM : Crewmate
{
    protected override RoleModifier Modify(RoleModifier roleModifier) =>
        base.Modify(roleModifier)
            .RoleColor(new Color(1f, 0.4f, 0.4f));
}