using System.Collections.Generic;
using System.Linq;
using Hazel;

namespace TownOfHost
{
    public class NameColorManager
    {
        public static NameColorManager Instance;
        public NameColorData DefaultData;

        public NameColorData GetData(byte seerId, byte targetId)
        {
            if (!Main.PlayerStates[seerId].TargetColorData.TryGetValue(targetId, out var color))
                return DefaultData;
            return new NameColorData(targetId, color);
        }
        public void Add(byte seerId, byte targetId, string color)
        {
            Remove(seerId, targetId);
            Main.PlayerStates[seerId].TargetColorData.Add(targetId, color);
        }
        public void Remove(byte seerId, byte targetId)
        {
            Main.PlayerStates[seerId].TargetColorData.Remove(targetId);
        }

        public void RpcAdd(byte seerId, byte targetId, string color)
        {
            if (!AmongUsClient.Instance.AmHost) return;

            MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.AddNameColorData, SendOption.Reliable, -1);
            writer.Write(seerId);
            writer.Write(targetId);
            writer.Write(color);

            AmongUsClient.Instance.FinishRpcImmediately(writer);

            RPC.AddNameColorData(seerId, targetId, color);
        }
        public void RpcRemove(byte seerId, byte targetId)
        {
            if (!AmongUsClient.Instance.AmHost) return;

            MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.RemoveNameColorData, SendOption.Reliable, -1);
            writer.Write(seerId);
            writer.Write(targetId);
            AmongUsClient.Instance.FinishRpcImmediately(writer);

            RPC.RemoveNameColorData(seerId, targetId);
        }
        public void RpcReset()
        {
            if (!AmongUsClient.Instance.AmHost) return;

            MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.ResetNameColorData, SendOption.Reliable, -1);
            AmongUsClient.Instance.FinishRpcImmediately(writer);

            RPC.ResetNameColorData();
        }

        public NameColorManager()
        {
            DefaultData = new NameColorData(0, null);
        }

        public static void Begin()
        {
            Logger.Info("NameColorManagerをリセット", "NameColorManager");
            Instance = new NameColorManager();
        }
    }
    public class NameColorData
    {
        public byte targetId;
        public string color;
        public NameColorData(byte targetId, string color)
        {
            this.targetId = targetId;
            this.color = color == null || color.StartsWith('#') ? color : "#" + color;
        }
        public string OpenTag => color != null ? $"<color={color}>" : "";
        public string CloseTag => color != null ? "</color>" : "";
    }
}