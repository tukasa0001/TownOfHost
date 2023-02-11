using System.Collections.Generic;
using System.Linq;
using TownOfHost.API;
using TownOfHost.Victory.Conditions;

namespace TownOfHost.Gamemodes.Colorwars;

public class ColorWarsWinCondition: IWinCondition
{
    public bool IsConditionMet(out List<PlayerControl> winners)
    {
        winners = null;
        // get the colors of all alive players, then get distinct color ids, then if there's 1 id remaining after all that it means a team has won
        List<int> currentColors = Game.GetAlivePlayers().Select(p => p.cosmetics.bodyMatProperties.ColorId).Distinct().ToList();
        if (currentColors.Count != 1) return false;
        int winningColor = currentColors[0];
        winners = Game.GetAllPlayers().Where(p => p.cosmetics.bodyMatProperties.ColorId == winningColor).ToList();

        return true;
    }

    public WinReason GetWinReason() => WinReason.GamemodeSpecificWin;
}