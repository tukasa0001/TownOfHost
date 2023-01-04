using AmongUs.GameOptions;
using HarmonyLib;
using TownOfHost.Extensions;
using TownOfHost.ReduxOptions;
using UnityEngine;

namespace TownOfHost.Roles;

public class TimeThief : Impostor
{
    private int meetingTimeSubtractor;
    private int minimumVotingTime;
    private bool returnTimeAfterDeath;

    [RoleAction(RoleActionType.AttemptKill)]
    public new bool TryKill(PlayerControl target) => base.TryKill(target);

    [RoleAction(RoleActionType.RoundEnd)]
    private void TimeThiefSubtractMeetingTime()
    {
        GameOptionOverride[] overrides = null;
        if (!((MyPlayer.Data.IsDead || MyPlayer.Data.Disconnected) && returnTimeAfterDeath))
        {
            NormalGameOptionsV07 normalOptions = DesyncOptions.OriginalHostOptions.AsNormalOptions();
            int discussionTime = normalOptions.DiscussionTime;
            int votingTime = normalOptions.VotingTime;

            int remainingStolenTime = 0;
            discussionTime -= meetingTimeSubtractor;
            if (discussionTime < 0)
            {
                remainingStolenTime = discussionTime;
                discussionTime = 1;
            }

            Logger.Msg($"{MyPlayer.GetDynamicName().RawName} | Time Thief | Meeting Time: {discussionTime} | Voting Time: {votingTime}", "TimeThiefStolen");
            votingTime = Mathf.Clamp(votingTime - remainingStolenTime, minimumVotingTime, votingTime);
            overrides = new GameOptionOverride[] { new(Override.DiscussionTime, discussionTime), new(Override.VotingTime, votingTime) };
        }
        Game.GetAllPlayers().Do(p => p.GetCustomRole().SyncOptions(overrides));
    }

    protected override SmartOptionBuilder RegisterOptions(SmartOptionBuilder optionStream) =>
        base.RegisterOptions(optionStream)
            .AddSubOption(sub => sub
                .Name("Meeting Time Stolen")
                .Bind(v => meetingTimeSubtractor = (int)v)
                .AddIntRangeValues(5, 120, 5, 4, "s")
                .Build())
            .AddSubOption(sub => sub
                .Name("Minimum Voting Time")
                .Bind(v => minimumVotingTime = (int)v)
                .AddIntRangeValues(5, 120, 5, 2, "s")
                .Build())
            .AddSubOption(sub => sub
                .Name("Return Stolen Time After Death")
                .Bind(v => returnTimeAfterDeath = (bool)v)
                .AddOnOffValues()
                .Build());
}