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
    public Func<RoleTypes> BaseRoleType;
    public CustomRoleTypes CustomRoleType;
    public Color RoleColor;
    public string RoleColorCode;
    public int ConfigId;
    public TabGroup Tab;
    public OptionItem RoleOption => CustomRoleSpawnChances[RoleName];
    public bool IsEnable = false;
    public OptionCreatorDelegate OptionCreator;
    public string ChatCommand;
    public bool RequireResetCam;
    private Func<AudioClip> introSound;
    public AudioClip IntroSound => introSound?.Invoke();
    private Func<bool> canMakeMadmate;
    public bool CanMakeMadmate => canMakeMadmate?.Invoke() == true;

    private SimpleRoleInfo(
        Type classType,
        Func<PlayerControl, RoleBase> createInstance,
        CustomRoles roleName,
        Func<RoleTypes> baseRoleType,
        CustomRoleTypes customRoleType,
        int configId,
        OptionCreatorDelegate optionCreator,
        string chatCommand,
        string colorCode,
        bool requireResetCam,
        TabGroup tab,
        Func<AudioClip> introSound,
        Func<bool> canMakeMadmate
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
        this.introSound = introSound;
        this.canMakeMadmate = canMakeMadmate;
        ChatCommand = chatCommand;

        if (colorCode == "")
            colorCode = customRoleType switch
            {
                CustomRoleTypes.Impostor or CustomRoleTypes.Madmate => "#ff1919",
                _ => "#ffffff"
            };
        RoleColorCode = colorCode;

        ColorUtility.TryParseHtmlString(colorCode, out RoleColor);

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
    public static SimpleRoleInfo Create(
        Type classType,
        Func<PlayerControl, RoleBase> createInstance,
        CustomRoles roleName,
        Func<RoleTypes> baseRoleType,
        CustomRoleTypes customRoleType,
        int configId,
        OptionCreatorDelegate optionCreator,
        string chatCommand,
        string colorCode = "",
        bool requireResetCam = false,
        TabGroup tab = TabGroup.MainSettings,
        Func<AudioClip> introSound = null,
        Func<bool> canMakeMadmate = null
    )
    {
        return
            new(
                classType,
                createInstance,
                roleName,
                baseRoleType,
                customRoleType,
                configId,
                optionCreator,
                chatCommand,
                colorCode,
                requireResetCam,
                tab,
                introSound,
                canMakeMadmate
            );
    }
    public static SimpleRoleInfo CreateForVanilla(
        Type classType,
        Func<PlayerControl, RoleBase> createInstance,
        RoleTypes baseRoleType,
        string colorCode = "",
        bool canMakeMadmate = false
    )
    {
        CustomRoles roleName;
        CustomRoleTypes customRoleType;

        switch (baseRoleType)
        {
            case RoleTypes.Engineer:
                roleName = CustomRoles.Engineer;
                customRoleType = CustomRoleTypes.Crewmate;
                break;
            case RoleTypes.Scientist:
                roleName = CustomRoles.Scientist;
                customRoleType = CustomRoleTypes.Crewmate;
                break;
            case RoleTypes.GuardianAngel:
                roleName = CustomRoles.GuardianAngel;
                customRoleType = CustomRoleTypes.Crewmate;
                break;
            case RoleTypes.Impostor:
                roleName = CustomRoles.Impostor;
                customRoleType = CustomRoleTypes.Impostor;
                break;
            case RoleTypes.Shapeshifter:
                roleName = CustomRoles.Shapeshifter;
                customRoleType = CustomRoleTypes.Impostor;
                break;
            default:
                roleName = CustomRoles.Crewmate;
                customRoleType = CustomRoleTypes.Crewmate;
                break;
        }
        return
            new(
                classType,
                createInstance,
                roleName,
                () => baseRoleType,
                customRoleType,
                -1,
                null,
                null,
                colorCode,
                false,
                TabGroup.MainSettings,
                null,
                () => canMakeMadmate
            );
    }
    public delegate void OptionCreatorDelegate();
}