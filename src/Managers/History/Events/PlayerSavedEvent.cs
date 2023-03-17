using TOHTOR.API;
using TOHTOR.Extensions;
using TOHTOR.Roles;
using VentLib.Utilities.Optionals;

namespace TOHTOR.Managers.History.Events;

public class PlayerSavedEvent : IRecipientEvent
{
    private PlayerControl savedPlayer;
    private Optional<CustomRole> playerRole;
    private Optional<PlayerControl> savior;
    private Optional<CustomRole> saviorRole;
    private Optional<PlayerControl> killer;
    private Optional<CustomRole> killerRole;

    private Timestamp timestamp = new();

    public PlayerSavedEvent(PlayerControl savedPlayer, PlayerControl? savior, PlayerControl? killer)
    {
        this.savedPlayer = savedPlayer;
        playerRole = Optional<CustomRole>.Of(this.savedPlayer.GetCustomRole());
        this.savior = Optional<PlayerControl>.Of(savior);
        this.saviorRole = this.savior.Map(p => p.GetCustomRole());
        this.killer = Optional<PlayerControl>.Of(killer);
        this.killerRole = this.killer.Map(p => p.GetCustomRole());
    }

    public PlayerControl Player() => savedPlayer;

    public Optional<CustomRole> RelatedRole() => playerRole;

    public Timestamp Timestamp() => timestamp;

    public bool IsCompletion() => true;

    public string Message()
    {
        string killerString = killer.Transform(k => $" from {Game.GetName(k)}", () => "");
        return savior.Transform(player => $"{Game.GetName(player)} saved {Game.GetName(savedPlayer)}{killerString}.",
            () => $"{Game.GetName(savedPlayer)} was saved{killerString}.");
    }

    public Optional<PlayerControl> Instigator() => savior;

    public Optional<CustomRole> InstigatorRole() => saviorRole;

    public Optional<PlayerControl> Killer() => killer;

    public Optional<CustomRole> KillerRole => killerRole;
}