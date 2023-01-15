using System.Collections.Generic;

namespace TownOfHost.Managers.History;

public class GameHistory
{
    public List<HistoryEvent> Events = new();

    public void AddEvent(HistoryEvent @event)
    {
        Events.Add(@event);
    }

}