using System.Collections.Generic;
using System.Linq;
using TownOfHost.Extensions;
using TownOfHost.Factions;
using TownOfHost.Managers;

namespace TownOfHost.Gamemodes.Conditions;

public interface IFactionWinCondition: IWinCondition
{
    bool IWinCondition.IsConditionMet(out List<PlayerControl> winners)
    {
        winners = null;
        if (!IsConditionMet(out List<Faction> factions)) return false;
        winners = Game.GetAllPlayers().Where(p => p.GetCustomRole().Factions.IsAllied(factions)).ToList();
        return true;
    }

    bool IsConditionMet(out List<Faction> factions);
}