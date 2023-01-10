using System.Collections.Generic;
using Hazel;
using TownOfHost.Extensions;

namespace VentLib;

public class PizzaExample
{
    public class PizzaOrder: IRpcSendable<PizzaOrder>
    {
        public float Cost;
        public List<string> Toppings = new();

        public PizzaOrder Read(MessageReader reader)
        {
            PizzaOrder order = new()
            {
                Cost = reader.ReadSingle(),
                Toppings = reader.ReadList<string>()
            };
            return order;
        }

        public void Write(MessageWriter writer)

        {
            writer.Write(Cost);
            writer.WriteList(Toppings);
        }

        public override string ToString() => $"Pizza(cost={Cost}, toppings={Toppings.PrettyString()})";
    }

    [ModRPC(400, senders: RpcActors.Everyone, receivers: RpcActors.Host)]
    public static void ProcessPizzaOrder(PizzaOrder order)
    {
        // Since the only receiver is host this code is only ran by the host
        order.Toppings.Add("Pineapple");
        order.Toppings.Add("Black Olive");
        order.Cost = order.Toppings.Count * 3.50f;

        // Unless "executeOnSend = true", calling a method with ModRPC won't run the actual method so this isn't infinite and is safe
        ProcessPizzaOrder(order);
    }


    // Creating a listener only method for non-hosts
    [ModRPC(400, senders: RpcActors.None, receivers: RpcActors.NonHosts)]
    public static void PrintFinalOrder(PizzaOrder order)
    {
        PlayerControl player = VentFramework.GetLastSender(400);
        TownOfHost.Logger.Blue($"{player.GetRawName()} sent over the order: {order}", "PizzaOrder");
    }
}