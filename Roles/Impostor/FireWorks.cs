using System.Collections.Generic;
using System.Linq;
using UnityEngine;

using AmongUs.GameOptions;
using TownOfHostForE.Roles.Core;
using TownOfHostForE.Roles.Core.Interfaces;
using static TownOfHostForE.Translator;

namespace TownOfHostForE.Roles.Impostor;

public sealed class FireWorks : RoleBase, IImpostor
{
    public enum FireWorksState
    {
        Initial = 1,
        SettingFireWorks = 2,
        WaitTime = 4,
        ReadyFire = 8,
        FireEnd = 16,
        CanUseKill = Initial | FireEnd
    }
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(FireWorks),
            player => new FireWorks(player),
            CustomRoles.FireWorks,
            () => RoleTypes.Shapeshifter,
            CustomRoleTypes.Impostor,
            10800,
            SetupCustomOption,
            "花火職人"
        );
    public FireWorks(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        FireWorksCount = OptionFireWorksCount.GetInt();
        FireWorksRadius = OptionFireWorksRadius.GetFloat();
        fireWorksNormalKill = OptionFireWorksNormalKill.GetBool();
        fireWorksOtherImpostersFire = OptionFireWorksOtherImpostersFire.GetBool();
    }

    static OptionItem OptionFireWorksCount;
    static OptionItem OptionFireWorksRadius;
    static OptionItem OptionFireWorksNormalKill;
    static OptionItem OptionFireWorksOtherImpostersFire;
    enum OptionName
    {
        FireWorksMaxCount,
        FireWorksRadius,
        FireWorksNormalKill,
        FireWorksOtherImpostersFire,
    }

    int FireWorksCount;
    float FireWorksRadius;
    int NowFireWorksCount;
    bool fireWorksNormalKill;
    bool fireWorksOtherImpostersFire;
    List<Vector3> FireWorksPosition = new();
    FireWorksState State = FireWorksState.Initial;

    public static void SetupCustomOption()
    {
        OptionFireWorksCount = IntegerOptionItem.Create(RoleInfo, 10, OptionName.FireWorksMaxCount, new(1, 3, 1), 1, false)
            .SetValueFormat(OptionFormat.Pieces);
        OptionFireWorksRadius = FloatOptionItem.Create(RoleInfo, 11, OptionName.FireWorksRadius, new(0.5f, 3f, 0.5f), 1f, false)
            .SetValueFormat(OptionFormat.Multiplier);
        OptionFireWorksNormalKill = BooleanOptionItem.Create(RoleInfo, 12, OptionName.FireWorksNormalKill, false, false);
        OptionFireWorksOtherImpostersFire = BooleanOptionItem.Create(RoleInfo, 13, OptionName.FireWorksOtherImpostersFire, false, false);
    }

    public override void Add()
    {
        NowFireWorksCount = FireWorksCount;
        FireWorksPosition.Clear();
        State = FireWorksState.Initial;
    }

    public bool CanUseKillButton()
    {
        if (!Player.IsAlive()) return false;
        return ((State & FireWorksState.CanUseKill) != 0) || fireWorksNormalKill;
    }
    public override void ApplyGameOptions(IGameOptions opt)
    {
        AURoleOptions.ShapeshifterDuration = State != FireWorksState.FireEnd ? 1f : 30f;
    }

    public override void OnShapeshift(PlayerControl target)
    {
        var shapeshifting = !Is(target);
        Logger.Info($"FireWorks ShapeShift", "FireWorks");
        if (!shapeshifting) return;
        switch (State)
        {
            case FireWorksState.Initial:
            case FireWorksState.SettingFireWorks:
                Logger.Info("花火を一個設置", "FireWorks");
                FireWorksPosition.Add(Player.transform.position);
                NowFireWorksCount--;
                if (NowFireWorksCount == 0)
                    State = (Main.AliveImpostorCount <= 1 || fireWorksOtherImpostersFire) ? FireWorksState.ReadyFire : FireWorksState.WaitTime;
                else
                    State = FireWorksState.SettingFireWorks;
                break;
            case FireWorksState.ReadyFire:
                Logger.Info("花火を爆破", "FireWorks");
                if (AmongUsClient.Instance.AmHost)
                {
                    //爆破処理はホストのみ
                    bool suicide = false;
                    foreach (var fireTarget in Main.AllAlivePlayerControls)
                    {
                        foreach (var pos in FireWorksPosition)
                        {
                            var dis = Vector2.Distance(pos, fireTarget.transform.position);
                            if (dis > FireWorksRadius) continue;

                            if (fireTarget == Player)
                            {
                                //自分は後回し
                                suicide = true;
                            }
                            else
                            {
                                PlayerState.GetByPlayerId(fireTarget.PlayerId).DeathReason = CustomDeathReason.Bombed;
                                fireTarget.SetRealKiller(Player);
                                fireTarget.RpcMurderPlayer(fireTarget);
                            }
                        }
                    }
                    if (suicide)
                    {
                        var totalAlive = Main.AllAlivePlayerControls.Count();
                        //自分が最後の生き残りの場合は勝利のために死なない
                        if (totalAlive != 1)
                        {
                            MyState.DeathReason = CustomDeathReason.Misfire;
                            Player.RpcMurderPlayer(Player);
                        }
                    }
                    Player.MarkDirtySettings();
                }
                State = FireWorksState.FireEnd;
                break;
            default:
                break;
        }
        Utils.NotifyRoles();
    }

    public override string GetLowerText(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false, bool isForHud = false)
    {
        string retText = "";

        if (State == FireWorksState.WaitTime && Main.AliveImpostorCount <= 1)
        {
            Logger.Info("爆破準備OK", "FireWorks");
            State = FireWorksState.ReadyFire;
            Utils.NotifyRoles();
        }
        switch (State)
        {
            case FireWorksState.Initial:
            case FireWorksState.SettingFireWorks:
                retText = string.Format(GetString("FireworksPutPhase"), NowFireWorksCount);
                break;
            case FireWorksState.WaitTime:
                retText = GetString("FireworksWaitPhase");
                break;
            case FireWorksState.ReadyFire:
                retText = GetString("FireworksReadyFirePhase");
                break;
            case FireWorksState.FireEnd:
                break;
        }
        return retText;
    }
    public override string GetAbilityButtonText()
    {
        if (State == FireWorksState.ReadyFire)
            return GetString("FireWorksExplosionButtonText");
        else
            return GetString("FireWorksInstallAtionButtonText");
    }
}