using AmongUs.GameOptions;
using TownOfHost.Extensions;
using VentLib.Logging;

namespace TownOfHost.RPC;

public static class CheckedRpc
{
    public static void CRpcShapeshift(this PlayerControl player, PlayerControl target, bool animate)
    {
        if (!player.IsAlive()) return;
        player.RpcShapeshift(target, animate);
    }

    public static void CRpcRevertShapeshift(this PlayerControl player, bool animate)
    {
        VentLogger.Trace("CRevertShapeshift");
        if (!player.IsAlive()) return;
        player.SetName(player.GetRawName());
        player.RpcRevertShapeshift(animate);
    }

    public static void CRpcSetRole(this PlayerControl player, RoleTypes role)
    {
        if (player.IsHost())
            player.SetRole(role);
        player.RpcSetRole(role);
    }
}