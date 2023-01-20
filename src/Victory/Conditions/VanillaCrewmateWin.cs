using System.Collections.Generic;
using TownOfHost.Factions;
using TownOfHost.Managers;

namespace TownOfHost.Victory.Conditions;

public class VanillaCrewmateWin: IFactionWinCondition
{
    private static readonly List<Faction> CrewmateFaction = new() { Faction.Crewmates };
    private WinReason winReason = WinReason.TasksComplete;

    public bool IsConditionMet(out List<Faction> factions)
    {
        factions = CrewmateFaction;
        winReason = WinReason.TasksComplete;

        if (GameStates.CountAliveImpostors() != 0)
            return GameData.Instance.TotalTasks == GameData.Instance.CompletedTasks;

        winReason = WinReason.FactionLastStanding;
        return true;
    }

    public WinReason GetWinReason() => winReason;

    public int Priority() => -1;
}