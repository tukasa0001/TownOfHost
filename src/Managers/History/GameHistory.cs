using System.Collections.Generic;
using System.Linq;

namespace TownOfHost.Managers.History;

public class GameHistory
{
    public List<string> LastWinners { get; internal set; } = new();
    public List<HistoryEvent> Events = new();

    public void AddEvent(HistoryEvent @event)
    {
        Events.Add(@event);
    }

    public List<T> GetEvents<T>() where T : HistoryEvent => Events.Cast<T>().ToList();
}