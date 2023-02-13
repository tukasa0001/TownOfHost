using System.Collections.Generic;
using UnityEngine;

using static TownOfHost.Options;

namespace TownOfHost.Roles;

public class RoleInfoBase
{
    public static RoleInfoBase Instance;
    public List<byte> PlayerIdList;
    public CustomRoles RoleName;
    public RoleType CustomRoleType;
    public Color32 RoleColor;
    public string RoleColorCode;
    public int ConfigId;
    public TabGroup Tab;
    public OptionItem RoleOption => CustomRoleSpawnChances[RoleName];
    public bool IsEnable = false;
    public RoleInfoBase(
        CustomRoles roleName,
        RoleType type,
        int configId,
        string colorCode = "",
        TabGroup tab = TabGroup.MainSettings
    )
    {
        RoleName = roleName;
        CustomRoleType = type;
        ConfigId = configId;

        if (colorCode == "")
            colorCode = type switch
            {
                RoleType.Impostor or RoleType.Madmate => "#ff1919",
                _ => "#ffffff"
            };
        RoleColorCode = colorCode;

        if (tab == TabGroup.MainSettings)
            tab = type switch
            {
                RoleType.Impostor => TabGroup.ImpostorRoles,
                RoleType.Madmate => TabGroup.ImpostorRoles,
                RoleType.Crewmate => TabGroup.CrewmateRoles,
                RoleType.Neutral => TabGroup.NeutralRoles,
                _ => tab
            };
        Tab = tab;


        RoleColor = Utils.GetRoleColor(roleName);
        CustomRoleManager.AllRoleBasicInfo.Add(this);
        Instance = this;
    }
    public virtual void SetupCustomOption() => SetupRoleOptions(ConfigId, Tab, RoleName);
}