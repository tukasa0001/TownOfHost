using AmongUs.GameOptions;

using TownOfHostForE.Roles.Core;

namespace TownOfHostForE.Roles.Crewmate;
public sealed class Bait : RoleBase
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(Bait),
            player => new Bait(player),
            CustomRoles.Bait,
            () => RoleTypes.Crewmate,
            CustomRoleTypes.Crewmate,
            30100,
            SetUpOptionItem,
            "ベイト",
            "#00f7ff"
        );
    public Bait(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        WaitTime = OptionWaitTime.GetFloat();

        //他視点用のMarkメソッド登録
        CustomRoleManager.MarkOthers.Add(GetMarkOthers);
    }
    private static OptionItem OptionWaitTime;
    enum OptionName
    {
        BaitWaitTime
    }
    private static float WaitTime;
    private static void SetUpOptionItem()
    {
        OptionWaitTime = FloatOptionItem.Create(RoleInfo, 10, OptionName.BaitWaitTime, new(0f, 15f, 1f), 0f, false)
            .SetValueFormat(OptionFormat.Seconds);
    }

    public override void Add()
    {
        BaitKillPlayer = null;
    }
    public override void OnMurderPlayerAsTarget(MurderInfo info)
    {
        var (killer, target) = info.AttemptTuple;
        BaitKillPlayer = killer;
        if (target.Is(CustomRoles.Bait) && !info.IsSuicide)
            _ = new LateTask(() =>
            {
                killer.CmdReportDeadBody(target.Data);
                BaitKillPlayer = null;
            }, 0.15f + WaitTime, "Bait Self Report");
    }

    public static PlayerControl BaitKillPlayer;
    /// <summary>
    /// キラーから見たベイト警告マーク
    /// キラーにはベイトをキルした時の警告マーク
    /// キラーが対象なためstatic実装
    /// </summary>
    /// <param name="seer">キラーの場合有効</param>
    /// <param name="seen">キラー自身またはスニッチの場合有効</param>
    /// <returns></returns>
    public static string GetMarkOthers(PlayerControl seer, PlayerControl seen, bool isForMeeting = false)
    {
        //seenが省略の場合seer
        seen ??= seer;

        //キラーじゃなければ無し
        if (BaitKillPlayer == null) return "";
        if (BaitKillPlayer == seer && seen == seer)
        {
            //キラー自身がseenのとき
            return Utils.ColorString(RoleInfo.RoleColor, "！");
        }
        //その他seenなら無し
        return "";
    }
}