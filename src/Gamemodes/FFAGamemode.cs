using System.Collections.Generic;
using System.Linq;
using AmongUs.GameOptions;
using HarmonyLib;
using TownOfHost.Extensions;
using TownOfHost.Interface.Menus;
using TownOfHost.Managers;
using TownOfHost.Roles;
using VentFramework;

namespace TownOfHost.Gamemodes;

public class FFAGamemode: IGamemode
{
    public static GameOptionTab FFATab = new("Free For All Options", "TownOfHost.assets.Tabs.TabIcon_FreeForAll.png");

    public string GetName() => "Free For All";

    public IEnumerable<GameOptionTab> EnabledTabs() => new[] { FFATab };

    public void AssignRoles(List<PlayerControl> players)
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
}