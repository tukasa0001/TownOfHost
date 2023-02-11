using System.Collections.Generic;
using System.Linq;
using TownOfHost.API;
using TownOfHost.Extensions;
using TownOfHost.Factions;
using TownOfHost.Patches.Systems;
using TownOfHost.Roles;
using VentLib.Utilities.Extensions;

namespace TownOfHost.Victory.Conditions;

public class SabotageWin: IWinCondition
{
    public bool IsConditionMet(out List<PlayerControl> winners)
    {
        winners = null!;
        if (SabotagePatch.CurrentSabotage == null || SabotagePatch.SabotageCountdown > 0) return false;
        List<PlayerControl> eligiblePlayers = Game.GetAllPlayers().Where(p => p.GetCustomRole() is Impostor i && i.CanSabotage()).ToList();
        List<PlayerControl> impostors = eligiblePlayers.Where(p => p.GetCustomRole().Factions.IsAllied(Faction.Impostors)).ToList();
        List<PlayerControl> others = eligiblePlayers.Except(impostors).ToList();

        if (impostors.Count >= others.Count)
            winners = impostors;
        else if (SabotagePatch.SabotageCaller != null)
            winners = eligiblePlayers.Where(p => p.GetCustomRole().IsAllied(SabotagePatch.SabotageCaller)).ToList();
        else if (eligiblePlayers.Count > 0)
            winners = new List<PlayerControl> { eligiblePlayers.GetRandom() };
        else
            winners = new List<PlayerControl>();
        return true;
    }

    public WinReason GetWinReason() => WinReason.Sabotage;

    public int Priority() => 3;
}