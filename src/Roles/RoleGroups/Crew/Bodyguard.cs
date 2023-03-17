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

[Localized($"Roles.{nameof(Bodyguard)}")]
public class Bodyguard: Crewmate
{
    private Optional<byte> guardedPlayer = Optional<byte>.Null();
    private bool protectAgainstHelpfulInteraction;
    private bool protectAgainstNeutralInteraction;
    private GuardMode guardMode;
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
    private void ProtectPlayer(Optional<PlayerControl> votedPlayer, ActionHandle handle)
    {
        // ReSharper disable once ConvertIfStatementToSwitchStatement
        if (guardedOnce && guardMode is GuardMode.Never) return;
        if (guardedOnce && guardedPlayer.Exists() && guardMode is GuardMode.OnDeath) return;
        if (castVote) return;
        castVote = true;
        handle.Cancel();
        if (!votedPlayer.Exists()) return;
        byte voted = votedPlayer.Get().PlayerId;

        if (MyPlayer.PlayerId == voted) { }
        else if (!guardedPlayer.Exists()) guardedPlayer = votedPlayer.Map(p => p.PlayerId);
        else guardedPlayer = guardedPlayer.Exists() ? new Optional<byte>() : new Optional<byte>(guardedPlayer.Get());

        Utils.SendMessage($"{protectingMessage} {guardedPlayer.FlatMap(GetPlayerName).OrElse("No One")}", MyPlayer.PlayerId);
    }

    [RoleAction(RoleActionType.AnyInteraction)]
    private void AnyPlayerInteraction(PlayerControl actor, PlayerControl target, Interaction interaction, ActionHandle handle)
    {
        Intent intent = interaction.Intent();
        if (Game.State is not GameState.Roaming) return;
        if (!guardedPlayer.Exists() || target.PlayerId != guardedPlayer.Get()) return;

        switch (intent)
        {
            case IHelpfulIntent when !protectAgainstHelpfulInteraction:
            case INeutralIntent when !protectAgainstNeutralInteraction:
            case IFatalIntent fatalIntent when fatalIntent.IsRanged():
                return;
        }

        if (interaction is IDelayedInteraction or IRangedInteraction or IIndirectInteraction) return;
        handle.Cancel();

        RoleUtils.SwapPositions(target, MyPlayer);
        Game.GameHistory.AddEvent(new PlayerSavedEvent(target, MyPlayer, actor));
        InteractionResult result = MyPlayer.InteractWith(actor, SimpleInteraction.FatalInteraction.Create(this));

        if (result is InteractionResult.Proceed) Game.GameHistory.AddEvent(new KillEvent(MyPlayer, actor));
        if (actor.GetCustomRole() is Impostor imp) imp.TryKill(MyPlayer);
        else
        {
            if (actor.InteractWith(MyPlayer, SimpleInteraction.FatalInteraction.Create(this)) is InteractionResult.Proceed)
                Game.GameHistory.AddEvent(new KillEvent(actor, MyPlayer));
        }
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
                .Build())
            .SubOption(sub => sub.Name("Protect against Beneficial Interactions")
                .BindBool(b => protectAgainstHelpfulInteraction = b)
                .AddOnOffValues(false)
                .Build())
            .SubOption(sub => sub.Name("Protect against Neutral Interactions")
                .BindBool(b => protectAgainstNeutralInteraction = b)
                .AddOnOffValues()
                .Build());

    private enum GuardMode
    {
        OnDeath,
        PerRound,
        Never
    }
}