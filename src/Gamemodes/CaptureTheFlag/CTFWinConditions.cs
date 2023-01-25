using System;
using System.Collections.Generic;
using System.Linq;
using TownOfHost.Managers;
using TownOfHost.Victory.Conditions;

namespace TownOfHost.Gamemodes.CaptureTheFlag;

public class CTFMostPointWinCondition: IWinCondition
{
    public bool IsConditionMet(out List<PlayerControl> winners)
    {
        winners = null;
        if ((DateTime.Now - Game.StartTime).TotalSeconds < CTFGamemode.GameDuration) return false;
        int winningTeam = CTFGamemode.TeamPoints[0] > CTFGamemode.TeamPoints[1] ? 0 : 1;
        winningTeam = CTFGamemode.TeamPoints[0] == CTFGamemode.TeamPoints[1] ? 2 : winningTeam;

        if (winningTeam == 2) winners = new List<PlayerControl>();
        else winners = Game.GetAllPlayers().Where(p => p.cosmetics.bodyMatProperties.ColorId == winningTeam).ToList();

        return true;
    }

    public WinReason GetWinReason() => WinReason.GamemodeSpecificWin;
}

public class CTFPointGoalWinCondition : IWinCondition
{
    public bool IsConditionMet(out List<PlayerControl> winners)
    {
        winners = null;
        return false;
    }

    public WinReason GetWinReason() => WinReason.GamemodeSpecificWin;
}