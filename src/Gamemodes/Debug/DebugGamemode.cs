using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using TOHTOR.API;
using TOHTOR.Extensions;
using TOHTOR.Options;
using VentLib.Options;
using TOHTOR.Roles;
using TOHTOR.Victory;
using VentLib.Logging;
using VentLib.Options.Game;
using VentLib.Options.Game.Tabs;
using VentLib.Utilities;

namespace TOHTOR.Gamemodes.Debug;

public class DebugGamemode: Gamemode
{
    private List<Option> specificOptions = new();
    private readonly Dictionary<byte, string> _roleAssignments = new();

    internal static GameOptionTab DebugTab = new("Debug Tab", () => Utils.LoadSprite("TOHTOR.assets.Tabs.Debug_Tab.png"));

    public override string GetName() => "Debug";
    public override IEnumerable<GameOptionTab> EnabledTabs() => new[] { DebugTab };

    public override void AssignRoles(List<PlayerControl> players)
    {
        players.Do(p =>
        {
            VentLogger.Debug($"Assigning {p.GetRawName()} => {_roleAssignments.GetValueOrDefault(p.PlayerId)}");
            CustomRole? role = CustomRoleManager.AllRoles.FirstOrDefault(r => r.RoleName.RemoveHtmlTags().ToLower().StartsWith(_roleAssignments.GetValueOrDefault(p.PlayerId)?.ToLower() ?? "HEHEXD"));
            Game.AssignRole(p, role ?? CustomRoleManager.Special.Debugger, true);
        });
    }

    public override void SetupWinConditions(WinDelegate winDelegate)
    {
        winDelegate.CancelGameWin();
    }

    public override void Activate()
    {
        foreach (PlayerControl player in PlayerControl.AllPlayerControls)
        {
            Option option = new GameOptionBuilder()
                .Name(player.GetRawName())
                .IsHeader(true)
                .Bind(v => _roleAssignments[player.PlayerId] = ((string)v).RemoveHtmlTags())
                .Values(CustomRoleManager.AllRoles.Select(s => s.RoleColor.Colorize(s.RoleName)))
                .Tab(DebugTab)
                .Build();
            specificOptions.Add(option);
        }
    }

    public override void Deactivate()
    {
        /*List<Option> allHolders = specificOptions.SelectMany(o => o.GetHoldersRecursive()).ToList();
        TOHPlugin.OptionManager.Options().RemoveAll(p => allHolders.Contains(p));
        TOHPlugin.OptionManager.Options().RemoveAll(p => allHolders.Contains(p));
        allHolders.Do(h =>
        {
            if (h.Tab == null) return;
            h.Tab.GetHolders().Remove(h);
        });*/
    }
}