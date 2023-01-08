using System;
using System.Collections.Generic;

namespace TownOfHost.Victory.Conditions;

public interface IWinCondition: IComparable<IWinCondition>
{
    bool IsConditionMet(out List<PlayerControl> winners);
    bool IsConditionMet() => IsConditionMet(out List<PlayerControl> winners);

    /// <summary>
    /// Gets the priority of this win condition, win conditions get checked in order of priority.
    /// So in the case of two win conditions being true, the one with the higher priority will be the only condition ran
    /// </summary>
    /// <returns></returns>
    int Priority() => 0;

    WinReason GetWinReason();

    int IComparable<IWinCondition>.CompareTo(IWinCondition other) => other.Priority().CompareTo(Priority());
}