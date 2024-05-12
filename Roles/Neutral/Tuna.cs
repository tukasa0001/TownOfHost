using AmongUs.GameOptions;
using Hazel;
using System.Linq;
using TownOfHostForE.Roles.Animals;
using TownOfHostForE.Roles.Core;
using TownOfHostForE.Roles.Core.Interfaces;
using UnityEngine;

namespace TownOfHostForE.Roles.Neutral;

public sealed class Tuna : RoleBase, IAdditionalWinner
{
    /// <summary>
    ///  20000:TOH4E役職
    ///   1000:陣営 1:crew 2:imp 3:Third 4:Animals
    ///    100:役職ID
    /// </summary>
    public static readonly SimpleRoleInfo RoleInfo =
         SimpleRoleInfo.Create(
            typeof(Tuna),
            player => new Tuna(player),
            CustomRoles.Tuna,
            () => setRoleType(),
            setCustomRoleType(),
            23400,
            SetupOptionItem,
            "マグロ",
            TunaTypeAnimals ? "#FF8C00" : "#00bfff"
        );
    public Tuna(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        TunaTypeAnimals = OptionAddAnimals.GetBool();
        waitTime = OptionWaitTime.GetFloat();
        additionalWin = OptionAdditionalWinner.GetBool();
        isFirst = false;
        TimeCounter = 0f;
    }
    enum OptionName
    {
        TunaWaitTime,
        additionalWinner,
    }

    private static OptionItem OptionWaitTime;
    private static OptionItem OptionAdditionalWinner;
    private static OptionItem OptionUseVent;
    public static OptionItem OptionAddAnimals;

    public static bool TunaTypeAnimals = false;

    private float waitTime = 0f;
    private bool additionalWin = false;

    //時間観測用
    private float UpdateTime;
    private float TimeCounter = 0;
    private Vector3 nowPosition = Vector3.zero;
    private bool isFirst = false;

    private static RoleTypes setRoleType()
    {
        if (OptionUseVent.GetBool())
        {
            return RoleTypes.Engineer;
        }
        else
        {
            return RoleTypes.Crewmate;
        }
    }
    private static CustomRoleTypes setCustomRoleType()
    {
        if (TunaTypeAnimals)
        {
            return CustomRoleTypes.Animals;
        }
        else
        {
            return CustomRoleTypes.Neutral;
        }
    }
    private static void SetupOptionItem()
    {
        OptionWaitTime = FloatOptionItem.Create(RoleInfo, 10, OptionName.TunaWaitTime, new(0f, 30f, 1f), 3f, false)
            .SetValueFormat(OptionFormat.Seconds);
        OptionAdditionalWinner = BooleanOptionItem.Create(RoleInfo, 11, OptionName.additionalWinner, false, false);
        OptionUseVent = BooleanOptionItem.Create(RoleInfo, 12, GeneralOption.CanVent, false, false);
        OptionAddAnimals = BooleanOptionItem.Create(RoleInfo, 13, GeneralOption.CountAnimals, false, false);
    }
    public bool CheckWin(ref CustomRoles winnerRole)
    {
        return Player.IsAlive() && additionalWin;
    }

    public override void OnStartMeeting()
    {
        isFirst = true;
    }
    public static void CheckAliveWin(PlayerControl pc)
    {
        //アニマルズ判定ならアニマルズ勝利
        if (TunaTypeAnimals)
        {
            Vulture.AnimalsBomb(CustomDeathReason.Bombed);
            Vulture.AnimalsWin();
            return;
        }

        //追加勝利ならそっちで処理
        if (OptionAdditionalWinner.GetBool()) return;

        CustomWinnerHolder.ResetAndSetWinner(CustomWinner.Tuna);
        CustomWinnerHolder.WinnerIds.Add(pc.PlayerId);
    }

    public override void OnFixedUpdate(PlayerControl player)
    {
        //タスク中のみ対象
        if (!GameStates.IsInTask) return;

        //初手会議が終わってなければ始めない。
        if (!isFirst) return;

        //死んでいたら対象外
        if (!Player.IsAlive()) return;

        UpdateTime -= Time.fixedDeltaTime;
        if (UpdateTime < 0) UpdateTime = 1f; //1秒ごとの更新

        if (UpdateTime == 1f)
        {
            if(player.CanMove) CheckWaitCount();
        }
    }

    private void CheckWaitCount()
    {
        if (nowPosition == Player.transform.position)
        {
            TimeCounter++;
            //設定秒経ってたら自爆
            if (TimeCounter >= waitTime)
            {
                PlayerState.GetByPlayerId(Player.PlayerId).DeathReason = CustomDeathReason.fishing;
                Player.RpcMurderPlayer(Player);
            }
        }
        else
        {
            nowPosition = Player.transform.position;
            TimeCounter = 0f;
        }

    }
}
