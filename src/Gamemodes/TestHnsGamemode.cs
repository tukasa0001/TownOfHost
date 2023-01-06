using System.Collections.Generic;
using TownOfHost.Interface.Menus;
using TownOfHost.Options;

namespace TownOfHost.Gamemodes;

public class TestHnsGamemode: IGamemode
{
    public static GameOptionTab HnsTab = new("Hide & Seek Options", "TownOfHost.assets.Tabs.TabIcon_HideAndSeek.png");

    public string GetName() => "Hide and Seek";

    public IEnumerable<GameOptionTab> EnabledTabs() => new[] { DefaultTabs.GeneralTab, HnsTab };

    public void AssignRoles(List<PlayerControl> players)
    {
        throw new System.NotImplementedException();
    }
}