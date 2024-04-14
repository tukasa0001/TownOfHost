using System;
using UnityEngine;
using System.Linq;
using AmongUs.GameOptions;
using TownOfHostForE.Roles.Core.Descriptions;


using static TownOfHostForE.Options;

namespace TownOfHostForE.Roles.Core;

public class SimpleRoleInfo
{
    public Type ClassType;
    public Func<PlayerControl, RoleBase> CreateInstance;
    public CustomRoles RoleName;
    public Func<RoleTypes> BaseRoleType;
    public CustomRoleTypes CustomRoleType;
    public CountTypes CountType;
    public Color RoleColor;
    public string RoleColorCode;
    public int ConfigId;
    public TabGroup Tab;
    public OptionItem RoleOption => CustomRoleSpawnChances[RoleName];
    public bool IsEnable = false;
    public OptionCreatorDelegate OptionCreator;
    public string ChatCommand;
    /// <summary>本人視点のみインポスターに見える役職</summary>
    public bool IsDesyncImpostor;
    private Func<AudioClip> introSound;
    public AudioClip IntroSound => introSound?.Invoke();
    private Func<bool> canMakeMadmate;
    public bool CanMakeMadmate => canMakeMadmate?.Invoke() == true;
    public RoleAssignInfo AssignInfo { get; }
    /// <summary>役職の説明関係</summary>
    public RoleDescription Description { get; private set; }

    private SimpleRoleInfo(
        Type classType,
        Func<PlayerControl, RoleBase> createInstance,
        CustomRoles roleName,
        Func<RoleTypes> baseRoleType,
        CustomRoleTypes customRoleType,
        CountTypes countType,
        int configId,
        OptionCreatorDelegate optionCreator,
        string chatCommand,
        string colorCode,
        bool isDesyncImpostor,
        TabGroup tab,
        Func<AudioClip> introSound,
        Func<bool> canMakeMadmate,
        RoleAssignInfo assignInfo
    )
    {
        ClassType = classType;
        CreateInstance = createInstance;
        RoleName = roleName;
        BaseRoleType = baseRoleType;
        CustomRoleType = customRoleType;
        CountType = countType;
        ConfigId = configId;
        OptionCreator = optionCreator;
        IsDesyncImpostor = isDesyncImpostor;
        this.introSound = introSound;
        this.canMakeMadmate = canMakeMadmate;
        ChatCommand = chatCommand;
        AssignInfo = assignInfo;

        if (colorCode == "")
            colorCode = customRoleType switch
            {
                CustomRoleTypes.Impostor or CustomRoleTypes.Madmate => "#ff1919",
                CustomRoleTypes.Crewmate => "#8cffff",
                _ => "#ffffff"
            };
        RoleColorCode = colorCode;

        _ = ColorUtility.TryParseHtmlString(colorCode, out RoleColor);

        if (tab == TabGroup.MainSettings)
            tab = CustomRoleType switch
            {
                CustomRoleTypes.Impostor => TabGroup.ImpostorRoles,
                CustomRoleTypes.Madmate => TabGroup.MadmateRoles,
                CustomRoleTypes.Crewmate => TabGroup.CrewmateRoles,
                CustomRoleTypes.Neutral => TabGroup.NeutralRoles,
                CustomRoleTypes.Animals => TabGroup.AnimalsRoles,
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
        bool isDesyncImpostor = false,
        TabGroup tab = TabGroup.MainSettings,
        Func<AudioClip> introSound = null,
        Func<bool> canMakeMadmate = null,
        CountTypes? countType = null,
        RoleAssignInfo assignInfo = null
    )
    {
        countType ??= customRoleType == CustomRoleTypes.Impostor ?
            CountTypes.Impostor :
            CountTypes.Crew;
        assignInfo ??= new RoleAssignInfo(roleName, customRoleType);

        var roleInfo = new SimpleRoleInfo(
            classType,
            createInstance,
            roleName,
            baseRoleType,
            customRoleType,
            countType.Value,
            configId,
            optionCreator,
            chatCommand,
            colorCode,
            isDesyncImpostor,
            tab,
            introSound,
            canMakeMadmate,
            assignInfo);
        roleInfo.Description = new SingleRoleDescription(roleInfo);
        return roleInfo;
    }
    public static SimpleRoleInfo CreateForVanilla(
        Type classType,
        Func<PlayerControl, RoleBase> createInstance,
        RoleTypes baseRoleType,
        string colorCode = "",
        bool canMakeMadmate = false,
        RoleAssignInfo assignInfo = null
    )
    {
        CustomRoles roleName;
        CustomRoleTypes customRoleType;
        CountTypes countType = CountTypes.Crew;

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
                countType = CountTypes.Impostor;
                break;
            case RoleTypes.Shapeshifter:
                roleName = CustomRoles.Shapeshifter;
                customRoleType = CustomRoleTypes.Impostor;
                countType = CountTypes.Impostor;
                break;
            default:
                roleName = CustomRoles.Crewmate;
                customRoleType = CustomRoleTypes.Crewmate;
                break;
        }
        var roleInfo = new SimpleRoleInfo(
            classType,
            createInstance,
            roleName,
            () => baseRoleType,
            customRoleType,
            countType,
            -1,
            null,
            null,
            colorCode,
            false,
            TabGroup.MainSettings,
            null,
            () => canMakeMadmate,
            assignInfo ?? new(roleName, customRoleType));
        roleInfo.Description = new VanillaRoleDescription(roleInfo, baseRoleType);
        return roleInfo;
    }
    public delegate void OptionCreatorDelegate();
}