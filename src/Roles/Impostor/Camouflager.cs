using System;
using System.Linq;
using HarmonyLib;
using TownOfHost.Managers;
using VentLib.Options;
using TownOfHost.Roles.Internals.Attributes;
using TownOfHost.RPC;

namespace TownOfHost.Roles;

public class Camouflager: Morphling
{
    private bool canVent;
    private DateTime lastShapeshift;
    private DateTime lastUnshapeshift;
    private bool camouflaged;

    [RoleAction(RoleActionType.AttemptKill)]
    public new bool TryKill(PlayerControl target) => base.TryKill(target);

    [RoleAction(RoleActionType.Shapeshift)]
    private void CamouflagerShapeshift(PlayerControl target)
    {
        if (camouflaged) return;
        camouflaged = true;
        Game.GetAlivePlayers().Where(p => p.PlayerId != MyPlayer.PlayerId).Do(p => p.CRpcShapeshift(target, true));
    }

    [RoleAction(RoleActionType.Unshapeshift)]
    private void CamouflagerUnshapeshift()
    {
        if (!camouflaged) return;
        camouflaged = false;
        Game.GetAlivePlayers().Where(p => p.PlayerId != MyPlayer.PlayerId).Do(p => p.CRpcRevertShapeshift(true));
    }

    protected override OptionBuilder RegisterOptions(OptionBuilder optionStream) =>
        base.RegisterOptions(optionStream)
            .SubOption(sub => sub
                .Name("Camouflage Cooldown")
                .Bind(v => shapeshiftCooldown = (float)v)
                .AddFloatRange(5, 120, 2.5f, 5, "s")
                .Build())
            .SubOption(sub => sub
                .Name("Camouflage Duration")
                .Bind(v => shapeshiftDuration = (float)v)
                .AddFloatRange(5, 60, 2.5f, 5, "s")
                .Build())
            .SubOption(sub => sub
                .Name("Can Vent")
                .Bind(v => canVent = (bool)v)
                .AddOnOffValues()
                .Build());

    protected override RoleModifier Modify(RoleModifier roleModifier) =>
        base.Modify(roleModifier).CanVent(canVent);
}