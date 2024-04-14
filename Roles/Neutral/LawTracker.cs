using System.Collections.Generic;
using System.Linq;
using Hazel;
using AmongUs.GameOptions;
using UnityEngine;

using TownOfHostForE.Roles.Core;
namespace TownOfHostForE.Roles.Neutral;
public sealed class LawTracker : RoleBase
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(LawTracker),
            player => new LawTracker(player),
            CustomRoles.LawTracker,
            () => RoleTypes.Crewmate,
            CustomRoleTypes.Neutral,
            60520,
            null,
            "追跡者",
            "#daa520",
            introSound: () => GetIntroSound(RoleTypes.Impostor)
        );
    public LawTracker(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        HasImpostorVision = Lawyer.OptionHasImpostorVision.GetBool();
        PursuerGuardNum = Lawyer.OptionPursuerGuardNum.GetInt();
    }

    enum OptionName
    {
        LawyerTargetKnows,
        LawyerKnowTargetRole,
        PursuerGuardNum
    }
    private static bool HasImpostorVision;
    private static int PursuerGuardNum;

    private int GuardCount = 0;

    public override void Add()
    {
        GuardCount = PursuerGuardNum;
    }
    public override void ApplyGameOptions(IGameOptions opt) => opt.SetVision(HasImpostorVision);

    public override bool OnCheckMurderAsTarget(MurderInfo info)
    {
        (var killer, var target) = info.AttemptTuple;
        // 直接キル出来る役職チェック
        if (killer.GetCustomRole().IsDirectKillRole()) return true;
        if (GuardCount > 0)
        {
            killer.RpcProtectedMurderPlayer(target);
            target.RpcProtectedMurderPlayer(target);
            GuardCount--;
            SendRPC();
            NameColorManager.Add(killer.PlayerId, target.PlayerId, RoleInfo.RoleColorCode);
            Utils.NotifyRoles(SpecifySeer: target);
            info.CanKill = false;
        }
        else
        {
            info.CanKill = true;
        }
        return true;
    }
    public override string GetProgressText(bool comms = false)
    => Utils.ColorString(RoleInfo.RoleColor, $"({GuardCount})");

    private void SendRPC()
    {
        if (!AmongUsClient.Instance.AmHost) return;
        using var sender = CreateSender(CustomRPC.LawTrackerSync);
        sender.Writer.Write(GuardCount);
    }
    public override void ReceiveRPC(MessageReader reader, CustomRPC rpcType)
    {
        if (rpcType != CustomRPC.LawTrackerSync) return;

        GuardCount = reader.ReadInt32();
    }

}