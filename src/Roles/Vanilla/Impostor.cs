using AmongUs.GameOptions;
using TownOfHost.Extensions;
using TownOfHost.Factions;
using TownOfHost.Options;
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
        set => _killCooldown = value;
        get => _killCooldown ?? DesyncOptions.OriginalHostOptions?.AsNormalOptions()?.GetFloat(FloatOptionNames.KillCooldown) ?? 60f;
    }
    private float? _killCooldown;

    [RoleAction(RoleActionType.AttemptKill, Subclassing = false)]
    public virtual bool TryKill(PlayerControl target)
    {
        InteractionResult result = CheckInteractions(target.GetCustomRole(), target);
        SyncOptions();
        return result != InteractionResult.Halt && RoleUtils.RoleCheckedMurder(MyPlayer, target);
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
            .CanVent(true)
            .OptionOverride(Override.KillCooldown, KillCooldown)
            .RoleColor(Color.red);
}