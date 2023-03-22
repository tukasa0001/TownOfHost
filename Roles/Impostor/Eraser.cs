using Hazel;
using System.Collections.Generic;
using UnityEngine;
using static TOHE.Translator;

namespace TOHE.Roles.Impostor;

internal static class Eraser
{
    private static readonly int Id = 905553;
    public static List<byte> playerIdList = new();
    private static OptionItem EraseLimitOpt;
    private static List<byte> didVote = new();
    private static Dictionary<byte, int> EraseLimit = new();
    private static List<byte> PlayerToErase = new();

    public static void SetupCustomOption()
    {
        Options.SetupRoleOptions(Id, TabGroup.OtherRoles, CustomRoles.Eraser);
        EraseLimitOpt = IntegerOptionItem.Create(Id + 10, "EraseLimit", new(1, 15, 1), 2, TabGroup.OtherRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Eraser])
            .SetValueFormat(OptionFormat.Times);
    }
    public static void Init()
    {
        playerIdList = new();
        EraseLimit = new();
    }
    public static void Add(byte playerId)
    {
        playerIdList.Add(playerId);
        EraseLimit.TryAdd(playerId, EraseLimitOpt.GetInt());
        Logger.Info($"{Utils.GetPlayerById(playerId)?.GetNameWithRole()} : 剩余{EraseLimit[playerId]}次", "Eraser");
    }
    public static bool IsEnable => playerIdList.Count > 0;
    private static void SendRPC(byte playerId)
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetEraseLimit, SendOption.Reliable, -1);
        writer.Write(playerId);
        writer.Write(EraseLimit[playerId]);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public static void ReceiveRPC(MessageReader reader)
    {
        byte PlayerId = reader.ReadByte();
        int Limit = reader.ReadInt32();
        if (EraseLimit.ContainsKey(PlayerId))
            EraseLimit[PlayerId] = Limit;
        else
            EraseLimit.Add(PlayerId, 0);
    }
    public static string GetProgressText(byte playerId) => Utils.ColorString(EraseLimit[playerId] > 0 ? Utils.GetRoleColor(CustomRoles.Eraser) : Color.gray, EraseLimit.TryGetValue(playerId, out var x) ? $"({x})" : "Invalid");

    public static void OnVote(PlayerControl player, PlayerControl target)
    {
        if (player == null || target == null) return;
        if (didVote.Contains(player.PlayerId)) return;
        didVote.Add(player.PlayerId);

        if (EraseLimit[player.PlayerId] < 1) return;

        if (target.GetCustomRole().IsNeutral())
        {
            Utils.SendMessage(string.Format(GetString("EraserEraseNeutralNotice"), target.GetRealName()), player.PlayerId, Utils.ColorString(Utils.GetRoleColor(CustomRoles.Eraser), GetString("EraserEraseMsgTitle")));
            return;
        }

        EraseLimit[player.PlayerId]--;
        SendRPC(player.PlayerId);

        if (!PlayerToErase.Contains(target.PlayerId))
            PlayerToErase.Add(target.PlayerId);

        Utils.SendMessage(string.Format(GetString("EraserEraseNotice"), target.GetRealName()), player.PlayerId, Utils.ColorString(Utils.GetRoleColor(CustomRoles.Eraser), GetString("EraserEraseMsgTitle")));

        Utils.NotifyRoles(player);
    }
    public static void OnMeetingStart()
    {
        PlayerToErase = new();
        didVote = new();
    }
    public static void OnMeetingDestroy()
    {
        foreach (var pc in PlayerToErase)
        {
            var player = Utils.GetPlayerById(pc);
            if (player == null) continue;
            player.RpcSetCustomRole(CustomRolesHelper.GetVNRole(player.GetCustomRole()));
            Utils.NotifyRoles(player);
            Logger.Info($"{player.GetNameWithRole()} 被擦除了", "Eraser");
        }
        Utils.MarkEveryoneDirtySettings();
    }
}
