using TOHTOR.Roles;

namespace TOHTOR.Managers.History.Events;

public interface IRoleChangeEvent : IHistoryEvent
{
    public CustomRole OriginalRole();

    public CustomRole NewRole();
}