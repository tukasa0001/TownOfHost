using TOHTOR.API;
using TOHTOR.Managers.History.Events;

namespace TOHTOR.Roles.Events;

public class BittenEvent : TargetedAbilityEvent, IRoleEvent
{
    public BittenEvent(PlayerControl killer, PlayerControl victim) : base(killer, victim)
    {
    }

    public override string Message()
    {
        return $"{Game.GetName(Player())} bit {Game.GetName(Target())}.";
    }
}

public class BittenDeathEvent : DeathEvent
{
    public BittenDeathEvent(PlayerControl deadPlayer, PlayerControl? killer) : base(deadPlayer, killer)
    {
    }

    public override string SimpleName() => ModConstants.DeathNames.Bitten;

    public override string Message()
    {
        string baseMessage = $"{Game.GetName(Player())} succumbed to their bite";
        return Instigator().Transform(klr => baseMessage + $" from {Game.GetName(klr)}.", () => baseMessage + ".");
    }
}