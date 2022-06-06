using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HarmonyLib;
using Hazel;
using UnhollowerBaseLib;

public class CustomRpcSender
{
    public MessageWriter writer;
    public SendOption sendOption;
    public bool isUnsafe;

    private State currentState = State.BeforeInit;

    private CustomRpcSender() { }
    public CustomRpcSender(SendOption sendOption, bool isUnsafe)
    {
        writer = MessageWriter.Get(sendOption);

        this.sendOption = sendOption;
        this.isUnsafe = isUnsafe;

        currentState = State.Ready;
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

        currentState = State.Writing;
        return writer;
    }
    public void EndRpc()
    {
        writer.EndMessage();
        writer.EndMessage();
        currentState = State.Ready;
    }
    public void SendMessage()
    {
        AmongUsClient.Instance.SendOrDisconnect(writer);
        currentState = State.Sent;
    }

    // Write
    public void Write(MessageWriter msg, bool includeHeader) => writer.Write(msg, includeHeader);
    public void Write(float val) => writer.Write(val);
    public void Write(string val) => writer.Write(val);
    public void Write(ulong val) => writer.Write(val);
    public void Write(int val) => writer.Write(val);
    public void Write(uint val) => writer.Write(val);
    public void Write(ushort val) => writer.Write(val);
    public void Write(byte val) => writer.Write(val);
    public void Write(sbyte val) => writer.Write(val);
    public void Write(bool val) => writer.Write(val);
    public void Write(Il2CppStructArray<byte> bytes) => writer.Write(bytes);
    public void Write(Il2CppStructArray<byte> bytes, int offset, int length) => writer.Write(bytes, offset, length);
    public void WriteBytesAndSize(Il2CppStructArray<byte> bytes) => writer.WriteBytesAndSize(bytes);
    public void WritePacked(int val) => writer.WritePacked(val);
    public void WritePacked(uint val) => writer.WritePacked(val);

    public enum State
    {
        BeforeInit = 0,
        Ready,
        Writing,
        Sent,
        Closed
    }
}