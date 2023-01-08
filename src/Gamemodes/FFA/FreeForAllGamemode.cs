using System.Collections.Generic;
using System.Linq;
using AmongUs.GameOptions;
using HarmonyLib;
using TownOfHost.Extensions;
using TownOfHost.Interface.Menus;
using TownOfHost.Managers;
using TownOfHost.Roles;
using TownOfHost.Victory;
using VentFramework;

namespace TownOfHost.Gamemodes.FFA;

public class FreeForAllGamemode: Gamemode
{
    public static GameOptionTab FFATab = new("Free For All Options", "TownOfHost.assets.Tabs.TabIcon_FreeForAll.png");

    public override string GetName() => "Free For All";

    public override IEnumerable<GameOptionTab> EnabledTabs() => new[] { FFATab };

    public override void AssignRoles(List<PlayerControl> players)
    {
        PlayerControl localPlayer = PlayerControl.LocalPlayer;
        localPlayer.SetRole(RoleTypes.Impostor);

        foreach (PlayerControl player in players)
        {
            RpcV2.Immediate(player.NetId, (byte)RpcCalls.SetRole).Write((byte)RoleTypes.Impostor).Send(player.GetClientId());
            RpcV2.Immediate(player.NetId, (byte)RpcCalls.SetRole).Write((byte)RoleTypes.Crewmate).SendToAll(player.GetClientId());
            Game.AssignRole(player, CustomRoleManager.Static.SerialKiller);
        }

        players.Where(p => p.PlayerId != localPlayer.PlayerId).Do(p => p.SetRole(RoleTypes.Crewmate));
    }

    public override bool AllowSabotage() => false;
    public override bool AllowBodyReport() => false;

    public override void SetupWinConditions(WinDelegate winDelegate)
    {
        winDelegate.AddWinCondition(new FFAWinCondition());
    }

}