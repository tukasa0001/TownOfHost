using System.Collections.Generic;
using AmongUs.GameOptions;
using Hazel;
using TownOfHostForE.Roles.Core;
using TownOfHostForE.Roles.Core.Interfaces;
using static TownOfHostForE.Roles.Neutral.Oniichan;
using static UnityEngine.GraphicsBuffer;

namespace TownOfHostForE.Roles.Neutral;
public sealed class Duelist : RoleBase, IAdditionalWinner
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(Duelist),
            player => new Duelist(player),
            CustomRoles.Duelist,
            () => RoleTypes.Crewmate,
            CustomRoleTypes.Neutral,
            60700,
            null,
            "決闘者",
            "#ff6347"
        );
    public Duelist(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        CustomRoleManager.MarkOthers.Add(GetMarkOthers);
        Duelists.Add(this);
        Archenemy = null;
    }
    public override void OnDestroy()
    {
        Duelists.Remove(this);
        CustomRoleManager.MarkOthers.Remove(GetMarkOthers);
    }

    private static HashSet<Duelist> Duelists = new(15);
    PlayerControl Archenemy;

    public override (byte? votedForId, int? numVotes, bool doVote) ModifyVote(byte voterId, byte sourceVotedForId, bool isIntentional)
    {
        var (votedForId, numVotes, doVote) = base.ModifyVote(voterId, sourceVotedForId, isIntentional);
        if (MeetingStates.FirstMeeting && voterId == Player.PlayerId && Player.IsAlive())
        {
            if (sourceVotedForId != Player.PlayerId && sourceVotedForId < 253)
            {
                numVotes = 0;//投票を見えなくする
                var VotedForPC = Utils.GetPlayerById(sourceVotedForId);
                VotedForPC.RpcSetCustomRole(CustomRoles.Archenemy);
                Archenemy = VotedForPC;
                SendRPC(VotedForPC.PlayerId);
                Utils.NotifyRoles();
            }
            else
            {
                MeetingHudPatch.TryAddAfterMeetingDeathPlayers(CustomDeathReason.Suicide, Player.PlayerId);
            }
        }
        return (votedForId, numVotes, doVote);
    }


    private void SendRPC(byte targetId)
    {
        if (!AmongUsClient.Instance.AmHost) return;
        using var sender = CreateSender(CustomRPC.SetDuelistTarget);
        sender.Writer.Write(targetId);
    }

    public override void ReceiveRPC(MessageReader reader, CustomRPC rpcType)
    {
        if (rpcType != CustomRPC.SetDuelistTarget) return;

        var targetId = reader.ReadByte();
        Archenemy = Utils.GetPlayerById(targetId);
    }

    public override string GetMark(PlayerControl seer, PlayerControl seen, bool _ = false)
    {
        //seenが省略の場合seer
        seen ??= seer;

        if (seer == Player && seen == Archenemy)
            return Utils.ColorString(RoleInfo.RoleColor, "χ");
        return string.Empty;
    }
    public string GetMarkOthers(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false)
    {
        seen ??= seer;

        if (seer == Archenemy && seen == Player)
            return Utils.ColorString(RoleInfo.RoleColor, "χ");
        return string.Empty;
    }

    public bool CheckWin(ref CustomRoles winnerRole)
    {
        return Player.IsAlive() && !Archenemy.IsAlive();
    }

    public static bool ArchenemyCheckWin(PlayerControl pc)
    {
        foreach (var duelist in Duelists)
        {
            if (pc == duelist.Archenemy && !duelist.Player.IsAlive() && pc.IsAlive()) return true;
        }
        return false;
    }
    public static bool CheckNotify(PlayerControl pc)
    {
        foreach (var duelist in Duelists)
        {
            if (pc == duelist.Archenemy || pc == duelist.Player) return true;
        }
        return false;
    }
}