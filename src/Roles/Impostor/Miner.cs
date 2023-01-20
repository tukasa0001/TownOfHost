using System;
using TownOfHost.Extensions;
using TownOfHost.GUI;
using TownOfHost.Options;
using UnityEngine;
using VentLib.Logging;


namespace TownOfHost.Roles;

public class Miner : Impostor
{
    [DynElement(UI.Cooldown)]
    private Cooldown minerAbilityCooldown;
    private Vector2 lastEnteredVentLocation = Vector2.zero;

    [RoleAction(RoleActionType.AttemptKill)]
    public override bool TryKill(PlayerControl target) => base.TryKill(target);

    [RoleAction(RoleActionType.AnyEnterVent)]
    private void EnterVent(Vent vent, PlayerControl player)
    {
        if (player.PlayerId != MyPlayer.PlayerId) return;
        lastEnteredVentLocation = vent.transform.position;
    }

    [RoleAction(RoleActionType.OnPet)]
    public void MinerVentAction()
    {
        if (minerAbilityCooldown.NotReady()) return;
        minerAbilityCooldown.Start();

        if (lastEnteredVentLocation == Vector2.zero) return;
        VentLogger.Trace($"{MyPlayer.Data.PlayerName}:{lastEnteredVentLocation}", "MinerTeleport");
        Utils.Teleport(MyPlayer.NetTransform, new Vector2(lastEnteredVentLocation.x, lastEnteredVentLocation.y + 0.3636f));
    }



    protected override SmartOptionBuilder RegisterOptions(SmartOptionBuilder optionStream) =>
        base.RegisterOptions(optionStream).AddSubOption(sub =>
            sub.Name("Miner Ability Cooldown")
                .Bind(v => minerAbilityCooldown.Duration = Convert.ToSingle(v))
                .AddFloatRangeValues(5, 50, 2.5f, 5, "s")
                .Build());
}