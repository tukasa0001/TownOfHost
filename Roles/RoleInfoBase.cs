using System.Collections.Generic;
using UnityEngine;

using static TownOfHost.Options;

namespace TownOfHost.Roles;

public abstract class RoleInfoBase
{
    public CustomRoles RoleName;
    public RoleType CustomRoleType;
    public Color32 RoleColor;
    public string RoleColorCode;
    public RoleInfoBase(
        CustomRoles roleName,
        RoleType type,
        string colorCode
    )
    {
        RoleName = roleName;
        CustomRoleType = type;

        if (colorCode == "")
            colorCode = type switch
            {
                RoleType.Impostor or RoleType.Madmate => "#ff1919",
                _ => "#ffffff"
            };
        RoleColorCode = colorCode;

        RoleColor = Utils.GetRoleColor(roleName);
    }
    public delegate void OptionCreatorDelegate();
}