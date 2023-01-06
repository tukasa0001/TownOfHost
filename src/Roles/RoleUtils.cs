using System.Collections.Generic;
using System.Linq;
using TownOfHost.Extensions;
using TownOfHost.Managers;
using TownOfHost.RPC;
using UnityEngine;
using VentFramework;

namespace TownOfHost.Roles;

public class RoleUtils
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
        byte reactorId = GameOptionsManager.Instance.CurrentGameOptions.MapId == 2 ? (byte)21 : (byte)3;
        RpcV2.Immediate(ShipStatus.Instance.NetId, RpcCalls.RepairSystem).Write(reactorId)
            .Write(player).Write((byte)128).Send(player.GetClientId());
    }

    public static void EndReactorsForPlayer(PlayerControl player)
    {
        byte reactorId = GameOptionsManager.Instance.CurrentGameOptions.MapId == 2 ? (byte)21 : (byte)3;
        RpcV2.Immediate(ShipStatus.Instance.NetId, RpcCalls.RepairSystem).Write(reactorId)
            .Write(player).Write((byte)16).Send(player.GetClientId());
        RpcV2.Immediate(ShipStatus.Instance.NetId, RpcCalls.RepairSystem).Write(reactorId)
            .Write(player).Write((byte)17).Send(player.GetClientId());
    }

    public static string Counter(object numerator, object denominator, Color? color = null)
    {
        color ??= new Color(0.92f, 0.77f, 0.22f);
        return Color.white.Colorize("(" + color.Value.Colorize($"{numerator}/{denominator}") + ")");
    }

    public static bool RoleCheckedMurder(PlayerControl killer, PlayerControl target)
    {
        if (!target.IsAlive()) return false;
        if (!target.GetCustomRole().CanBeKilled()) return false;

        killer.RpcMurderPlayer(target);
        ActionHandle ignored = ActionHandle.NoInit();
        if (target.IsAlive()) Game.TriggerForAll(RoleActionType.SuccessfulAngelProtect, ref ignored, target, killer);
        return true;
    }
}