using AmongUs.GameOptions;
using TownOfHostForE.Roles.Core;

namespace TownOfHostForE.Roles.Crewmate;
using static TownOfHostForE.Utils;

public sealed class Sympathizer : RoleBase
{
    public static readonly SimpleRoleInfo RoleInfo =
         SimpleRoleInfo.Create(
            typeof(Sympathizer),
            player => new Sympathizer(player),
            CustomRoles.Sympathizer,
            () => RoleTypes.Crewmate,
            CustomRoleTypes.Crewmate,
            40700,
            SetupOptionItem,
            "共鳴者",
            "#f08080",
            introSound: () => DestroyableSingleton<HudManager>.Instance.TaskUpdateSound
        );
    public Sympathizer(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        SympaCheckedTasks = OptionSympaCheckedTasks.GetInt();
    }

    private static OptionItem OptionSympaCheckedTasks;
    enum OptionName
    {
        SympaCheckedTasks,
    }

    private static int SympaCheckedTasks;

    private static void SetupOptionItem()
    {
        OptionSympaCheckedTasks = IntegerOptionItem.Create(RoleInfo, 10, OptionName.SympaCheckedTasks, new(1, 20, 1), 5, false)
            .SetValueFormat(OptionFormat.Pieces);
    }
    public override string GetMark(PlayerControl seer, PlayerControl seen, bool isForMeeting = false)
    {
        //seenが省略の場合seer
        seen ??= seer;

        if (seer.Is(CustomRoles.Sympathizer) && seen.Is(CustomRoles.Sympathizer)
            && seer.GetPlayerTaskState().CompletedTasksCount >= SympaCheckedTasks
            && seen.GetPlayerTaskState().CompletedTasksCount >= SympaCheckedTasks)
            return ColorString(RoleInfo.RoleColor, "◎");

        return string.Empty;
    }
}