using UnityEngine;

namespace TownOfHost.Roles;

public abstract class RoleInfoBase
{
    public CustomRoles RoleName;
    public CustomRoleTypes CustomRoleType;
    public Color32 RoleColor;
    public string RoleColorCode;
    public RoleInfoBase(
        CustomRoles roleName,
        CustomRoleTypes type,
        string colorCode
    )
    {
        RoleName = roleName;
        CustomRoleType = type;

        if (colorCode == "")
            colorCode = type switch
            {
                CustomRoleTypes.Impostor or CustomRoleTypes.Madmate => "#ff1919",
                _ => "#ffffff"
            };
        RoleColorCode = colorCode;

        RoleColor = Utils.GetRoleColor(roleName);
    }
    public delegate void OptionCreatorDelegate();
}