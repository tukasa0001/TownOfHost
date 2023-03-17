using TOHTOR.Roles;
using VentLib.Utilities.Optionals;

namespace TOHTOR.Managers.History.Events;

public interface IRecipientEvent : IHistoryEvent
{
    public Optional<PlayerControl> Instigator();

    public Optional<CustomRole> InstigatorRole();
}