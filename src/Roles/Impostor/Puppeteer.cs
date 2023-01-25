using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using TownOfHost.Extensions;
using TownOfHost.GUI;
using TownOfHost.Managers;
using TownOfHost.Roles.Internals;
using TownOfHost.Roles.Internals.Attributes;
using UnityEngine;
using VentLib.Utilities.Extensions;

namespace TownOfHost.Roles;

public class Puppeteer: Impostor
{
    private DateTime lastCheck = DateTime.Now;
    private List<PlayerControl> cursedPlayers;

    protected override void Setup(PlayerControl player) => cursedPlayers = new List<PlayerControl>();

    [RoleAction(RoleActionType.AttemptKill)]
    public override bool TryKill(PlayerControl target)
    {
        InteractionResult result = CheckInteractions(target.GetCustomRole(), target);
        if (result is InteractionResult.Halt) return false;

        cursedPlayers.Add(target);
        target.GetDynamicName().AddRule(GameState.Roaming, UI.Misc, new DynamicString(new Color(0.36f, 0f, 0.58f).Colorize("◆")), MyPlayer.PlayerId);
        target.GetDynamicName().AddRule(GameState.InMeeting, UI.Name, new DynamicString(new Color(0.36f, 0f, 0.58f).Colorize("{0} ◆")), MyPlayer.PlayerId);
        target.GetDynamicName().RenderFor(MyPlayer);
        MyPlayer.RpcGuardAndKill(target);
        return true;
    }

    [RoleAction(RoleActionType.FixedUpdate)]
    private void PuppeteerKillCheck()
    {
        double elapsed = (DateTime.Now - lastCheck).TotalSeconds;
        if (elapsed < ModConstants.RoleFixedUpdateCooldown) return;
        lastCheck = DateTime.Now;
        foreach (PlayerControl player in new List<PlayerControl>(cursedPlayers))
        {

            if (player.Data.IsDead) {
                RemovePuppet(player);
                continue;
            }
            List<PlayerControl> inRangePlayers = player.GetPlayersInAbilityRangeSorted().Where(p => !p.GetCustomRole().IsAllied(MyPlayer)).ToList();
            if (inRangePlayers.Count == 0) continue;
            player.RpcMurderPlayer(inRangePlayers.GetRandom());
            RemovePuppet(player);
        }

        cursedPlayers.Where(p => p.Data.IsDead).ToArray().Do(RemovePuppet);
    }

    private void RemovePuppet(PlayerControl puppet)
    {
        puppet.GetDynamicName().RemoveRule(GameState.Roaming, UI.Role, MyPlayer.PlayerId);
        puppet.GetDynamicName().RenderFor(MyPlayer);
        cursedPlayers.Remove(puppet);
    }

    protected override RoleModifier Modify(RoleModifier roleModifier) =>
        base.Modify(roleModifier)
            .OptionOverride(Override.KillCooldown, KillCooldown * 2);
}