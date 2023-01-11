using System.Collections.Generic;
using HarmonyLib;
using TownOfHost.Managers;
using TownOfHost.Options;
using TownOfHost.Roles;
using TownOfHost.Victory;

namespace TownOfHost.Gamemodes.Debug;

public class DebugGamemode: Gamemode
{
    internal static GameOptionTab DebugTab = new("Debug Tab", "TownOfHost.assets.Tabs.Debug_Tab.png");

    public override string GetName() => "Debug";
    public override IEnumerable<GameOptionTab> EnabledTabs() => new[] { DebugTab };

    public override void AssignRoles(List<PlayerControl> players)
    {
        players.Do(p => Game.AssignRole(p, CustomRoleManager.Special.Debugger));
    }

    public override void SetupWinConditions(WinDelegate winDelegate)
    {
        winDelegate.CancelGameWin();
    }
}