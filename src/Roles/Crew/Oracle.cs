using UnityEngine;

namespace TOHTOR.Roles;

public class Oracle: Crewmate
{





    protected override RoleModifier Modify(RoleModifier roleModifier) =>
        base.Modify(roleModifier)
            .RoleColor(new Color(0.78f, 0.55f, 0.82f));
}