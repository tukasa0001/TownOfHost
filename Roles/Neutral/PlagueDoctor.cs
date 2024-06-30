using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using AmongUs.GameOptions;

using TownOfHost.Roles.Core;
using TownOfHost.Roles.Core.Interfaces;
using static TownOfHost.Translator;
using Hazel;

namespace TownOfHost.Roles.Neutral;

public sealed class PlagueDoctor : RoleBase, IKiller
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(PlagueDoctor),
            player => new PlagueDoctor(player),
            CustomRoles.PlagueDoctor,
            () => RoleTypes.Impostor,
            CustomRoleTypes.Neutral,
            51100,
            SetupOptionItem,
            "pd",
            "#ff6633",
            true,
            introSound: () => GetIntroSound(RoleTypes.Crewmate)
        );
    public PlagueDoctor(PlayerControl player)
    : base(
        RoleInfo,
        player,
        () => HasTask.False
    )
    {
        PlagueDoctors.Add(this);
        if (PlagueDoctors.Count == 1)
        {
            InfectLimit = OptionInfectLimit.GetInt();
            InfectWhenKilled = OptionInfectWhenKilled.GetBool();
            InfectTime = OptionInfectTime.GetFloat();
            InfectDistance = OptionInfectDistance.GetFloat();
            InfectInactiveTime = OptionInfectInactiveTime.GetFloat();
            CanInfectSelf = OptionInfectCanInfectSelf.GetBool();
            CanInfectVent = OptionInfectCanInfectVent.GetBool();

            InfectInfos = new(GameData.Instance.PlayerCount);
            //他視点用のMarkメソッド登録
            CustomRoleManager.MarkOthers.Add(GetMarkOthers);
            CustomRoleManager.LowerOthers.Add(GetLowerTextOthers);
            CustomRoleManager.OnFixedUpdateOthers.Add(OnFixedUpdateOthers);
            CustomRoleManager.OnMurderPlayerOthers.Add(OnMurderPlayerOthers);
        }
    }
    public override void OnDestroy()
    {
        PlagueDoctors.Clear();
    }
    public bool CanKill { get; private set; } = false;

    private static OptionItem OptionInfectLimit;
    private static OptionItem OptionInfectWhenKilled;
    private static OptionItem OptionInfectTime;
    private static OptionItem OptionInfectDistance;
    private static OptionItem OptionInfectInactiveTime;
    private static OptionItem OptionInfectCanInfectSelf;
    private static OptionItem OptionInfectCanInfectVent;

    private static int InfectLimit;
    private static bool InfectWhenKilled;
    private static float InfectTime;
    private static float InfectDistance;
    private static float InfectInactiveTime;
    private static bool CanInfectSelf;
    private static bool CanInfectVent;
    enum OptionName
    {
        PlagueDoctorInfectLimit,
        PlagueDoctorInfectWhenKilled,
        PlagueDoctorInfectTime,
        PlagueDoctorInfectDistance,
        PlagueDoctorInfectInactiveTime,
        PlagueDoctorCanInfectSelf,
        PlagueDoctorCanInfectVent,
    }
    private static void SetupOptionItem()
    {
        OptionInfectLimit = IntegerOptionItem.Create(RoleInfo, 10, OptionName.PlagueDoctorInfectLimit, new(1, 3, 1), 1, false)
            .SetValueFormat(OptionFormat.Times);
        OptionInfectWhenKilled = BooleanOptionItem.Create(RoleInfo, 11, OptionName.PlagueDoctorInfectWhenKilled, false, true);
        OptionInfectTime = FloatOptionItem.Create(RoleInfo, 12, OptionName.PlagueDoctorInfectTime, new(3f, 20f, 1f), 8f, false)
           .SetValueFormat(OptionFormat.Seconds);
        OptionInfectDistance = FloatOptionItem.Create(RoleInfo, 13, OptionName.PlagueDoctorInfectDistance, new(0.5f, 2f, 0.25f), 1.5f, false);
        OptionInfectInactiveTime = FloatOptionItem.Create(RoleInfo, 14, OptionName.PlagueDoctorInfectInactiveTime, new(0.5f, 10f, 0.5f), 5f, false)
           .SetValueFormat(OptionFormat.Seconds);
        OptionInfectCanInfectSelf = BooleanOptionItem.Create(RoleInfo, 15, OptionName.PlagueDoctorCanInfectSelf, false, true);
        OptionInfectCanInfectVent = BooleanOptionItem.Create(RoleInfo, 16, OptionName.PlagueDoctorCanInfectVent, false, true);
    }

    private int InfectCount;
    private static Dictionary<byte, float> InfectInfos;
    private static bool InfectActive;
    private static bool LateCheckWin;
    private static List<PlagueDoctor> PlagueDoctors = new();

    public override void Add()
    {
        InfectCount = InfectLimit;

        InfectActive = true;
        if (Main.NormalOptions.MapId == 4)
            //エアシップのリスポーン選択分固定で遅延させる
            InfectInactiveTime += 5f;
    }
    public bool CanUseKillButton() => InfectCount != 0;
    public bool CanUseImpostorVentButton() => false;
    public bool OverrideKillButtonText(out string text)
    {
        text = GetString("Infected");
        return true;
    }
    public bool CanUseSabotageButton() => false;
    public override string GetProgressText(bool comms = false)
    {
        return Utils.ColorString(RoleInfo.RoleColor.ShadeColor(0.25f), $"({InfectCount})");
    }
    public override void ApplyGameOptions(IGameOptions opt)
    {
        opt.SetVision(false);
    }
    public static bool CanInfect(PlayerControl player)
    {
        var pd = PlagueDoctors.FirstOrDefault(x => x.Player == player);
        //ペスト医師でないか、自己感染可能かつ感染者作成済み
        return pd == null || (CanInfectSelf && pd.InfectCount == 0);
    }
    public void SendRPC(byte targetId, float rate)
    {
        using var sender = CreateSender();
        sender.Writer.Write(targetId);
        sender.Writer.Write(rate);
    }
    public override void ReceiveRPC(MessageReader reader)
    {
        var targetId = reader.ReadByte();
        var rate = reader.ReadSingle();
        InfectInfos[targetId] = rate;
    }
    public void OnCheckMurderAsKiller(MurderInfo info)
    {
        var (killer, target) = info.AttemptTuple;
        if (InfectCount > 0)
        {
            InfectCount--;
            killer.RpcProtectedMurderPlayer(target);
            DirectInfect(target);
        }
        info.DoKill = false;
    }
    public override void OnMurderPlayerAsTarget(MurderInfo info)
    {
        var (killer, target) = info.AttemptTuple;
        if (InfectWhenKilled && InfectCount > 0)
        {
            InfectCount = 0;
            DirectInfect(killer);
        }
    }
    public static void OnMurderPlayerOthers(MurderInfo info)
    {
        //非感染者が死んだ場合勝利するかもしれない
        LateCheckWin = true;
    }
    public override void OnReportDeadBody(PlayerControl reporter, NetworkedPlayerInfo target)
    {
        InfectActive = false;
    }
    public static void OnFixedUpdateOthers(PlayerControl player)
    {
        if (!AmongUsClient.Instance.AmHost) return;

        if (!GameStates.IsInTask) return;
        if (LateCheckWin)
        {
            //吊り/キルの後、念のため勝利条件チェック
            LateCheckWin = false;
            CheckWin();
        }
        if (!player.IsAlive() || !InfectActive) return;

        if (InfectInfos.TryGetValue(player.PlayerId, out var rate) && rate >= 100)
        {
            //感染者の場合
            var changed = false;
            var inVent = player.inVent;
            foreach (var target in Main.AllAlivePlayerControls)
            {
                //ペスト医師は自身が感染できない場合は除外
                if (!CanInfect(target)) continue;
                //ベント内外であれば除外
                if (!CanInfectVent && target.inVent != inVent) continue;

                InfectInfos.TryGetValue(target.PlayerId, out var oldRate);
                //感染者は除外
                if (oldRate >= 100) continue;

                //範囲外は除外
                var distance = UnityEngine.Vector3.Distance(player.transform.position, target.transform.position);
                if (distance > InfectDistance) continue;

                var newRate = oldRate + Time.fixedDeltaTime / InfectTime * 100;
                newRate = Math.Clamp(newRate, 0, 100);
                InfectInfos[target.PlayerId] = newRate;
                if ((oldRate < 50 && newRate >= 50) || newRate >= 100)
                {
                    changed = true;
                    Logger.Info($"InfectRate[{target.GetNameWithRole()}]:{newRate}%", "OnCheckMurderAsKiller");
                    PlagueDoctors[0].SendRPC(target.PlayerId, newRate);
                }
            }
            if (changed)
            {
                //誰かの感染が進行していたら
                CheckWin();
                Utils.NotifyRoles();
            }
        }
    }
    public override void AfterMeetingTasks()
    {
        if (PlagueDoctors[0] == this)
        {
            //非感染者が吊られた場合勝利するかもしれない
            LateCheckWin = true;

            _ = new LateTask(() =>
            {
                Logger.Info("InfectActive", "PlagueDoctor");
                InfectActive = true;
            },
            InfectInactiveTime, "ResetInfectInactiveTime");
        }
    }

    public static string GetMarkOthers(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false)
    {
        seen ??= seer;
        if (!CanInfect(seen)) return "";
        if (!seer.Is(CustomRoles.PlagueDoctor) && seer.IsAlive()) return "";
        var str = new StringBuilder(40);
        str.Append($"<color={RoleInfo.RoleColorCode}>");
        str.Append(GetInfectRateCharactor(seen));
        str.Append("</color>");
        return str.ToString();
    }
    public static string GetLowerTextOthers(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false, bool isForHud = false)
    {
        seen ??= seer;
        if (!seen.Is(CustomRoles.PlagueDoctor)) return "";
        if (!seer.Is(CustomRoles.PlagueDoctor) && seer.IsAlive()) return "";
        var str = new StringBuilder(40);
        str.Append($"<color={RoleInfo.RoleColorCode}>");
        foreach (var player in Main.AllAlivePlayerControls)
        {
            if (!player.Is(CustomRoles.PlagueDoctor))
                str.Append(GetInfectRateCharactor(player));
        }
        str.Append("</color>");
        return str.ToString();
    }
    public static bool IsInfected(byte playerId)
    {
        InfectInfos.TryGetValue(playerId, out var rate);
        return rate >= 100;
    }
    public static string GetInfectRateCharactor(PlayerControl player)
    {
        if (!CanInfect(player) || !player.IsAlive()) return "";
        InfectInfos.TryGetValue(player.PlayerId, out var rate);
        return rate switch
        {
            < 50 => "\u2581",
            >= 50 and < 100 => "\u2584",
            >= 100 => "\u2588",
            _ => ""
        };
    }
    public void DirectInfect(PlayerControl player)
    {
        Logger.Info($"InfectRate[{player.GetNameWithRole()}]:100%", "OnCheckMurderAsKiller");
        InfectInfos[player.PlayerId] = 100;
        SendRPC(player.PlayerId, 100);
        Utils.NotifyRoles();
        CheckWin();
    }
    public static void CheckWin()
    {
        if (!AmongUsClient.Instance.AmHost) return;
        //だれかの勝利処理中なら無効
        if (CustomWinnerHolder.WinnerTeam != CustomWinner.Default) return;

        bool comprete = Main.AllAlivePlayerControls.All(p => p.Is(CustomRoles.PlagueDoctor) || IsInfected(p.PlayerId));

        if (comprete)
        {
            InfectActive = false;

            foreach (var player in Main.AllAlivePlayerControls)
            {
                if (player.Is(CustomRoles.PlagueDoctor)) continue;
                player.SetRealKiller(null);
                player.RpcMurderPlayer(player);
                var state = PlayerState.GetByPlayerId(player.PlayerId);
                state.DeathReason = CustomDeathReason.Infected;
                state.SetDead();
            }
            CustomWinnerHolder.ResetAndSetWinner(CustomWinner.PlagueDoctor);
            foreach (var plagueDoctor in Main.AllPlayerControls.Where(p => p.Is(CustomRoles.PlagueDoctor)))
                CustomWinnerHolder.WinnerIds.Add(plagueDoctor.PlayerId);
        }
    }
}
