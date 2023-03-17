using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using TOHTOR.API;
using TOHTOR.Extensions;
using TOHTOR.Factions;
using TOHTOR.GUI;
using TOHTOR.Roles.Events;
using TOHTOR.Roles.Internals;
using TOHTOR.Roles.Internals.Attributes;
using TOHTOR.Roles.RoleGroups.Vanilla;
using UnityEngine;
using VentLib.Logging;
using VentLib.Networking.RPC;
using VentLib.Options.Game;
using VentLib.Utilities;
using VentLib.Utilities.Extensions;
using VentLib.Utilities.Optionals;

namespace TOHTOR.Roles.RoleGroups.Impostors;

public class Swooper: Impostor
{
    private bool canVentNormally;
    private bool endsAtOriginalVent;
    private bool canBeSeenByAllied;
    private Optional<Vent> initialVent = null!;

    [DynElement(UI.Cooldown)]
    private Cooldown swooperCooldown = null!;
    private Cooldown swoopingDuration = null!;

    private DateTime lastEntered = DateTime.Now;

    [RoleAction(RoleActionType.Attack)]
    public override bool TryKill(PlayerControl target) => base.TryKill(target);

    [RoleAction(RoleActionType.MyEnterVent)]
    private void SwooperInvisible(Vent vent, ActionHandle handle)
    {
        if (swooperCooldown.NotReady() || swoopingDuration.NotReady())
        {
            if (canVentNormally) return;
            if (swoopingDuration.IsReady()) handle.Cancel();
            return;
        }

        PlayerControl[] unaffected = GetUnaffected().ToArray();
        unaffected.Where(p => p.PlayerId != MyPlayer.PlayerId).ForEach(p =>
        {
            MyPlayer.GetDynamicName().AddRule(GameState.Roaming, UI.Misc, new DynamicString(() => swoopingDuration.NotReady() ? $"{{0}} {Color.red.Colorize("Swooping")}" : ""), p.PlayerId);
        });

        MyPlayer.GetDynamicName().SetComponentValue(UI.Cooldown, new DynamicString(() => RoleUtils.Cooldown(swoopingDuration, new Color(0.2f, 0.63f, 0.29f))));

        initialVent = Optional<Vent>.Of(vent);

        swoopingDuration.Start();
        Game.GameHistory.AddEvent(new GenericAbilityEvent(MyPlayer, $"{MyPlayer.GetRawName()} began swooping."));
        lastEntered = DateTime.Now;
        Async.Schedule(() => RpcV2.Immediate(MyPlayer.MyPhysics.NetId, RpcCalls.BootFromVent).WritePacked(vent.Id).SendInclusive(unaffected.Select(p => p.GetClientId()).ToArray()), 0.4f);
        Async.Schedule(EndSwooping, swoopingDuration.Duration);
    }

    [RoleAction(RoleActionType.VentExit)]
    private void SwooperExitHandle(Vent vent, ActionHandle handle)
    {
        if (swoopingDuration.IsReady() || DateTime.Now.Subtract(lastEntered).TotalSeconds < 0.5) return;
        VentLogger.Trace("Handling Swooping Exit");
        lastEntered = DateTime.Now;
        handle.Cancel();
        Async.Schedule(() => RpcV2.Immediate(MyPlayer.MyPhysics.NetId, RpcCalls.BootFromVent).WritePacked(vent.Id).SendInclusive( GetUnaffected().Select(p => p.GetClientId()).ToArray()), 0.4f);
    }

    private void EndSwooping()
    {
        int ventId = initialVent.Map(v => v.Id).OrElse(0);
        VentLogger.Trace($"Ending Swooping (ID: {ventId})");

        Async.Schedule(() =>
        {
            if (endsAtOriginalVent && initialVent.Exists())
            {
                Vector2 position = initialVent.Get().transform.position;
                Utils.Teleport(MyPlayer.NetTransform, new Vector2(position.x, position.y + 0.3636f));
            }
            MyPlayer.MyPhysics.RpcBootFromVent(ventId);
        }, 0.4f);

        MyPlayer.GetDynamicName().SetComponentValue(UI.Cooldown, new DynamicString(() => RoleUtils.Cooldown(swooperCooldown)));
        swooperCooldown.Start();
    }

    private IEnumerable<PlayerControl> GetUnaffected() => Game.GetAllPlayers().Where(p => !p.IsAlive() || canBeSeenByAllied && p.GetCustomRole().Factions.IsImpostor()).AddItem(MyPlayer);


    protected override GameOptionBuilder RegisterOptions(GameOptionBuilder optionStream) => base.RegisterOptions(optionStream)
        .SubOption(sub => sub.Name("Invisibility Cooldown")
            .AddFloatRange(5, 120, 2.5f, 16, "s")
            .BindFloat(swooperCooldown.SetDuration)
            .Build())
        .SubOption(sub => sub.Name("Swooping Duration")
            .AddFloatRange(5, 60, 1f, 5, "s")
            .BindFloat(swoopingDuration.SetDuration)
            .Build())
        .SubOption(sub => sub.Name("Ends Swooping at initial Vent")
            .AddOnOffValues()
            .BindBool(b => endsAtOriginalVent = b)
            .Build())
        .SubOption(sub => sub.Name("Can be Seen By Allies")
            .AddOnOffValues()
            .BindBool(b => canBeSeenByAllied = b)
            .Build())
        .SubOption(sub => sub.Name("Can Vent During Cooldown")
            .AddOnOffValues(false)
            .BindBool(b => canVentNormally = b)
            .Build());
}