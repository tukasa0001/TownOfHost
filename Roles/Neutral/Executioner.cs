using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Hazel;
using AmongUs.GameOptions;

using TownOfHost.Roles.Core;
using static TownOfHost.Options;

namespace TownOfHost.Roles.Neutral;
public sealed class Executioner : RoleBase
{
    public static readonly SimpleRoleInfo RoleInfo =
        new(
            typeof(Executioner),
            player => new Executioner(player),
            CustomRoles.Executioner,
            () => RoleTypes.Crewmate,
            CustomRoleTypes.Neutral,
            50700,
            SetupOptionItem,
            "#611c3a"
        );
    public Executioner(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        CanTargetImpostor = OptionCanTargetImpostor.GetBool();
        CanTargetNeutralKiller = OptionCanTargetNeutralKiller.GetBool();
        ChangeRolesAfterTargetKilled = ChangeRoles[OptionChangeRolesAfterTargetKilled.GetValue()];

        Executioners.Add(this);
        CustomRoleManager.MarkOthers.Add(GetMarkOthers);
        CustomRoleManager.OnMurderPlayerOthers.Add(OnMurderPlayerOthers);
    }
    public static byte WinnerID;

    private static OptionItem OptionCanTargetImpostor;
    private static OptionItem OptionCanTargetNeutralKiller;
    public static OptionItem OptionChangeRolesAfterTargetKilled;
    enum OptionName
    {
        ExecutionerCanTargetImpostor,
        ExecutionerCanTargetNeutralKiller,
        ExecutionerChangeRolesAfterTargetKilled
    }

    private static bool CanTargetImpostor;
    private static bool CanTargetNeutralKiller;
    public static CustomRoles ChangeRolesAfterTargetKilled;

    public static HashSet<Executioner> Executioners = new(15);
    public byte TargetId;
    public static readonly CustomRoles[] ChangeRoles =
    {
            CustomRoles.Crewmate, CustomRoles.Jester, CustomRoles.Opportunist,
    };

    private static void SetupOptionItem()
    {
        var cRolesString = ChangeRoles.Select(x => x.ToString()).ToArray();
        OptionCanTargetImpostor = BooleanOptionItem.Create(RoleInfo, 10, OptionName.ExecutionerCanTargetImpostor, false, false);
        OptionCanTargetNeutralKiller = BooleanOptionItem.Create(RoleInfo, 12, OptionName.ExecutionerCanTargetNeutralKiller, false, false);
        OptionChangeRolesAfterTargetKilled = StringOptionItem.Create(RoleInfo, 11, OptionName.ExecutionerChangeRolesAfterTargetKilled, cRolesString, 1, false);
    }
    public override void Add()
    {
        //ターゲット割り当て
        if (!AmongUsClient.Instance.AmHost) return;

        var playerId = Player.PlayerId;
        List<PlayerControl> targetList = new();
        var rand = IRandom.Instance;
        foreach (var target in Main.AllPlayerControls)
        {
            if (playerId == target.PlayerId) continue;
            else if (!CanTargetImpostor && target.Is(CustomRoleTypes.Impostor)) continue;
            else if (!CanTargetNeutralKiller && target.IsNeutralKiller()) continue;
            if (target.Is(CustomRoles.GM)) continue;

            targetList.Add(target);
        }
        var SelectedTarget = targetList[rand.Next(targetList.Count)];
        TargetId = SelectedTarget.PlayerId;
        SendRPC();
        Logger.Info($"{Player.GetNameWithRole()}:{SelectedTarget.GetNameWithRole()}", "Executioner");
    }
    public override void OnDestroy()
    {
        Executioners.Clear();
    }
    public void SendRPC()
    {
        if (!AmongUsClient.Instance.AmHost) return;

        using var sender = CreateSender(CustomRPC.SetExecutionerTarget);
        sender.Writer.Write(TargetId);
    }
    public override void ReceiveRPC(MessageReader reader, CustomRPC rpcType)
    {
        byte targetId = reader.ReadByte();
        TargetId = targetId;
    }
    public static void OnMurderPlayerOthers(MurderInfo info)
    {
        var target = info.AttemptTarget;

        foreach (var executioner in Executioners)
        {
            if (executioner.TargetId == target.PlayerId)
                executioner.ChangeRole();
            else if (executioner.Is(target))
            {
                executioner.TargetId = byte.MaxValue;
                executioner.SendRPC();
            }
        }
    }
    public override void OnExileWrapUp(GameData.PlayerInfo exiled, ref bool DecidedWinner)
    {
        if (!AmongUsClient.Instance.AmHost) return;
        if (Player == null || !Player.IsAlive()) return;
        if (exiled.PlayerId != TargetId) return;

        if (exiled.GetCustomRole() == CustomRoles.Jester)
        {
            CustomWinnerHolder.AdditionalWinnerTeams.Add(AdditionalWinners.Executioner);
        }
        else if (!DecidedWinner)
        {
            if (CustomWinnerHolder.WinnerTeam != CustomWinner.Default) return; //勝者がいるなら処理をスキップ
            CustomWinnerHolder.ResetAndSetWinner(CustomWinner.Executioner);
        }
        CustomWinnerHolder.WinnerIds.Add(Player.PlayerId);
    }
    public void ChangeRole()
    {
        Player.RpcSetCustomRole(ChangeRolesAfterTargetKilled);
        TargetId = byte.MaxValue;
        SendRPC();
        Utils.NotifyRoles();
    }

    public static void ChangeRoleByTarget(byte targetId)
    {
        foreach (var executioner in Executioners)
        {
            if (executioner.TargetId != targetId) continue;

            executioner.ChangeRole();
            break;
        }
    }
    public static string GetMarkOthers(PlayerControl seer, PlayerControl seen, bool isForMeeting = false)
    {
        //seenが省略の場合seer
        seen ??= seer;

        if (seer.GetRoleClass() is not Executioner executioner) return ""; //エクスキューショナー以外処理しない
        if (executioner.TargetId != seen.PlayerId) return "";

        return Utils.ColorString(RoleInfo.RoleColor, "♦");
    }
}