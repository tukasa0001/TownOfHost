using System;
using TOHTOR.GUI.Patches;
using TOHTOR.Roles.Internals.Attributes;
using UnityEngine;
using VentLib.Utilities.Optionals;

namespace TOHTOR.Roles;

public class Dictator: Crewmate
{
    [RoleAction(RoleActionType.MyVote)]
    private void DictatorVote(Optional<PlayerControl> target)
    {
        if (!target.Exists()) return;
        MeetingHud.VoterState[] voterStates = Array.Empty<MeetingHud.VoterState>();
        CheckForEndVotingPatch.EndVoting(MeetingHud.Instance, voterStates, target.Get().Data, false);
    }

    protected override RoleModifier Modify(RoleModifier roleModifier) =>
        base.Modify(roleModifier).RoleColor(new Color(0.87f, 0.61f, 0f));
}