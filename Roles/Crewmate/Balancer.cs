using System.Collections.Generic;
using System.Linq;
using Hazel;
using UnityEngine;
using AmongUs.GameOptions;

using TownOfHostForE.Roles.Core;
using TownOfHostForE.Roles.Core.Class;

namespace TownOfHostForE.Roles.Crewmate;


public sealed class Balancer : BalancerManager
{
    /// <summary>
    ///  20000:TOH4E役職
    ///   1000:陣営 1:crew 2:imp 3:Third 4:Animals
    ///    100:役職ID
    /// </summary>
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(Balancer),
            player => new Balancer(player),
            CustomRoles.Balancer,
            () => RoleTypes.Crewmate,
            CustomRoleTypes.Crewmate,
            21900,
            SetupOptionItem,
            "真実の天秤",
            "#f8cd46",
            introSound: () => GetIntroSound(RoleTypes.Crewmate)
        );
    public Balancer(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        chanceCount = OptionChanceCount.GetInt();
        killaway = OptionKillAway.GetInt();
        hidePlayer = OptionhidePlayer.GetBool();
        balancerPri = OptionbalancerPri.GetBool();
    }

    private static OptionItem OptionChanceCount;
    private static OptionItem OptionKillAway;
    private static OptionItem OptionhidePlayer;
    private static OptionItem OptionbalancerPri;

    enum OptionName
    {
        BalancerChanceCount,
        BalancerKillAway,
        BalancerHidePlayer,
        BalancerCountEqual
    }
    private static void SetupOptionItem()
    {
        OptionChanceCount = IntegerOptionItem.Create(RoleInfo, 10, OptionName.BalancerChanceCount, new(1, 15, 1), 1, false)
            .SetValueFormat(OptionFormat.Pieces);
        OptionKillAway = IntegerOptionItem.Create(RoleInfo, 11, OptionName.BalancerKillAway, new(1, 15, 1), 3, false)
            .SetValueFormat(OptionFormat.Pieces);
        OptionhidePlayer = BooleanOptionItem.Create(RoleInfo, 12, OptionName.BalancerHidePlayer, false, false);
        OptionbalancerPri = BooleanOptionItem.Create(RoleInfo, 13, OptionName.BalancerCountEqual, false, false);
    }

}
