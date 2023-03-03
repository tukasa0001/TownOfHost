using AmongUs.GameOptions;
using Hazel;
using System;
using System.Collections.Generic;
using System.Linq;
using static TOHE.Translator;

namespace TOHE;

class EAC
{
    public static List<string> Msgs = new();
    public static void WarnHost()
    {
        ErrorText.Instance.SBDetected = true;
        ErrorText.Instance.AddError(ErrorCode.SBDetected);
    }
    public static bool Receive(PlayerControl pc, byte callId, MessageReader reader)
    {
        if (pc == null || reader == null) return true;
        try
        {
            MessageReader sr = MessageReader.Get(reader);
            var rpc = (RpcCalls)callId;
            switch (rpc)
            {
                case RpcCalls.SetName:
                    string name = sr.ReadString();
                    if (sr.BytesRemaining > 0 && sr.ReadBoolean()) return false;
                    if (
                        ((name.Contains("<size") || name.Contains("size>")) && name.Contains("?") && !name.Contains("color")) ||
                        name.Length > 160 ||
                        name.Count(f => f.Equals("\"\\n\"")) > 3 ||
                        name.Count(f => f.Equals("\n")) > 3 ||
                        name.Count(f => f.Equals("\r")) > 3 ||
                        name.Contains("░") ||
                        name.Contains("▄") ||
                        name.Contains("█") ||
                        name.Contains("▌") ||
                        name.Contains("▒") ||
                        name.Contains("习近平")
                        )
                    {
                        WarnHost();
                        Report(pc, "非法设置游戏名称");
                        Logger.Fatal($"非法修改玩家【{pc.GetClientId()}:{pc.GetRealName()}】的游戏名称，已驳回", "EAC");
                        return true;
                    }
                    break;
                case RpcCalls.SetRole:
                    var role = (RoleTypes)sr.ReadUInt16();
                    if (GameStates.IsLobby && (role is RoleTypes.CrewmateGhost or RoleTypes.ImpostorGhost))
                    {
                        WarnHost();
                        Report(pc, "非法设置状态为幽灵");
                        Logger.Fatal($"非法设置玩家【{pc.GetClientId()}:{pc.GetRealName()}】的状态为幽灵，已驳回", "EAC");
                        return true;
                    }
                    break;
                case RpcCalls.SendChat:
                    var text = sr.ReadString();
                    if (Msgs.Contains(text)) return true;
                    Msgs.Add(text);
                    if (Msgs.Count > 3) Msgs.Remove(Msgs[0]);
                    if (
                        text.Contains("░") ||
                        text.Contains("▄") ||
                        text.Contains("█") ||
                        text.Contains("▌") ||
                        text.Contains("▒") ||
                        text.Contains("习近平")
                        )
                    {
                        Report(pc, "非法消息");
                        Logger.Fatal($"玩家【{pc.GetClientId()}:{pc.GetRealName()}】发送非法消息，已驳回", "EAC");
                        return true;
                    }
                    break;
                case RpcCalls.StartMeeting:
                case RpcCalls.ReportDeadBody:
                    var p = Utils.GetPlayerById(sr.ReadByte());
                    if (GameStates.IsMeeting || GameStates.IsLobby)
                    {
                        WarnHost();
                        Report(pc, "非法召集会议");
                        Logger.Fatal($"玩家【{pc.GetClientId()}:{pc.GetRealName()}】非法召集会议：【{p?.GetNameWithRole() ?? "null"}】，已驳回", "EAC");
                        return true;
                    }
                    break;
                case RpcCalls.SetColor:
                case RpcCalls.CheckColor:
                    var color = sr.ReadByte();
                    var time = 0;
                    foreach (var apc in PlayerControl.AllPlayerControls)
                        if (apc.Data.DefaultOutfit.ColorId == color) time++;
                    if (!GameStates.IsLobby || color == 18 || time >= 2)
                    {
                        WarnHost();
                        Report(pc, "非法设置颜色");
                        Logger.Fatal($"玩家【{pc.GetClientId()}:{pc.GetRealName()}】非法设置颜色，已驳回", "EAC");
                        return true;
                    }
                    break;
                case RpcCalls.MurderPlayer:
                    bool legal = false;
                    if (CustomRolesHelper.RoleExist(CustomRoles.Mafia)) legal = true;
                    if (CustomRolesHelper.RoleExist(CustomRoles.Counterfeiter)) legal = true;
                    if (!legal && (GameStates.IsMeeting || GameStates.IsLobby || !pc.IsAlive()))
                    {
                        WarnHost();
                        Report(pc, "非法击杀");
                        Logger.Fatal($"玩家【{pc.GetClientId()}:{pc.GetRealName()}】非法击杀，已驳回", "EAC");
                        return true;
                    }
                    break;
            }
            switch (callId)
            {
                case 101:
                    var AUMChat = sr.ReadString();
                    Report(pc, "AUM");
                    Logger.Fatal($"玩家【{pc.GetClientId()}:{pc.GetRealName()}】非法使用AUM发送消息", "EAC");
                    break;
                case 7:
                    if (GameStates.IsInGame)
                    {
                        WarnHost();
                        Report(pc, "非法设置颜色");
                        Logger.Fatal($"玩家【{pc.GetClientId()}:{pc.GetRealName()}】非法设置颜色，已驳回", "EAC");
                        return true;
                    }
                    break;
                case 11:
                    var p = Utils.GetPlayerById(sr.ReadByte());
                    if (GameStates.IsMeeting || GameStates.IsLobby)
                    {
                        WarnHost();
                        Report(pc, "非法召集会议");
                        Logger.Fatal($"玩家【{pc.GetClientId()}:{pc.GetRealName()}】非法召集会议：【{p?.GetNameWithRole() ?? "null"}】，已驳回", "EAC");
                        return true;
                    }
                    break;
                case 5:
                    string name = sr.ReadString();
                    if (GameStates.IsInGame)
                    {
                        Report(pc, "非法设置游戏名称");
                        Logger.Fatal($"非法修改玩家【{pc.GetClientId()}:{pc.GetRealName()}】的游戏名称，已驳回", "EAC");
                        return true;
                    }
                    break;
                case 47:
                    if (GameStates.IsMeeting || GameStates.IsLobby)
                    {
                        Report(pc, "非法击杀");
                        Logger.Fatal($"玩家【{pc.GetClientId()}:{pc.GetRealName()}】非法击杀，已驳回", "EAC");
                        return true;
                    }
                    break;
                case 41:
                    if (GameStates.IsInGame)
                    {
                        Report(pc, "非法设置宠物");
                        Logger.Fatal($"玩家【{pc.GetClientId()}:{pc.GetRealName()}】非法设置宠物，已驳回", "EAC");
                        return true;
                    }
                    break;
                case 40:
                    if (GameStates.IsInGame)
                    {
                        Report(pc, "非法设置皮肤");
                        Logger.Fatal($"玩家【{pc.GetClientId()}:{pc.GetRealName()}】非法设置皮肤，已驳回", "EAC");
                        return true;
                    }
                    break;
                case 42:
                    if (GameStates.IsInGame)
                    {
                        Report(pc, "非法设置面部装扮");
                        Logger.Fatal($"玩家【{pc.GetClientId()}:{pc.GetRealName()}】非法设置面部装扮，已驳回", "EAC");
                        return true;
                    }
                    break;
                case 39:
                    if (GameStates.IsInGame)
                    {
                        Report(pc, "非法设置帽子");
                        Logger.Fatal($"玩家【{pc.GetClientId()}:{pc.GetRealName()}】非法设置帽子，已驳回", "EAC");
                        return true;
                    }
                    break;
                case 43:
                    if (sr.BytesRemaining > 0 && sr.ReadBoolean()) return false;
                    if (GameStates.IsInGame)
                    {
                        Report(pc, "非法设置游戏名称");
                        Logger.Fatal($"玩家【{pc.GetClientId()}:{pc.GetRealName()}】非法设置名称，已驳回", "EAC");
                        return true;
                    }
                    break;
            }
        }
        catch (Exception e)
        {
            Logger.Exception(e, "EAC");
            throw e;
        }
        return false;
    }
    public static void Report(PlayerControl pc, string reason)
    {
        if (pc == null) return;
        string msg = $"{pc.GetClientId()}|{pc.FriendCode}|{pc.Data.PlayerName}|{reason}";
        Cloud.SendData(msg);
        Logger.Fatal($"EAC报告：{pc.GetRealName()}: {reason}", "EAC Cloud");
    }
    public static bool CheckAUM(byte callId, ref string text)
    {
        switch (callId)
        {
            case 85:
                text = GetString("Cheat.AUM");
                break;
        }
        if (text == "") return false;
        else return true;
    }
}
