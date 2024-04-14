using AmongUs.GameOptions;

using TownOfHostForE.Modules;
using TownOfHostForE.Roles.Core;

namespace TownOfHostForE.Roles.Crewmate;
public sealed class Chairman : RoleBase
{
    public static readonly SimpleRoleInfo RoleInfo =
         SimpleRoleInfo.Create(
            typeof(Chairman),
            player => new Chairman(player),
            CustomRoles.Chairman,
            () => RoleTypes.Engineer,
            CustomRoleTypes.Crewmate,
            40300,
            SetupOptionItem,
            "チェアマン",
            "#204d42",
            introSound: () => GetIntroSound(RoleTypes.Crewmate)
        );
    public Chairman(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        NumOfUseButton = OptionNumOfUseButton.GetInt();
        IgnoreSkip = OptionIgnoreSkip.GetBool();

        LeftButtonCount = NumOfUseButton;
    }

    private static OptionItem OptionNumOfUseButton;
    private static OptionItem OptionIgnoreSkip;
    enum OptionName
    {
        MayorNumOfUseButton,
        ChairmanIgnoreSkip,
    }
    public static int NumOfUseButton;
    public static bool IgnoreSkip;

    public int LeftButtonCount;
    private static void SetupOptionItem()
    {
        OptionNumOfUseButton = IntegerOptionItem.Create(RoleInfo, 10, OptionName.MayorNumOfUseButton, new(1, 20, 1), 2, false)
            .SetValueFormat(OptionFormat.Times);
        OptionIgnoreSkip = BooleanOptionItem.Create(RoleInfo, 11, OptionName.ChairmanIgnoreSkip, false, false);
    }
    public override void ApplyGameOptions(IGameOptions opt)
    {
        Logger.Warn($"{LeftButtonCount} <= 0", "Mayor.ApplyGameOptions");
        AURoleOptions.EngineerCooldown =
            LeftButtonCount <= 0
            ? 255f
            : opt.GetInt(Int32OptionNames.EmergencyCooldown);
        AURoleOptions.EngineerInVentMaxTime = 1;
    }
    public override bool OnReportDeadBody(PlayerControl reporter, GameData.PlayerInfo target)
    {
        if (reporter == Player && target == null) //ボタン
            LeftButtonCount--;
        return true;
    }
    public override bool OnEnterVent(PlayerPhysics physics, int ventId)
    {
        if (LeftButtonCount > 0)
        {
            var user = physics.myPlayer;
            physics.RpcBootFromVent(ventId);
            user?.ReportDeadBody(null);
        }

        return false;
    }

    public override (byte? votedForId, int? numVotes, bool doVote) ModifyVote(byte voterId, byte sourceVotedForId, bool isIntentional)
    {
        var (votedForId, numVotes, doVote) = base.ModifyVote(voterId, sourceVotedForId, isIntentional);
        var baseVote = (votedForId, numVotes, doVote);
        if (!isIntentional || voterId != Player.PlayerId || sourceVotedForId == Player.PlayerId || sourceVotedForId >= 253 || !Player.IsAlive())
        {
            return baseVote;
        }
        MeetingVoteManager.Instance.ClearAndExile(Player.PlayerId, 253);
        return (votedForId, numVotes, false);
    }

    public override void AfterMeetingTasks()=> Player.RpcResetAbilityCooldown();
}