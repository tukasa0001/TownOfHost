using System.Collections.Generic;
using AmongUs.GameOptions;

using TownOfHost.Roles.Core;

namespace TownOfHost.Roles.Crewmate;
public sealed class Mayor : RoleBase
{
    public static readonly SimpleRoleInfo RoleInfo =
        new(
            typeof(Mayor),
            player => new Mayor(player),
            CustomRoles.Mayor,
            () => OptionHasPortableButton.GetBool() ? RoleTypes.Engineer : RoleTypes.Crewmate,
            CustomRoleTypes.Crewmate,
            20200,
            SetupOptionItem,
            "#204d42"
        );
    public Mayor(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        AdditionalVote = OptionAdditionalVote.GetInt();
        HasPortableButton = OptionHasPortableButton.GetBool();
        NumOfUseButton = OptionNumOfUseButton.GetInt();

        UsedButtonCount = 0;
    }

    private static OptionItem OptionAdditionalVote;
    private static OptionItem OptionHasPortableButton;
    private static OptionItem OptionNumOfUseButton;
    enum OptionName
    {
        MayorAdditionalVote,
        MayorHasPortableButton,
        MayorNumOfUseButton,
    }
    public static int AdditionalVote;
    public static bool HasPortableButton;
    public static int NumOfUseButton;

    public int UsedButtonCount;
    private static void SetupOptionItem()
    {
        var id = RoleInfo.ConfigId;
        var tab = RoleInfo.Tab;
        var parent = RoleInfo.RoleOption;
        OptionAdditionalVote = IntegerOptionItem.Create(id + 10, OptionName.MayorAdditionalVote, new(1, 99, 1), 1, tab, false).SetParent(parent)
            .SetValueFormat(OptionFormat.Votes);
        OptionHasPortableButton = BooleanOptionItem.Create(id + 11, OptionName.MayorHasPortableButton, false, tab, false).SetParent(parent);
        OptionNumOfUseButton = IntegerOptionItem.Create(id + 12, OptionName.MayorNumOfUseButton, new(1, 99, 1), 1, tab, false).SetParent(OptionHasPortableButton)
            .SetValueFormat(OptionFormat.Times);
    }
    public override void ApplyGameOptions(IGameOptions opt)
    {
        AURoleOptions.EngineerCooldown =
            UsedButtonCount < NumOfUseButton
            ? opt.GetInt(Int32OptionNames.EmergencyCooldown)
            : 300f;
        AURoleOptions.EngineerInVentMaxTime = 1;
    }
    public override bool OnReportDeadBody(PlayerControl reporter, GameData.PlayerInfo target)
    {
        if (reporter.Is(CustomRoles.Mayor) && target == null) //ボタン
            UsedButtonCount++;

        return true;
    }
    public override bool OnEnterVent(PlayerPhysics physics, int ventId)
    {
        if (UsedButtonCount >= NumOfUseButton)
        {
            var user = physics.myPlayer;
            physics.RpcBootFromVent(ventId);
            user?.ReportDeadBody(null);

            return false;
        }

        return true;
    }
    public override bool OnCheckForEndVoting(ref List<MeetingHud.VoterState> statesList, PlayerVoteArea pva)
    {
        for (var i = 0; i < AdditionalVote; i++)
        {
            statesList.Add(new MeetingHud.VoterState()
            {
                VoterId = pva.TargetPlayerId,
                VotedForId = pva.VotedFor
            });
        }
        return true;
    }
    public override void AfterMeetingTasks()
    {
        if (HasPortableButton)
            Player.RpcResetAbilityCooldown();
    }
}