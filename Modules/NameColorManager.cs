using System.Collections.Generic;
using System.Linq;
using Hazel;
using UnityEngine;

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
        public static void Add(byte seerId, byte targetId, string colorCode = "")
        {
            var seer = Utils.GetPlayerById(seerId);
            var target = Utils.GetPlayerById(targetId);
            if (seer == null || target == null) return;

            if (colorCode == "")
                colorCode = target.GetRoleColorCode();

            var state = Main.PlayerStates[seerId];
            if (state.TargetColorData.TryGetValue(targetId, out var value) && colorCode == value) return;
            state.TargetColorData.Add(targetId, colorCode);

            SendRPC(seerId, targetId, colorCode);
        }
        public static void Remove(byte seerId, byte targetId)
        {
            var seer = Utils.GetPlayerById(seerId);
            var target = Utils.GetPlayerById(targetId);
            if (seer == null || target == null) return;

            var state = Main.PlayerStates[seerId];
            if (!state.TargetColorData.ContainsKey(targetId)) return;
            state.TargetColorData.Remove(targetId);

            SendRPC(seerId, targetId);

        }
        public static void RemoveAll(byte seerId)
        {
            var seer = Utils.GetPlayerById(seerId);
            if (seer == null) return;

            Main.PlayerStates[seerId].TargetColorData.Clear();

            SendRPC(seerId);
        }
        private static void SendRPC(byte seerId, byte targetId = byte.MaxValue, string colorCode = "")
        {
            if (!AmongUsClient.Instance.AmHost) return;

            MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetNameColorData, SendOption.Reliable, -1);
            writer.Write(seerId);
            writer.Write(targetId);
            writer.Write(colorCode);
            AmongUsClient.Instance.FinishRpcImmediately(writer);
        }
        public static void ReceiveRPC(MessageReader reader)
        {
            byte seerId = reader.ReadByte();
            byte targetId = reader.ReadByte();
            string colorCode = reader.ReadString();

            if (targetId == byte.MaxValue)
                RemoveAll(seerId);
            else if (colorCode == "")
                Remove(seerId, targetId);
            else
                Add(seerId, targetId, colorCode);
        }
    }
}