using System.Collections.Generic;
using TownOfHost.Interface.Menus;
using TownOfHost.Victory;

namespace TownOfHost.Gamemodes;

public interface IGamemode
{
    internal void Activate()
    {
        TOHPlugin.OptionManager.SetTabs(EnabledTabs());
    }

    bool AllowSabotage();

    bool AllowBodyReport();

    void AssignRoles(List<PlayerControl> players);

    IEnumerable<GameOptionTab> EnabledTabs();

    void FixedUpdate();

    string GetName();

    void SetupWinConditions(WinDelegate winDelegate);
}