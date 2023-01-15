using TownOfHost.Extensions;
using VentLib.RPC;

namespace TownOfHost.Player;

public class Pet
{
    public static void Guarantee(PlayerControl player)
    {
        RpcV2.Immediate(player.NetId, RpcCalls.SetPetStr).Write("pet_clank").Send(player.GetClientId());
    }

}