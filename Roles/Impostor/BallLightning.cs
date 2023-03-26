using Hazel;
using System.Collections.Generic;
using System.Linq;
using TOHE.Roles.Neutral;
using UnityEngine;
using static TOHE.Options;

namespace TOHE.Roles.Impostor;

public static class BallLightning
{
    private static readonly int Id = 901533;
    public static List<byte> playerIdList = new();

    private static OptionItem KillCooldown;
    private static OptionItem ConvertTime;
    private static OptionItem KillerConvertGhost;

    private static List<byte> GhostPlayer;
    private static Dictionary<byte, PlayerControl> RealKiller;
    public static void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.OtherRoles, CustomRoles.BallLightning);
        KillCooldown = FloatOptionItem.Create(Id + 10, "BallLightningKillCooldown", new(0f, 180f, 2.5f), 30f, TabGroup.OtherRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.BallLightning])
            .SetValueFormat(OptionFormat.Seconds);
        ConvertTime = FloatOptionItem.Create(Id + 12, "BallLightningConvertTime", new(0f, 180f, 2.5f), 10f, TabGroup.OtherRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.BallLightning])
            .SetValueFormat(OptionFormat.Seconds);
        KillerConvertGhost = BooleanOptionItem.Create(Id + 14, "BallLightningKillerConvertGhost", true, TabGroup.OtherRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.BallLightning]);
    }
    public static void Init()
    {
        playerIdList = new();
        GhostPlayer = new();
        RealKiller = new();
    }
    public static void Add(byte playerId)
    {
        playerIdList.Add(playerId);
    }
    public static bool IsEnable => playerIdList.Count > 0;
    private static void SendRPC(byte playerId)
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetGhostPlayer, SendOption.Reliable, -1);
        writer.Write(playerId);
        writer.Write(IsGhost(playerId));
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public static void ReceiveRPC(MessageReader reader)
    {
        byte GhostId = reader.ReadByte();
        bool isGhost = reader.ReadBoolean();
        if (GhostId == byte.MaxValue)
        {
            GhostPlayer = new();
            return;
        }
        if (isGhost)
        {
            if (!GhostPlayer.Contains(GhostId))
                GhostPlayer.Add(GhostId);
        }
        else
        {
            if (GhostPlayer.Contains(GhostId))
                GhostPlayer.Remove(GhostId);
        }
    }
    public static void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = KillCooldown.GetFloat();
    public static bool IsGhost(PlayerControl player) => GhostPlayer.Contains(player.PlayerId);
    public static bool IsGhost(byte id) => GhostPlayer.Contains(id);
    public static bool CheckMurder(PlayerControl target) => IsGhost(target);
    public static bool CheckBallLightningMurder(PlayerControl killer, PlayerControl target)
    {
        if (killer == null || target == null || !killer.Is(CustomRoles.BallLightning)) return false;
        if (IsGhost(target)) return false;
        killer.SetKillCooldown();
        StartConvertCountDown(killer, target);
        return true;
    }
    private static void StartConvertCountDown(PlayerControl killer, PlayerControl target)
    {
        new LateTask(() =>
        {
            if (GameStates.IsInGame && GameStates.IsInTask && !GameStates.IsMeeting && target.IsAlive() && !Pelican.IsEaten(target.PlayerId))
            {
                GhostPlayer.Add(target.PlayerId);
                SendRPC(target.PlayerId);
                RealKiller.TryAdd(target.PlayerId, killer);
                if (!killer.inVent) killer.RpcGuardAndKill(killer);
                Utils.NotifyRoles();
                Logger.Info($"{target.GetNameWithRole()} 转化为量子幽灵", "BallLightning");
            }
        }, ConvertTime.GetFloat(), "BallLightning Convert Player To Ghost");
    }
    public static void MurderPlayer(PlayerControl killer, PlayerControl target)
    {
        if (killer == null || target == null || !target.Is(CustomRoles.BallLightning)) return;
        if (!KillerConvertGhost.GetBool() || IsGhost(killer)) return;
        RealKiller.TryAdd(killer.PlayerId, target);
        StartConvertCountDown(target, killer);
    }
    public static void OnFixedUpdate()
    {
        if (!IsEnable || !GameStates.IsInTask) return;
        List<byte> deList = new();
        foreach (var ghost in GhostPlayer)
        {
            var gs = Utils.GetPlayerById(ghost);
            if (gs == null || !gs.IsAlive() || gs.Data.Disconnected)
            {
                deList.Add(gs.PlayerId);
                continue;
            }
            foreach (var pc in Main.AllAlivePlayerControls.Where(x => x.PlayerId != gs.PlayerId && x.IsAlive() && !x.Is(CustomRoles.BallLightning) && !IsGhost(x) && !Pelican.IsEaten(x.PlayerId)))
            {
                var pos = gs.transform.position;
                var dis = Vector2.Distance(pos, pc.transform.position);
                if (dis > 0.3f) continue;

                deList.Add(gs.PlayerId);
                Main.PlayerStates[gs.PlayerId].IsDead = true;
                Main.PlayerStates[gs.PlayerId].deathReason = PlayerState.DeathReason.Quantization;
                gs.SetRealKiller(RealKiller[gs.PlayerId]);
                gs.RpcMurderPlayerV3(gs);

                Logger.Info($"{gs.GetNameWithRole()} 作为量子幽灵因碰撞而死", "BallLightning");
                break;
            }
        }
        if (deList.Count > 0)
        {
            GhostPlayer.RemoveAll(deList.Contains);
            foreach (var gs in deList) SendRPC(gs);
            Utils.NotifyRoles();
        }
    }
    public static void OnMeetingStart()
    {
        foreach (var ghost in GhostPlayer)
        {
            var gs = Utils.GetPlayerById(ghost);
            if (gs == null) continue;
            CheckForEndVotingPatch.TryAddAfterMeetingDeathPlayers(PlayerState.DeathReason.Quantization, gs.PlayerId);
            gs.SetRealKiller(RealKiller[gs.PlayerId]);
            Logger.Info($"{gs.GetNameWithRole()} 作为量子幽灵参与会议，将在会议后死亡", "BallLightning");
        }
        GhostPlayer = new();
        SendRPC(byte.MaxValue);
        Utils.NotifyRoles();
    }
}