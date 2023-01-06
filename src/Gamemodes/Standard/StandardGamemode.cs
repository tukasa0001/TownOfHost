using System.Collections.Generic;
using TownOfHost.Interface.Menus;
using TownOfHost.Options;

namespace TownOfHost.Gamemodes.Standard;

public class StandardGamemode: IGamemode
{
    public string GetName() => "Standard";

    public IEnumerable<GameOptionTab> EnabledTabs() => DefaultTabs.All;

    public void AssignRoles(List<PlayerControl> players)
    {
        StandardAssignRoles.StandardAssign(players);
    }
}