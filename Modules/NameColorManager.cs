using System.Collections.Generic;
using System.Linq;
using Hazel;

namespace TownOfHost
{
    public class NameColorManager
    {
        public static NameColorManager Instance;

        public List<NameColorData> NameColors;
        public NameColorData DefaultData;

        public List<NameColorData> GetDataBySeer(byte seerId)
            => NameColors.Where(data => data.seerId == seerId).ToList();

        public NameColorData GetData(byte seerId, byte targetId)
        {
            NameColorData data = NameColors.Where(data => data.seerId == seerId && data.targetId == targetId).FirstOrDefault();
            if (data == null) data = DefaultData;
            return data;
        }
        public void Add(byte seerId, byte targetId, string color)
        {
            Add(new NameColorData(seerId, targetId, color));
        }
        public void Add(NameColorData data)
        {
            Remove(data.seerId, data.targetId);
            NameColors.Add(data);
        }
        public void Remove(byte seerId, byte targetId)
        {
            NameColors.RemoveAll(data => data.seerId == seerId && data.targetId == targetId);
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
            NameColors = new List<NameColorData>();
            DefaultData = new NameColorData(0, 0, null);
        }

        public static void Begin()
        {
            Logger.Info("NameColorManagerをリセット", "NameColorManager");
            Instance = new NameColorManager();
        }
    }
    public class NameColorData
    {
        public byte seerId;
        public byte targetId;
        public string color;
        public NameColorData(byte seerId, byte targetId, string color)
        {
            this.seerId = seerId;
            this.targetId = targetId;
            this.color = color == null || color.StartsWith('#') ? color : "#" + color;
        }
        public string OpenTag => color != null ? $"<color={color}>" : "";
        public string CloseTag => color != null ? "</color>" : "";
    }
}