using System.Collections.Generic;
using HarmonyLib;
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
        Activate();
        TOHPlugin.OptionManager.SetTabs(EnabledTabs());
    }

    internal void InternalDeactivate()
    {
        Deactivate();
        EnabledTabs().Do(tab => tab.SetActive(false));
    }

    public void Trigger(GameAction action, params object[] args);
}