using TOHTOR.API;
using TOHTOR.Extensions;
using TOHTOR.GUI;
using TOHTOR.Roles.Internals;
using TOHTOR.Roles.Internals.Attributes;
using UnityEngine;
using VentLib.Localization.Attributes;
using VentLib.Utilities.Optionals;

namespace TOHTOR.Roles;

[Localized($"Roles.{nameof(Oracle)}")]
public class Oracle: Crewmate
{
    private Optional<byte> selectedPlayer = Optional<byte>.Null();
    private bool targetLockedIn;

    [Localized("VotePlayerInfo")]
    private static string votePlayerMessage = "Vote to select a player to reveal on your death. You can re-vote a player to unselect them.\nAfter confirming your target cannot be changed.";
    [Localized("SelectRole")]
    private static string selectRoleMsg = "You have selected:";
    private static string skipMsg = "Press \"Skip Vote\" to continue.";

    [RoleAction(RoleActionType.RoundEnd)]
    private void OracleSendMessage()
    {
        if (selectedPlayer.Exists()) return;
        Utils.SendMessage(votePlayerMessage, MyPlayer.PlayerId);
    }

    [RoleAction(RoleActionType.MyVote)]
    private void OracleLockInTarget(Optional<PlayerControl> target, ActionHandle handle)
    {
        if (targetLockedIn) return;
        handle.Cancel();
        if (selectedPlayer.Exists() && !target.Exists())
        {
            targetLockedIn = true;
            return;
        }
        selectedPlayer = target.Map(p => p.PlayerId);
        Utils.SendMessage($"{selectRoleMsg} {target.Get().GetRawName()}\n{skipMsg}");
    }

    [RoleAction(RoleActionType.MyDeath)]
    private void OracleDies()
    {
        if (!selectedPlayer.Exists()) return;
        PlayerControl target = Utils.GetPlayerById(selectedPlayer.Get())!;
        DynamicName name = target.GetDynamicName();
        name.AddRule(GameState.Roaming, UI.Role, new DynamicString("{0}"));
        name.AddRule(GameState.InMeeting, UI.Role, new DynamicString("{0}"));
    }


    protected override RoleModifier Modify(RoleModifier roleModifier) =>
        base.Modify(roleModifier)
            .RoleColor(new Color(0.78f, 0.55f, 0.82f));
}