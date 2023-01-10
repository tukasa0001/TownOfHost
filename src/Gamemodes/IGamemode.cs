using System.Collections.Generic;
using TownOfHost.Options;
using TownOfHost.Victory;

namespace TownOfHost.Gamemodes;

public interface IGamemode
{
    string GetName();

    GameAction IgnoredActions();

    IEnumerable<GameOptionTab> EnabledTabs();

    void Activate();

    void Deactivate();

    void FixedUpdate();

    void AssignRoles(List<PlayerControl> players);

    void SetupWinConditions(WinDelegate winDelegate);

    internal void InternalActivate()
    {
        TOHPlugin.OptionManager.SetTabs(EnabledTabs());
        Activate();
    }
}