using TOHTOR.API;
using TOHTOR.Managers.History.Events;

namespace TOHTOR.Roles.Events;

public class CursedEvent : TargetedAbilityEvent, IRoleEvent
{
    public CursedEvent(PlayerControl killer, PlayerControl victim) : base(killer, victim)
    {
    }

    public override string Message()
    {
        return $"{Game.GetName(Player())} cursed {Game.GetName(Target())}.";
    }
}

public class CursedDeathEvent : DeathEvent
{
    public CursedDeathEvent(PlayerControl deadPlayer, PlayerControl? killer) : base(deadPlayer, killer)
    {
    }

    public override string SimpleName() => ModConstants.DeathNames.Cursed;

    public override string Message()
    {
        string baseMessage = $"{Game.GetName(Player())}'s curse";
        return $"{Game.GetName(Player())}'s curse {Instigator().Map(klr => baseMessage + $"from {Game.GetName(klr)}").OrElse("")} got the best of them.";
    }
}