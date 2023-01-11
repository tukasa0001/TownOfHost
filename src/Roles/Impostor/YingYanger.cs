using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using TownOfHost.Extensions;
using TownOfHost.Interface.Menus.CustomNameMenu;
using TownOfHost.Managers;
using TownOfHost.ReduxOptions;
using UnityEngine;

namespace TownOfHost.Roles;

public class YingYanger : Impostor
{
    private DateTime lastCheck = DateTime.Now;
    private List<PlayerControl> cursedPlayers;

    private float YingYangCD;
    private bool ResetToYingYang;

    private bool InYingMode;

    protected override void Setup(PlayerControl player) => cursedPlayers = new List<PlayerControl>();

    [RoleAction(RoleActionType.AttemptKill)]
    public override bool TryKill(PlayerControl target)
    {
        InteractionResult result = CheckInteractions(target.GetCustomRole(), target);
        if (result is InteractionResult.Halt) return false;
        if (!InYingMode) return true;

        cursedPlayers.Add(target);
        target.GetDynamicName().AddRule(GameState.Roaming, UI.Misc, new DynamicString(new Color(0.36f, 0f, 0.58f).Colorize("◆")), MyPlayer.PlayerId);
        target.GetDynamicName().RenderFor(MyPlayer);
        MyPlayer.RpcGuardAndKill(target);

        if (cursedPlayers.Count >= 2) InYingMode = false;
        return true;
    }

    [RoleAction(RoleActionType.RoundStart)]
    private void RoundStart() => InYingMode = true;

    [RoleAction(RoleActionType.RoundEnd)]
    private void RoundEnd() => cursedPlayers.Clear();

    [RoleAction(RoleActionType.FixedUpdate)]
    private void YingYangerKillCheck()
    {
        double elapsed = (DateTime.Now - lastCheck).TotalSeconds;
        if (elapsed < ModConstants.RoleFixedUpdateCooldown) return;
        lastCheck = DateTime.Now;
        foreach (PlayerControl player in new List<PlayerControl>(cursedPlayers))
        {
            if (player.Data.IsDead)
            {
                RemovePuppet(player);
                continue;
            }
            List<PlayerControl> inRangePlayers = player.GetPlayersInAbilityRangeSorted().Where(p => !p.GetCustomRole().IsAllied(MyPlayer) && cursedPlayers.Contains(p)).ToList();
            if (inRangePlayers.Count == 0) continue;
            player.RpcMurderPlayer(inRangePlayers.GetRandom());
            RemovePuppet(player);
        }
        cursedPlayers.Where(p => p.Data.IsDead).ToArray().Do(RemovePuppet);
        if (cursedPlayers.Count <= 2 && !InYingMode && ResetToYingYang) InYingMode = true;
    }

    private void RemovePuppet(PlayerControl puppet)
    {
        puppet.GetDynamicName().RemoveRule(GameState.Roaming, UI.Role, MyPlayer.PlayerId);
        puppet.GetDynamicName().RenderFor(MyPlayer);
        cursedPlayers.Remove(puppet);
    }

    protected override RoleModifier Modify(RoleModifier roleModifier) =>
        base.Modify(roleModifier)
            .OptionOverride(Override.KillCooldown, KillCooldown * 2, () => InYingMode);
    protected override SmartOptionBuilder RegisterOptions(SmartOptionBuilder optionStream) =>
        base.RegisterOptions(optionStream)
        .AddSubOption(sub => sub
            .Name("Ying Yang Cooldown")
            .BindFloat(v => YingYangCD = v)
            .AddFloatRangeValues(2.5f, 180, 2.5f, 5)
            .Build())
        .AddSubOption(sub => sub
            .Name("Reset to Ying Yang on Target Death")
            .Bind(v => ResetToYingYang = (bool)v)
            .AddOnOffValues()
            .Build());
}