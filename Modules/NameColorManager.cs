using System.Collections.Generic;
using System.Linq;
using Hazel;

namespace TownOfHost
{
    public static class NameColorManager
    {
        public static string ApplyNameColorData(this string name, byte seerId, byte targetId)
        {
            string openTag = "", closeTag = "";
            if (Main.PlayerStates[seerId].TargetColorData.TryGetValue(targetId, out var color) && color != null)
            {
                if (!color.StartsWith('#'))
                    color = "#" + color;
                openTag = $"<color={color}>";
                closeTag = "</color>";
            }
            return openTag + name + closeTag;
        }
        public static void Add(byte seerId, byte targetId, string color)
        {
            Remove(seerId, targetId);
            Main.PlayerStates[seerId].TargetColorData.Add(targetId, color);
        }
        public static void Remove(byte seerId, byte targetId)
        {
            Main.PlayerStates[seerId].TargetColorData.Remove(targetId);
        }

        public static void RpcAdd(byte seerId, byte targetId, string color)
        {
            if (!AmongUsClient.Instance.AmHost) return;

            MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.AddNameColorData, SendOption.Reliable, -1);
            writer.Write(seerId);
            writer.Write(targetId);
            writer.Write(color);

            AmongUsClient.Instance.FinishRpcImmediately(writer);

            Add(seerId, targetId, color);
        }
        public static void RpcRemove(byte seerId, byte targetId)
        {
            if (!AmongUsClient.Instance.AmHost) return;

            MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.RemoveNameColorData, SendOption.Reliable, -1);
            writer.Write(seerId);
            writer.Write(targetId);
            AmongUsClient.Instance.FinishRpcImmediately(writer);

            Remove(seerId, targetId);
        }
    }
}