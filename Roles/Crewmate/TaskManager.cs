using AmongUs.GameOptions;

using TownOfHostForE.Roles.Core;
using static TownOfHostForE.Utils;

namespace TownOfHostForE.Roles.Crewmate;
public sealed class TaskManager : RoleBase
{
    public static readonly SimpleRoleInfo RoleInfo =
         SimpleRoleInfo.Create(
            typeof(TaskManager),
            player => new TaskManager(player),
            CustomRoles.TaskManager,
            () => RoleTypes.Crewmate,
            CustomRoleTypes.Crewmate,
            40100,
            SetupOptionItem,
            "タスクマネージャー",
            "#80ffdd",
            introSound: () => GetIntroSound(RoleTypes.Scientist)
        );
    public TaskManager(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        //SeeNowtask = OptionSeeNowtask.GetBool();
    }

    private static OptionItem OptionSeeNowtask;
    enum OptionName
    {
        TaskmanagerSeeNowtask,
    }
    private static bool SeeNowtask = true;

    private static void SetupOptionItem()
    {
        //OptionSeeNowtask = BooleanOptionItem.Create(RoleInfo, 10, OptionName.TaskmanagerSeeNowtask, false, false);
    }

    public override string GetProgressText(bool comms = false)
    {
        var nowtask = "?";
        int completetask;
        int alltask;
        (completetask, alltask) = GetTasksState();

        if ((GameStates.IsMeeting || !Player.IsAlive() || SeeNowtask)
            && !comms)
            nowtask = $"{completetask}";

        return ColorString(RoleInfo.RoleColor, $"({nowtask}/{alltask})");
    }
}