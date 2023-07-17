using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TownOfHost.Roles.Core.Interfaces;
using TownOfHost.Roles.Core;
using AmongUs.GameOptions;
using TMPro;
using Hazel;
using Il2CppSystem.Runtime.Remoting.Messaging;
using System.Numerics;
using UnityEngine;
using MS.Internal.Xml.XPath;
using static UnityEngine.GraphicsBuffer;
using System.Threading.Channels;

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
            introSound: () => GetIntroSound(RoleTypes.Crewmate)
        );
    public PlagueDoctor(PlayerControl player)
    : base(
        RoleInfo,
        player,
        () => HasTask.False
    )
    {
        InfectLimit = OptinInfectLimit.GetInt();
        InfectTime = OptionInfectTime.GetFloat();
        InfectDistance = OptionInfectDistance.GetFloat();
        InfectInactiveTime = OptionInfectInactiveTime.GetFloat();

        InfectInfos = new(GameData.Instance.PlayerCount);
        if (FirstPlagueDoctor == null)
        {
            FirstPlagueDoctor = this;
            //他視点用のMarkメソッド登録
            CustomRoleManager.MarkOthers.Add(GetMarkOthers);
            CustomRoleManager.LowerOthers.Add(GetLowerTextOthers);
            CustomRoleManager.OnFixedUpdateOthers.Add(OnFixedUpdateOthers);
        }
    }
    public override void OnDestroy()
    {
        FirstPlagueDoctor = null;
    }
    public bool CanKill { get; private set; } = false;

    private static OptionItem OptinInfectLimit;
    private static OptionItem OptionInfectTime;
    private static OptionItem OptionInfectDistance;
    private static OptionItem OptionInfectInactiveTime;

    private static int InfectLimit;
    private static float InfectTime;
    private static float InfectDistance;
    private static float InfectInactiveTime;
    enum OptionName
    {
        PlagueDoctorInfectLimit,
        PlagueDoctorInfectTime,
        PlagueDoctorInfectDistance,
        PlagueDoctorInfectInactiveTime
    }
    private static void SetupOptionItem()
    {
        OptinInfectLimit = IntegerOptionItem.Create(RoleInfo, 10, OptionName.PlagueDoctorInfectLimit, new(1, 3, 1), 1, false)
            .SetValueFormat(OptionFormat.Times);
        OptionInfectTime = FloatOptionItem.Create(RoleInfo, 11, OptionName.PlagueDoctorInfectTime, new(5f, 20f, 1f), 10f, false)
           .SetValueFormat(OptionFormat.Seconds);
        OptionInfectDistance = FloatOptionItem.Create(RoleInfo, 12, OptionName.PlagueDoctorInfectDistance, new(0.5f, 2f, 0.25f), 1.5f, false);
        OptionInfectInactiveTime = FloatOptionItem.Create(RoleInfo, 13, OptionName.PlagueDoctorInfectInactiveTime, new(0.5f, 10f, 0.5f), 5f, false)
           .SetValueFormat(OptionFormat.Seconds);
    }

    int InfectCount;
    static Dictionary<byte, float> InfectInfos;
    static bool InfectActive;
    static PlagueDoctor FirstPlagueDoctor;
    public override void Add()
    {
        InfectCount = InfectLimit;

        InfectActive = true;
        if (Main.NormalOptions.MapId == 4)
            //エアシップのリスポーン選択分固定で遅延させる
            InfectInactiveTime += 5f;
    }
    public bool CanUseKillButton() => InfectCount != 0;
    public override bool OnInvokeSabotage(SystemTypes systemType) => false;
    public override string GetProgressText(bool comms = false)
    {
        return Utils.ColorString(RoleInfo.RoleColor.ShadeColor(0.25f), $"({InfectCount})");
    }
    public override void ApplyGameOptions(IGameOptions opt)
    {
        opt.SetVision(false);
    }

    public void SendRPC(byte targetId, float rate)
    {
        using var sender = CreateSender(CustomRPC.SyncPlagueDoctor);
        sender.Writer.Write(targetId);
        sender.Writer.Write(rate);

    }
    public override void ReceiveRPC(MessageReader reader, CustomRPC rpcType)
    {
        if (rpcType != CustomRPC.SyncPlagueDoctor) return;

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
            killer.RpcGuardAndKill(target);
            DirectInfect(target);
        }
        info.DoKill = false;
    }
    public override void OnMurderPlayerAsTarget(MurderInfo info)
    {
        var (killer, target) = info.AttemptTuple;
        if (InfectCount > 0)
        {
            InfectCount = 0;
            DirectInfect(killer);
        }
    }
    public static void OnFixedUpdateOthers(PlayerControl player)
    {
        if (!AmongUsClient.Instance.AmHost) return;

        if (!GameStates.IsInTask) return;
        if (!player.IsAlive() || !InfectActive) return;

        if (InfectInfos.TryGetValue(player.PlayerId, out var rate) && rate >= 100)
        {
            //感染者の場合
            var changed = false;
            foreach (var target in Main.AllAlivePlayerControls)
            {
                if (target.Is(CustomRoles.PlagueDoctor)) continue;
                InfectInfos.TryGetValue(target.PlayerId, out var oldRate);
                if (oldRate >= 100) continue;
                var distance = UnityEngine.Vector3.Distance(player.transform.position, target.transform.position);
                if (distance > InfectDistance) continue;
                var newRate = oldRate + Time.fixedDeltaTime / InfectTime * 100;
                newRate = Math.Clamp(newRate, 0, 100);
                InfectInfos[target.PlayerId] = newRate;
                if ((oldRate < 50 && newRate >= 50) || newRate >= 100)
                {
                    changed = true;
                    Logger.Info($"InfectRate[{target.GetNameWithRole()}]:{newRate}%", "OnCheckMurderAsKiller");
                    FirstPlagueDoctor.SendRPC(target.PlayerId, newRate);
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
        if (FirstPlagueDoctor == this)
        {
            InfectActive = false;
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
        if (seen.Is(CustomRoles.PlagueDoctor)) return "";
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
            str.Append(GetInfectRateCharactor(player));
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
        if (player.Is(CustomRoles.PlagueDoctor) || !player.IsAlive()) return "";
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

        bool comprete = Main.AllAlivePlayerControls.All(p => p.Is(CustomRoles.PlagueDoctor) || IsInfected(p.PlayerId));

        if (comprete)
        {
            InfectActive = false;

            CustomWinnerHolder.ShiftWinnerAndSetWinner(CustomWinner.PlagueDoctor);
            foreach (var plagueDoctor in Main.AllPlayerControls.Where(p => p.Is(CustomRoles.PlagueDoctor)))
                CustomWinnerHolder.WinnerIds.Add(plagueDoctor.PlayerId);
            foreach (var player in Main.AllAlivePlayerControls)
            {
                if (player.Is(CustomRoles.PlagueDoctor)) continue;
                player.SetRealKiller(null);
                player.RpcMurderPlayer(player);
                var state = PlayerState.GetByPlayerId(player.PlayerId);
                state.DeathReason = CustomDeathReason.Infected;
                state.SetDead();
            }
        }
    }

}
