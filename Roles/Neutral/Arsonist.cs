using System.Collections.Generic;
using UnityEngine;
using AmongUs.GameOptions;

using TownOfHost.Roles.Core;
using static TownOfHost.Translator;
using Hazel;

namespace TownOfHost.Roles.Neutral;
public sealed class Arsonist : RoleBase
{
    public static readonly SimpleRoleInfo RoleInfo =
        new(
            typeof(Arsonist),
            player => new Arsonist(player),
            CustomRoles.Arsonist,
            () => RoleTypes.Impostor,
            CustomRoleTypes.Neutral,
            50500,
            SetupOptionItem,
            "#ff6633"
        );
    public Arsonist(PlayerControl player)
    : base(
        RoleInfo,
        player,
        false
    )
    {
        DouseTime = OptionDouseTime.GetFloat();
        DouseCooldown = OptionDouseCooldown.GetFloat();

        TargetInfo = null;
        IsDoused = new(GameData.Instance.PlayerCount);
        CustomRoleManager.MarkOthers.Add(GetMarkOthers);
    }
    private static OptionItem OptionDouseTime;
    private static OptionItem OptionDouseCooldown;

    enum OptionName
    {
        ArsonistDouseTime,
        Cooldown
    }
    private static float DouseTime;
    private static float DouseCooldown;

    public class TimerInfo
    {
        public byte TargetId;
        public float Timer;
        public TimerInfo(byte targetId, float timer)
        {
            TargetId = targetId;
            Timer = timer;
        }
    }
    private TimerInfo TargetInfo;
    public Dictionary<byte, bool> IsDoused;

    private static void SetupOptionItem()
    {
        OptionDouseTime = FloatOptionItem.Create(RoleInfo, 10, OptionName.ArsonistDouseTime, new(1f, 10f, 1f), 3f, false)
            .SetValueFormat(OptionFormat.Seconds);
        OptionDouseCooldown = FloatOptionItem.Create(RoleInfo, 11, OptionName.Cooldown, new(5f, 100f, 1f), 10f, false)
            .SetValueFormat(OptionFormat.Seconds);
    }
    public override void Add()
    {
        foreach (var ar in Main.AllPlayerControls)
            IsDoused.Add(ar.PlayerId, false);
    }
    public override bool CanUseKillButton() => !IsDouseDone(Player);
    public override float SetKillCooldown() => DouseCooldown;
    public override string GetProgressText(bool comms = false)
    {
        var doused = GetDousedPlayerCount(Player.PlayerId);
        return Utils.ColorString(((Color)RoleInfo.RoleColor).ShadeColor(0.25f), $"({doused.Item1}/{doused.Item2})");
    }
    public override void ApplyGameOptions(IGameOptions opt)
    {
        opt.SetVision(false);
    }
    public override void ReceiveRPC(MessageReader reader, CustomRPC rpcType)
    {
        var arsonistId = reader.ReadByte();
        switch (rpcType)
        {
            case CustomRPC.SetDousedPlayer:
                byte DousedId = reader.ReadByte();
                bool doused = reader.ReadBoolean();
                IsDoused[DousedId] = doused;
                break;
            case CustomRPC.SetCurrentDousingTarget:
                byte dousingTargetId = reader.ReadByte();
                if (PlayerControl.LocalPlayer.PlayerId == arsonistId)
                    Main.currentDousingTarget = dousingTargetId;
                break;
        }
    }
    public override void OnCheckMurderAsKiller(MurderInfo info)
    {
        var (killer, target) = info.AttemptTuple;

        Logger.Info("Arsonist start douse", "OnCheckMurderAsKiller");
        killer.SetKillCooldown(DouseTime);
        if (!IsDoused[target.PlayerId] && TargetInfo == null)
        {
            TargetInfo = new(target.PlayerId, 0f);
            Utils.NotifyRoles(SpecifySeer: killer);
            SetCurrentDousingTarget(target.PlayerId);
        }
        info.DoKill = false;
    }
    public override bool OnReportDeadBody(PlayerControl reporter, GameData.PlayerInfo target)
    {
        TargetInfo = null;

        return true;
    }
    public override void OnFixedUpdate()
    {
        if (GameStates.IsInTask && TargetInfo != null)//アーソニストが誰かを塗っているとき
        {
            if (!Player.IsAlive())
            {
                TargetInfo = null;
                Utils.NotifyRoles(SpecifySeer: Player);
                SetCurrentDousingTarget();
            }
            else
            {
                var ar_target = Utils.GetPlayerById(TargetInfo.TargetId);//塗られる人
                var ar_time = TargetInfo.Timer;//塗った時間
                if (!ar_target.IsAlive())
                {
                    TargetInfo = null;
                }
                else if (ar_time >= DouseTime)//時間以上一緒にいて塗れた時
                {
                    Player.SetKillCooldown();
                    TargetInfo = null;//塗が完了したのでTupleから削除
                    IsDoused[ar_target.PlayerId] = true;//塗り完了
                    Player.RpcSetDousedPlayer(ar_target, true);
                    Utils.NotifyRoles();//名前変更
                    SetCurrentDousingTarget();
                }
                else
                {
                    float dis;
                    dis = Vector2.Distance(Player.transform.position, ar_target.transform.position);//距離を出す
                    if (dis <= 1.75f)//一定の距離にターゲットがいるならば時間をカウント
                    {
                        TargetInfo.Timer += Time.fixedDeltaTime;
                    }
                    else//それ以外は削除
                    {
                        TargetInfo = null;
                        Utils.NotifyRoles(SpecifySeer: Player);
                        SetCurrentDousingTarget();

                        Logger.Info($"Canceled: {Player.GetNameWithRole()}", "Arsonist");
                    }
                }

            }
        }
    }
    public override bool OnEnterVent(PlayerPhysics physics, int ventId)
    {
        if (GameStates.IsInGame && IsDouseDone(Player))
        {
            foreach (var pc in Main.AllAlivePlayerControls)
            {
                if (pc.PlayerId != Player.PlayerId)
                {
                    //生存者は焼殺
                    pc.SetRealKiller(Player);
                    pc.RpcMurderPlayer(pc);
                    Main.PlayerStates[pc.PlayerId].deathReason = PlayerState.DeathReason.Torched;
                    Main.PlayerStates[pc.PlayerId].SetDead();
                }
                else
                    RPC.PlaySoundRPC(pc.PlayerId, Sounds.KillSound);
            }
            CustomWinnerHolder.ShiftWinnerAndSetWinner(CustomWinner.Arsonist); //焼殺で勝利した人も勝利させる
            CustomWinnerHolder.WinnerIds.Add(Player.PlayerId);
            return true;
        }
        return false;
    }
    public override string GetKillButtonText() => GetString("ArsonistDouseButtonText");
    public void SetCurrentDousingTarget(byte targetId = byte.MaxValue)
    {
        using var sender = CreateSender(CustomRPC.SetCurrentDousingTarget);
        sender.Writer.Write(targetId);
    }

    public static string GetMarkOthers(PlayerControl seer, PlayerControl seen, bool isForMeeting = false)
    {
        if (seer.GetRoleClass() is not null and Arsonist arsonist)
        {
            if (IsDousedPlayer(seer, seen)) //seerがtargetに既にオイルを塗っている(完了)
                return Utils.ColorString(RoleInfo.RoleColor, "▲");
            else if (!isForMeeting && (arsonist.TargetInfo?.TargetId ?? byte.MaxValue) == seen.PlayerId) //オイルを塗っている対象がtarget
                return Utils.ColorString(RoleInfo.RoleColor, "△");
        }

        return "";
    }
    public static bool IsDousedPlayer(PlayerControl arsonist, PlayerControl target)
    {
        if (arsonist.GetRoleClass() is not Arsonist arsonistClass) return false;
        if (!arsonistClass.IsDoused.TryGetValue(target.PlayerId, out bool isDoused)) return false;

        return isDoused;
    }
    public static bool IsDouseDone(PlayerControl player)
    {
        if (!player.Is(CustomRoles.Arsonist)) return false;
        var count = GetDousedPlayerCount(player.PlayerId);
        return count.Item1 == count.Item2;
    }
    public static (int, int) GetDousedPlayerCount(byte playerId)
    {
        int doused = 0, all = 0;
        if (CustomRoleManager.GetByPlayerId(playerId) is Arsonist arsonist)
        {
            //多分この方がMain.isDousedでforeachするより他のアーソニストの分ループ数少なくて済む
            foreach (var pc in Main.AllAlivePlayerControls)
            {
                if (pc.PlayerId == playerId) continue; //アーソニストは除外

                all++;
                if (arsonist.IsDoused.TryGetValue(pc.PlayerId, out var isDoused) && isDoused)
                    //塗れている場合
                    doused++;
            }
        }

        return (doused, all);
    }
}