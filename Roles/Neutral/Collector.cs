using Hazel;
using System.Collections.Generic;
using System.Linq;

namespace TOHE.Roles.Neutral;

public static class Collector
{
    private static readonly int Id = 5051123;
    public static OptionItem CollectorCollectAmount;
    private static List<byte> playerIdList = new();
    public static Dictionary<byte, byte> CollectorVoteFor = new();
    public static Dictionary<byte, int> CollectVote = new();
    public static Dictionary<byte, int> NewVote = new();
    public static void SetupCustomOption()
    {
        Options.SetupRoleOptions(Id, TabGroup.NeutralRoles, CustomRoles.Collector);
        CollectorCollectAmount = IntegerOptionItem.Create(Id + 13, "CollectorCollectAmount", new(1, 999, 1), 20, TabGroup.NeutralRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Collector])
            .SetValueFormat(OptionFormat.Votes);
    }
    public static void Init()
    {
        playerIdList = new();
        CollectorVoteFor = new();
        CollectVote = new();
    }
    public static void Add(byte playerId)
    {
        playerIdList.Add(playerId);
        CollectVote.TryAdd(playerId, 0);
    }
    public static bool IsEnable => playerIdList.Count > 0;
    private static void SendRPC(byte playerId)
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetCollectorVotes, SendOption.Reliable, -1);
        writer.Write(playerId);
        writer.Write(CollectVote[playerId]);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public static void ReceiveRPC(MessageReader reader)
    {
        byte PlayerId = reader.ReadByte();
        int Num = reader.ReadInt32();
        CollectVote.TryAdd(PlayerId, 0);
        CollectVote[PlayerId] = Num;
    }
    public static string GetProgressText(byte playerId)
    {
        int VoteAmount = CollectVote[playerId];
        int CollectNum = CollectorCollectAmount.GetInt();
        return Utils.ColorString(Utils.GetRoleColor(CustomRoles.Collector).ShadeColor(0.25f), $"({VoteAmount}/{CollectNum})");
    }
    public static bool CollectorWin(bool check = true)
    {
        var pc = Main.AllPlayerControls.Where(x => x.Is(CustomRoles.Collector) && x.IsAlive() && CollectDone(x));
        if (pc.Count() > 0)
        {
            if (check) return true;
            CustomWinnerHolder.ResetAndSetWinner(CustomWinner.Collector);
            foreach (var winner in pc) CustomWinnerHolder.WinnerIds.Add(winner.PlayerId);
            return true;
        }
        return false;
    }
    public static bool CollectDone(PlayerControl player)
    {
        if (player.Is(CustomRoles.Collector))
        {
            var pcid = player.PlayerId;
            int VoteAmount = CollectVote[pcid];
            int CollectNum = CollectorCollectAmount.GetInt();
            if (VoteAmount >= CollectNum) return true;
        }
        return false;
    }
    public static void CollectorVotes(PlayerControl target, PlayerVoteArea ps)//集票者投票给谁
    {
        if (CheckForEndVotingPatch.CheckRole(ps.TargetPlayerId, CustomRoles.Collector))
            CollectorVoteFor.TryAdd(target.PlayerId, ps.TargetPlayerId);
    }
    public static void CollectAmount(Dictionary<byte, int> VotingData, MeetingHud __instance)//得到集票者收集到的票
    {
        int VoteAmount;
        foreach (var pva in __instance.playerStates)
        {
            if (pva == null) continue;
            PlayerControl pc = Utils.GetPlayerById(pva.TargetPlayerId);
            if (pc == null) continue;
            foreach (var data in VotingData)
                if (CollectorVoteFor.ContainsKey(data.Key) && pc.PlayerId == CollectorVoteFor[data.Key] && pc.Is(CustomRoles.Collector))
                {
                    VoteAmount = data.Value;
                    CollectVote.TryAdd(pc.PlayerId, 0);
                    CollectVote[pc.PlayerId] = CollectVote[pc.PlayerId] + VoteAmount;
                    SendRPC(pc.PlayerId);
                }
        }
    }
}
