using System;
using System.Collections.Generic;
using System.Linq;
using TOHTOR.API;

namespace TOHTOR.Victory.Conditions;

public class FallbackCondition: IWinCondition
{
    private readonly List<PlayerControl> noWinners = new();

    public bool IsConditionMet(out List<PlayerControl> winners)
    {
        winners = noWinners;
        return !Game.GetAlivePlayers().Any();
    }

    public WinReason GetWinReason() => WinReason.NoWinCondition;

    public int Priority() => Int32.MinValue;
}