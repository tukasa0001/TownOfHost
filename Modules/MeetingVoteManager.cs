using System;
using System.Linq;
using System.Collections.Generic;
using TownOfHost.Roles.Core;

namespace TownOfHost.Modules;

public class MeetingVoteManager
{
    public IReadOnlyDictionary<byte, VoteData> AllVotes => allVotes;
    private Dictionary<byte, VoteData> allVotes = new(15);
    private readonly MeetingHud meetingHud;

    public static MeetingVoteManager Instance => _instance;
    private static MeetingVoteManager _instance;
    private static LogHandler logger = Logger.Handler(nameof(MeetingVoteManager));

    private MeetingVoteManager()
    {
        meetingHud = MeetingHud.Instance;
        ClearVotes();
    }

    public static void Start()
    {
        _instance = new();
    }

    /// <summary>
    /// 投票を初期状態にします
    /// </summary>
    public void ClearVotes()
    {
        foreach (var voteArea in meetingHud.playerStates)
        {
            allVotes[voteArea.TargetPlayerId] = new(voteArea.TargetPlayerId);
        }
    }
    /// <summary>
    /// 今までに行われた投票をすべて削除し，特定の投票先に1票投じられた状態で会議を強制終了します
    /// </summary>
    /// <param name="voter">投票を行う人</param>
    /// <param name="exiled">追放先</param>
    public void ClearAndExile(byte voter, byte exiled)
    {
        logger.Info($"{Utils.GetPlayerById(voter).GetNameWithRole()} によって {GetVoteName(exiled)} が追放されます");
        ClearVotes();
        var vote = new VoteData(voter);
        vote.DoVote(exiled, 1);
        allVotes[voter] = vote;
        EndMeeting(false);
    }
    /// <summary>
    /// 投票を追加します
    /// </summary>
    /// <param name="voter">投票者</param>
    /// <param name="voteFor">投票先</param>
    /// <param name="numVotes">票数</param>
    public void AddVote(byte voter, byte voteFor, int numVotes = 1)
    {
        if (!allVotes.TryGetValue(voter, out var vote))
        {
            logger.Warn($"ID: {voter}の投票データがありません。新規作成します");
            vote = new(voter);
        }
        if (vote.HasVoted)
        {
            logger.Info($"ID: {voter}の投票を上書きします");
        }

        bool doVote = true;
        foreach (var role in CustomRoleManager.AllActiveRoles.Values)
        {
            var (roleVoteFor, roleNumVotes, roleDoVote) = role.OnVote(voter, voteFor);
            if (roleVoteFor.HasValue)
            {
                logger.Info($"{role.Player.GetNameWithRole()} が {Utils.GetPlayerById(voter).GetNameWithRole()} の投票先を {GetVoteName(roleVoteFor.Value)} に変更します");
                voteFor = roleVoteFor.Value;
            }
            if (roleNumVotes.HasValue)
            {
                logger.Info($"{role.Player.GetNameWithRole()} が {Utils.GetPlayerById(voter).GetNameWithRole()} の投票数を {roleNumVotes.Value} に変更します");
                numVotes = roleNumVotes.Value;
            }
            if (!roleDoVote)
            {
                logger.Info($"{role.Player.GetNameWithRole()} によって投票は取り消されます");
                doVote = roleDoVote;
            }
        }

        if (doVote)
        {
            vote.DoVote(voteFor, numVotes);
        }
    }
    /// <summary>
    /// 議論時間が終わってる or 全員が投票を終えていれば会議を終了します
    /// </summary>
    public void CheckAndEndMeeting()
    {
        if (meetingHud.discussionTimer - (float)Main.NormalOptions.DiscussionTime >= Main.NormalOptions.VotingTime || AllVotes.Values.All(vote => vote.HasVoted))
        {
            EndMeeting();
        }
    }
    /// <summary>
    /// 無条件で会議を終了します
    /// </summary>
    /// <param name="applyVoteMode">スキップと同数投票の設定を適用するかどうか</param>
    public void EndMeeting(bool applyVoteMode = true)
    {
        var result = CountVotes(applyVoteMode);
        var logName = result.Exiled == null ? (result.IsTie ? "同数" : "スキップ") : result.Exiled.Object.GetNameWithRole();
        logger.Info($"追放者: {logName} で会議を終了します");

        var states = new List<MeetingHud.VoterState>();
        foreach (var voteArea in meetingHud.playerStates)
        {
            var voteData = AllVotes.TryGetValue(voteArea.TargetPlayerId, out var value) ? value : null;
            if (voteData == null)
            {
                logger.Warn($"{Utils.GetPlayerById(voteArea.TargetPlayerId).GetNameWithRole()} の投票データがありません");
                continue;
            }
            for (var i = 0; i < voteData.NumVotes; i++)
            {
                states.Add(new()
                {
                    VoterId = voteArea.TargetPlayerId,
                    VotedForId = voteData.VotedFor,
                });
            }
        }

        if (AntiBlackout.OverrideExiledPlayer)
        {
            meetingHud.RpcVotingComplete(states.ToArray(), null, true);
            ExileControllerWrapUpPatch.AntiBlackout_LastExiled = result.Exiled;
        }
        else
        {
            meetingHud.RpcVotingComplete(states.ToArray(), result.Exiled, result.IsTie);
        }
        if (result.Exiled != null)
        {
            MeetingHudPatch.CheckForDeathOnExile(CustomDeathReason.Vote, result.Exiled.PlayerId);
        }
        Destroy();
    }
    /// <summary>
    /// <see cref="AllVotes"/>から投票をカウントします
    /// </summary>
    /// <param name="applyVoteMode">スキップと同数投票の設定を適用するかどうか</param>
    /// <returns>([Key: 投票先,Value: 票数]の辞書, 追放される人, 同数投票かどうか)</returns>
    public VoteResult CountVotes(bool applyVoteMode)
    {
        // 投票モードに従って投票を変更
        if (applyVoteMode && Options.VoteMode.GetBool())
        {
            ApplySkipAndNoVoteMode();
        }

        // Key: 投票された人
        // Value: 票数
        Dictionary<byte, int> votes = new();
        foreach (var voteArea in meetingHud.playerStates)
        {
            votes[voteArea.TargetPlayerId] = 0;
        }
        votes[Skip] = 0;
        foreach (var vote in AllVotes.Values)
        {
            if (vote.VotedFor == NoVote)
            {
                continue;
            }
            votes[vote.VotedFor] += vote.NumVotes;
        }

        return new VoteResult(votes);
    }
    /// <summary>
    /// スキップモードと無投票モードに応じて，投票を変更したりプレイヤーを死亡させたりします
    /// </summary>
    private void ApplySkipAndNoVoteMode()
    {
        var ignoreSkipModeDueToFirstMeeting = MeetingStates.FirstMeeting && Options.WhenSkipVoteIgnoreFirstMeeting.GetBool();
        var ignoreSkipModeDueToNoDeadBody = !MeetingStates.IsExistDeadBody && Options.WhenSkipVoteIgnoreNoDeadBody.GetBool();
        var ignoreSkipModeDueToEmergency = MeetingStates.IsEmergencyMeeting && Options.WhenSkipVoteIgnoreEmergency.GetBool();
        var ignoreSkipMode = ignoreSkipModeDueToFirstMeeting || ignoreSkipModeDueToNoDeadBody || ignoreSkipModeDueToEmergency;

        var skipMode = Options.GetWhenSkipVote();
        var noVoteMode = Options.GetWhenNonVote();
        foreach (var voteData in AllVotes)
        {
            var vote = voteData.Value;
            if (!vote.HasVoted)
            {
                var voterName = Utils.GetPlayerById(vote.Voter).GetNameWithRole();
                switch (noVoteMode)
                {
                    case VoteMode.Suicide:
                        MeetingHudPatch.TryAddAfterMeetingDeathPlayers(CustomDeathReason.Suicide, vote.Voter);
                        logger.Info($"無投票のため {voterName} を自殺させます");
                        break;
                    case VoteMode.SelfVote:
                        vote.ChangeVoteTarget(vote.Voter);
                        logger.Info($"無投票のため {voterName} に自投票させます");
                        break;
                    case VoteMode.Skip:
                        vote.ChangeVoteTarget(Skip);
                        logger.Info($"無投票のため {voterName} にスキップさせます");
                        break;
                }
            }
            else if (!ignoreSkipMode && vote.IsSkip)
            {
                var voterName = Utils.GetPlayerById(vote.Voter).GetNameWithRole();
                switch (skipMode)
                {
                    case VoteMode.Suicide:
                        MeetingHudPatch.TryAddAfterMeetingDeathPlayers(CustomDeathReason.Suicide, vote.Voter);
                        logger.Info($"スキップしたため {voterName} を自殺させます");
                        break;
                    case VoteMode.SelfVote:
                        vote.ChangeVoteTarget(vote.Voter);
                        logger.Info($"スキップしたため {voterName} に自投票させます");
                        break;
                }
            }
        }
    }
    public void Destroy()
    {
        _instance = null;
    }

    public static string GetVoteName(byte num)
    {
        string name = "invalid";
        var player = Utils.GetPlayerById(num);
        if (num < 15 && player != null) name = player?.GetNameWithRole();
        else if (num == Skip) name = "Skip";
        else if (num == NoVote) name = "None";
        else if (num == 255) name = "Dead";
        return name;
    }

    public class VoteData
    {
        public byte Voter { get; private set; } = byte.MaxValue;
        public byte VotedFor { get; private set; } = NoVote;
        public int NumVotes { get; private set; } = 1;
        public bool IsSkip => VotedFor == Skip && !PlayerState.GetByPlayerId(Voter).IsDead;
        public bool HasVoted => VotedFor != NoVote || PlayerState.GetByPlayerId(Voter).IsDead;

        public VoteData(byte voter) => Voter = voter;

        public void DoVote(byte voteTo, int numVotes)
        {
            logger.Info($"投票: {Utils.GetPlayerById(Voter).GetNameWithRole()} => {GetVoteName(voteTo)} x {numVotes}");
            VotedFor = voteTo;
            NumVotes = numVotes;
        }
        public void ChangeVoteTarget(byte voteTarget)
        {
            logger.Info($"{Utils.GetPlayerById(Voter).GetNameWithRole()}の投票を{GetVoteName(VotedFor)}から{GetVoteName(voteTarget)}に変更");
            VotedFor = voteTarget;
        }
    }

    public readonly struct VoteResult
    {
        /// <summary>
        /// Key: 投票された人<br/>
        /// Value: 得票数
        /// </summary>
        public IReadOnlyDictionary<byte, int> VotedCounts => votedCounts;
        private readonly Dictionary<byte, int> votedCounts;
        /// <summary>
        /// 追放されるプレイヤー
        /// </summary>
        public readonly GameData.PlayerInfo Exiled;
        /// <summary>
        /// 同数投票かどうか
        /// </summary>
        public readonly bool IsTie;

        public VoteResult(Dictionary<byte, int> votedCounts)
        {
            this.votedCounts = votedCounts;

            // 票数順に整列された投票
            var orderedVotes = votedCounts.OrderByDescending(vote => vote.Value);
            // 最も票を得た人の票数
            var maxVoteNum = orderedVotes.FirstOrDefault().Value;
            // 最多票数のプレイヤー全員
            var mostVotedPlayers = votedCounts.Where(vote => vote.Value == maxVoteNum).Select(vote => vote.Key).ToArray();

            // 最多票数のプレイヤーが複数人いる場合
            if (mostVotedPlayers.Length > 1)
            {
                IsTie = true;
                Exiled = null;
                logger.Info($"{string.Join(',', mostVotedPlayers.Select(id => GetVoteName(id)))} が同数");
            }
            else
            {
                IsTie = false;
                Exiled = GameData.Instance.GetPlayerById(mostVotedPlayers[0]);
                logger.Info($"最多得票者: {GetVoteName(mostVotedPlayers[0])}");
            }

            // 同数投票時の特殊モード
            if (IsTie && Options.VoteMode.GetBool())
            {
                var tieMode = (TieMode)Options.WhenTie.GetValue();
                switch (tieMode)
                {
                    case TieMode.All:
                        var toExile = mostVotedPlayers.Where(id => id != Skip).ToArray();
                        foreach (var playerId in toExile)
                        {
                            Utils.GetPlayerById(playerId)?.SetRealKiller(null);
                        }
                        MeetingHudPatch.TryAddAfterMeetingDeathPlayers(CustomDeathReason.Vote, toExile);
                        Exiled = null;
                        logger.Info("全員追放します");
                        break;
                    case TieMode.Random:
                        var exileId = mostVotedPlayers.OrderBy(_ => Guid.NewGuid()).FirstOrDefault();
                        Exiled = GameData.Instance.GetPlayerById(exileId);
                        IsTie = false;
                        logger.Info($"ランダム追放: {GetVoteName(exileId)}");
                        break;
                }
            }
        }
    }

    public const byte Skip = 253;
    public const byte NoVote = 254;
}
