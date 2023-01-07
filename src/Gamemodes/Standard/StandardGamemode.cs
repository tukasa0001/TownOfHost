using System.Collections.Generic;
using TownOfHost.Gamemodes.Conditions;
using TownOfHost.Interface.Menus;
using TownOfHost.Managers;
using TownOfHost.Options;

namespace TownOfHost.Gamemodes.Standard;

public class StandardGamemode: IGamemode
{
    public void AssignRoles(List<PlayerControl> players)
    {
        StandardAssignRoles.StandardAssign(players);
    }

    public IEnumerable<GameOptionTab> EnabledTabs() => DefaultTabs.All;

    public string GetName() => "Standard";

    public void SetupWinConditions(WinDelegate winDelegate)
    {
        winDelegate.AddWinCondition(new VanillaCrewmateWin());
        winDelegate.AddWinCondition(new VanillaImpostorWin());
        winDelegate.AddWinCondition(new StandardWinConditions.LoversWin());
        winDelegate.AddWinCondition(new StandardWinConditions.SoloKillingWin());
        winDelegate.AddWinCondition(new StandardWinConditions.SoloRoleWin());
    }
}