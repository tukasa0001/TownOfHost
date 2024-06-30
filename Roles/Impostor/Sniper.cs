using System.Collections.Generic;
using System.Linq;
using Hazel;
using UnityEngine;
using AmongUs.GameOptions;

using TownOfHost.Roles.Core;
using TownOfHost.Roles.Core.Interfaces;
using static TownOfHost.Translator;

namespace TownOfHost.Roles.Impostor;
public sealed class Sniper : RoleBase, IImpostor
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(Sniper),
            player => new Sniper(player),
            CustomRoles.Sniper,
            () => RoleTypes.Shapeshifter,
            CustomRoleTypes.Impostor,
            1800,
            SetupOptionItem,
            "snp"
        );
    public Sniper(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        MaxBulletCount = SniperBulletCount.GetInt();
        PrecisionShooting = SniperPrecisionShooting.GetBool();
        AimAssist = SniperAimAssist.GetBool();
        AimAssistOneshot = SniperAimAssistOnshot.GetBool();

        CustomRoleManager.MarkOthers.Add(GetMarkOthers);
    }

    public override void OnDestroy()
    {
        Snipers.Clear();
    }
    static OptionItem SniperBulletCount;
    static OptionItem SniperPrecisionShooting;
    static OptionItem SniperAimAssist;
    static OptionItem SniperAimAssistOnshot;
    enum OptionName
    {
        SniperBulletCount,
        SniperPrecisionShooting,
        SniperAimAssist,
        SniperAimAssistOneshot
    }
    Vector3 SnipeBasePosition;
    Vector3 LastPosition;
    int BulletCount;
    List<byte> ShotNotify = new();
    bool IsAim;
    float AimTime;

    static HashSet<Sniper> Snipers = new();

    int MaxBulletCount;
    bool PrecisionShooting;
    bool AimAssist;
    bool AimAssistOneshot;

    bool MeetingReset;
    public static void SetupOptionItem()
    {
        SniperBulletCount = IntegerOptionItem.Create(RoleInfo, 10, OptionName.SniperBulletCount, new(1, 5, 1), 2, false)
            .SetValueFormat(OptionFormat.Pieces);
        SniperPrecisionShooting = BooleanOptionItem.Create(RoleInfo, 11, OptionName.SniperPrecisionShooting, false, false);
        SniperAimAssist = BooleanOptionItem.Create(RoleInfo, 12, OptionName.SniperAimAssist, false, false);
        SniperAimAssistOnshot = BooleanOptionItem.Create(RoleInfo, 13, OptionName.SniperAimAssistOneshot, false, false, SniperAimAssist);
    }
    public override void Add()
    {
        Logger.Disable("Sniper");

        SnipeBasePosition = new();
        LastPosition = new();
        BulletCount = MaxBulletCount;
        ShotNotify.Clear();
        IsAim = false;
        AimTime = 0f;
        MeetingReset = false;

        Snipers.Add(this);
    }
    private void SendRPC()
    {
        Logger.Info($"{Player.GetNameWithRole()}:SendRPC", "Sniper");
        using var sender = CreateSender();

        var snList = ShotNotify;
        sender.Writer.Write(snList.Count);
        foreach (var sn in snList)
        {
            sender.Writer.Write(sn);
        }
    }

    public override void ReceiveRPC(MessageReader reader)
    {
        ShotNotify.Clear();
        var count = reader.ReadInt32();
        while (count > 0)
        {
            ShotNotify.Add(reader.ReadByte());
            count--;
        }
        Logger.Info($"{Player.GetNameWithRole()}:ReceiveRPC", "Sniper");
    }
    public bool CanUseKillButton()
    {
        return Player.IsAlive() && BulletCount <= 0;
    }
    /// <summary>
    /// 狙撃の場合死因設定
    /// </summary>
    /// <param name="info"></param>
    public void OnMurderPlayerAsKiller(MurderInfo info)
    {
        //AttemptKillerは自分確定
        //スナイパーがAppearanceKillerだった場合は狙撃じゃない
        //ターゲットが自殺扱いなら狙撃
        if (!Is(info.AppearanceKiller) && info.IsFakeSuicide)
        {
            PlayerState.GetByPlayerId(info.AttemptTarget.PlayerId).DeathReason = CustomDeathReason.Sniped;
        }
    }

    Dictionary<PlayerControl, float> GetSnipeTargets()
    {
        var targets = new Dictionary<PlayerControl, float>();
        //変身開始地点→解除地点のベクトル
        var snipeBasePos = SnipeBasePosition;
        var snipePos = Player.transform.position;
        var dir = (snipePos - snipeBasePos).normalized;

        //至近距離で外す対策に一歩後ろから判定を開始する
        snipePos -= dir;

        foreach (var target in Main.AllAlivePlayerControls)
        {
            //自分には当たらない
            if (target.PlayerId == Player.PlayerId) continue;
            //死んでいない対象の方角ベクトル作成
            var target_pos = target.transform.position - snipePos;
            //自分より後ろの場合はあたらない
            if (target_pos.magnitude < 1) continue;
            //正規化して
            var target_dir = target_pos.normalized;
            //内積を取る
            var target_dot = Vector3.Dot(dir, target_dir);
            Logger.Info($"{target?.Data?.PlayerName}:pos={target_pos} dir={target_dir}", "Sniper");
            Logger.Info($"  Dot={target_dot}", "Sniper");

            //ある程度正確なら登録
            if (target_dot < 0.995) continue;

            if (PrecisionShooting)
            {
                //射線との誤差確認
                //単位ベクトルとの外積をとれば大きさ=誤差になる。
                var err = Vector3.Cross(dir, target_pos).magnitude;
                Logger.Info($"  err={err}", "Sniper");
                if (err < 0.5)
                {
                    //ある程度正確なら登録
                    targets.Add(target, err);
                }
            }
            else
            {
                //近い順に判定する
                var err = target_pos.magnitude;
                Logger.Info($"  err={err}", "Sniper");
                targets.Add(target, err);
            }
        }
        return targets;

    }
    public override void OnShapeshift(PlayerControl target)
    {
        var shapeshifting = Player.PlayerId != target.PlayerId;

        if (BulletCount <= 0) return;

        //弾が残ってたら
        if (shapeshifting)
        {
            //Aim開始
            MeetingReset = false;

            //スナイプ地点の登録
            SnipeBasePosition = Player.transform.position;

            LastPosition = Player.transform.position;
            IsAim = true;
            AimTime = 0f;

            return;
        }

        //エイム終了
        IsAim = false;
        AimTime = 0f;

        //ミーティングによる変身解除なら射撃しない
        if (MeetingReset)
        {
            MeetingReset = false;
            return;
        }

        //一発消費して
        BulletCount--;

        //命中判定はホストのみ行う
        if (!AmongUsClient.Instance.AmHost) return;

        var targets = GetSnipeTargets();

        if (targets.Count != 0)
        {
            //一番正確な対象がターゲット
            var snipedTarget = targets.OrderBy(c => c.Value).First().Key;
            CustomRoleManager.OnCheckMurder(
                Player, snipedTarget,       // sniperがsnipedTargetを打ち抜く
                snipedTarget, snipedTarget  // 表示上はsnipedTargetの自爆
            );

            //あたった通知
            Player.RpcProtectedMurderPlayer();

            //スナイプが起きたことを聞こえそうな対象に通知したい
            targets.Remove(snipedTarget);
            var snList = ShotNotify;
            snList.Clear();
            foreach (var otherPc in targets.Keys)
            {
                snList.Add(otherPc.PlayerId);
                Utils.NotifyRoles(SpecifySeer: otherPc);
            }
            SendRPC();
            _ = new LateTask(
                () =>
                {
                    snList.Clear();
                    if (targets.Count != 0)
                    {
                        foreach (var otherPc in targets.Keys)
                        {
                            Utils.NotifyRoles(SpecifySeer: otherPc);
                        }
                        SendRPC();
                    }
                },
                0.5f, "Sniper shot Notify"
                );
        }
    }
    public override void OnFixedUpdate(PlayerControl player)
    {
        if (!Player.IsAlive()) return;

        if (!AimAssist) return;

        if (!IsAim) return;

        if (!GameStates.IsInTask)
        {
            //エイム終了
            IsAim = false;
            AimTime = 0f;
            return;
        }

        var pos = Player.transform.position;
        if (pos != LastPosition)
        {
            AimTime = 0f;
            LastPosition = pos;
        }
        else
        {
            AimTime += Time.fixedDeltaTime;
            Utils.NotifyRoles(SpecifySeer: Player);
        }
    }
    public override void OnReportDeadBody(PlayerControl reporter, NetworkedPlayerInfo target)
    {
        MeetingReset = true;
    }
    public override string GetProgressText(bool comms = false)
    {
        return Utils.ColorString(Color.yellow, $"({BulletCount})");
    }
    public override string GetMark(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false)
    {
        seen ??= seer;
        if (!Is(seer) || !Is(seen)) return "";

        if (AimAssist)
        {
            //エイムアシスト中のスナイパー
            if (0.5f < AimTime && (!AimAssistOneshot || AimTime < 1.0f))
            {
                if (GetSnipeTargets().Count > 0)
                {
                    return $"<size=200%>{Utils.ColorString(Palette.ImpostorRed, "◎")}</size>";
                }
            }
        }
        return "";
    }
    public static string GetMarkOthers(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false)
    {
        //各スナイパーから
        foreach (var sniper in Snipers)
        {
            //射撃音が聞こえるプレイヤー
            var snList = sniper.ShotNotify;
            if (snList.Count > 0 && snList.Contains(seer.PlayerId))
            {
                return $"<size=200%>{Utils.ColorString(Palette.ImpostorRed, "!")}</size>";
            }
        }
        return "";
    }
    public override string GetAbilityButtonText()
    {
        return GetString(BulletCount <= 0 ? "DefaultShapeshiftText" : "SniperSnipeButtonText");
    }
}