using System.Collections.Generic;
using System.Linq;
using TownOfHost.Factions;
using TownOfHost.Managers;

namespace TownOfHost.Gamemodes.Conditions;

public class VanillaImpostorWin: IFactionWinCondition
{
    public bool IsConditionMet(out List<Faction> factions)
    {
        factions = new List<Faction> { Faction.Impostors };
        List<PlayerControl> aliveImpostors = Game.GetAliveImpostors();
        HashSet<byte> byteImpostors = aliveImpostors.Select(p => p.PlayerId).ToHashSet();

        List<PlayerControl> others = Game.GetAllPlayers().Where(p => !byteImpostors.Contains(p.PlayerId)).ToList();

        return aliveImpostors.Count >= others.Count;
    }

    public WinReason WinReason() => Conditions.WinReason.FactionLastStanding;
}