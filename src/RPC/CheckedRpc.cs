using AmongUs.GameOptions;
using TownOfHost.Extensions;

namespace TownOfHost.RPC;

public static class CheckedRpc
{
    public static void CRpcShapeshift(this PlayerControl player, PlayerControl target, bool animate)
    {
        if (player.Data.IsDead) return;
        player.RpcShapeshift(target, animate);
    }

    public static void CRpcRevertShapeshift(this PlayerControl player, bool animate)
    {
        if (player.Data.IsDead) return;
        player.RpcRevertShapeshift(animate);
    }

    public static void CRpcSetRole(this PlayerControl player, RoleTypes role)
    {
        if (player.IsHost())
            player.SetRole(role);
        player.RpcSetRole(role);
    }
}