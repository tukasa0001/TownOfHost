using Hazel;
using System.Collections.Generic;
using UnityEngine;
namespace TOHE.Roles.Neutral;

public static class Pelican
{
    private static readonly int Id = 5053175;
    private static List<byte> playerIdList = new();
    private static readonly Dictionary<byte, int> EatenNum = new();
    private static Dictionary<byte, List<byte>> eatenList = new();
    private static readonly Dictionary<byte, Vector2> originalPosition = new();
    private static readonly Dictionary<byte, float> originalSpeed = new();
    public static OptionItem KillCooldown;
    public static OptionItem CanVent;
    public static void SetupCustomOption()
    {
        Options.SetupRoleOptions(Id, TabGroup.OtherRoles, CustomRoles.Pelican);
        KillCooldown = FloatOptionItem.Create(Id + 10, "PelicanKillCooldown", new(2.5f, 180f, 2.5f), 30f, TabGroup.OtherRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Pelican])
            .SetValueFormat(OptionFormat.Seconds);
        CanVent = BooleanOptionItem.Create(Id + 11, "CanVent", true, TabGroup.OtherRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Pelican]);
    }
    public static void Init()
    {
        playerIdList = new();
        eatenList = new();
    }
    public static void Add(byte playerId)
    {
        playerIdList.Add(playerId);
        EatenNum.TryAdd(playerId, 0);
    }
    public static bool IsEnable => playerIdList.Count > 0;
    private static void SyncEatenNum(byte playerId)
    {
        var eatenNum = 0;
        if (eatenList.ContainsKey(playerId))
            eatenNum = eatenList[playerId].Count;
        if (EatenNum.ContainsKey(playerId))
            EatenNum[playerId] = eatenNum;
        else
            EatenNum.TryAdd(playerId, eatenNum);
        SendRPC(playerId);
    }
    private static void SendRPC(byte playerId)
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetPelicanEtenNum, SendOption.Reliable, -1);
        writer.Write(playerId);
        writer.Write(EatenNum[playerId]);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public static void ReceiveRPC(MessageReader reader)
    {
        byte PlayerId = reader.ReadByte();
        int Num = reader.ReadInt32();
        if (EatenNum.ContainsKey(PlayerId))
            EatenNum[PlayerId] = Num;
        else
            EatenNum.Add(PlayerId, 0);
    }
    public static bool IsEaten(PlayerControl pc, byte id) => eatenList.ContainsKey(pc.PlayerId) && eatenList[pc.PlayerId].Contains(id);
    public static bool IsEaten(byte id)
    {
        if (AmongUsClient.Instance.AmHost)
        {
            foreach (var el in eatenList)
                if (el.Value.Contains(id))
                    return true;
        }
        else
        {
            var target = Utils.GetPlayerById(id);
            if (target == null) return false;
            var pos = GetBlackRoomPS();
            var dis = Vector2.Distance(pos, target.GetTruePosition());
            if (dis < 1.5f) return true;
        }
        return false;
    }
    public static bool CanEat(PlayerControl pc, byte id)
    {
        if (!pc.Is(CustomRoles.Pelican) || GameStates.IsMeeting) return false;
        var target = Utils.GetPlayerById(id);
        return target != null && target.IsAlive() && !target.inVent && !target.Is(CustomRoles.GM) && !IsEaten(pc, id) && !IsEaten(id);
    }
    public static Vector2 GetBlackRoomPS()
    {
        return Main.NormalOptions.MapId switch
        {
            0 => new(-27f, 3.3f), // The Skeld
            1 => new(-11.4f, 8.2f), // MIRA HQ
            2 => new(42.6f, -19.9f), // Polus
            4 => new(-16.8f, -6.2f), // Airship
            _ => throw new System.NotImplementedException(),
        };
    }
    public static string GetProgressText(byte playerId)
    {
        var player = Utils.GetPlayerById(playerId);
        if (player == null) return null;
        var eatenNum = 0;
        if (eatenList.ContainsKey(playerId))
            eatenNum = eatenList[playerId].Count;
        if (!AmongUsClient.Instance.AmHost && EatenNum.ContainsKey(playerId))
            eatenNum = EatenNum[playerId];
        return Utils.ColorString(Utils.GetRoleColor(CustomRoles.Pelican), $"({eatenNum})");
    }
    public static void EatPlayer(PlayerControl pc, PlayerControl target)
    {
        if (pc == null || target == null || !CanEat(pc, target.PlayerId)) return;
        if (!eatenList.ContainsKey(pc.PlayerId)) eatenList.Add(pc.PlayerId, new());
        eatenList[pc.PlayerId].Add(target.PlayerId);

        originalPosition.Remove(target.PlayerId);
        originalPosition.Add(target.PlayerId, target.GetTruePosition());

        originalSpeed.Remove(target.PlayerId);
        originalSpeed.Add(target.PlayerId, Main.AllPlayerSpeed[target.PlayerId]);

        Utils.TP(target.NetTransform, GetBlackRoomPS());
        Main.AllPlayerSpeed[target.PlayerId] = 0.5f;
        ReportDeadBodyPatch.CanReport[target.PlayerId] = false;
        target.MarkDirtySettings();

        Utils.NotifyRoles(SpecifySeer: pc);
        Utils.NotifyRoles(SpecifySeer: target);
        SyncEatenNum(pc.PlayerId);
        Logger.Info($"{pc.GetRealName()} 吞掉了 {target.GetRealName()}", "Pelican");
    }

    public static void OnReport()
    {
        foreach (var pc in eatenList)
        {
            foreach (var tar in pc.Value)
            {
                var target = Utils.GetPlayerById(tar);
                var killer = Utils.GetPlayerById(pc.Key);
                if (killer == null || target == null) continue;
                Main.AllPlayerSpeed[tar] = Main.AllPlayerSpeed[tar] - 0.5f + originalSpeed[tar];
                ReportDeadBodyPatch.CanReport[tar] = true;
                target.SetRealKiller(killer);
                target.Data.IsDead = true;
                Main.PlayerStates[tar].deathReason = PlayerState.DeathReason.Eaten;
                target.RpcExileV2();
                Main.PlayerStates[tar].SetDead();
                Logger.Info($"{killer.GetRealName()} 消化了 {target.GetRealName()}", "Pelican");
            }
        }
        eatenList.Clear();
        foreach (var pc in playerIdList)
            SyncEatenNum(pc);
    }

    public static void OnPelicanDied(byte pc)
    {
        if (!eatenList.ContainsKey(pc)) return;
        foreach (var tar in eatenList[pc])
        {
            var target = Utils.GetPlayerById(tar);
            var palyer = Utils.GetPlayerById(pc);
            if (palyer == null || target == null) continue;
            Utils.TP(target.NetTransform, palyer.GetTruePosition());
            Main.AllPlayerSpeed[tar] = Main.AllPlayerSpeed[tar] - 0.5f + originalSpeed[tar];
            ReportDeadBodyPatch.CanReport[tar] = true;
            target.MarkDirtySettings();
            RPC.PlaySoundRPC(tar, Sounds.TaskComplete);
            Utils.NotifyRoles(SpecifySeer: target);
            Logger.Info($"{Utils.GetPlayerById(pc).GetRealName()} 吐出了 {target.GetRealName()}", "Pelican");
        }
        eatenList.Remove(pc);
        SyncEatenNum(pc);
    }

    private static int Count = 0;
    public static void FixedUpdate()
    {
        if (!GameStates.IsInTask)
        {
            eatenList.Clear();
            return;
        }

        if (!IsEnable) return; Count--; if (Count > 0) return; Count = 30;

        foreach (var pc in eatenList)
        {
            foreach (var tar in pc.Value)
            {
                var target = Utils.GetPlayerById(tar);
                if (target == null) continue;
                var pos = GetBlackRoomPS();
                var dis = Vector2.Distance(pos, target.GetTruePosition());
                if (dis < 1f) continue;
                Utils.TP(target.NetTransform, pos);
                Utils.NotifyRoles(SpecifySeer: target);
            }
        }
    }
}