using System.Collections.Generic;
using System.Linq;
using TownOfHost.API;
using TownOfHost.Extensions;
using TownOfHost.Factions;
using TownOfHost.Roles;

namespace TownOfHost.Victory.Conditions;

public class VanillaCrewmateWin: IFactionWinCondition
{
    private static readonly List<Faction> CrewmateFaction = new() { Faction.Crewmates };
    private WinReason winReason = WinReason.TasksComplete;

    public bool IsConditionMet(out List<Faction> factions)
    {
        factions = CrewmateFaction;
        winReason = WinReason.TasksComplete;

        // Any player that is really an impostor but is also not allied to the crewmates
        if (Game.GetAlivePlayers().Any(p => { CustomRole role = p.GetCustomRole(); return !role.Factions.IsAllied(Faction.Crewmates) && role.RealRole.IsImpostor(); }))
            return GameData.Instance.TotalTasks == GameData.Instance.CompletedTasks;

        winReason = WinReason.FactionLastStanding;
        return true;
    }

    public WinReason GetWinReason() => winReason;

    public int Priority() => -1;
}