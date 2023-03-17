using System;
using System.Collections.Generic;
using System.Linq;
using TOHTOR.API;
using TOHTOR.Extensions;
using TOHTOR.GUI;
using TOHTOR.Managers.History.Events;
using TOHTOR.Patches.Systems;
using TOHTOR.Roles.Interactions;
using TOHTOR.Roles.Interactions.Interfaces;
using TOHTOR.Roles.Internals;
using TOHTOR.Roles.Internals.Attributes;
using UnityEngine;
using VentLib.Networking.RPC;
using VentLib.Utilities;
using VentLib.Utilities.Optionals;

namespace TOHTOR.Roles;

public static class RoleUtils
{
    public static string Arrows = "→↗↑↖←↙↓↘・";

    public static char CalculateArrow(PlayerControl source, PlayerControl target, Color color = default)
    {
        Vector2 sourcePosition = source.GetTruePosition();
        Vector2 targetPosition = target.GetTruePosition();
        float distance = Vector2.Distance(sourcePosition, targetPosition);
        if (distance < ModConstants.ArrowActivationMin) return Arrows[8];

        float deltaX = targetPosition.x - sourcePosition.x;
        float deltaY = targetPosition.y - sourcePosition.y;

        float angle = Mathf.Atan2(deltaY, deltaX) * Mathf.Rad2Deg;
        if (angle < 0)
            angle = 360 + angle;

        int arrow = Mathf.RoundToInt(angle / 45);
        return Arrows[arrow < 8 ?  arrow : 0];
    }

    public static IEnumerable<PlayerControl> GetPlayersWithinDistance(PlayerControl source, float distance)
    {
        Vector2 position = source.GetTruePosition();
        return Game.GetAlivePlayers().Where(p => p.PlayerId != source.PlayerId && Vector2.Distance(position, p.GetTruePosition()) <= distance);
    }

    public static IEnumerable<PlayerControl> GetPlayersWithinDistance(Vector2 position, float distance)
    {
        return Game.GetAlivePlayers().Where(p => Vector2.Distance(position, p.GetTruePosition()) <= distance);
    }

    public static IEnumerable<PlayerControl> GetPlayersOutsideDistance(PlayerControl source, float distance)
    {
        Vector2 sourcePosition = source.GetTruePosition();
        return Game.GetAlivePlayers().Where(p => Vector2.Distance(sourcePosition, p.GetTruePosition()) > distance);
    }

    public static void PlayReactorsForPlayer(PlayerControl player)
    {
        if (SabotagePatch.CurrentSabotage is SabotageType.Reactor) return;
        byte reactorId = GameOptionsManager.Instance.CurrentGameOptions.MapId == 2 ? (byte)21 : (byte)3;
        RpcV2.Immediate(ShipStatus.Instance.NetId, RpcCalls.RepairSystem).Write(reactorId)
            .Write(player).Write((byte)128).Send(player.GetClientId());
    }

    public static void EndReactorsForPlayer(PlayerControl player)
    {
        if (SabotagePatch.CurrentSabotage is SabotageType.Reactor) return;
        byte reactorId = GameOptionsManager.Instance.CurrentGameOptions.MapId == 2 ? (byte)21 : (byte)3;
        RpcV2.Immediate(ShipStatus.Instance.NetId, RpcCalls.RepairSystem).Write(reactorId)
            .Write(player).Write((byte)16).Send(player.GetClientId());
        RpcV2.Immediate(ShipStatus.Instance.NetId, RpcCalls.RepairSystem).Write(reactorId)
            .Write(player).Write((byte)17).Send(player.GetClientId());
    }

    public static string Counter(object numerator, object? denominator = null, Color? color = null)
    {
        color ??= new Color(0.92f, 0.77f, 0.22f);
        return denominator == null
            ? Color.white.Colorize("(" + color.Value.Colorize($"{numerator}") + ")")
            : Color.white.Colorize("(" + color.Value.Colorize($"{numerator}/{denominator}") + ")");
    }

    public static string Cooldown(Cooldown cooldown, Color? color1 = null, Color? color2 = null)
    {
        color1 ??= new Color(0.93f, 0.57f, 0.28f);
        color2 ??= Color.white;
        return cooldown.ToString() == "0" ? "" : $"{color1.Value.Colorize("CD:")} {color2.Value.Colorize(cooldown + "s")}";
    }

    public static bool Attack(this PlayerControl killer, PlayerControl target, Func<IDeathEvent>? causeOfDeath = null)
    {
        if (!target.IsAlive()) return false;
        ActionHandle handle = ActionHandle.NoInit();
        Optional<IDeathEvent> deathEvent = Optional<IDeathEvent>.Of(causeOfDeath?.Invoke());
        Game.TriggerForAll(RoleActionType.PlayerAttacked, ref handle, killer, target, deathEvent);

        if (handle.IsCanceled)
        {
            Game.GameHistory.AddEvent(new KillEvent(killer, target, false));
            killer.RpcGuardAndKill(target);
            return false;
        }

        if (!target.GetCustomRole().CanBeKilled()) {
            ShowGuardianShield(target);
            return false;
        }

        Optional<IDeathEvent> currentDeathEvent = Game.GameHistory.GetCauseOfDeath(target.PlayerId);
        deathEvent.IfPresent(death => Game.GameHistory.SetCauseOfDeath(target.PlayerId, death));
        killer.RpcMurderPlayer(target);
        ActionHandle ignored = ActionHandle.NoInit();
        if (target.IsAlive()) Game.TriggerForAll(RoleActionType.SuccessfulAngelProtect, ref ignored, target, killer);
        else currentDeathEvent.IfPresent(de => Game.GameHistory.SetCauseOfDeath(target.PlayerId, de));
        return true;
    }

    public static InteractionResult InteractWith(this PlayerControl player, PlayerControl target, Interaction interaction)
    {
        ActionHandle handle = ActionHandle.NoInit();
        PlayerControl.AllPlayerControls.ToArray().Where(p => p.PlayerId != interaction.Emitter().MyPlayer.PlayerId).Trigger(RoleActionType.AnyInteraction, ref handle, player, target, interaction);
        if (player.PlayerId != target.PlayerId) target.GetCustomRole().Trigger(RoleActionType.Interaction, ref handle, player, interaction);
        if (!handle.IsCanceled || interaction is IUnblockedInteraction) interaction.Intent().Action(player, target);
        if (handle.IsCanceled) interaction.Intent().Halted(player, target);
        return handle.IsCanceled ? InteractionResult.Halt : InteractionResult.Proceed;
    }

    public static void ShowGuardianShield(PlayerControl target) {
        PlayerControl? randomPlayer = Game.GetAllPlayers().FirstOrDefault(p => p.PlayerId != target.PlayerId);
        if (randomPlayer == null) return;

        RpcV2.Immediate(target.NetId, RpcCalls.ProtectPlayer).Write(target).Write(0).Send(target.GetClientId());
        Async.Schedule(() => RpcV2.Immediate(randomPlayer.NetId, RpcCalls.MurderPlayer).Write(target).Send(target.GetClientId()), NetUtils.DeriveDelay(0.1f));
    }

    public static void SwapPositions(PlayerControl player1, PlayerControl player2)
    {
        if (player1.inVent) player1.MyPhysics.ExitAllVents();
        if (player2.inVent) player2.MyPhysics.ExitAllVents();

        player1.MyPhysics.ResetMoveState();
        player2.MyPhysics.ResetMoveState();

        Vector2 player1Position = player1.GetTruePosition();
        Vector2 player2Position = player2.GetTruePosition();

        if (player1.IsAlive())
            Utils.Teleport(player1.NetTransform, new Vector2(player2Position.x, player2Position.y + 0.3636f));
        if (player2.IsAlive())
            Utils.Teleport(player2.NetTransform, new Vector2(player1Position.x, player1Position.y + 0.3636f));

        player1.moveable = true;
        player2.moveable = true;
        player1.Collider.enabled = true;
        player2.Collider.enabled = true;
        player1.NetTransform.enabled = true;
        player2.NetTransform.enabled = true;
    }

    public static Action<bool> BindOnOffListSetting<T>(List<T> list, T obj)
    {
        return b =>
        {
            if (!b) list.Remove(obj);
            else if (!list.Contains(obj)) list.Add(obj);
        };
    }
}