using System.Collections.Generic;
using AmongUs.GameOptions;

using TownOfHost.Roles.Core;
using static TownOfHost.CheckForEndVotingPatch;

namespace TownOfHost.Roles.Crewmate;
public sealed class Dictator : RoleBase
{
    public static readonly SimpleRoleInfo RoleInfo =
        new(
            typeof(Dictator),
            player => new Dictator(player),
            CustomRoles.Dictator,
            () => RoleTypes.Crewmate,
            CustomRoleTypes.Crewmate,
            20900,
            null,
            "#df9b00"
        );
    public Dictator(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    { }
    public override bool OnCheckForEndVoting(ref List<MeetingHud.VoterState> statesList, PlayerVoteArea pva)
    {
        //死んでいないディクテーターが投票済み
        if (pva.DidVote && Player.PlayerId != pva.VotedFor && pva.VotedFor < 253 && Player.IsAlive())
        {
            var voteTarget = Utils.GetPlayerById(pva.VotedFor);
            TryAddAfterMeetingDeathPlayers(CustomDeathReason.Suicide, Player.PlayerId);
            statesList.Add(new()
            {
                VoterId = pva.TargetPlayerId,
                VotedForId = pva.VotedFor
            });
            var states = statesList.ToArray();
            if (AntiBlackout.OverrideExiledPlayer)
            {
                MeetingHud.Instance.RpcVotingComplete(states, null, true);
                ExileControllerWrapUpPatch.AntiBlackout_LastExiled = voteTarget.Data;
            }
            else MeetingHud.Instance.RpcVotingComplete(states, voteTarget.Data, false); //通常処理

            CheckForDeathOnExile(CustomDeathReason.Vote, pva.VotedFor);
            Logger.Info($"ディクテーターによる強制会議終了(追放者:{voteTarget.GetNameWithRole()})", "Special Phase");
            voteTarget.SetRealKiller(Player);
        }
        return false;
    }
}