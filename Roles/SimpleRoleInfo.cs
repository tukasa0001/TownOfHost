using System;
using System.Reflection;
using UnityEngine;

using static TownOfHost.Options;

namespace TownOfHost.Roles;

public class SimpleRoleInfo
{
    public Type ClassType;
    public CustomRoles RoleName;
    public CustomRoleTypes CustomRoleType;
    public Color32 RoleColor;
    public string RoleColorCode;
    public int ConfigId;
    public TabGroup Tab;
    public OptionItem RoleOption => CustomRoleSpawnChances[RoleName];
    public bool IsEnable = false;
    public OptionCreatorDelegate OptionCreator;

    public SimpleRoleInfo(
        Type classType,
        CustomRoles roleName,
        CustomRoleTypes customRoleType,
        int configId,
        OptionCreatorDelegate optionCreator,
        string colorCode = "",
        TabGroup tab = TabGroup.MainSettings
    )
    {
        ClassType = classType;
        RoleName = roleName;
        CustomRoleType = customRoleType;
        ConfigId = configId;
        OptionCreator = optionCreator;

        if (colorCode == "")
            colorCode = customRoleType switch
            {
                CustomRoleTypes.Impostor or CustomRoleTypes.Madmate => "#ff1919",
                _ => "#ffffff"
            };
        RoleColorCode = colorCode;

        RoleColor = Utils.GetRoleColor(roleName);

        if (tab == TabGroup.MainSettings)
            tab = CustomRoleType switch
            {
                CustomRoleTypes.Impostor => TabGroup.ImpostorRoles,
                CustomRoleTypes.Madmate => TabGroup.ImpostorRoles,
                CustomRoleTypes.Crewmate => TabGroup.CrewmateRoles,
                CustomRoleTypes.Neutral => TabGroup.NeutralRoles,
                _ => tab
            };
        Tab = tab;

        CustomRoleManager.AllRolesInfo.Add(this);
    }
    public delegate void OptionCreatorDelegate();
}