using System.Collections.Generic;
using System;
using System.Linq;
using AmongUs.GameOptions;
using UnityEngine;

using TownOfHostForE.Roles.Core;

namespace TownOfHostForE.Roles.Madmate;
public sealed class MadTricker : RoleBase
{
    /// <summary>
    ///  20000:TOH4E役職
    ///   1000:陣営 1:crew 2:imp 3:Third 4:Animals 5:Madmate
    ///    100:役職ID
    /// </summary>
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(MadTricker),
            player => new MadTricker(player),
            CustomRoles.MadTricker,
            () => RoleTypes.Engineer,
            CustomRoleTypes.Madmate,
            25100,
            SetupOptionItem,
            "マッドトリッカー",
            introSound: () => GetIntroSound(RoleTypes.Impostor)
        );
    public MadTricker(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        ventcool = OptionVentCooldown.GetFloat();
        abilityCount = OptionAbilityCount.GetInt();
    }

    public static OptionItem OptionVentCooldown;
    public static float ventcool;
    public static OptionItem OptionAbilityCount;
    public static int abilityCount;
    enum OptionName
    {
        VentCool,
        ShuffleCount,
    }
    public static void SetupOptionItem()
    {
        OptionVentCooldown = FloatOptionItem.Create(RoleInfo, 10, OptionName.VentCool, new(0, 180, 5), 30, false)
            .SetValueFormat(OptionFormat.Seconds);
        OptionAbilityCount = IntegerOptionItem.Create(RoleInfo, 11, OptionName.ShuffleCount, new(1, 30, 1), 5, false)
            .SetValueFormat(OptionFormat.None);
        Options.SetUpAddOnOptions(RoleInfo.ConfigId + 20, RoleInfo.RoleName, RoleInfo.Tab);
    }

    public override void ApplyGameOptions(IGameOptions opt)
    {
        AURoleOptions.EngineerCooldown = ventcool;
        AURoleOptions.EngineerInVentMaxTime = 1f;
    }

    public override string GetProgressText(bool comms = false)
        => Utils.ColorString(RoleInfo.RoleColor, $"({abilityCount})");

    public override bool OnEnterVent(PlayerPhysics physics, int ventId)
    {
        //回数下回ったら普通のベント
        if (abilityCount <= 0) return true;

        List<Vector3> posList = new();
        foreach (var pc in Main.AllAlivePlayerControls)
        {
            //PCが自分だったら処理しない
            if (pc == Player) continue;
            //対象がベントに入っていたら処理しない
            if (pc.inVent) continue;
            //梯子、ぬーんの確認
            if (!CheckLadderorNoonPos(pc.transform.position)) continue;
            //梯子の確認
            if (pc.MyPhysics.Animations.IsPlayingAnyLadderAnimation()) continue;

            posList.Add(pc.transform.position);
        }

        //リストシャッフル
        posList = posList.OrderBy(a => Guid.NewGuid()).ToList();

        //テレポ処理
        foreach (var pc in Main.AllAlivePlayerControls)
        {
            //PCが自分だったら処理しない
            if (pc == Player) continue;
            //対象がベントに入っていたら処理しない
            if (pc.inVent) continue;
            //梯子、ぬーんの確認
            if (!CheckLadderorNoonPos(pc.transform.position)) continue;
            //梯子の確認
            if (pc.MyPhysics.Animations.IsPlayingAnyLadderAnimation()) continue;
            pc.RpcSnapToForced(posList[0]);
            posList.RemoveAt(0);
        }

        abilityCount--;
        Utils.NotifyRoles();
        return true;
    }

    private bool CheckLadderorNoonPos(Vector2 nowpos)
    {
        bool result = true;

        //mapがairshipでない場合は処理しない
        if (Main.NormalOptions.MapId != 4) return result;

        //ミーティング梯子確認
        if ((nowpos.x >= 4.4 && nowpos.x <= 4.6) && //梯子上ってるときのx
            (nowpos.y <= 14.5 && nowpos.y >= 9.7)) //梯子の上から下
        {
            Logger.Info("MTG梯子を上っている", "POS");
            result = false;
        }

        //昇降機梯子確認
        if ((nowpos.x >= 10.7 && nowpos.x <= 10.9) && //梯子上ってるときのx
            (nowpos.y <= 8.1 && nowpos.y >= 6.4)) //梯子の上から下
        {
            Logger.Info("昇降機梯子を上っている", "POS");
            result = false;
        }

        //エレキはしご確認
        if ((nowpos.x >= 12.8 && nowpos.x <= 13) && //梯子上ってるときのx
            (nowpos.y <= -3.5 && nowpos.y >= -5.4)) //梯子の上から下
        {
            Logger.Info("エレキ梯子を上っている", "POS");
            result = false;
        }

        //昇降機のぬーん
        if ((nowpos.y >= 8.7 && nowpos.y <= 9.0) && //梯子上ってるときのx
            (nowpos.x <= 10.0 && nowpos.x >= 5.8)) //梯子の上から下
        {
            Logger.Info("ぬーんを使っている", "POS");
            result = false;
        }


        return result;
    }
    
}
