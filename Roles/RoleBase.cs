using System.Collections.Generic;
using UnityEngine;

using static TownOfHost.Options;

namespace TownOfHost.Roles;

public abstract class RoleBase
{
    public List<byte> PlayerIdList;
    public CustomRoles RoleName;
    public RoleType CustomRoleType;
    public Color32 RoleColor;
    public string RoleColorCode;
    public int ConfigId;
    public TabGroup Tab;
    public OptionItem RoleOption => CustomRoleSpawnChances[RoleName];
    public bool IsEnable = false;


    public RoleBase(
        CustomRoles roleName,
        RoleType type,
        int configId,
        string colorCode = "",
        TabGroup tab = TabGroup.MainSettings
    )
    {
    }
}