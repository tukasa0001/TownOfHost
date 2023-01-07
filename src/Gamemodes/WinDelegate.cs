using System;
using System.Collections.Generic;
using HarmonyLib;
using TownOfHost.Gamemodes.Conditions;

namespace TownOfHost.Managers;

public class WinDelegate
{
    private readonly List<IWinCondition> winConditions = new();
    private readonly List<Action<WinDelegate>> winNotifiers = new();

    private List<PlayerControl> winners = new();
    private bool forcedWin;
    private bool forcedCancel;

    public List<PlayerControl> GetWinners() => winners;

    public bool IsGameOver()
    {
        bool isWin = false;
        foreach (IWinCondition winCondition in winConditions)
        {
            isWin = winCondition.IsConditionMet(out winners);
            if (!isWin) continue;
            break;
        }

        if (isWin)
            winNotifiers.Do(notify => notify(this));
        return forcedWin || (isWin && !forcedCancel);
    }

    /// <summary>
    /// Adds a consumer which gets triggered when the game has detected a possible win. This allows for pre-win interactions
    /// as well as the possibility to cancel a game win via CancelGameWin() or to modify the game winners
    /// </summary>
    /// <param name="consumer"></param>
    public void AddWinNotifier(Action<WinDelegate> consumer)
    {
        winNotifiers.Add(consumer);
    }

    public void AddWinCondition(IWinCondition condition)
    {
        winConditions.Add(condition);
    }

    public void ForceGameWin()
    {
        forcedWin = true;
    }

    public void CancelGameWin()
    {
        forcedCancel = true;
    }
}