using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using TownOfHost.Extensions;
using TownOfHost.Managers;
using TownOfHost.Options;
using TownOfHost.Roles;
using TownOfHost.Victory;
using VentLib.Logging;

namespace TownOfHost.Gamemodes.Debug;

public class DebugGamemode: Gamemode
{
    private List<OptionHolder> specificOptions = new();
    private readonly Dictionary<byte, string> _roleAssignments = new();

    internal static GameOptionTab DebugTab = new("Debug Tab", "TownOfHost.assets.Tabs.Debug_Tab.png");

    public override string GetName() => "Debug";
    public override IEnumerable<GameOptionTab> EnabledTabs() => new[] { DebugTab };

    public override void AssignRoles(List<PlayerControl> players)
    {
        players.Do(p =>
        {
            VentLogger.Debug($"Assigning {p.GetRawName()} => {_roleAssignments.GetValueOrDefault(p.PlayerId)}");
            CustomRole? role = CustomRoleManager.AllRoles.FirstOrDefault(r => r.RoleName.RemoveHtmlTags().ToLower().StartsWith(_roleAssignments.GetValueOrDefault(p.PlayerId)?.ToLower() ?? "HEHEXD"));
            Game.AssignRole(p, role ?? CustomRoleManager.Special.Debugger);
        });

        AntiBlackout.SendGameData();
    }

    public override void SetupWinConditions(WinDelegate winDelegate)
    {
        winDelegate.CancelGameWin();
    }

    public override void Activate()
    {
        foreach (PlayerControl player in PlayerControl.AllPlayerControls)
        {
            OptionHolder option = new SmartOptionBuilder()
                .Name(player.GetRawName())
                .IsHeader(true)
                .Bind(v => _roleAssignments[player.PlayerId] = ((string)v).RemoveHtmlTags())
                .AddValues(0, CustomRoleManager.AllRoles.Select(s => s.RoleColor.Colorize(s.RoleName)).ToArray())
                .Tab(DebugTab)
                .Build();
            specificOptions.Add(option);
            TOHPlugin.OptionManager.Add(option);
        }
    }

    public override void Deactivate()
    {
        List<OptionHolder> allHolders = specificOptions.SelectMany(o => o.GetHoldersRecursive()).ToList();
        TOHPlugin.OptionManager.Options().RemoveAll(p => allHolders.Contains(p));
        TOHPlugin.OptionManager.Options().RemoveAll(p => allHolders.Contains(p));
        allHolders.Do(h =>
        {
            if (h.Tab == null) return;
            h.Tab.GetHolders().Remove(h);
        });
    }
}