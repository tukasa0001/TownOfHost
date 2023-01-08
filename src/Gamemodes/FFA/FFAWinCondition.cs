using System.Collections.Generic;
using System.Linq;
using TownOfHost.Managers;
using TownOfHost.Victory.Conditions;

namespace TownOfHost.Gamemodes.FFA;

// ReSharper disable once InconsistentNaming
public class FFAWinCondition: IWinCondition
{
    public bool IsConditionMet(out List<PlayerControl> winners)
    {
        winners = null;
        List<PlayerControl> players = Game.GetAlivePlayers().ToList();
        if (players.Count != 1) return false;
        winners = new List<PlayerControl> { players[0] };
        return true;
    }

    public WinReason GetWinReason() => WinReason.RoleSpecificWin;
}