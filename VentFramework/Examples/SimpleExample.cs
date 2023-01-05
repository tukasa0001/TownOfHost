using TownOfHost.Extensions;

namespace VentFramework;

public class SimpleExample
{
    [ModRPC((uint)ExampleRpc.SayHello)]
    public static void SayHello(PlayerControl player, string message)
    {
        TownOfHost.Logger.Info($"{player.GetRawName()} says: {message}", "VentHello");
    }

    [ModRPC((uint)ExampleRpc.SayHello, receivers: RpcActors.Host)]
    public static void SayHelloToHost(PlayerControl player, string message)
    {
        TownOfHost.Logger.Info($"Am Host? {AmongUsClient.Instance.AmHost}", "VentHost");
        TownOfHost.Logger.Info($"{player.GetRawName()} says: {message}", "VentHost");
    }

    [ModRPC((uint)ExampleRpc.SayGoodbye, senders: RpcActors.Host)]
    public static void HostSaysGoodbye(PlayerControl player, string message)
    {
        TownOfHost.Logger.Info($"Host sent: {player.GetRawName()} says: {message}", "VentGoodbye");
    }

    [ModRPC((uint)ExampleRpc.SayGoodbye, receivers: RpcActors.NonHosts)]
    public static void NonHostHearsGoodbye(PlayerControl player, string message)
    {
        TownOfHost.Logger.Info($"Am Host? {AmongUsClient.Instance.AmHost}", "VentHost");
        TownOfHost.Logger.Info($"My Goodbye is from {player.GetRawName()} saying: {message}", "VentGoodbye");
    }

    [ModRPC((uint) ExampleRpc.HostToClientCheck, senders: RpcActors.Host, receivers: RpcActors.NonHosts)]
    public static void HostValidatesClient(int number) // Vent doesn't require net objects in the parameters
    {
        const int arbitraryNumber = 3011;
        TownOfHost.Logger.Info($"Am Host? {AmongUsClient.Instance.AmHost}", "VentHost"); // will always be false

        if (number != arbitraryNumber)
            ClientHatesHost(number);
    }

    [ModRPC((uint) ExampleRpc.ClientRespondsToHostCheck, senders: RpcActors.NonHosts, receivers: RpcActors.Host)]
    public static void ClientHatesHost(int number)
    {
        PlayerControl sender = ModRPC.GetLastSender((uint)ExampleRpc.ClientRespondsToHostCheck);
        TownOfHost.Logger.Info($"{sender} has the number: {number}", "VentNumber");
    }

}


public enum ExampleRpc
{
    SayHello = 50,
    SayGoodbye,
    HostToClientCheck,
    ClientRespondsToHostCheck
}