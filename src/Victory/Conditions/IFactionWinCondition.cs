using System.Collections.Generic;
using System.Linq;
using TownOfHost.Extensions;
using TownOfHost.Factions;
using TownOfHost.Managers;

namespace TownOfHost.Victory.Conditions;

public interface IFactionWinCondition: IWinCondition
{
    bool IWinCondition.IsConditionMet(out List<PlayerControl> winners)
    {
        winners = null;
        if (!IsConditionMet(out List<Faction> factions)) return false;
        winners = Game.GetAllPlayers().Where(p => factions.IsAllied(p.GetCustomRole().Factions)).ToList();
        return true;
    }

    bool IsConditionMet(out List<Faction> factions);
}