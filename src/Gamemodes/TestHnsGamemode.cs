using System.Collections.Generic;
using TownOfHost.Interface.Menus;
using TownOfHost.Options;
using TownOfHost.Victory;

namespace TownOfHost.Gamemodes;

public class TestHnsGamemode: Gamemode
{
    public override string GetName() => "Hide and Seek";

    public static GameOptionTab HnsTab = new("Hide & Seek Options", "TownOfHost.assets.Tabs.TabIcon_HideAndSeek.png");

    public override void AssignRoles(List<PlayerControl> players)
    {
        throw new System.NotImplementedException();
    }

    public override IEnumerable<GameOptionTab> EnabledTabs() => new[] { DefaultTabs.GeneralTab, HnsTab };

    public override void SetupWinConditions(WinDelegate winDelegate)
    {
        throw new System.NotImplementedException();
    }
}