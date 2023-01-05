using AmongUs.GameOptions;
using TownOfHost.Extensions;
using TownOfHost.Factions;
using TownOfHost.ReduxOptions;
using UnityEngine;

namespace TownOfHost.Roles;

public class Impostor : CustomRole
{
    public virtual bool CanSabotage() => canSabotage;
    public virtual bool CanKill() => canKill;
    protected bool canSabotage = true;
    protected bool canKill = true;
    public float KillCooldown
    {
        protected set => _killCooldown = value;
        get => _killCooldown ?? DesyncOptions.OriginalHostOptions?.AsNormalOptions()?.GetFloat(FloatOptionNames.KillCooldown) ?? 60f;
    }
    private float? _killCooldown;

    [RoleAction(RoleActionType.AttemptKill, Subclassing = false)]
    public virtual bool TryKill(PlayerControl target)
    {
        InteractionResult result = CheckInteractions(target.GetCustomRole(), target);
        if (result == InteractionResult.Halt) return false;

        bool canKillTarget = target.GetCustomRole().CanBeKilled();
        if (canKillTarget)
            MyPlayer.RpcMurderPlayer(target);
        return canKillTarget;
    }

    [RoleInteraction(typeof(Veteran))]
    public InteractionResult VeteranInteraction(PlayerControl vet)
    {
        return vet.GetCustomRole<Veteran>().TryKill(MyPlayer) ? InteractionResult.Halt : InteractionResult.Proceed;
    }

    protected override RoleModifier Modify(RoleModifier roleModifier) =>
        roleModifier
            .VanillaRole(RoleTypes.Impostor)
            .Factions(Faction.Impostors)
            .OptionOverride(Override.KillCooldown, KillCooldown)
            .RoleColor(Color.red);
}