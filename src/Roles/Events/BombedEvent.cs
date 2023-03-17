using TOHTOR.Managers.History.Events;

namespace TOHTOR.Roles.Events;

public class BombedEvent : DeathEvent
{
    public BombedEvent(PlayerControl deadPlayer, PlayerControl? killer) : base(deadPlayer, killer)
    {
    }

    public override string SimpleName() => ModConstants.DeathNames.Bombed;
}