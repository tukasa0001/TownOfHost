using System;
using HarmonyLib;
using TownOfHost.Extensions;
using TownOfHost.Gamemodes.Conditions;
using TownOfHost.Managers;
using TownOfHost.ReduxOptions;

namespace TownOfHost.Gamemodes;

[HarmonyPatch(typeof(LogicGameFlowNormal), nameof(LogicGameFlowNormal.CheckEndCriteria))]
public class CheckEndGamePatch2
{
    public static bool Prefix()
    {
        if (!AmongUsClient.Instance.AmHost) return true;
        "CEGP2".DebugLog();
        WinDelegate winDelegate = Game.GetWinDelegate();
        if (StaticOptions.NoGameEnd)
            winDelegate.CancelGameWin();



        bool isGameWin = winDelegate.IsGameOver();
        if (!isGameWin) return false;

        GameOverReason reason = winDelegate.GetWinReason() switch
        {
            WinReason.FactionLastStanding => GameOverReason.ImpostorByKill,
            WinReason.TasksComplete => GameOverReason.HumansByTask,
            WinReason.RoleSpecificWin => GameOverReason.ImpostorByKill,
            _ => throw new ArgumentOutOfRangeException()
        };

        GameManager.Instance.RpcEndGame(reason, false);

        return false;
    }
}