using System.Collections.Generic;
using UnityEngine;
using AmongUs.GameOptions;

using TownOfHost.Roles.Core;
using TownOfHost.Roles.Core.Interfaces;
using static TownOfHost.Translator;
using Hazel;

namespace TownOfHost.Roles.Neutral;
public sealed class Arsonist : RoleBase, IKiller
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(Arsonist),
            player => new Arsonist(player),
            CustomRoles.Arsonist,
            () => RoleTypes.Impostor,
            CustomRoleTypes.Neutral,
            50500,
            SetupOptionItem,
            "ar",
            "#ff6633",
            true,
            introSound: () => GetIntroSound(RoleTypes.Crewmate)
        );
    public Arsonist(PlayerControl player)
    : base(
        RoleInfo,
        player,
        () => HasTask.False
    )
    {
        DouseTime = OptionDouseTime.GetFloat();
        DouseCooldown = OptionDouseCooldown.GetFloat();

        TargetInfo = null;
        IsDoused = new(GameData.Instance.PlayerCount);
    }
    private static OptionItem OptionDouseTime;
    private static OptionItem OptionDouseCooldown;

    enum OptionName
    {
        ArsonistDouseTime
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
    public bool CanKill { get; private set; } = false;
    private TimerInfo TargetInfo;
    public Dictionary<byte, bool> IsDoused;

    private static void SetupOptionItem()
    {
        OptionDouseTime = FloatOptionItem.Create(RoleInfo, 10, OptionName.ArsonistDouseTime, new(1f, 10f, 1f), 3f, false)
            .SetValueFormat(OptionFormat.Seconds);
        OptionDouseCooldown = FloatOptionItem.Create(RoleInfo, 11, GeneralOption.Cooldown, new(5f, 100f, 1f), 10f, false)
            .SetValueFormat(OptionFormat.Seconds);
    }
    public override void Add()
    {
        foreach (var ar in Main.AllPlayerControls)
            IsDoused.Add(ar.PlayerId, false);
    }
    public bool CanUseKillButton() => !IsDouseDone(Player);
    public float CalculateKillCooldown() => DouseCooldown;
    public override bool OnInvokeSabotage(SystemTypes systemType) => false;
    public override string GetProgressText(bool comms = false)
    {
        var doused = GetDousedPlayerCount();
        return Utils.ColorString(RoleInfo.RoleColor.ShadeColor(0.25f), $"({doused.Item1}/{doused.Item2})");
    }
    public override void ApplyGameOptions(IGameOptions opt)
    {
        opt.SetVision(false);
    }
    public void SendRPC(CustomRPC rpcType, byte targetId = byte.MaxValue, bool isDoused = false)
    {
        using var sender = CreateSender(rpcType);
        sender.Writer.Write(targetId);

        if (rpcType == CustomRPC.SetDousedPlayer)
            sender.Writer.Write(isDoused);
    }
    public override void ReceiveRPC(MessageReader reader, CustomRPC rpcType)
    {
        var targetId = reader.ReadByte();
        switch (rpcType)
        {
            case CustomRPC.SetDousedPlayer:
                bool doused = reader.ReadBoolean();
                IsDoused[targetId] = doused;
                break;
            case CustomRPC.SetCurrentDousingTarget:
                TargetInfo = new(targetId, 0f);
                break;
        }
    }
    public void OnCheckMurderAsKiller(MurderInfo info)
    {
        var (killer, target) = info.AttemptTuple;

        Logger.Info("Arsonist start douse", "OnCheckMurderAsKiller");
        killer.SetKillCooldown(DouseTime);
        if (!IsDoused[target.PlayerId] && TargetInfo == null)
        {
            TargetInfo = new(target.PlayerId, 0f);
            Utils.NotifyRoles(SpecifySeer: killer);
            SendRPC(CustomRPC.SetCurrentDousingTarget, target.PlayerId);
        }
        info.DoKill = false;
    }
    public override void OnReportDeadBody(PlayerControl reporter, GameData.PlayerInfo target)
    {
        TargetInfo = null;
    }
    public override void OnFixedUpdate(PlayerControl player)
    {
        if (!AmongUsClient.Instance.AmHost) return;

        if (GameStates.IsInTask && TargetInfo != null)//アーソニストが誰かを塗っているとき
        {
            if (!Player.IsAlive())
            {
                TargetInfo = null;
                Utils.NotifyRoles(SpecifySeer: Player);
                SendRPC(CustomRPC.SetCurrentDousingTarget);
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
                    SendRPC(CustomRPC.SetDousedPlayer, ar_target.PlayerId, true);
                    Utils.NotifyRoles();//名前変更
                    SendRPC(CustomRPC.SetCurrentDousingTarget);
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
                        SendRPC(CustomRPC.SetCurrentDousingTarget);

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
                    var state = PlayerState.GetByPlayerId(pc.PlayerId);
                    state.DeathReason = CustomDeathReason.Torched;
                    state.SetDead();
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
    public bool OverrideKillButtonText(out string text)
    {
        text = GetString("ArsonistDouseButtonText");
        return true;
    }

    public override string GetMark(PlayerControl seer, PlayerControl seen, bool isForMeeting = false)
    {
        //seenが省略の場合seer
        seen ??= seer;

        if (IsDousedPlayer(seen.PlayerId)) //seerがtargetに既にオイルを塗っている(完了)
            return Utils.ColorString(RoleInfo.RoleColor, "▲");
        if (!isForMeeting && TargetInfo?.TargetId == seen.PlayerId) //オイルを塗っている対象がtarget
            return Utils.ColorString(RoleInfo.RoleColor, "△");

        return "";
    }
    public override string GetLowerText(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false, bool isForHud = false)
    {
        if (isForMeeting) return "";
        //seenが省略の場合seer
        seen ??= seer;
        //seeおよびseenが自分である場合以外は関係なし
        if (!Is(seer) || !Is(seen)) return "";

        return IsDouseDone(Player) ? Utils.ColorString(RoleInfo.RoleColor, GetString("EnterVentToWin")) : "";
    }
    public bool IsDousedPlayer(byte targetId) => IsDoused.TryGetValue(targetId, out bool isDoused) && isDoused;
    public static bool IsDouseDone(PlayerControl player)
    {
        if (player.GetRoleClass() is not Arsonist arsonist) return false;
        var count = arsonist.GetDousedPlayerCount();
        return count.Item1 == count.Item2;
    }
    public (int, int) GetDousedPlayerCount()
    {
        int doused = 0, all = 0;
        //多分この方がMain.isDousedでforeachするより他のアーソニストの分ループ数少なくて済む
        foreach (var pc in Main.AllAlivePlayerControls)
        {
            if (pc.PlayerId == Player.PlayerId) continue; //アーソニストは除外

            all++;
            if (IsDoused.TryGetValue(pc.PlayerId, out var isDoused) && isDoused)
                //塗れている場合
                doused++;
        }

        return (doused, all);
    }
}