using System.Collections.Generic;
using AmongUs.GameOptions;

using TownOfHostForE.Modules;
using TownOfHostForE.Roles.Core;

namespace TownOfHostForE.Roles.Madmate;
public sealed class MadNimrod : RoleBase
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(MadNimrod),
            player => new MadNimrod(player),
            CustomRoles.MadNimrod,
            () => OptionCanVent.GetBool() ? RoleTypes.Engineer : RoleTypes.Crewmate,
            CustomRoleTypes.Madmate,
            5700,
            SetupOptionItem,
            "マッドニムロッド",
            introSound: () => GetIntroSound(RoleTypes.Impostor)
        );
    public MadNimrod(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        playerIdList = new();
        IsExecutionMeeting = byte.MaxValue;
        GuessManager.NotGueesFlag = false;
    }
    public override void OnDestroy()
    {
        playerIdList.Clear();
    }

    private static OptionItem OptionCanVent;

    public static List<byte> playerIdList = new();
    private static byte IsExecutionMeeting = byte.MaxValue;

    public static void SetupOptionItem()
    {
        OptionCanVent = BooleanOptionItem.Create(RoleInfo, 10, GeneralOption.CanVent, false, false);
        Options.SetUpAddOnOptions(RoleInfo.ConfigId + 20, RoleInfo.RoleName, RoleInfo.Tab);
    }
    public override void Add()
    {
        playerIdList.Add(Player.PlayerId);
    }

    public static GameData.PlayerInfo VoteChange(GameData.PlayerInfo Exiled)
    {
        if (Exiled == null || !playerIdList.Contains(Exiled.PlayerId)) return Exiled;

        _ = new LateTask(() =>
        {
            Utils.GetPlayerById(Exiled.PlayerId).NoCheckStartMeeting(Exiled);
            IsExecutionMeeting = Exiled.PlayerId;
            GuessManager.NotGueesFlag = true;
            playerIdList.Remove(Exiled.PlayerId);
        }, 15f, "NimrodExiled");
        return null;
    }
    public override void OnStartMeeting()
    {
        if (IsExecutionMeeting == byte.MaxValue) return;

        Utils.SendMessage(Translator.GetString("IsNimrodMeetingText"),
            title: $"<color={RoleInfo.RoleColorCode}>{Translator.GetString("IsNimrodMeetingTitle")}</color>");
    }

    public override (byte? votedForId, int? numVotes, bool doVote) ModifyVote(byte voterId, byte sourceVotedForId, bool isIntentional)
    {
        var (votedForId, numVotes, doVote) = base.ModifyVote(voterId, sourceVotedForId, isIntentional);
        var baseVote = (votedForId, numVotes, doVote);
        if (IsExecutionMeeting != Player.PlayerId || voterId != Player.PlayerId)
        {
            return baseVote;
        }
        IsExecutionMeeting = byte.MaxValue;
        GuessManager.NotGueesFlag = false;
        MeetingHudPatch.TryAddAfterMeetingDeathPlayers(CustomDeathReason.Vote, Player.PlayerId);

        if (sourceVotedForId <= 15)
        {
            Utils.GetPlayerById(sourceVotedForId).SetRealKiller(Player);
            PlayerState.GetByPlayerId(sourceVotedForId).DeathReason = CustomDeathReason.Execution;
        }
        MeetingVoteManager.Instance.ClearAndExile(Player.PlayerId, sourceVotedForId);
        return (votedForId, numVotes, false);
    }
}