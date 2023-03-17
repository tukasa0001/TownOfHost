using TOHTOR.Roles;
using VentLib.Utilities.Optionals;

namespace TOHTOR.Managers.History.Events;

public interface ITargetedEvent : IHistoryEvent
{
    public PlayerControl Target();

    public Optional<CustomRole> TargetRole();
}