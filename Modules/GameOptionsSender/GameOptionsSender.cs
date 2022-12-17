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
        public readonly static List<GameOptionsSender> AllSenders = new(15);

        public static void SendAllGameOptions()
        {
            AllSenders.ForEach(sender => sender.SendGameOptions());
        }
        #endregion

        public IGameOptions BasedGameOptions { get; }


        public virtual void SendGameOptions()
        {
            var opt = BuildGameOptions();

            SendOptionsArray(AmongUsClient.Instance.gameOptionsFactory.ToBytes(opt));
        }
        protected virtual void SendOptionsArray(byte[] optionArray, int targetClientId = -1)
        {
            var writer = MessageWriter.Get(SendOption.Reliable);

            writer.StartMessage(targetClientId == -1 ? Tags.GameData : Tags.GameDataTo);
            {
                writer.Write(AmongUsClient.Instance.GameId);
                if (targetClientId != -1) writer.WritePacked(targetClientId);
                writer.StartMessage(1);
                {
                    writer.WritePacked(GameManager.Instance.NetId);
                    for (int i = 0; i < GameManager.Instance.LogicComponents.Count; i++)
                    {
                        // LogicOptionsのIndexを探し、そのIndexでメッセージを始める。
                        if (GameManager.Instance.LogicComponents[i] is LogicOptions lo)
                        {
                            writer.StartMessage((byte)i);
                            writer.WriteBytesAndSize(optionArray);
                            writer.EndMessage();
                        }
                    }
                    writer.EndMessage();
                }
                writer.EndMessage();
            }

            AmongUsClient.Instance.SendOrDisconnect(writer);
            writer.Recycle();
        }
        public abstract IGameOptions BuildGameOptions();
    }
}