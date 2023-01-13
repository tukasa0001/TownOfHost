using System.Linq;
using InnerNet;

namespace VentLib.Utilities;

public static class NetworkUtils
{
    public static ClientData? GetClient(this PlayerControl player)
    {
        var client = AmongUsClient.Instance.allClients.ToArray().FirstOrDefault(cd => cd.Character.PlayerId == player.PlayerId);
        return client;
    }
    public static int GetClientId(this PlayerControl player)
    {
        var client = player.GetClient();
        return client?.Id ?? -1;
    }
}