using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using AmongUs.GameOptions;
using TownOfHostForE.Roles.Core;
using TownOfHostForE.Roles.Core.Interfaces;

using static TownOfHostForE.Utils;
using static TownOfHostForE.Translator;
using Hazel;
using static UnityEngine.GraphicsBuffer;

namespace TownOfHostForE.Roles.Crewmate;
public sealed class FortuneTeller : RoleBase
{
    public static readonly SimpleRoleInfo RoleInfo =
         SimpleRoleInfo.Create(
            typeof(FortuneTeller),
            player => new FortuneTeller(player),
            CustomRoles.FortuneTeller,
            () => RoleTypes.Crewmate,
            CustomRoleTypes.Crewmate,
            41100,
            SetupOptionItem,
            "占い師",
            "#9370db"
        );
    public FortuneTeller(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        NumOfForecast = OptionNumOfForecast.GetInt();
        ForecastTaskTrigger = OptionForecastTaskTrigger.GetInt();
        CanForecastNoDeadBody = OptionCanForecastNoDeadBody.GetBool();
        ConfirmCamp = OptionConfirmCamp.GetBool();
        KillerOnly = OptionKillerOnly.GetBool();

        ForecastTarget = null;
        ForecastResult = new();
    }

    private static OptionItem OptionNumOfForecast;
    private static OptionItem OptionForecastTaskTrigger;
    private static OptionItem OptionCanForecastNoDeadBody;
    private static OptionItem OptionConfirmCamp;
    private static OptionItem OptionKillerOnly;

    private enum TargetOperation : byte
    {
        /// <summary>
        /// ターゲットを設定する
        /// </summary>
        SetTarget,
        /// <summary>
        /// レポートがはじまる
        /// </summary>
        nowOnReport,
    }
    enum OptionName
    {
        FortuneTellerNumOfForecast,
        FortuneTellerForecastTaskTrigger,
        FortuneTellerCanForecastNoDeadBody,
        FortuneTellerConfirmCamp,
        FortuneTellerKillerOnly,
    }

    private static int NumOfForecast;
    private static int ForecastTaskTrigger;
    private static bool CanForecastNoDeadBody;
    private static bool ConfirmCamp;
    private static bool KillerOnly;

    private PlayerControl ForecastTarget = null;
    private Dictionary<byte, PlayerControl> ForecastResult = new();

    private static void SetupOptionItem()
    {
        OptionNumOfForecast = IntegerOptionItem.Create(RoleInfo, 10, OptionName.FortuneTellerNumOfForecast, new(1, 20, 1), 2, false)
            .SetValueFormat(OptionFormat.Times);
        OptionForecastTaskTrigger = IntegerOptionItem.Create(RoleInfo, 11, OptionName.FortuneTellerForecastTaskTrigger, new(0, 20, 1), 5, false)
            .SetValueFormat(OptionFormat.Pieces);
        OptionCanForecastNoDeadBody = BooleanOptionItem.Create(RoleInfo, 12, OptionName.FortuneTellerCanForecastNoDeadBody, false, false);
        OptionConfirmCamp = BooleanOptionItem.Create(RoleInfo, 13, OptionName.FortuneTellerConfirmCamp, true, false);
        OptionKillerOnly = BooleanOptionItem.Create(RoleInfo, 14, OptionName.FortuneTellerKillerOnly, true, false);
    }

    public override (byte? votedForId, int? numVotes, bool doVote) ModifyVote(byte voterId, byte sourceVotedForId, bool isIntentional)
    {
        var baseVote = base.ModifyVote(voterId, sourceVotedForId, isIntentional);
        if (voterId == Player.PlayerId && sourceVotedForId != Player.PlayerId && sourceVotedForId < 253 && Player.IsAlive())
        {
            VoteForecastTarget(sourceVotedForId);
        }
        return baseVote;
    }

    private void VoteForecastTarget(byte targetId)
    {
        if (!CanForecastNoDeadBody &&
            !GameData.Instance.AllPlayers.ToArray().Any(x => x.IsDead)) //死体無し
        {
            Logger.Info($"VoteForecastTarget NotForecast NoDeadBody player: {Player.name}, targetId: {targetId}", "FortuneTeller");
            return;
        }
        if (MyTaskState.CompletedTasksCount < ForecastTaskTrigger) //占い可能タスク数
        {
            Logger.Info($"VoteForecastTarget NotForecast LessTasks player: {Player.name}, targetId: {targetId}, task: {MyTaskState.CompletedTasksCount}/{ForecastTaskTrigger}", "FortuneTeller");
            return;
        }

        SendRPC(targetId);
        var target = GetPlayerById(targetId);
        if (target == null || !target.IsAlive()) return;
        if (ForecastResult.ContainsKey(targetId)) return;  //既に占い結果があるときはターゲットにならない

        ForecastTarget = target;
        Logger.Info($"SetForecastTarget player: {Player.name}, target: {ForecastTarget.name}", "FortuneTeller");
    }
    private void SendRPC(byte targetId)
    {
        if (!AmongUsClient.Instance.AmHost) return;
        using var sender = CreateSender(CustomRPC.SetFortuneTellerTarget);
        sender.Writer.Write((byte)TargetOperation.SetTarget);
        sender.Writer.Write(targetId);
    }
    private void SendReportRPC()
    {
        if (!AmongUsClient.Instance.AmHost) return;
        using var sender = CreateSender(CustomRPC.SetFortuneTellerTarget);
        sender.Writer.Write((byte)TargetOperation.nowOnReport);
    }
    public override void ReceiveRPC(MessageReader reader, CustomRPC rpcType)
    {
        if (rpcType != CustomRPC.SetFortuneTellerTarget) return;

        var operation = (TargetOperation)reader.ReadByte();
        if (operation == TargetOperation.SetTarget)
        {
            var targetId = reader.ReadByte();
            var target = GetPlayerById(targetId);
            if (target == null || !target.IsAlive()) return;
            if (ForecastResult.ContainsKey(targetId)) return;  //既に占い結果があるときはターゲットにならない

            ForecastTarget = target;
            Logger.Info($"SetForecastTarget player: {Player.name}, target: {ForecastTarget.name}", "FortuneTeller");
        }
        else if (operation == TargetOperation.nowOnReport)
        {
            SetForecastResult();
        }
    }
    public override bool OnReportDeadBody(PlayerControl reporter, GameData.PlayerInfo target)
    {
        SendReportRPC();
        SetForecastResult();
        return true;
    }
    private void SetForecastResult()
    {
        if (ForecastTarget == null) return;
        if (!ForecastTarget.IsAlive() || ForecastResult.Count >= NumOfForecast)
        {
            Logger.Info($"SetForecastResult NotSet player: {Player?.name}, target: {ForecastTarget?.name} dead: {ForecastTarget?.Data.IsDead}, disconnected: {ForecastTarget?.Data.Disconnected}, canCount: {NumOfForecast}", "FortuneTeller");
            ForecastTarget = null;
            return;
        }

        ForecastResult[ForecastTarget.PlayerId] = ForecastTarget;
        var canSeeRole = ForecastTarget.GetRoleClass() is IKiller;
        if (canSeeRole && Player != null)
            NameColorManager.Add(Player.PlayerId, ForecastTarget.PlayerId);
        Logger.Info($"SetForecastResult SetTarget player: {Player?.name}, target: {ForecastTarget.name}, canSeeRole: {canSeeRole}", "FortuneTeller");

        ForecastTarget = null;
    }
    public bool HasForecastResult() => ForecastResult.Count > 0;
    private int ForecastLimit => NumOfForecast - ForecastResult.Count;
    public override string GetProgressText(bool comms = false)
    {
        if (MyTaskState.CompletedTasksCount < ForecastTaskTrigger) return string.Empty;

        return ColorString(ForecastLimit > 0 ? RoleInfo.RoleColor : Color.gray, $"({ForecastLimit})");
    }
    public override string GetMark(PlayerControl seer, PlayerControl seen, bool isForMeeting = false)
    {
        if (seen == null || !isForMeeting) return string.Empty;
        return ForecastResult.ContainsKey(seen.PlayerId) ? Utils.ColorString(RoleInfo.RoleColor, "★") : string.Empty;
    }
    public override void OverrideDisplayRoleNameAsSeer(PlayerControl seen,bool isMeeting, ref bool enabled, ref Color roleColor, ref string roleText)
    {
        if (!isMeeting) return;
        if (!ForecastResult.ContainsKey(seen.PlayerId)) return;
        if (KillerOnly &&
            !(seen.GetCustomRole().IsImpostor() || seen.IsNeutralKiller() || seen.IsCrewKiller() || seen.IsAnimalsKiller()
            || seen.Is(CustomRoles.MadSheriff) || seen.Is(CustomRoles.GrudgeSheriff))) return;

        enabled = true;

        if (!ConfirmCamp) return;   //役職表示

        //陣営表示
        if (seen.GetCustomRole().IsImpostor() || seen.GetCustomRole().IsMadmate())
        {
            roleColor = Palette.ImpostorRed;
            roleText = GetString("TeamImpostor");
        }
        else if (seen.GetCustomRole().IsNeutral())
        {
            roleColor = Color.gray;
            roleText = GetString("Neutral");
        }
        else if (seen.GetCustomRole().IsAnimals())
        {
            string colorString = "#FF8C00";
            Color AnimalsColor;
            ColorUtility.TryParseHtmlString(colorString, out AnimalsColor);
            roleColor = AnimalsColor;
            roleText = GetString("Animals");
        }
        else
        {
            roleColor = Palette.CrewmateBlue;
            roleText = GetString("TeamCrewmate");
        }
    }
    public static bool KnowTargetRoleColor(PlayerControl seer, PlayerControl target, bool isMeeting)
    {
        if (seer == null) return false;
        if (!seer.Is(CustomRoles.FortuneTeller)) return false;
        return ((FortuneTeller)seer.GetRoleClass()).KnowTargetRoleColor(target, isMeeting);
    }
    private bool KnowTargetRoleColor(PlayerControl target, bool isMeeting)
    {
        if (!isMeeting) return false;
        if (!ForecastResult.ContainsKey(target.PlayerId)) return false;
        if (ConfirmCamp) return false;
        if (KillerOnly &&
            !(target.GetCustomRole().IsImpostor() || target.IsNeutralKiller() || target.IsCrewKiller() || target.IsAnimalsKiller())) return false;
        return true;
    }
}