using System;
using UnityEngine;
using AmongUs.GameOptions;

using static TownOfHost.Options;

namespace TownOfHost.Roles.Core;

public class SimpleRoleInfo
{
    public Type ClassType;
    public Func<PlayerControl, RoleBase> CreateInstance;
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
        Func<PlayerControl, RoleBase> createInstance,
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
        CreateInstance = createInstance;
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

        CustomRoleManager.AllRolesInfo.Add(roleName, this);
    }
    public delegate void OptionCreatorDelegate();
}