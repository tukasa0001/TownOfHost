using AmongUs.GameOptions;
using TOHTOR.API;
using TOHTOR.Extensions;
using TOHTOR.Factions;
using TOHTOR.Managers.History.Events;
using TOHTOR.Options;
using TOHTOR.Roles.Interactions;
using TOHTOR.Roles.Internals;
using TOHTOR.Roles.Internals.Attributes;
using TOHTOR.Roles.Internals.Interfaces;
using UnityEngine;
using VentLib.Logging;

namespace TOHTOR.Roles.RoleGroups.Vanilla;

public partial class Impostor : CustomRole, IModdable
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

    [RoleAction(RoleActionType.Attack, Subclassing = false)]
    public virtual bool TryKill(PlayerControl target)
    {
        VentLogger.Fatal("Triggering Try Kill");
        SyncOptions();
        InteractionResult result = MyPlayer.InteractWith(target, SimpleInteraction.FatalInteraction.Create(this));
        Game.GameHistory.AddEvent(new KillEvent(MyPlayer, target, result is InteractionResult.Proceed));
        return result is InteractionResult.Proceed;
    }

    protected override RoleModifier Modify(RoleModifier roleModifier) =>
        roleModifier
            .VanillaRole(RoleTypes.Impostor)
            .Factions(Faction.Impostors)
            .CanVent(true)
            .OptionOverride(Override.KillCooldown, KillCooldown)
            .RoleColor(Color.red);

}