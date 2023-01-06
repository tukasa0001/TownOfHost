using System.Collections.Generic;
using TownOfHost.Interface.Menus;
using TownOfHost.Options;

namespace TownOfHost.Gamemodes;

public class Gamemode: IGamemode
{
    public string Name;

    public string GetName() => Name;

    public IEnumerable<GameOptionTab> EnabledTabs() => DefaultTabs.All;
}