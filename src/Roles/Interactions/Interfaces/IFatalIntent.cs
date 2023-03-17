using TOHTOR.API;
using TOHTOR.Extensions;
using TOHTOR.Managers.History.Events;
using TOHTOR.Roles.Internals;
using TOHTOR.Roles.Internals.Attributes;
using VentLib.Utilities.Optionals;

namespace TOHTOR.Roles.Interactions.Interfaces;

public interface IFatalIntent : Intent
{
    public Optional<IDeathEvent> CauseOfDeath();

    public void KillTarget(PlayerControl actor, PlayerControl target);

    void Intent.Action(PlayerControl actor, PlayerControl target)
    {
        Optional<IDeathEvent> deathEvent = CauseOfDeath();

        if (!target.GetCustomRole().CanBeKilled())
        {
            actor.RpcGuardAndKill(target);
            return;
        }

        Optional<IDeathEvent> currentDeathEvent = Game.GameHistory.GetCauseOfDeath(target.PlayerId);
        deathEvent.IfPresent(death => Game.GameHistory.SetCauseOfDeath(target.PlayerId, death));
        KillTarget(actor, target);
        ActionHandle ignored = ActionHandle.NoInit();
        if (target.IsAlive()) Game.TriggerForAll(RoleActionType.SuccessfulAngelProtect, ref ignored, target, actor);
        else currentDeathEvent.IfPresent(de => Game.GameHistory.SetCauseOfDeath(target.PlayerId, de));
    }

    public bool IsRanged();
}