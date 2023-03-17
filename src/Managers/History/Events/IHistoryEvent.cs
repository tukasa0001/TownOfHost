using TOHTOR.Roles;
using VentLib.Utilities.Optionals;

namespace TOHTOR.Managers.History.Events;

public interface IHistoryEvent
{
    public PlayerControl Player();
    public Optional<CustomRole> RelatedRole();
    public Timestamp Timestamp();

    /// <summary>
    /// An open-ended indicator that confirms this event "led to a new, un-revertable state"<br/>
    /// For example:
    /// A player trying to kill another player (but for some reason failing) is not a completed event
    /// BUT, in a similar scenario- if a player DOES kill another player than that event would be considered completed.
    /// <br/>
    /// This field is used to get a rough end-game metric
    /// </summary>
    /// <returns></returns>
    public bool IsCompletion();

    public string Message();

    public string GenerateMessage() => $"[{Timestamp().ToString(@"mm\:ss")}] {Message()}";
}