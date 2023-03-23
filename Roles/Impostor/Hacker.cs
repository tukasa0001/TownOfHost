using Hazel;
using System.Collections.Generic;
using UnityEngine;
using static TOHE.Translator;
using static TOHE.Options;

namespace TOHE.Roles.Impostor;

public static class Hacker
{
    private static readonly int Id = 901585;
    private static List<byte> playerIdList = new();

    private static OptionItem HackLimitOpt;
    private static OptionItem KillCooldown;

    private static Dictionary<byte, int> HackLimit = new();
    private static List<byte> DeadBodyList = new();

    public static void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.ImpostorRoles, CustomRoles.Hacker);
        KillCooldown = FloatOptionItem.Create(Id + 2, "KillCooldown", new(0f, 180f, 2.5f), 30f, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Hacker])
            .SetValueFormat(OptionFormat.Seconds);
        HackLimitOpt = IntegerOptionItem.Create(Id + 4, "HackLimit", new(1, 15, 1), 3, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Hacker])
            .SetValueFormat(OptionFormat.Times);
    }
    public static void Init()
    {
        playerIdList = new();
        HackLimit = new();
        DeadBodyList = new();
    }
    public static void Add(byte playerId)
    {
        playerIdList.Add(playerId);
        HackLimit.TryAdd(playerId, HackLimitOpt.GetInt());
    }
    public static bool IsEnable => playerIdList.Count > 0;
    private static void SendRPC(byte playerId)
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetHackerHackLimit, SendOption.Reliable, -1);
        writer.Write(playerId);
        writer.Write(HackLimit[playerId]);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public static void ReceiveRPC(MessageReader reader)
    {
        byte PlayerId = reader.ReadByte();
        int Limit = reader.ReadInt32();
        HackLimit.TryAdd(PlayerId, HackLimitOpt.GetInt());
        HackLimit[PlayerId] = Limit;
    }
    public static void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = KillCooldown.GetFloat();
    public static void ApplyGameOptions()
    {
        AURoleOptions.ShapeshifterCooldown = 1f;
        AURoleOptions.ShapeshifterDuration = 1f;
    }
    public static string GetHackLimit(byte playerId) => Utils.ColorString((HackLimit.TryGetValue(playerId, out var x) && x >= 1) ? Color.red : Color.gray, HackLimit.TryGetValue(playerId, out var hackLimit) ? $"({hackLimit})" : "Invalid");
    public static void GetAbilityButtonText(HudManager __instance, byte playerId)
    {
        if (HackLimit.TryGetValue(playerId, out var x) && x >= 1)
            __instance.AbilityButton.OverrideText($"{GetString("HackerShapeshiftText")}");
    }
    public static void OnMeetingStart() => DeadBodyList = new();
    public static void AddDeadBody(PlayerControl target)
    {
        if (target != null && !DeadBodyList.Contains(target.PlayerId))
            DeadBodyList.Add(target.PlayerId);
    }
    public static void OnShapeshift(PlayerControl pc, bool shapeshifting, PlayerControl ssTarget)
    {
        if (!shapeshifting || !HackLimit.TryGetValue(pc.PlayerId, out var x) || x < 1 || ssTarget == null || ssTarget.Is(CustomRoles.Needy)) return;
        HackLimit[pc.PlayerId]--;
        SendRPC(pc.PlayerId);

        var targetId = byte.MaxValue;

        // 寻找骇客击杀的尸体
        foreach (var db in DeadBodyList)
        {
            var dp = Utils.GetPlayerById(db);
            if (dp == null || dp.GetRealKiller() == null) continue;
            if (dp.GetRealKiller().PlayerId == pc.PlayerId) targetId = db;
        }

        // 未找到骇客击杀的尸体，寻找其他尸体
        if (targetId == byte.MaxValue && DeadBodyList.Count >= 1)
            targetId = DeadBodyList[IRandom.Instance.Next(0, DeadBodyList.Count)];

        if (targetId == byte.MaxValue)
            new LateTask(() => ssTarget?.NoCheckStartMeeting(ssTarget?.Data), 0.15f, "Hacker Hacking Report Self");
        else
            new LateTask(() => ssTarget?.NoCheckStartMeeting(Utils.GetPlayerById(targetId)?.Data), 0.15f, "Hacker Hacking Report");
    }
}