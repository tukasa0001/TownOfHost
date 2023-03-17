using TOHTOR.API;
using TOHTOR.Extensions;
using TOHTOR.Roles;
using VentLib.Utilities.Optionals;

namespace TOHTOR.Managers.History.Events;

public class KillEvent : ITargetedEvent
{
    private PlayerControl killer;
    private Optional<CustomRole> killerRole;

    private PlayerControl victim;
    private Optional<CustomRole> victimRole;

    private bool successful;
    private Timestamp timestamp = new();

    public KillEvent(PlayerControl killer, PlayerControl victim, bool successful = true)
    {
        this.killer = killer;
        killerRole = Optional<CustomRole>.Of(killer.GetCustomRole());
        this.victim = victim;
        victimRole = Optional<CustomRole>.Of(victim.GetCustomRole());
        this.successful = successful;
    }

    public PlayerControl Player() => this.killer;

    public Optional<CustomRole> RelatedRole() => this.killerRole;

    public Timestamp Timestamp() => timestamp;

    public bool IsCompletion() => successful;

    public virtual string Message() => $"{Game.GetName(killer)} killed {Game.GetName(victim)}.";

    public PlayerControl Target() => victim;

    public Optional<CustomRole> TargetRole() => victimRole;
}