using TOHTOR.API;
using TOHTOR.Extensions;
using TOHTOR.Roles;
using VentLib.Utilities.Optionals;

namespace TOHTOR.Managers.History.Events;

public class DeathEvent : IDeathEvent
{
    private PlayerControl deadPlayer;
    private CustomRole playerRole;
    private Optional<PlayerControl> killer;
    private Optional<CustomRole> killerRole;

    private Timestamp timestamp = new();

    public DeathEvent(PlayerControl deadPlayer, PlayerControl? killer)
    {
        this.deadPlayer = deadPlayer;
        playerRole = this.deadPlayer.GetCustomRole();
        this.killer = Optional<PlayerControl>.Of(killer);
        this.killerRole = this.killer.Map(p => p.GetCustomRole());
    }

    public PlayerControl Player() => deadPlayer;

    public Optional<CustomRole> RelatedRole() => Optional<CustomRole>.NonNull(playerRole);

    public Timestamp Timestamp() => timestamp;

    public bool IsCompletion() => true;

    public virtual string Message()
    {
        string baseMessage = $"{Game.GetName(deadPlayer)} was {SimpleName().ToLower()}";
        return killer.Transform(klr => baseMessage + $" by {Game.GetName(klr)}.", () => baseMessage + ".");
    }

    public Optional<PlayerControl> Instigator() => killer;

    public Optional<CustomRole> InstigatorRole() => killerRole;

    public virtual string SimpleName() => ModConstants.DeathNames.Killed;
}