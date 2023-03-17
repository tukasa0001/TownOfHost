using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using TOHTOR.API;
using TOHTOR.Extensions;
using TOHTOR.GUI;
using TOHTOR.Roles.Internals;
using TOHTOR.Roles.Internals.Attributes;
using UnityEngine;
using VentLib.Options.Game;
using VentLib.Utilities;
using VentLib.Utilities.Extensions;

namespace TOHTOR.Roles.RoleGroups.Impostors;

public class YingYanger : Vanilla.Impostor
{
    private DateTime lastCheck = DateTime.Now;
    private List<PlayerControl> cursedPlayers;

    private float YingYangCD;
    private bool ResetToYingYang;
    private bool InYingMode;

    protected override void Setup(PlayerControl player) => cursedPlayers = new List<PlayerControl>();

    [RoleAction(RoleActionType.Attack)]
    public override bool TryKill(PlayerControl target)
    {
        InteractionResult result = CheckInteractions(target.GetCustomRole(), target);
        if (result is InteractionResult.Halt) return false;
        if (!InYingMode) return false;

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

    protected override GameOptionBuilder RegisterOptions(GameOptionBuilder optionStream) =>
        base.RegisterOptions(optionStream)
        .SubOption(sub => sub
            .Name("Ying Yang Cooldown")
            .BindFloat(v => YingYangCD = v)
            .AddFloatRange(2.5f, 180, 2.5f, 5, "s")
            .Build())
        .SubOption(sub => sub
            .Name("Reset to Ying Yang on Target Death")
            .BindBool(v => ResetToYingYang = v)
            .AddOnOffValues()
            .Build());

    protected override RoleModifier Modify(RoleModifier roleModifier) =>
        base.Modify(roleModifier)
            .OptionOverride(Override.KillCooldown, YingYangCD * 2, () => InYingMode);
}