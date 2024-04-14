using System.Collections.Generic;
using UnityEngine;
using TownOfHostForE.Roles.Core;
using static TownOfHostForE.Options;
using Hazel;
using TownOfHostForE.Modules;
using TownOfHostForE.Attributes;

namespace TownOfHostForE.Roles.AddOns.Common;

public static class Chu2Byo
{
    private static readonly int Id = 85400;
    private static Color RoleColor = Utils.GetRoleColor(CustomRoles.Chu2Byo);
    public static string SubRoleMark = Utils.ColorString(RoleColor, "†");
    private static List<byte> playerIdList = new();
    public static Dictionary<string, bool> Ch2Power = new();

    public static void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.Addons, CustomRoles.Chu2Byo);
    }
    [GameModuleInitializer]
    public static void Init()
    {
        playerIdList = new();
        Ch2Power.Clear();
    }
    public static void Add(byte playerId)
    {
        if (!playerIdList.Contains(playerId))
            playerIdList.Add(playerId);
    }
    public static bool IsEnable => playerIdList.Count > 0;
    public static bool IsThisRole(byte playerId) => playerIdList.Contains(playerId);

    public static void Chu2OnReceiveChat(PlayerControl player, string text)
    {
        if (!GameStates.IsMeeting) return;
        string[] args = text.Split(' ');

        bool SubRoleCheckResult = GetPlayerSubRoles(player.PlayerId, CustomRoles.Chu2Byo);

        if (SubRoleCheckResult && !GameStates.IsLobby)
        {
            string words = Chu2SettingWords(player, args[0]);
            if (words != "")
            {
                if (words == "＼†EMPOWERMENT†／")
                {
                    Ch2Power[player.FriendCode] = true;
                    SendRPCChu2Attack(player.FriendCode);
                    Logger.Info("中二パワー解放！！" + player.GetRealName(), "Chu2");
                }
                Utils.SendMessage(words, player.PlayerId, "");
            }
        }
    }
    public static void SendRPCChu2Attack(string friendCode)
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SendChu2Attack, SendOption.Reliable, -1);
        writer.Write(friendCode);
        writer.Write(Ch2Power[friendCode]);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    private static string Chu2SettingWords(PlayerControl Player, string SendedWords)
    {
        switch (SendedWords)
        {
            case "暗黒の魔力よ我が身に宿りし時":
                return "Next:蒼ざめた月光が我を包む";
            case "蒼ざめた月光が我を包む":
                return "Last:堕天使から授かりし権能を解き放て";
            case "堕天使から授かりし権能を解き放て":
                return "＼†EMPOWERMENT†／";
            default:
                return "";
        }
    }

    public static (byte? votedForId, int? numVotes, bool doVote) Ch2OnVote(byte voterId, byte sourceVotedForId, PlayerControl pc)
    {
        byte? votedForId = null;
        int? numVotes = null;
        bool doVote = true;

        var baseVote = (votedForId, numVotes, doVote);

        if (voterId != pc.PlayerId || sourceVotedForId == pc.PlayerId || sourceVotedForId >= 253 || !pc.IsAlive())
        {
            return baseVote;
        }
        MeetingHudPatch.TryAddAfterMeetingDeathPlayers(CustomDeathReason.Suicide, pc.PlayerId);
        Utils.GetPlayerById(sourceVotedForId).SetRealKiller(pc);
        MeetingVoteManager.Instance.ClearAndExile(pc.PlayerId, sourceVotedForId);
        return (votedForId, numVotes, false);
    }

    public static bool Ch2PowerWatch(PlayerControl pc)
    {
        if (!Ch2Power.ContainsKey(pc.FriendCode)) return false;
        if (!GetPlayerSubRoles(pc.PlayerId, CustomRoles.Chu2Byo)) return false;

        if (Ch2Power[pc.FriendCode])
        {
            Ch2Power[pc.FriendCode] = false;
            return true;
        }
        return false;
    }
    static private bool GetPlayerSubRoles(byte playerId, CustomRoles NowCustomRole)
    {
        var playerState = PlayerState.GetByPlayerId(playerId);
        if (playerState.SubRoles == null) return false;
        foreach (var subRole in playerState.SubRoles)
        {
            if (NowCustomRole == subRole) return true;
        }
        return false;
    }

    public static bool CheckCh2Words(PlayerControl pc,string text)
    {
        //中二病のみ実施
        if(!GetPlayerSubRoles(pc.PlayerId, CustomRoles.Chu2Byo)) return false;

        switch (text)
        {
            case "暗黒の魔力よ我が身に宿りし時":
            case "蒼ざめた月光が我を包む":
            case "堕天使から授かりし権能を解き放て":
                return true;
            default:
                return false;
        }
    }

}