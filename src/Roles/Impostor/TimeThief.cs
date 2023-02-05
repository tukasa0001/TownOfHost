using AmongUs.GameOptions;
using HarmonyLib;
using TownOfHost.Extensions;
using TownOfHost.Managers;
using TownOfHost.Options;
using VentLib.Options;
using TownOfHost.Roles.Internals;
using TownOfHost.Roles.Internals.Attributes;
using UnityEngine;
using VentLib.Logging;

namespace TownOfHost.Roles;

public class TimeThief : Impostor
{
    private int kills;
    private int meetingTimeSubtractor;
    private int minimumVotingTime;
    private bool returnTimeAfterDeath;

    [RoleAction(RoleActionType.AttemptKill)]
    public new bool TryKill(PlayerControl target)
    {
        var flag = base.TryKill(target);
        if (flag)
            kills++;
        return flag;
    }

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
            discussionTime -= meetingTimeSubtractor * kills;
            if (discussionTime < 0)
            {
                remainingStolenTime = discussionTime;
                discussionTime = 1;
            }

            VentLogger.Info($"{MyPlayer.GetDynamicName().RawName} | Time Thief | Meeting Time: {discussionTime} | Voting Time: {votingTime}", "TimeThiefStolen");
            votingTime = Mathf.Clamp(votingTime - remainingStolenTime, minimumVotingTime, votingTime);
            overrides = new GameOptionOverride[] { new(Override.DiscussionTime, discussionTime), new(Override.VotingTime, votingTime) };
        }
        Game.GetAllPlayers().Do(p => p.GetCustomRole().SyncOptions(overrides));
    }

    protected override OptionBuilder RegisterOptions(OptionBuilder optionStream) =>
        base.RegisterOptions(optionStream)
            .SubOption(sub => sub
                .Name("Meeting Time Stolen")
                .Bind(v => meetingTimeSubtractor = (int)v)
                .AddIntRange(5, 120, 5, 4, "s")
                .Build())
            .SubOption(sub => sub
                .Name("Minimum Voting Time")
                .Bind(v => minimumVotingTime = (int)v)
                .AddIntRange(5, 120, 5, 1, "s")
                .Build())
            .SubOption(sub => sub
                .Name("Return Stolen Time After Death")
                .Bind(v => returnTimeAfterDeath = (bool)v)
                .AddOnOffValues()
                .Build());
}