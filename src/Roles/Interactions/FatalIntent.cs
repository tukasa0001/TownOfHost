using System;
using TOHTOR.Extensions;
using TOHTOR.Managers.History.Events;
using TOHTOR.Roles.Interactions.Interfaces;
using VentLib.Utilities.Optionals;

namespace TOHTOR.Roles.Interactions;

public class FatalIntent : IFatalIntent
{
    private Func<IDeathEvent>? causeOfDeath;
    private bool ranged;

    public FatalIntent(bool ranged = false, Func<IDeathEvent>? causeOfDeath = null)
    {
        this.ranged = ranged;
        this.causeOfDeath = causeOfDeath;
    }

    public Optional<IDeathEvent> CauseOfDeath() => Optional<IDeathEvent>.Of(causeOfDeath?.Invoke());

    public void KillTarget(PlayerControl actor, PlayerControl target)
    {
        if (ranged) target.RpcMurderPlayer(target);
        else actor.RpcMurderPlayer(target);
    }

    public bool IsRanged() => ranged;

    public void Halted(PlayerControl actor, PlayerControl target)
    {
        actor.RpcGuardAndKill(target);
    }
}