using System.Collections.Generic;

namespace TownOfHost.Gamemodes.Conditions;

public interface IWinCondition
{
    bool IsConditionMet(out List<PlayerControl> winners);

    WinReason WinReason();

    bool IsConditionMet() => IsConditionMet(out List<PlayerControl> winners);
}