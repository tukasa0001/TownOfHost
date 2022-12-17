using System;
using System.Linq;
using System.Collections.Generic;
using Il2CppSystem.Linq;
using InnerNet;
using Hazel;
using AmongUs.GameOptions;

namespace TownOfHost.Modules
{
    public abstract class GameOptionsSender
    {
        #region Static
        public readonly static List<GameOptionsSender> AllSenders = new(15) { new NormalGameOptionsSender() };

        public static void SendAllGameOptions()
        {
            AllSenders.ForEach(sender => sender.SendGameOptions());
        }
        #endregion

        public abstract IGameOptions BasedGameOptions { get; }
        public byte[] SentBytesCache;


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
            if (!IsSameBytes(SentBytesCache, writer))
            {
                if (SentBytesCache == null || writer.Position != SentBytesCache.Length) SentBytesCache = new byte[writer.Position];
                for (int i = 0; i < SentBytesCache.Length; i++)
                    SentBytesCache[i] = writer.Buffer[i];

                SendOptionsArray(SentBytesCache);
            }
            writer.Recycle();
        }
        public virtual void SendOptionsArray(byte[] optionArray)
        {
            SendOptionsArray(
                optionArray,
                (byte)GameManager.Instance.LogicComponents.FindIndex((Func<GameLogicComponent, bool>)(c => c is LogicOptions)),
                -1
            );
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
                        writer.EndMessage();
                    }
                    writer.EndMessage();
                }
                writer.EndMessage();
            }

            AmongUsClient.Instance.SendOrDisconnect(writer);
            writer.Recycle();
        }
        public abstract IGameOptions BuildGameOptions();
        public bool IsSameBytes(byte[] array, MessageWriter writer)
        {
            if (array == null || writer == null || array.Length != writer.Position) return false;

            for (int i = 0; i < array.Length; i++)
            {
                if (array[i] != writer.Buffer[i]) return false;
            }
            return true;
        }
    }
}