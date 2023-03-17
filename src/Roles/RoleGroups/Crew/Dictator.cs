using System;
using TOHTOR.API;
using TOHTOR.GUI.Patches;
using TOHTOR.Managers.History.Events;
using TOHTOR.Roles.Internals.Attributes;
using TOHTOR.Roles.RoleGroups.Vanilla;
using UnityEngine;
using VentLib.Utilities.Optionals;

namespace TOHTOR.Roles.RoleGroups.Crew;

public class Dictator: Crewmate
{
    [RoleAction(RoleActionType.MyVote)]
    private void DictatorVote(Optional<PlayerControl> target)
    {
        if (!target.Exists()) return;
        MeetingHud.VoterState[] voterStates = Array.Empty<MeetingHud.VoterState>();
        CheckForEndVotingPatch.EndVoting(MeetingHud.Instance, voterStates, target.Get().Data, false);
        Game.GameHistory.AddEvent(new DictatorVoteEvent(MyPlayer, target.Get()));
    }

    protected override RoleModifier Modify(RoleModifier roleModifier) =>
        base.Modify(roleModifier).RoleColor(new Color(0.87f, 0.61f, 0f));

    private class DictatorVoteEvent : KillEvent, IRoleEvent
    {
        public DictatorVoteEvent(PlayerControl killer, PlayerControl victim) : base(killer, victim)
        {
        }

        public override string Message() => $"{Game.GetName(Player())} lynched {Game.GetName(Target())}.";
    }
}