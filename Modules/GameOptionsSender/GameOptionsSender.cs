using System;
using System.Linq;
using System.Collections.Generic;
using InnerNet;
using Hazel;
using AmongUs.GameOptions;

namespace TownOfHost.Modules
{
    public abstract class GameOptionsSender
    {
        public static GameOptionsSender CurrentSender
        {
            get => _currentSender;
            set
            {
                if (value != null)
                    _currentSender = value;
            }
        }
        private static GameOptionsSender _currentSender;

        public IGameOptions BasedGameOptions { get; }


        public virtual void SendGameOptions()
        {
            var opt = BuildGameOptions();
            var writer = MessageWriter.Get(SendOption.Reliable);

            writer.StartMessage(Tags.GameData);
            {
                writer.Write(AmongUsClient.Instance.GameId);
                writer.StartMessage(1);
                {
                    writer.WritePacked(GameManager.Instance.NetId);
                    for (int i = 0; i < GameManager.Instance.LogicComponents.Count; i++)
                    {
                        // LogicOptionsのIndexを探し、そのIndexでメッセージを始める。
                        if (GameManager.Instance.LogicComponents[i] is LogicOptions lo)
                        {
                            writer.StartMessage((byte)i);
                            writer.WriteBytesAndSize(lo.gameOptionsFactory.ToBytes(opt));
                            writer.EndMessage();
                        }
                    }
                    writer.EndMessage();
                }
                writer.EndMessage();
            }
        }
        public abstract IGameOptions BuildGameOptions();
    }
}