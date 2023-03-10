using System.Linq;
using TOHTOR.API;
using TOHTOR.Extensions;
using TOHTOR.Roles.Internals;
using TOHTOR.Roles.Internals.Attributes;
using UnityEngine;
using VentLib.Localization.Attributes;
using VentLib.Options.Game;
using VentLib.Utilities.Extensions;
using VentLib.Utilities.Optionals;

namespace TOHTOR.Roles;

[Localized($"Roles.{nameof(Bodyguard)}")]
public class Bodyguard: Crewmate
{
    private Optional<byte> guardedPlayer = Optional<byte>.Null();
    private GuardMode guardMode;
    private bool skippedVote;
    private bool guardedOnce;

    [Localized("ProtectingMessage")]
    private static string protectingMessage = "You are currently protecting: ";
    [Localized("VotePlayerInfo")]
    private static string votePlayerMessage = "Vote to select a player to guard. You can re-vote a player to unselect them.";
    private static string skipMsg = "Press \"Skip Vote\" to continue.";

    [RoleAction(RoleActionType.RoundStart)]
    private void SwapGuard() => guardedOnce = guardedOnce || guardedPlayer.Exists();

    [RoleAction(RoleActionType.RoundEnd)]
    private void SendAndCheckGuarded()
    {
        skippedVote = false;
        guardedPlayer.IfPresent(b =>
        {
            if (Game.GetAlivePlayers().All(p => p.PlayerId != b)) guardedPlayer = Optional<byte>.Null();
        });

        Utils.SendMessage($"{protectingMessage} {guardedPlayer.FlatMap(GetPlayerName).OrElse("Mo One")}\n{votePlayerMessage}\n{skipMsg}");
    }

    [RoleAction(RoleActionType.MyVote)]
    private void ProtectPlayer(Optional<PlayerControl> votedPlayer, ActionHandle handle)
    {
        if (guardedOnce && guardMode is GuardMode.Never) return;
        if (guardedPlayer.Exists() && guardMode is GuardMode.OnDeath) return;
        if (skippedVote) return;
        handle.Cancel();
        if (!votedPlayer.Exists()) skippedVote = true;
        byte voted = votedPlayer.Get().PlayerId;

        if (MyPlayer.PlayerId == voted) { }
        else if (!guardedPlayer.Exists()) guardedPlayer = votedPlayer.Map(p => p.PlayerId);
        else guardedPlayer = guardedPlayer.FlatMap(b => b == voted ? new Optional<byte>() : new Optional<byte>(voted));

        Utils.SendMessage($"{protectingMessage} {guardedPlayer.FlatMap(GetPlayerName).OrElse("Mo One")}\n{votePlayerMessage}\n{skipMsg}");
    }

    [RoleAction(RoleActionType.AnyMurder)]
    private void AnyPlayerTargeted(PlayerControl killer, PlayerControl target, ActionHandle handle)
    {
        if (Game.State is not GameState.Roaming) return;
        if (!guardedPlayer.Exists()) return;
        if (target.PlayerId != guardedPlayer.Get()) return;
        handle.Cancel();
        RoleUtils.SwapPositions(target, MyPlayer);
        RoleUtils.RoleCheckedMurder(MyPlayer, killer);
        RoleUtils.RoleCheckedMurder(killer, MyPlayer);
    }

    private static Optional<string> GetPlayerName(byte b) => Game.GetAlivePlayers().FirstOrOptional(p => p.PlayerId == b).Map(p => p.GetRawName());

    protected override RoleModifier Modify(RoleModifier roleModifier) =>
        base.Modify(roleModifier).RoleColor(new Color(0.36f, 0.36f, 0.36f));

    protected override GameOptionBuilder RegisterOptions(GameOptionBuilder optionStream) =>
        base.RegisterOptions(optionStream)
            .SubOption(sub => sub
                .Name("Change Guarded Player")
                .Value(v => v.Text("When Guarded Player Dies").Value(0).Build())
                .Value(v => v.Text("Any Meeting").Value(1).Build())
                .Value(v => v.Text("Never").Value(2).Build())
                .BindInt(o => guardMode = (GuardMode)o)
                .Build());

    private enum GuardMode
    {
        OnDeath,
        PerRound,
        Never
    }
}