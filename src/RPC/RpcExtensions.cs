namespace TownOfHost.RPC;

public static class RpcExtensions
{
    public static void RpcShapeshiftV2(this PlayerControl player, PlayerControl target, bool animate)
    {
        if (player.Data.IsDead) return;
        player.RpcShapeshift(target, animate);
    }

    public static void RpcRevertShapeshiftV2(this PlayerControl player, bool animate)
    {
        if (player.Data.IsDead) return;
        player.RpcRevertShapeshift(animate);
    }
}