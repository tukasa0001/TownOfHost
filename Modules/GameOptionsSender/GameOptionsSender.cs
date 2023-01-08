using System;
using System.Collections.Generic;
using AmongUs.GameOptions;
using Hazel;
using InnerNet;

namespace TownOfHost.Modules
{
    public abstract class GameOptionsSender
    {
        #region Static
        public readonly static List<GameOptionsSender> AllSenders = new(15) { new NormalGameOptionsSender() };

        public static void SendAllGameOptions()
        {
            AllSenders.RemoveAll(s => !s.AmValid());
            foreach (var sender in AllSenders)
            {
                if (sender.IsDirty) sender.SendGameOptions();
                sender.IsDirty = false;
            }
        }
        #endregion

        public abstract IGameOptions BasedGameOptions { get; }
        public abstract bool IsDirty { get; protected set; }
        public byte[] ByteArray = new byte[0]; // 送信時に使い回す配列


        public virtual void SendGameOptions()
        {
            var opt = BuildGameOptions();

            // option => byte[]
            MessageWriter writer = MessageWriter.Get(SendOption.None);
            writer.Write(opt.Version);
            writer.StartMessage(0);
            writer.Write((byte)opt.GameMode);
            if (opt.TryCast<NormalGameOptionsV07>(out var normalOpt))
                NormalGameOptionsV07.Serialize(writer, normalOpt);
            else if (opt.TryCast<HideNSeekGameOptionsV07>(out var hnsOpt))
                HideNSeekGameOptionsV07.Serialize(writer, hnsOpt);
            else
            {
                writer.Recycle();
                Logger.Error("オプションのキャストに失敗しました", this.ToString());
            }
            writer.EndMessage();

            // 配列化&送信
            Span<byte> writerSpan = new(writer.Buffer, 1, writer.Length - 1);
            if (ByteArray == null || ByteArray.Length != writerSpan.Length) ByteArray = new byte[writerSpan.Length];
            for (int i = 0; i < ByteArray.Length; i++)
                ByteArray[i] = writerSpan[i];

            SendOptionsArray(ByteArray);
            writer.Recycle();
        }
        public virtual void SendOptionsArray(byte[] optionArray)
        {
            for (byte i = 0; i < GameManager.Instance.LogicComponents.Count; i++)
            {
                if (GameManager.Instance.LogicComponents[i].TryCast<LogicOptions>(out _))
                {
                    SendOptionsArray(optionArray, i, -1);
                }
            }
        }
        protected virtual void SendOptionsArray(byte[] optionArray, byte LogicOptionsIndex, int targetClientId)
        {
            var writer = MessageWriter.Get(SendOption.Reliable);

            writer.StartMessage(targetClientId == -1 ? Tags.GameData : Tags.GameDataTo);
            {
                writer.Write(AmongUsClient.Instance.GameId);
                if (targetClientId != -1) writer.WritePacked(targetClientId);
                writer.StartMessage(1);
                {
                    writer.WritePacked(GameManager.Instance.NetId);
                    writer.StartMessage(LogicOptionsIndex);
                    {
                        writer.WriteBytesAndSize(optionArray);
                    }
                    writer.EndMessage();
                }
                writer.EndMessage();
            }
            writer.EndMessage();

            AmongUsClient.Instance.SendOrDisconnect(writer);
            writer.Recycle();
        }
        public abstract IGameOptions BuildGameOptions();

        public virtual bool AmValid() => true;
    }
}