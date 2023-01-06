using System.Collections.Generic;
using TownOfHost.Interface.Menus;
using TownOfHost.Options;

namespace TownOfHost.Gamemodes;

public class TestHnsGamemode: IGamemode
{
    private static GameOptionTab HnsTab = new("Hide & Seek Options", "TownOfHost.assets.TabIcon_HideAndSeek.png");

    public string GetName() => "Hide and Seek";

    public IEnumerable<GameOptionTab> EnabledTabs() => new[] { DefaultTabs.GeneralTab, HnsTab };


}