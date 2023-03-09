using System.Collections.Generic;
using TOHTOR.Options;
using TOHTOR.Victory;
using TOHTOR.Victory.Conditions;
using VentLib.Options.Game.Tabs;

namespace TOHTOR.Gamemodes.Standard;

public class StandardGamemode: Gamemode
{
    public override string GetName() => "Standard";

    public override void AssignRoles(List<PlayerControl> players)
    {
        StandardAssignRoles.StandardAssign(players);
    }

    public override IEnumerable<GameOptionTab> EnabledTabs() => DefaultTabs.All;

    public override void SetupWinConditions(WinDelegate winDelegate)
    {
        winDelegate.AddWinCondition(new VanillaCrewmateWin());
        winDelegate.AddWinCondition(new VanillaImpostorWin());
        winDelegate.AddWinCondition(new SabotageWin());
        winDelegate.AddWinCondition(new StandardWinConditions.LoversWin());
        winDelegate.AddWinCondition(new StandardWinConditions.SoloKillingWin());
        winDelegate.AddWinCondition(new StandardWinConditions.SoloRoleWin());
    }
}