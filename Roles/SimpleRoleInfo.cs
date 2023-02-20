
using static TownOfHost.Options;

namespace TownOfHost.Roles;

public class SimpleRoleInfo : RoleInfoBase
{
    public int ConfigId;
    public TabGroup Tab;
    public OptionItem RoleOption => CustomRoleSpawnChances[RoleName];
    public bool IsEnable = false;
    public OptionCreatorDelegate OptionCreator;

    public SimpleRoleInfo(
        CustomRoles roleName,
        CustomRoleTypes type,
        int configId,
        OptionCreatorDelegate optionCreator,
        string colorCode = "",
        TabGroup tab = TabGroup.MainSettings
    ) :
    base(
        roleName,
        type,
        colorCode
    )
    {
        ConfigId = configId;
        OptionCreator = optionCreator;

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
}