using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using AmongUs.GameOptions;

using TownOfHostForE.Roles.Core;
using TownOfHostForE.Roles.Core.Interfaces;
using static TownOfHostForE.Translator;
using Hazel;

namespace TownOfHostForE.Roles.Impostor;

public sealed class Detonator : RoleBase, IImpostor
{
    /// <summary>
    ///  20000:TOH4E役職
    ///   1000:陣営 1:crew 2:imp 3:Third 4:Animals
    ///    100:役職ID
    /// </summary>
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(Detonator),
            player => new Detonator(player),
            CustomRoles.Detonator,
            () => RoleTypes.Shapeshifter,
            CustomRoleTypes.Impostor,
            22800,
            SetupOptionItem,
            "デトネーター"
        );
    public Detonator(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        raderTargetIds = byte.MaxValue;
        targetCount = new();
        distance = OptionDistance.GetFloat();
        plageTime = OptionPlageTime.GetInt();
        InfectInactiveTime = OptionInfectInactiveTime.GetFloat();
    }

    private enum OptionName
    {
        RaderDistance,
        RaderPlageTime,
        RaderInfectInactiveTime
    }

    //時間観測用
    private float UpdateTime;
    //時間観測用
    private float InfectInactiveTime;
    //レーダーのID
    private byte raderTargetIds = byte.MaxValue;
    //接触した人の情報
    private Dictionary<byte, byte> targetCount = new ();

    //距離感
    private static OptionItem OptionDistance;
    //感染時間
    private static OptionItem OptionPlageTime;
    //感染時間
    private static OptionItem OptionInfectInactiveTime;

    //距離感
    private float distance = 0;
    //感染時間
    private int plageTime = 0;

    private bool InfectActive = false;



    private static void SetupOptionItem()
    {
        OptionDistance = FloatOptionItem.Create(RoleInfo, 10, OptionName.RaderDistance, new(0.5f, 20f, 0.5f), 1f, false)
            .SetValueFormat(OptionFormat.Multiplier);
        OptionPlageTime = IntegerOptionItem.Create(RoleInfo, 11, OptionName.RaderPlageTime, new(5, 60, 1), 20, false)
            .SetValueFormat(OptionFormat.Seconds);
        OptionInfectInactiveTime = FloatOptionItem.Create(RoleInfo, 12, OptionName.RaderInfectInactiveTime, new(0.5f, 10f, 0.5f), 5f, false)
           .SetValueFormat(OptionFormat.Seconds);
    }

    public override void ApplyGameOptions(IGameOptions opt)
    {
        AURoleOptions.ShapeshifterCooldown = 1f;
        AURoleOptions.ShapeshifterDuration = 1f;
    }

    private bool CanTarget()
    {
        return raderTargetIds != byte.MaxValue ? Utils.GetPlayerById(raderTargetIds).IsAlive() : false;
    }
    public override bool OnCheckShapeshift(PlayerControl target, ref bool animate)
    {
        //ターゲット出来ない、もしくはターゲットが味方の場合は処理しない
        //※どちらにしろシェイプシフトは出来ない
        if (!CanTarget()) return false;
        if (!GameStates.IsInTask) return false;

        var raderTarget = Utils.GetPlayerById(raderTargetIds);
        PlayerState.GetByPlayerId(raderTarget.PlayerId).DeathReason = CustomDeathReason.Bombed;
        raderTarget.SetRealKiller(Player);
        raderTarget.RpcMurderPlayer(raderTarget);

        Utils.NotifyRoles();
        AURoleOptions.ShapeshifterCooldown = 255f;
        return false;
    }
    public override void Add()
    {
        foreach (var target in Main.AllPlayerControls)
        {
            if (target.PlayerId == Player.PlayerId) continue;
            targetCount.Add(target.PlayerId,0);
        }
        InfectActive = true;
        if (Main.NormalOptions.MapId == 4)
            //エアシップのリスポーン選択分固定で遅延させる
            InfectInactiveTime += 5f;
    }

    public override bool OnReportDeadBody(PlayerControl reporter, GameData.PlayerInfo target)
    {
        InfectActive = false;
        return true;
    }

    public override void AfterMeetingTasks()
    {
        _ = new LateTask(() =>
        {
            Logger.Info("InfectActive", "PlagueDoctor");
            InfectActive = true;
        },
        InfectInactiveTime, "ResetInfectInactiveTime");
    }

    public void OnCheckMurderAsKiller(MurderInfo info)
    {
        if (!info.CanKill) return;

        var (killer, target) = info.AttemptTuple;
        if (raderTargetIds == byte.MaxValue)
        {
            info.DoKill = killer.CheckDoubleTrigger(target, () => { SetRaderTarget(target.PlayerId); });
            //キルク調整
            killer.RpcProtectedMurderPlayer();
            //反映に少し時間を置く
            _ = new LateTask(() =>
            {
                Utils.NotifyRoles();
            },
            1f, "SetTarget");
        }
    }

    private void SetRaderTarget(byte targetId)
    {
        if (raderTargetIds != byte.MaxValue) return;
        raderTargetIds = targetId;
        targetCount.Remove(targetId);

    }
    public override string GetProgressText(bool comms = false) => raderTargetIds == byte.MaxValue ? Utils.ColorString(Color.white,":SET") : Utils.ColorString(Color.yellow,":ON AIR");

    public override void OnFixedUpdate(PlayerControl player)
    {
        if (InfectActive == false) return;
        UpdateTime -= Time.fixedDeltaTime;
        if (UpdateTime < 0) UpdateTime = 1f; //1秒ごとの更新

        if (UpdateTime == 1f)
        {
            if (!AmongUsClient.Instance.AmHost) return;
            if (!GameStates.IsInTask) return;
            CheckRaderData();
        }
    }

    private void CheckRaderData()
    {
        if (raderTargetIds == byte.MaxValue) return;

        PlayerControl raderPlayer = Utils.GetPlayerById(raderTargetIds);
        if (!raderPlayer.IsAlive()) return;

        foreach (var target in Main.AllAlivePlayerControls)
        {
            if (target.PlayerId == Player.PlayerId) continue;
            if (!targetCount.ContainsKey(target.PlayerId)) continue;
            if (targetCount[target.PlayerId] >= plageTime) continue;

            var pos = raderPlayer.transform.position;
            var dis = Vector2.Distance(pos, target.transform.position);
            if (dis > distance) continue;

            //範囲内
            //最大までカウントされてるなら処理しない
            targetCount[target.PlayerId]++;
        }
        Utils.NotifyRoles();
    }

    private bool CheckDispRoleName(byte playerid)
    {
        if(!targetCount.ContainsKey(playerid)) return false;
        return targetCount[playerid] >= plageTime;
    }

    public override void OverrideDisplayRoleNameAsSeer(PlayerControl seen, bool isMeeting, ref bool enabled, ref Color roleColor, ref string roleText)
    {
        enabled |= CheckDispRoleName(seen.PlayerId);
    }
    public override void OverrideProgressTextAsSeer(PlayerControl seen, ref bool enabled, ref string text)
    {
        enabled |= CheckDispRoleName(seen.PlayerId);
    }
    //public override string GetMark(PlayerControl seer, PlayerControl seen, bool isForMeeting = false)
    //{
    //    //seenが省略の場合seer
    //    seen ??= seer;
    //    var mark = new StringBuilder(50);

    //    // 死亡したLoversのマーク追加
    //    if (seen.Is(CustomRoles.Lovers) && !seer.Is(CustomRoles.Lovers))
    //        mark.Append(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Lovers), "♡"));

    //    return mark.ToString();
    //}

}
