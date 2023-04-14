using AmongUs.GameOptions;

using TownOfHost.Modules.Extensions;
using TownOfHost.Roles.Core;

namespace TownOfHost.Roles.Crewmate;
public sealed class Lighter : RoleBase
{
    public static readonly SimpleRoleInfo RoleInfo =
        new(
            typeof(Lighter),
            player => new Lighter(player),
            CustomRoles.Lighter,
            () => RoleTypes.Crewmate,
            CustomRoleTypes.Crewmate,
            20100,
            SetupOptionItem,
            "#eee5be"
        );
    public Lighter(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        TaskCompletedVision = OptionTaskCompletedVision.GetFloat();
        TaskCompletedDisableLightOut = OptionTaskCompletedDisableLightOut.GetBool();

        IsTaskFinished = false;
    }

    private static OptionItem OptionTaskCompletedVision;
    private static OptionItem OptionTaskCompletedDisableLightOut;
    enum OptionName
    {
        LighterTaskCompletedVision,
        LighterTaskCompletedDisableLightOut
    }

    private static float TaskCompletedVision;
    private static bool TaskCompletedDisableLightOut;

    private bool IsTaskFinished;

    private static void SetupOptionItem()
    {
        OptionTaskCompletedVision = FloatOptionItem.Create(RoleInfo, 10, OptionName.LighterTaskCompletedVision, new(0f, 5f, 0.25f), 2f, false)
            .SetValueFormat(OptionFormat.Multiplier);
        OptionTaskCompletedDisableLightOut = BooleanOptionItem.Create(RoleInfo, 11, OptionName.LighterTaskCompletedDisableLightOut, true, false);
    }

    public override void ApplyGameOptions(IGameOptions opt)
    {
        if (!IsTaskFinished) return;

        var crewLightMod = FloatOptionNames.CrewLightMod;

        opt.SetFloat(crewLightMod, TaskCompletedVision);
        if (TaskCompletedDisableLightOut && Utils.IsActive(SystemTypes.Electrical))
        {
            opt.SetFloat(crewLightMod, TaskCompletedVision * 5);
        }
    }
    public override bool OnCompleteTask()
    {
        if (Player.GetPlayerTaskState().IsTaskFinished)
        {
            IsTaskFinished = true;
            Player.MarkDirtySettings();
        }

        return true;
    }
}