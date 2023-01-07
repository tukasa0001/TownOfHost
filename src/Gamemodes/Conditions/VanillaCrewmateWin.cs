using System.Collections.Generic;
using TownOfHost.Factions;

namespace TownOfHost.Gamemodes.Conditions;

public class VanillaCrewmateWin: IFactionWinCondition
{
    public bool IsConditionMet(out List<Faction> factions)
    {
        factions = new List<Faction> { Faction.Crewmates };
        return GameData.Instance.TotalTasks == GameData.Instance.CompletedTasks;
    }

    public WinReason WinReason() => Conditions.WinReason.TasksComplete;
}