using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HarmonyLib;
using Hazel;

public class CustomRpcSender
{
    public MessageWriter writer;
    public SendOption sendOption;
    public bool isUnsafe;

    private ActionTypes latestActionType;

    private CustomRpcSender() { }
    public CustomRpcSender(SendOption sendOption, bool isUnsafe)
    {
        writer = MessageWriter.Get(sendOption);

        this.sendOption = sendOption;
        this.isUnsafe = isUnsafe;
    }
    public static CustomRpcSender Create(SendOption sendOption = SendOption.None, bool isUnsafe = false)
    {
        return new CustomRpcSender(sendOption, isUnsafe);
    }

    public MessageWriter StartRpc(
      uint targetNetId,
      byte callId,
      int targetClientId = -1)
    {
        if (targetClientId < 0)
        {
            // 全員に対するRPC
            writer.StartMessage(5);
            writer.Write(AmongUsClient.Instance.GameId);
        }
        else
        {
            // 特定のクライアントに対するRPC (Desync)
            writer.StartMessage(6);
            writer.Write(AmongUsClient.Instance.GameId);
            writer.WritePacked(targetClientId);
        }
        writer.StartMessage(2);
        writer.WritePacked(targetNetId);
        writer.Write(callId);

        return writer;
    }
    public void EndRpc()
    {
        writer.EndMessage();
        writer.EndMessage();
    }
    public void SendMessage()
    {
        AmongUsClient.Instance.SendOrDisconnect(writer);
    }

    public enum ActionTypes
    {
        Free = 0,
    }
}