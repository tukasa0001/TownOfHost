using System.Collections.Generic;
using TownOfHost.Factions;

namespace TownOfHost.Gamemodes.Conditions;

public class VanillaCrewmateWin: IFactionWinCondition
{
    private static List<Faction> crewmateFaction = new() { Faction.Crewmates };

    public bool IsConditionMet(out List<Faction> factions)
    {
        factions = crewmateFaction;
        return GameData.Instance.TotalTasks == GameData.Instance.CompletedTasks;
    }

    public WinReason WinReason() => Conditions.WinReason.TasksComplete;

    public int Priority() => -1;
}