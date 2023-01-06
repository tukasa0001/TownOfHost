using System.Collections.Generic;
using TownOfHost.Interface.Menus;

namespace TownOfHost.Gamemodes;

public interface IGamemode
{
    string GetName();

    IEnumerable<GameOptionTab> EnabledTabs();

    public void Activate()
    {
        TOHPlugin.OptionManager.SetTabs(EnabledTabs());
    }

    public void AssignRoles(List<PlayerControl> players);
}