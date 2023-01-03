using Reactor.Networking.Attributes;

namespace TownOfHost.RPC;

public enum ModRPC
{
    CamouflageActivation,
    TestRPC
}

public static class TestRPC
{
    [MethodRpc((uint) ModRPC.TestRPC)]
    public static void RpcSayHello(PlayerControl player, string text)
    {
       Logger.Info($"{player.Data.PlayerName} said: {text}", "TestRPC");
    }


}