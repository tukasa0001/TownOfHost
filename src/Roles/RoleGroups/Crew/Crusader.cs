using AmongUs.GameOptions;
using TOHTOR.API;
using TOHTOR.Extensions;
using TOHTOR.Managers.History.Events;
using TOHTOR.Options;
using TOHTOR.Roles.Interactions;
using TOHTOR.Roles.Interactions.Interfaces;
using TOHTOR.Roles.Internals;
using TOHTOR.Roles.Internals.Attributes;
using TOHTOR.Roles.RoleGroups.Vanilla;
using UnityEngine;
using VentLib.Options.Game;
using VentLib.Utilities.Optionals;

namespace TOHTOR.Roles.RoleGroups.Crew;

public class Crusader: Crewmate
{
    private Optional<byte> protectedPlayer = Optional<byte>.Null();
    private bool protectAgainstHelpfulInteraction;
    private bool protectAgainstNeutralInteraction;

    [RoleAction(RoleActionType.Attack)]
    private void SelectTarget(PlayerControl target)
    {
        if (MyPlayer.InteractWith(target, SimpleInteraction.HelpfulInteraction.Create(this)) == InteractionResult.Halt) return;
        protectedPlayer = Optional<byte>.NonNull(target.PlayerId);
        MyPlayer.RpcGuardAndKill(target);
        Game.GameHistory.AddEvent(new ProtectEvent(MyPlayer, target));
    }

    [RoleAction(RoleActionType.AnyInteraction)]
    private void AnyPlayerTargeted(PlayerControl killer, PlayerControl target, Interaction interaction, ActionHandle handle)
    {
        if (Game.State is not GameState.Roaming) return;
        if (!protectedPlayer.Exists()) return;
        if (target.PlayerId != protectedPlayer.Get()) return;
        Intent intent = interaction.Intent();

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
        bool killed = MyPlayer.InteractWith(killer, SimpleInteraction.FatalInteraction.Create(this)) is InteractionResult.Proceed;
        Game.GameHistory.AddEvent(new PlayerSavedEvent(target, MyPlayer, killer));
        Game.GameHistory.AddEvent(new KillEvent(MyPlayer, killer, killed));
    }

    protected override GameOptionBuilder RegisterOptions(GameOptionBuilder optionStream) =>
        base.RegisterOptions(optionStream)
            .SubOption(sub => sub.Name("Protect against Beneficial Interactions")
                .BindBool(b => protectAgainstHelpfulInteraction = b)
                .AddOnOffValues(false)
                .Build())
            .SubOption(sub => sub.Name("Protect against Neutral Interactions")
                .BindBool(b => protectAgainstNeutralInteraction = b)
                .AddOnOffValues()
                .Build());

    protected override RoleModifier Modify(RoleModifier roleModifier) =>
        base.Modify(roleModifier)
            .DesyncRole(RoleTypes.Impostor)
            .RoleColor(new Color(0.78f, 0.36f, 0.22f))
            .OptionOverride(Override.KillCooldown, () => DesyncOptions.OriginalHostOptions.GetFloat(FloatOptionNames.KillCooldown) * 2);
}