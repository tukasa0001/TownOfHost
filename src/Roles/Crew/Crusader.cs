using AmongUs.GameOptions;
using TOHTOR.API;
using TOHTOR.Extensions;
using TOHTOR.Options;
using TOHTOR.Roles.Internals;
using TOHTOR.Roles.Internals.Attributes;
using UnityEngine;
using VentLib.Utilities.Optionals;

namespace TOHTOR.Roles;

public class Crusader: Crewmate
{
    private Optional<byte> protectedPlayer = Optional<byte>.Null();

    [RoleAction(RoleActionType.AttemptKill)]
    private void SelectTarget(PlayerControl target)
    {
        InteractionResult result = CheckInteractions(target.GetCustomRole(), target);
        if (result == InteractionResult.Halt) return;
        protectedPlayer = Optional<byte>.NonNull(target.PlayerId);
        MyPlayer.RpcGuardAndKill(target);
    }

    [RoleInteraction(typeof(Veteran))]
    public InteractionResult VeteranInteraction(PlayerControl vet) => vet.GetCustomRole<Veteran>().TryKill(MyPlayer) ? InteractionResult.Halt : InteractionResult.Proceed;

    [RoleAction(RoleActionType.AnyMurder)]
    private void AnyPlayerTargeted(PlayerControl killer, PlayerControl target, ActionHandle handle)
    {
        if (Game.State is not GameState.Roaming) return;
        if (!protectedPlayer.Exists()) return;
        if (target.PlayerId != protectedPlayer.Get()) return;
        handle.Cancel();
        RoleUtils.SwapPositions(target, MyPlayer);
        RoleUtils.RoleCheckedMurder(MyPlayer, killer);
    }

    protected override RoleModifier Modify(RoleModifier roleModifier) =>
        base.Modify(roleModifier)
            .DesyncRole(RoleTypes.Impostor)
            .RoleColor(new Color(0.78f, 0.36f, 0.22f))
            .OptionOverride(Override.KillCooldown, DesyncOptions.OriginalHostOptions.GetFloat(FloatOptionNames.KillCooldown) * 2);
}