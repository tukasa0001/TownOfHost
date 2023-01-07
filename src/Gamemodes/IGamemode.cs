using System.Collections.Generic;
using TownOfHost.Interface.Menus;
using TownOfHost.Managers;

namespace TownOfHost.Gamemodes;

public interface IGamemode
{
    void Activate()
    {
        TOHPlugin.OptionManager.SetTabs(EnabledTabs());
    }

    bool AllowSabotage() => true;

    void AssignRoles(List<PlayerControl> players);

    IEnumerable<GameOptionTab> EnabledTabs();

    string GetName();

    void FixedUpdate() { }

    void SetupWinConditions(WinDelegate winDelegate) { }
}