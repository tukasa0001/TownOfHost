using AmongUs.GameOptions;
using Hazel;
using InnerNet;
using System.Collections.Generic;

namespace TOHE;

// 来源：https://github.com/Yumenopai/TownOfHost_Y
public static class DarkHide
{
    public static readonly int Id = 60200;
    public static List<byte> playerIdList = new();

    public static OptionItem KillCooldown;
    public static OptionItem HasImpostorVision;
    public static OptionItem CanCountNeutralKiller;

    public static Dictionary<byte, float> CurrentKillCooldown = new();
    public static Dictionary<byte, bool> IsWinKill = new();

    public static void SetupCustomOption()
    {
        Options.SetupSingleRoleOptions(Id, TabGroup.NeutralRoles, CustomRoles.DarkHide, 1);
        KillCooldown = FloatOptionItem.Create(Id + 10, "KillCooldown", new(0f, 180f, 2.5f), 30f, TabGroup.NeutralRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.DarkHide])
            .SetValueFormat(OptionFormat.Seconds);
        HasImpostorVision = BooleanOptionItem.Create(Id + 11, "ImpostorVision", false, TabGroup.NeutralRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.DarkHide]);
        CanCountNeutralKiller = BooleanOptionItem.Create(Id + 12, "CanCountNeutralKiller", false, TabGroup.NeutralRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.DarkHide]);
    }
    public static void Init()
    {
        playerIdList = new();
        CurrentKillCooldown = new();
        IsWinKill = new();
    }
    public static void Add(byte playerId)
    {
        playerIdList.Add(playerId);
        CurrentKillCooldown.Add(playerId, KillCooldown.GetFloat());
        IsWinKill[playerId] = false;

        Utils.GetPlayerById(playerId)?.DRpcSetKillCount();

        if (!Main.ResetCamPlayerList.Contains(playerId))
            Main.ResetCamPlayerList.Add(playerId);
    }
    public static bool IsEnable => playerIdList.Count > 0;

    public static void ReceiveRPC(MessageReader msg)
    {
        byte DarkHiderId = msg.ReadByte();
        bool IsKillerKill = msg.ReadBoolean();
        if (IsWinKill.ContainsKey(DarkHiderId))
            IsWinKill[DarkHiderId] = IsKillerKill;
        else
            IsWinKill.Add(DarkHiderId, false);
        Logger.Info($"Player{DarkHiderId}:ReceiveRPC", "DarkHide");
    }
    public static void DRpcSetKillCount(this PlayerControl player)
    {
        if (!AmongUsClient.Instance.AmHost) return;
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetDarkHiderKillCount, Hazel.SendOption.Reliable, -1);
        writer.Write(player.PlayerId);
        writer.Write(IsWinKill[player.PlayerId]);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }

    public static void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = CurrentKillCooldown[id];
    public static bool CanUseKillButton(PlayerControl player) => !player.Data.IsDead;

    public static void ApplyGameOptions(IGameOptions opt) => opt.SetVision(HasImpostorVision.GetBool());

    public static void OnCheckMurder(PlayerControl killer, PlayerControl Ktarget)
    {
        var targetRole = Ktarget.GetCustomRole();
        var succeeded = targetRole.IsImpostor();
        if (CanCountNeutralKiller.GetBool() && !Ktarget.Is(CustomRoles.Arsonist))
        {
            succeeded = succeeded || ExtendedPlayerControl.IsNeutralKiller(Ktarget);
        }
        if (succeeded)
            IsWinKill[killer.PlayerId] = true;

        killer.DRpcSetKillCount();
        MessageWriter SabotageFixWriter = AmongUsClient.Instance.StartRpcImmediately(ShipStatus.Instance.NetId, (byte)RpcCalls.RepairSystem, SendOption.Reliable, killer.GetClientId());
        SabotageFixWriter.Write((byte)SystemTypes.Electrical);
        MessageExtensions.WriteNetObject(SabotageFixWriter, killer);
        AmongUsClient.Instance.FinishRpcImmediately(SabotageFixWriter);

        foreach (var target in Main.AllPlayerControls)
        {
            if (target == killer || target.Data.Disconnected) continue;
            SabotageFixWriter = AmongUsClient.Instance.StartRpcImmediately(ShipStatus.Instance.NetId, (byte)RpcCalls.RepairSystem, SendOption.Reliable, target.GetClientId());
            SabotageFixWriter.Write((byte)SystemTypes.Electrical);
            MessageExtensions.WriteNetObject(SabotageFixWriter, target);
            AmongUsClient.Instance.FinishRpcImmediately(SabotageFixWriter);
        }
    }

}
