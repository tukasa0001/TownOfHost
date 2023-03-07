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

    private static List<byte> GhostPlayer;
    private static Dictionary<byte, PlayerControl> RealKiller;
    public static void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.ImpostorRoles, CustomRoles.BallLightning);
        KillCooldown = FloatOptionItem.Create(Id + 10, "KillCooldown", new(2.5f, 180f, 2.5f), 20f, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.BallLightning])
            .SetValueFormat(OptionFormat.Seconds);
        ConvertTime = FloatOptionItem.Create(Id + 12, "BallLightningConvertTime", new(2.5f, 180f, 2.5f), 10f, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.BallLightning])
            .SetValueFormat(OptionFormat.Seconds);
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
    public static void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = KillCooldown.GetFloat();
    public static bool IsGhost(PlayerControl player) => GhostPlayer.Contains(player.PlayerId);
    public static bool CheckMurder(PlayerControl target) => IsGhost(target);
    public static bool CheckBallLightningMurder(PlayerControl killer, PlayerControl target)
    {
        if (killer == null || target == null || !killer.Is(CustomRoles.BallLightning)) return false;
        if (IsGhost(target)) return false;

        killer.SetKillCooldown();
        killer.RpcGuardAndKill(killer);

        new LateTask(() =>
        {
            if (GameStates.IsInTask)
            {
                GhostPlayer.Add(target.PlayerId);
                RealKiller.TryAdd(target.PlayerId, killer);
                killer.RpcGuardAndKill(killer);
                Logger.Info($"{target.GetNameWithRole()} 转化为量子幽灵", "BallLightning");
            }
        }, ConvertTime.GetFloat(), "BallLightning Convert Player To Ghost");

        return true;
    }
    public static void FixedUpdate()
    {
        if (!IsEnable || GhostPlayer.Count < 1 || !GameStates.IsInTask) return;
        List<byte> deList = new();
        foreach (var ghost in GhostPlayer)
        {
            var gs = Utils.GetPlayerById(ghost);
            if (gs == null) continue;
            foreach (var pc in Main.AllAlivePlayerControls.Where(x => x.PlayerId != gs.PlayerId && x.IsAlive() && !x.Is(CustomRoles.BallLightning) && !IsGhost(x) && !Pelican.IsEaten(x.PlayerId)))
            {
                var pos = gs.transform.position;
                var dis = Vector2.Distance(pos, pc.transform.position);
                if (dis > 0.3f) continue;

                deList.Add(gs.PlayerId);
                Main.PlayerStates[gs.PlayerId].IsDead = true;
                Main.PlayerStates[gs.PlayerId].deathReason = PlayerState.DeathReason.Bombed;
                gs.SetRealKiller(RealKiller[gs.PlayerId]);
                gs.RpcMurderPlayer(gs);

                Logger.Info($"{gs.GetNameWithRole()} 作为量子幽灵因碰撞而死", "BallLightning");
                break;
            }
        }
        GhostPlayer.RemoveAll(deList.Contains);
    }
    public static void OnMeetingStart()
    {
        foreach (var ghost in GhostPlayer)
        {
            var gs = Utils.GetPlayerById(ghost);
            if (gs == null) continue;
            CheckForEndVotingPatch.TryAddAfterMeetingDeathPlayers(PlayerState.DeathReason.Bombed, gs.PlayerId);
            gs.SetRealKiller(RealKiller[gs.PlayerId]);
            Logger.Info($"{gs.GetNameWithRole()} 作为量子幽灵参与会议，将在会议后死亡", "BallLightning");
        }
        GhostPlayer = new();
    }
}