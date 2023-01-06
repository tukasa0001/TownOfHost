using System.Collections.Generic;
using TownOfHost.Interface.Menus;
using TownOfHost.Options;

namespace TownOfHost.Gamemodes;

public class StandardGamemode: IGamemode
{
    public string GetName() => "Standard";

    public IEnumerable<GameOptionTab> EnabledTabs() => DefaultTabs.All;
}