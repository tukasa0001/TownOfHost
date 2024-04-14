using AmongUs.GameOptions;
using TownOfHostForE.Modules;
using TownOfHostForE.Roles.Core;
using Unity.Services.Core.Telemetry.Internal;

namespace TownOfHostForE.Roles.Crewmate;
public sealed class Mayor : RoleBase
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(Mayor),
            player => new Mayor(player),
            CustomRoles.Mayor,
            () => OptionHasPortableButton.GetBool() ? RoleTypes.Engineer : RoleTypes.Crewmate,
            CustomRoleTypes.Crewmate,
            30300,
            SetupOptionItem,
            "メイヤー",
            "#204d42",
            introSound: () => GetIntroSound(RoleTypes.Crewmate)
        );
    public Mayor(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        AdditionalVote = OptionAdditionalVote.GetInt();
        HasPortableButton = OptionHasPortableButton.GetBool();
        NumOfUseButton = OptionNumOfUseButton.GetInt();
        SkipPlusVote = OptionSkipPlusVote.GetBool();

        LeftButtonCount = NumOfUseButton;
    }

    private static OptionItem OptionAdditionalVote;
    private static OptionItem OptionHasPortableButton;
    private static OptionItem OptionNumOfUseButton;
    private static OptionItem OptionSkipPlusVote;
    enum OptionName
    {
        MayorAdditionalVote,
        MayorHasPortableButton,
        MayorNumOfUseButton,
        MayorSkipPlusVote,
    }
    public static int AdditionalVote;
    public static bool HasPortableButton;
    public static int NumOfUseButton;
    private static bool SkipPlusVote = false;

    private int TotalVote;

    public int LeftButtonCount;
    private static void SetupOptionItem()
    {
        OptionAdditionalVote = IntegerOptionItem.Create(RoleInfo, 10, OptionName.MayorAdditionalVote, new(1, 99, 1), 1, false)
            .SetValueFormat(OptionFormat.Votes);
        OptionHasPortableButton = BooleanOptionItem.Create(RoleInfo, 11, OptionName.MayorHasPortableButton, false, false);
        OptionNumOfUseButton = IntegerOptionItem.Create(RoleInfo, 12, OptionName.MayorNumOfUseButton, new(1, 99, 1), 1, false, OptionHasPortableButton)
            .SetValueFormat(OptionFormat.Times);
        OptionSkipPlusVote = BooleanOptionItem.Create(RoleInfo, 13, OptionName.MayorSkipPlusVote, false, false);
    }
    public override void Add()
    {
        TotalVote = 1;
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
        if (Is(reporter) && target == null) //ボタン
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
    public override string GetProgressText(bool comms = false) =>　SkipPlusVote ? Utils.ColorString(UnityEngine.Color.yellow,$"({TotalVote})") : "";

    public override (byte? votedForId, int? numVotes, bool doVote) ModifyVote(byte voterId, byte sourceVotedForId, bool isIntentional)
    {
        // 既定値
        var (votedForId, numVotes, doVote) = base.ModifyVote(voterId, sourceVotedForId, isIntentional);
        if (voterId == Player.PlayerId)
        {
            if (SkipPlusVote)
            {
                //skip
                if (sourceVotedForId == MeetingVoteManager.Skip)
                {
                    TotalVote += AdditionalVote;
                    numVotes = 1;
                }
                //無投票は無視
                else if (sourceVotedForId == MeetingVoteManager.NoVote)
                { }
                //通常投票
                else
                {
                    numVotes = TotalVote;
                    //基礎投票数リセット
                    TotalVote = 1;
                }
            }
            else
            {
                numVotes = AdditionalVote + 1;
            }

        }
        return (votedForId, numVotes, doVote);
    }
    public override void AfterMeetingTasks()
    {
        if (HasPortableButton)
            Player.RpcResetAbilityCooldown();
    }
}