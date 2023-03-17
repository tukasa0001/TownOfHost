using TOHTOR.GUI;
using TOHTOR.Roles.Internals.Attributes;
using UnityEngine;
using VentLib.Logging;
using VentLib.Options.Game;

namespace TOHTOR.Roles.RoleGroups.Impostors;

public class Miner : Vanilla.Impostor
{
    [DynElement(UI.Cooldown)]
    private Cooldown minerAbilityCooldown;
    private Vector2 lastEnteredVentLocation = Vector2.zero;

    [RoleAction(RoleActionType.Attack)]
    public override bool TryKill(PlayerControl target) => base.TryKill(target);

    [RoleAction(RoleActionType.MyEnterVent)]
    private void EnterVent(Vent vent)
    {
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



    protected override GameOptionBuilder RegisterOptions(GameOptionBuilder optionStream) =>
        base.RegisterOptions(optionStream).SubOption(sub =>
            sub.Name("Miner Ability Cooldown")
                .BindFloat(minerAbilityCooldown.SetDuration)
                .AddFloatRange(5, 50, 2.5f, 5, "s")
                .Build());
}