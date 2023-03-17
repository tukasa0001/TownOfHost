using System.Collections.Generic;

namespace TOHTOR.Managers.History.Events;

public interface IMultiTargetEvent : IHistoryEvent
{
    public List<PlayerControl> Targets();
}