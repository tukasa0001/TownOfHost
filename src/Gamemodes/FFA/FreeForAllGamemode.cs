using System.Collections.Generic;
using TownOfHost.Interface.Menus;
using TownOfHost.Options;
using TownOfHost.ReduxOptions;
using TownOfHost.Roles;
using TownOfHost.Victory;
// ReSharper disable InconsistentNaming

namespace TownOfHost.Gamemodes.FFA;

public class FreeForAllGamemode: Gamemode
{
    public static GameOptionTab FFATab = new("Free For All Options", "TownOfHost.assets.Tabs.TabIcon_FreeForAll.png");

    public override string GetName() => "Free For All";
    public override IEnumerable<GameOptionTab> EnabledTabs() => new[] { FFATab };
    public override GameAction IgnoredActions() => GameAction.CallSabotage | GameAction.ReportBody;

    public FreeForAllGamemode()
    {
        OptionHolder skOptions = CustomRoleManager.Static.SerialKiller.GetOptionBuilder().Tab(FFATab).Build();
        TOHPlugin.OptionManager.Add(skOptions);
    }

    public override void AssignRoles(List<PlayerControl> players)
    {
        FFAAssignRoles.AssignRoles(players);
    }

    public override void SetupWinConditions(WinDelegate winDelegate)
    {
        winDelegate.AddWinCondition(new FFAWinCondition());
    }

}