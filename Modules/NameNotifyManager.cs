using Hazel;
using System;
using System.Collections.Generic;

namespace TOHE;

public static class NameNotifyManager
{
    private static Dictionary<byte, (string, long)> Notice = new();
    public static void Reset() => Notice = new();
    public static bool Notifying(this PlayerControl pc) => Notice.ContainsKey(pc.PlayerId);
    public static void Notify(this PlayerControl pc, string text, float time = 4f)
    {
        if (!GameStates.IsInTask) return;
        if (!text.Contains("<color=#")) text = Utils.ColorString(Utils.GetRoleColor(pc.GetCustomRole()), text);
        Notice.Remove(pc.PlayerId);
        Notice.Add(pc.PlayerId, new(text, Utils.GetTimeStamp(DateTime.Now) + (long)time));
        SendRPC(pc.PlayerId);
        Utils.NotifyRoles(pc);
        Logger.Info($"New name notify for {pc.GetNameWithRole().RemoveHtmlTags()}: {text} ({time}s)", "Name Notify");
    }
    public static void OnFixedUpdate(PlayerControl player)
    {
        if (!GameStates.IsInTask)
        {
            Notice = new();
            return;
        }
        if (Notice.ContainsKey(player.PlayerId) && Notice[player.PlayerId].Item2 < Utils.GetTimeStamp(DateTime.Now))
        {
            Notice.Remove(player.PlayerId);
            Utils.NotifyRoles(player);
        }
    }
    public static bool GetNameNotify(PlayerControl player, ref string name)
    {
        if (!Notice.ContainsKey(player.PlayerId)) return false;
        name = Notice[player.PlayerId].Item1;
        return true;
    }
    private static void SendRPC(byte playerId)
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SyncNameNotify, SendOption.Reliable, -1);
        writer.Write(playerId);
        if (Notice.ContainsKey(playerId))
        {
            writer.Write(true);
            writer.Write(Notice[playerId].Item1);
            writer.Write(Notice[playerId].Item2 - Utils.GetTimeStamp(DateTime.Now));
        }
        else writer.Write(false);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public static void ReceiveRPC(MessageReader reader)
    {
        byte PlayerId = reader.ReadByte();
        Notice.Remove(PlayerId);
        if (reader.ReadBoolean())
            Notice.Add(PlayerId, new(reader.ReadString(), Utils.GetTimeStamp(DateTime.Now) + (long)reader.ReadSingle()));
        Logger.Info($"New name notify for {Main.AllPlayerNames[PlayerId]}: {Notice[PlayerId].Item1} ({Notice[PlayerId].Item2 - Utils.GetTimeStamp(DateTime.Now)}s)", "Name Notify");
    }
}