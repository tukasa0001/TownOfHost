using System.Collections.Generic;
using System.Linq;
using Hazel;
using AmongUs.GameOptions;
using UnityEngine;

using TownOfHostForE.Roles.Core;

namespace TownOfHostForE.Roles.Neutral;
public sealed class Lawyer : RoleBase
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(Lawyer),
            player => new Lawyer(player),
            CustomRoles.Lawyer,
            () => RoleTypes.Crewmate,
            CustomRoleTypes.Neutral,
            60500,
            SetupOptionItem,
            "弁護士",
            "#daa520",
            countType: CountTypes.Crew,
            introSound: () => GetIntroSound(RoleTypes.Impostor)
        );
    public Lawyer(PlayerControl player)
    : base(
        RoleInfo,
        player,
        () => HasTask.False
    )
    {
        HasImpostorVision = OptionHasImpostorVision.GetBool();
        KnowTargetRole = OptionKnowTargetRole.GetBool();
        TargetKnows = OptionTargetKnows.GetBool();

        CustomRoleManager.MarkOthers.Add(GetMarkOthers);
        CustomRoleManager.OnMurderPlayerOthers.Add(OnMurderPlayerOthers);

        TargetId = byte.MaxValue;
        TargetIdPair.Clear();

        Lawyers.Add(this);
    }

    public static OptionItem OptionHasImpostorVision;
    private static OptionItem OptionKnowTargetRole;
    private static OptionItem OptionTargetKnows;
    public static OptionItem OptionPursuerGuardNum;

    public static HashSet<Lawyer> Lawyers = new(15);
    enum OptionName
    {
        LawyerTargetKnows,
        LawyerKnowTargetRole,
        PursuerGuardNum
    }
    private static bool HasImpostorVision;
    private static bool KnowTargetRole;
    private static bool TargetKnows;

    public byte TargetId;
    public static Dictionary<byte, byte> TargetIdPair = new();

    private static void SetupOptionItem()
    {
        OptionHasImpostorVision = BooleanOptionItem.Create(RoleInfo, 10, GeneralOption.ImpostorVision, false, false);
        OptionTargetKnows = BooleanOptionItem.Create(RoleInfo, 11, OptionName.LawyerTargetKnows, false, false);
        OptionKnowTargetRole = BooleanOptionItem.Create(RoleInfo, 12, OptionName.LawyerKnowTargetRole, false, false);
        OptionPursuerGuardNum = IntegerOptionItem.Create(RoleInfo, 13, OptionName.PursuerGuardNum, new(0, 20, 1), 1, false)
            .SetValueFormat(OptionFormat.Times);
    }
    public override void OnDestroy()
    {
        Lawyers.Remove(this);

        if (Lawyers.Count <= 0)
        {
            CustomRoleManager.OnMurderPlayerOthers.Remove(OnMurderPlayerOthers);
        }
    }

    public override void Add()
    {
        //ターゲット割り当て
        if (!AmongUsClient.Instance.AmHost) return;
        List<PlayerControl> targetList = new();
        var targetRand = IRandom.Instance;
        foreach (var target in Main.AllPlayerControls)
        {
            if (Player.PlayerId == target.PlayerId) continue;
            if (target.Is(CustomRoles.GM)) continue;
            if (target.Is(CustomRoles.Lovers)) continue;

            var cRole = target.GetCustomRole();

            //インポスターもしくは第3陣営のキラー、アニマルズのキラーなら対象にする
            if (cRole.GetCustomRoleTypes() == CustomRoleTypes.Impostor ||
                target.IsNeutralKiller() ||
                target.IsAnimalsKiller())
                targetList.Add(target);
        }
        var SelectedTarget = targetList[targetRand.Next(targetList.Count)];
        TargetId = SelectedTarget.PlayerId;
        TargetIdPair[Player.PlayerId] = SelectedTarget.PlayerId;
        SendRPC();
    }

    public override void ApplyGameOptions(IGameOptions opt) => opt.SetVision(HasImpostorVision);

    private void SendRPC()
    {
        if (!AmongUsClient.Instance.AmHost) return;
        using var sender = CreateSender(CustomRPC.SetLawyerTarget);
        var sendTargetId = TargetId;
        sender.Writer.Write(sendTargetId);
    }
    public override void ReceiveRPC(MessageReader reader, CustomRPC rpcType)
    {
        if (rpcType != CustomRPC.SetLawyerTarget) return;
        TargetId = reader.ReadByte();
        TargetIdPair[Player.PlayerId] = TargetId;
    }

    public override void OverrideDisplayRoleNameAsSeer(PlayerControl seen, bool isMeeting, ref bool enabled, ref Color roleColor, ref string roleText)
    {
        if (KnowTargetRole && TargetId != byte.MaxValue && seen.PlayerId == TargetId)
            enabled = true;
    }

    public override string GetMark(PlayerControl seer, PlayerControl seen, bool _ = false)
    {
        //seenが省略の場合seer
        seen ??= seer;

        return setTargetString(seen.PlayerId);
    }

    private string setTargetString(byte targetId)
    {
        if (targetId != TargetId) return "";
        return "<color=#daa520>§</color>"; ;
    }

    public static string GetMarkOthers(PlayerControl seer, PlayerControl seen, bool isForMeeting = false)
    {
        //seenが省略の場合seer
        seen ??= seer;

        //シーアもしくはシーンが死んでいたら処理しない。
        if (!seer.IsAlive() || !seen.IsAlive()) return "";
        if (!TargetKnows) return "";
        if (!CheckIds(seer.PlayerId, seen.PlayerId)) return "";

        return Utils.ColorString(RoleInfo.RoleColor, "§");
    }

    private static bool CheckIds(byte targetId, byte LawyerId)
    {
        foreach (var ids in TargetIdPair)
        {
            if (targetId == ids.Value &&
                LawyerId == ids.Key)
            {
                return true;
            }

        }
        return false;
    }
    public static void OnMurderPlayerOthers(MurderInfo info)
    {
        var target = info.AttemptTarget;

        foreach (var lawyer in Lawyers)
        {
            if (lawyer.TargetId == target.PlayerId)
            {
                lawyer.ChangeRole();
                break;
            }
        }
    }
    public static void ChangeRoleByTarget(PlayerControl target)
    {
        foreach (var lawyer in Lawyers)
        {
            if (lawyer.TargetId != target.PlayerId) continue;
            lawyer.ChangeRole();
            break;
        }
    }
    public void ChangeRole()
    {
        if (Player == null)
        {
            Logger.Info("Playerがnullになったらしい","弁護士");
            return;
        }
        TargetId = byte.MaxValue;
        Player.RpcSetCustomRole(CustomRoles.LawTracker);
        Utils.NotifyRoles();
    }
    public static void EndGameCheck()
    {
        foreach (var pc in Main.AllPlayerControls)
        {
            var cRole = pc.GetCustomRole();
            if (cRole == CustomRoles.Lawyer)
            {
                var role = (Lawyer)pc.GetRoleClass();

                var targetRole = Utils.GetPlayerById(role.TargetId).GetCustomRole();
                bool winTeam = false;
                foreach (var winRole in CustomWinnerHolder.WinnerRoles)
                {
                    if (winRole == targetRole) { winTeam = true; break; }
                }
                bool winIds = CustomWinnerHolder.WinnerIds.Contains(role.TargetId);

                // 弁護士
                // 勝者に依頼人が含まれている時
                if (winIds || winTeam)
                {
                    // 弁護士が生きている時 リセットして単独勝利
                    if (pc.IsAlive())
                    {
                        CustomWinnerHolder.ResetAndSetWinner(CustomWinner.Lawyer);
                        CustomWinnerHolder.WinnerIds.Add(pc.PlayerId);
                    }
                    // 弁護士が死んでいる時 勝者と共に追加勝利
                    else
                    {
                        CustomWinnerHolder.WinnerIds.Add(pc.PlayerId);
                        CustomWinnerHolder.AdditionalWinnerRoles.Add(CustomRoles.Lawyer);
                    }
                }
            }
            else if (cRole == CustomRoles.LawTracker)
            {
                // 追跡者
                // 追跡者が生き残った場合ここで追加勝利
                if (pc.IsAlive())
                {
                    CustomWinnerHolder.WinnerIds.Add(pc.PlayerId);
                    CustomWinnerHolder.AdditionalWinnerRoles.Add(CustomRoles.LawTracker);
                }
            }
        }
    }
}