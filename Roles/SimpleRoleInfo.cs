using System;
using UnityEngine;
using AmongUs.GameOptions;

using static TownOfHost.Options;

namespace TownOfHost.Roles;

public class SimpleRoleInfo
{
    public Type ClassType;
    public CustomRoles RoleName;
    public RoleTypes BaseRoleType;
    public CustomRoleTypes CustomRoleType;
    public Color32 RoleColor;
    public string RoleColorCode;
    public int ConfigId;
    public TabGroup Tab;
    public OptionItem RoleOption => CustomRoleSpawnChances[RoleName];
    public bool IsEnable = false;
    public OptionCreatorDelegate OptionCreator;
    public bool RequireResetCam;

    public SimpleRoleInfo(
        Type classType,
        CustomRoles roleName,
        RoleTypes baseRoleType,
        CustomRoleTypes customRoleType,
        int configId,
        OptionCreatorDelegate optionCreator,
        string colorCode = "",
        bool requireResetCam = false,
        TabGroup tab = TabGroup.MainSettings
    )
    {
        ClassType = classType;
        RoleName = roleName;
        BaseRoleType = baseRoleType;
        CustomRoleType = customRoleType;
        ConfigId = configId;
        OptionCreator = optionCreator;
        RequireResetCam = requireResetCam;

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