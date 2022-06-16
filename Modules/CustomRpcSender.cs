using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HarmonyLib;
using InnerNet;
using Hazel;
using UnhollowerBaseLib;

namespace TownOfHost
{
    public class CustomRpcSender
    {
        public MessageWriter stream;
        public readonly string name;
        public readonly SendOption sendOption;
        public bool isUnsafe;
        public delegate void onSendDelegateType();
        public onSendDelegateType onSendDelegate;

        public State CurrentState
        {
            get { return currentState; }
            set
            {
                if (isUnsafe) currentState = value;
                else Logger.Warn("CurrentStateはisUnsafeがtrueの時のみ上書きできます", "CustomRpcSender");
            }
        }
        private State currentState = State.BeforeInit;

        private CustomRpcSender() { }
        public CustomRpcSender(string name, SendOption sendOption, bool isUnsafe)
        {
            stream = MessageWriter.Get(sendOption);

            this.name = name;
            this.sendOption = sendOption;
            this.isUnsafe = isUnsafe;
            onSendDelegate = () => Logger.Info($"{this.name}'s onSendDelegate =>", "CustomRpcSender");

            currentState = State.Ready;
            Logger.Info($"\"{name}\" is ready", "CusomRpcSender");
        }
        public static CustomRpcSender Create(string name = "No Name Sender", SendOption sendOption = SendOption.None, bool isUnsafe = false)
        {
            return new CustomRpcSender(name, sendOption, isUnsafe);
        }

        public CustomRpcSender StartMessage(int targetClientId = -1)
        {
            if (currentState != State.Ready)
            {
                string errorMsg = $"Messageを開始しようとしましたが、StateがReadyではありません (in: \"{name}\")";
                if (isUnsafe)
                {
                    Logger.Warn(errorMsg, "CustomRpcSender.Warn");
                }
                else
                {
                    throw new InvalidOperationException(errorMsg);
                }
            }

            if (targetClientId < 0)
            {
                // 全員に対するRPC
                stream.StartMessage(5);
                stream.Write(AmongUsClient.Instance.GameId);
            }
            else
            {
                // 特定のクライアントに対するRPC (Desync)
                stream.StartMessage(6);
                stream.Write(AmongUsClient.Instance.GameId);
                stream.WritePacked(targetClientId);
            }

            currentState = State.InRootMessage;
            return this;
        }
        public CustomRpcSender EndMessage(int targetClientId = -1)
        {
            if (currentState != State.InRootMessage)
            {
                string errorMsg = $"Messageを終了しようとしましたが、StateがInRootMessageではありません (in: \"{name}\")";
                if (isUnsafe)
                {
                    Logger.Warn(errorMsg, "CustomRpcSender.Warn");
                }
                else
                {
                    throw new InvalidOperationException(errorMsg);
                }
            }
            stream.EndMessage();

            currentState = State.Ready;
            return this;
        }
        public CustomRpcSender StartRpc(uint targetNetId, RpcCalls rpcCall)
            => StartRpc(targetNetId, (byte)rpcCall);
        public CustomRpcSender StartRpc(
          uint targetNetId,
          byte callId)
        {
            if (currentState != State.InRootMessage)
            {
                string errorMsg = $"RPCを開始しようとしましたが、StateがInRootMessageではありません (in: \"{name}\")";
                if (isUnsafe)
                {
                    Logger.Warn(errorMsg, "CustomRpcSender.Warn");
                }
                else
                {
                    throw new InvalidOperationException(errorMsg);
                }
            }

            stream.StartMessage(2);
            stream.WritePacked(targetNetId);
            stream.Write(callId);

            currentState = State.InRpc;
            return this;
        }
        public void EndRpc()
        {
            if (currentState != State.InRpc)
            {
                string errorMsg = $"RPCを終了しようとしましたが、StateがInRpcではありません (in: \"{name}\")";
                if (isUnsafe)
                {
                    Logger.Warn(errorMsg, "CustomRpcSender.Warn");
                }
                else
                {
                    throw new InvalidOperationException(errorMsg);
                }
            }

            stream.EndMessage();
            currentState = State.InRootMessage;
        }
        public void SendMessage()
        {
            if (currentState != State.Ready)
            {
                string errorMsg = $"RPCを送信しようとしましたが、StateがReadyではありません (in: \"{name}\")";
                if (isUnsafe)
                {
                    Logger.Warn(errorMsg, "CustomRpcSender.Warn");
                }
                else
                {
                    throw new InvalidOperationException(errorMsg);
                }
            }

            AmongUsClient.Instance.SendOrDisconnect(stream);
            onSendDelegate();
            currentState = State.Finished;
            Logger.Info($"\"{name}\" is finished", "CusomRpcSender");
            stream.Recycle();
        }

        // Write
        #region PublicWriteMethods
        public CustomRpcSender Write(float val) => Write(w => w.Write(val));
        public CustomRpcSender Write(string val) => Write(w => w.Write(val));
        public CustomRpcSender Write(ulong val) => Write(w => w.Write(val));
        public CustomRpcSender Write(int val) => Write(w => w.Write(val));
        public CustomRpcSender Write(uint val) => Write(w => w.Write(val));
        public CustomRpcSender Write(ushort val) => Write(w => w.Write(val));
        public CustomRpcSender Write(byte val) => Write(w => w.Write(val));
        public CustomRpcSender Write(sbyte val) => Write(w => w.Write(val));
        public CustomRpcSender Write(bool val) => Write(w => w.Write(val));
        public CustomRpcSender Write(Il2CppStructArray<byte> bytes) => Write(w => w.Write(bytes));
        public CustomRpcSender Write(Il2CppStructArray<byte> bytes, int offset, int length) => Write(w => w.Write(bytes, offset, length));
        public CustomRpcSender WriteBytesAndSize(Il2CppStructArray<byte> bytes) => Write(w => w.WriteBytesAndSize(bytes));
        public CustomRpcSender WritePacked(int val) => Write(w => w.WritePacked(val));
        public CustomRpcSender WritePacked(uint val) => Write(w => w.WritePacked(val));
        public CustomRpcSender WriteNetObject(InnerNetObject obj) => Write(w => w.WriteNetObject(obj));
        #endregion

        private CustomRpcSender Write(Action<MessageWriter> action)
        {
            if (currentState != State.InRpc)
            {
                string errorMsg = $"RPCを書き込もうとしましたが、StateがWrite(書き込み中)ではありません (in: \"{name}\")";
                if (isUnsafe)
                {
                    Logger.Warn(errorMsg, "CustomRpcSender.Warn");
                }
                else
                {
                    throw new InvalidOperationException(errorMsg);
                }
            }
            action(stream);

            return this;
        }

        public enum State
        {
            BeforeInit = 0, //初期化前 何もできない
            Ready, //送信準備完了 StartMessageとSendMessageを実行可能
            InRootMessage, //StartMessage～EndMessageの間の状態 StartRpcとEndMessageを実行可能
            InRpc, //StartRpc～EndRpcの間の状態 WriteとEndRpcを実行可能
            Finished, //送信後 何もできない
        }
    }

    public static class CustomRpcSenderExtentions
    {
        public static void RpcSetRole(this CustomRpcSender sender, PlayerControl player, RoleTypes role, int targetClientId = -1)
        {
            sender.StartRpc(player.NetId, (byte)RpcCalls.SetRole, targetClientId)
                  .Write((ushort)role)
                  .EndRpc();
        }
        public static void RpcMurderPlayer(this CustomRpcSender sender, PlayerControl player, PlayerControl target, int targetClientId = -1)
        {
            sender.StartRpc(player.NetId, (byte)RpcCalls.MurderPlayer, targetClientId)
                  .WriteNetObject(target)
                  .EndRpc();
        }
    }
}