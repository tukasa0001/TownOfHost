using System;
using System.Linq;
using System.Collections.Generic;
using Il2CppSystem.Linq;
using InnerNet;
using Hazel;
using AmongUs.GameOptions;
using System.Text;

namespace TownOfHost.Modules
{
    public abstract class GameOptionsSender
    {
        #region Static
        public readonly static List<GameOptionsSender> AllSenders = new(15) { new NormalGameOptionsSender() };

        public static void SendAllGameOptions()
        {
            foreach (var sender in AllSenders)
            {
                if (sender.IsDirty) sender.SendGameOptions();
                sender.IsDirty = false;
            }
        }
        #endregion

        public abstract IGameOptions BasedGameOptions { get; }
        public abstract bool IsDirty { get; protected set; }
        public byte[] SentBytesCache = new byte[0];


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

            // キャッシュと比較&送信
            Span<byte> cacheSpan = new(SentBytesCache);
            Span<byte> writerSpan = new(writer.Buffer, 1, writer.Length - 1);
            if (!IsSameBytes(cacheSpan, writerSpan))
            {
                if (SentBytesCache == null || SentBytesCache.Length != writerSpan.Length) SentBytesCache = new byte[writerSpan.Length];
                for (int i = 0; i < SentBytesCache.Length; i++)
                    SentBytesCache[i] = writerSpan[i];

                SendOptionsArray(SentBytesCache);
            }
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
        public bool IsSameBytes(Span<byte> arr1, Span<byte> arr2)
        {
            if (arr1 == null || arr2 == null || arr1.Length != arr2.Length) return false;

            for (int i = 0; i < arr1.Length; i++)
            {
                if (arr1[i] != arr2[i]) return false;
            }
            return true;
        }
    }
}