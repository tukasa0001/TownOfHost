using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using TownOfHost.Extensions;
using TownOfHost.Options;
using TownOfHost.Victory.Conditions;
using VentLib.Extensions;
using VentLib.Logging;

namespace TownOfHost.Victory;

public class WinDelegate
{
    private readonly List<IWinCondition> winConditions = new() { new FallbackCondition() };
    private readonly List<Action<WinDelegate>> winNotifiers = new();

    private List<PlayerControl> winners = new();
    private WinReason winReason;
    private bool forcedWin;
    private bool forcedCancel;

    public WinReason GetWinReason() => winReason;
    public void SetWinReason(WinReason reason) => winReason = reason;
    public List<PlayerControl> GetWinners() => winners;
    public void SetWinners(List<PlayerControl> winners) => this.winners = winners;

    public bool IsGameOver()
    {
        bool isWin = false;
        foreach (IWinCondition winCondition in winConditions)
        {
            isWin = winCondition.IsConditionMet(out winners);
            if (!isWin) continue;
            winReason = winCondition.GetWinReason();
            if (!StaticOptions.NoGameEnd)
                VentLogger.Info($"Triggering Win by \"{winCondition.GetType()}\", winners={winners.Select(p => p.GetRawName()).StrJoin()}, reason={winReason}", "WinCondition");
            break;
        }

        if (isWin)
            winNotifiers.Do(notify => notify(this));
        isWin = forcedWin || (isWin && !forcedCancel);
        forcedCancel = false;
        return isWin;
    }

    /// <summary>
    /// Adds a consumer which gets triggered when the game has detected a possible win. This allows for pre-win interactions
    /// as well as the possibility to cancel a game win via CancelGameWin() or to modify the game winners
    /// </summary>
    /// <param name="consumer"></param>
    public void AddSubscriber(Action<WinDelegate> consumer)
    {
        winNotifiers.Add(consumer);
    }

    public void AddWinCondition(IWinCondition condition)
    {
        winConditions.Add(condition);
        winConditions.Sort();
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