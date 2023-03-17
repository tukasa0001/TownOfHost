using System.Linq;
using TOHTOR.API;
using TOHTOR.Extensions;
using TOHTOR.Managers.History.Events;
using TOHTOR.Roles.Interactions;
using TOHTOR.Roles.Interactions.Interfaces;
using TOHTOR.Roles.Internals;
using TOHTOR.Roles.Internals.Attributes;
using TOHTOR.Roles.RoleGroups.Vanilla;
using UnityEngine;
using VentLib.Localization.Attributes;
using VentLib.Options.Game;
using VentLib.Utilities.Extensions;
using VentLib.Utilities.Optionals;

namespace TOHTOR.Roles.RoleGroups.Crew;

[Localized($"Roles.{nameof(Medic)}")]
public class Medic: Crewmate
{
    private Optional<byte> guardedPlayer = Optional<byte>.Null();
    private GuardMode mode;
    private bool castVote;
    private bool guardedOnce;

    [Localized("ProtectingMessage")]
    private static string protectingMessage = "You are currently protecting:";
    [Localized("VotePlayerInfo")]
    private static string votePlayerMessage = "Vote to select a player to guard.";

    [RoleAction(RoleActionType.RoundStart)]
    private void SwapGuard()
    {
        guardedOnce = guardedOnce || guardedPlayer.Exists();
        guardedPlayer.IfPresent(player =>
        {
            Game.GameHistory.AddEvent(new ProtectEvent(MyPlayer, Utils.GetPlayerById(player)!));
        });
    }

    [RoleAction(RoleActionType.RoundEnd)]
    private void SendAndCheckGuarded()
    {
        castVote = false;
        guardedPlayer.IfPresent(b =>
        {
            if (Game.GetAlivePlayers().All(p => p.PlayerId != b)) guardedPlayer = Optional<byte>.Null();
        });

        Utils.SendMessage($"{protectingMessage} {guardedPlayer.FlatMap(GetPlayerName).OrElse("No One")}\n{votePlayerMessage}", MyPlayer.PlayerId);
    }

    [RoleAction(RoleActionType.MyVote)]
    protected void SelectTarget(Optional<PlayerControl> votedPlayer, ActionHandle handle)
    {
        // ReSharper disable once ConvertIfStatementToSwitchStatement
        if (guardedOnce && mode is GuardMode.Never) return;
        if (guardedOnce && guardedPlayer.Exists() && mode is GuardMode.OnDeath) return;
        if (this.castVote) return;
        this.castVote = true;
        handle.Cancel();

        if (!votedPlayer.Exists()) return;
        byte voted = votedPlayer.Get().PlayerId;

        if (MyPlayer.PlayerId == voted) { }
        else if (!guardedPlayer.Exists()) guardedPlayer = votedPlayer.Map(p => p.PlayerId);
        else guardedPlayer = guardedPlayer.Exists() ? new Optional<byte>() : new Optional<byte>(guardedPlayer.Get());

        Utils.SendMessage($"{protectingMessage} {guardedPlayer.FlatMap(GetPlayerName).OrElse("No One")}", MyPlayer.PlayerId);
    }

    [RoleAction(RoleActionType.AnyInteraction)]
    protected void ProtectTarget(PlayerControl killer, PlayerControl target, Interaction interaction, ActionHandle handle)
    {
        if (Game.State is not GameState.Roaming) return;
        if (!guardedPlayer.Exists()) return;
        if (target.PlayerId != guardedPlayer.Get()) return;
        if (interaction.Intent() is not (IHostileIntent or IFatalIntent)) return;

        handle.Cancel();
        Game.GameHistory.AddEvent(new PlayerSavedEvent(target, MyPlayer, killer));
    }

    private static Optional<string> GetPlayerName(byte b) => Game.GetAlivePlayers().FirstOrOptional(p => p.PlayerId == b).Map(p => p.GetRawName());

    protected override RoleModifier Modify(RoleModifier roleModifier) => base.Modify(roleModifier).RoleColor(new Color(0f, 0.4f, 0f));

    protected override GameOptionBuilder RegisterOptions(GameOptionBuilder optionStream) =>
        base.RegisterOptions(optionStream)
            .SubOption(sub => sub
                .Name("Change Guarded Player")
                .Value(v => v.Text("When Guarded Player Dies").Value(0).Build())
                .Value(v => v.Text("Any Meeting").Value(1).Build())
                .Value(v => v.Text("Never").Value(2).Build())
                .BindInt(o => mode = (GuardMode)o)
                .Build());

    protected enum GuardMode
    {
        OnDeath,
        PerRound,
        Never
    }
}