using System.Collections.Generic;
using System.Linq;
using TownOfHost.Extensions;
using TownOfHost.Factions;
using TownOfHost.Gamemodes.Conditions;
using TownOfHost.Managers;

namespace TownOfHost.Gamemodes.Standard;

public static class StandardWinConditions
{
    public class SoloRoleWin : IWinCondition
    {
        public bool IsConditionMet(out List<PlayerControl> winners)
        {
            winners = null;
            List<PlayerControl> allPlayers = Game.GetAllPlayers().ToList();
            if (allPlayers.Count != 1) return false;

            PlayerControl lastPlayer = allPlayers[0];
            return lastPlayer.GetCustomRole().Factions.Contains(Faction.Solo);
        }

        // TODO: Figure this out
        public WinReason WinReason() => Conditions.WinReason.FactionLastStanding;
    }

}