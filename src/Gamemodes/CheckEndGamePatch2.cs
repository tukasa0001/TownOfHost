using HarmonyLib;
using TownOfHost.Managers;
using TownOfHost.Patches;
using TownOfHost.ReduxOptions;

namespace TownOfHost.Gamemodes;

[HarmonyPatch(typeof(LogicGameFlowNormal), nameof(LogicGameFlowNormal.CheckEndCriteria))]
public class CheckEndGamePatch2
{
    private static GameEndPredicate predicate;

    public static bool Prefix()
    {
        if (!AmongUsClient.Instance.AmHost) return true;
        WinDelegate winDelegate = Game.GetWinDelegate();
        if (StaticOptions.NoGameEnd)
            winDelegate.CancelGameWin();

        bool isGameWin = winDelegate.IsGameOver();

        return false;
    }
}