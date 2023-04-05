using AmongUs.GameOptions;
using Hazel;
using System.Collections.Generic;
using UnityEngine;
using static TOHE.Options;
namespace TOHE.Roles.Neutral;

public static class Gamer
{
    private static readonly int Id = 5060956;
    public static List<byte> playerIdList = new();

    private static Dictionary<byte, int> PlayerHealth;
    private static Dictionary<byte, int> GamerHealth;

    private static OptionItem KillCooldown;
    public static OptionItem CanVent;
    private static OptionItem HasImpostorVision;
    private static OptionItem HealthMax;
    private static OptionItem Damage;
    private static OptionItem SelfHealthMax;
    private static OptionItem SelfDamage;

    public static void SetupCustomOption()
    {
        //玩家只能有一人
        SetupSingleRoleOptions(Id, TabGroup.NeutralRoles, CustomRoles.Gamer, 1, zeroOne: false);
        KillCooldown = FloatOptionItem.Create(Id + 10, "GamerKillCooldown", new(1f, 180f, 1f), 2f, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Gamer])
            .SetValueFormat(OptionFormat.Seconds);
        CanVent = BooleanOptionItem.Create(Id + 11, "CanVent", true, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Gamer]);
        HasImpostorVision = BooleanOptionItem.Create(Id + 13, "ImpostorVision", false, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Gamer]);
        HealthMax = IntegerOptionItem.Create(Id + 15, "GamerHealthMax", new(5, 990, 5), 100, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Gamer])
            .SetValueFormat(OptionFormat.Health);
        Damage = IntegerOptionItem.Create(Id + 16, "GamerDamage", new(1, 100, 1), 15, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Gamer])
            .SetValueFormat(OptionFormat.Health);
        SelfHealthMax = IntegerOptionItem.Create(Id + 17, "GamerSelfHealthMax", new(100, 100, 5), 100, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Gamer])
            .SetValueFormat(OptionFormat.Health);
        SelfDamage = IntegerOptionItem.Create(Id + 18, "GamerSelfDamage", new(1, 100, 1), 35, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Gamer])
            .SetValueFormat(OptionFormat.Health);
    }
    public static void Init()
    {
        playerIdList = new();
        GamerHealth = new();
        PlayerHealth = new();
    }
    public static void Add(byte playerId)
    {
        playerIdList.Add(playerId);
        GamerHealth.TryAdd(playerId, SelfHealthMax.GetInt());
        foreach (var pc in Main.AllAlivePlayerControls)
            PlayerHealth.TryAdd(pc.PlayerId, HealthMax.GetInt());

        if (!AmongUsClient.Instance.AmHost) return;
        if (!Main.ResetCamPlayerList.Contains(playerId))
            Main.ResetCamPlayerList.Add(playerId);
    }
    public static bool IsEnable => playerIdList.Count > 0;
    public static void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = KillCooldown.GetFloat();
    public static void ApplyGameOptions(IGameOptions opt) => opt.SetVision(HasImpostorVision.GetBool());
    public static void CanUseVent(PlayerControl player)
    {
        bool gamer_canUse = CanVent.GetBool();
        DestroyableSingleton<HudManager>.Instance.ImpostorVentButton.ToggleVisible(gamer_canUse && !player.Data.IsDead);
        player.Data.Role.CanVent = gamer_canUse;
    }
    private static void SendRPC(byte playerId)
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetGamerHealth, SendOption.Reliable, -1);
        writer.Write(playerId);
        if (GamerHealth.ContainsKey(playerId))
            writer.Write(GamerHealth[playerId]);
        else
            writer.Write(PlayerHealth[playerId]);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public static void ReceiveRPC(MessageReader reader)
    {
        byte PlayerId = reader.ReadByte();
        int Health = reader.ReadInt32();
        if (GamerHealth.ContainsKey(PlayerId))
            GamerHealth[PlayerId] = Health;
        else
            PlayerHealth[PlayerId] = Health;
    }
    public static bool CheckGamerMurder(PlayerControl killer, PlayerControl target)
    {
        if (killer == null || target == null || !killer.Is(CustomRoles.Gamer) || target.Is(CustomRoles.Gamer) || !PlayerHealth.ContainsKey(target.PlayerId)) return false;
        killer.SetKillCooldown();

        if (PlayerHealth[target.PlayerId] - Damage.GetInt() < 1)
        {
            PlayerHealth.Remove(target.PlayerId);
            killer.RpcMurderPlayerV3(target);
            Utils.NotifyRoles(killer);
            return false;
        }

        PlayerHealth[target.PlayerId] -= Damage.GetInt();
        SendRPC(target.PlayerId);
        RPC.PlaySoundRPC(killer.PlayerId, Sounds.KillSound);
        Utils.NotifyRoles(killer);

        Logger.Info($"{killer.GetNameWithRole()} 对玩家 {target.GetNameWithRole()} 造成了 {Damage.GetInt()} 点伤害", "Gamer");
        return true;
    }
    public static bool CheckMurder(PlayerControl killer, PlayerControl target)
    {
        if (killer == null || target == null || !target.Is(CustomRoles.Gamer) || killer.Is(CustomRoles.Gamer)) return true;

        if (GamerHealth[target.PlayerId] - SelfDamage.GetInt() < 1)
        {
            GamerHealth.Remove(target.PlayerId);
            Utils.NotifyRoles(target);
            return true;
        }

        killer.SetKillCooldown();

        GamerHealth[target.PlayerId] -= SelfDamage.GetInt();
        SendRPC(target.PlayerId);
        RPC.PlaySoundRPC(target.PlayerId, Sounds.KillSound);
        killer.RpcGuardAndKill(target);
        Utils.NotifyRoles(target);

        Logger.Info($"{killer.GetNameWithRole()} 对玩家 {target.GetNameWithRole()} 造成了 {SelfDamage.GetInt()} 点伤害", "Gamer");
        return false;
    }
    public static string TargetMark(PlayerControl seer, PlayerControl target)
    {
        if (!seer.Is(CustomRoles.Gamer) || !seer.IsAlive()) return "";
        if (seer.PlayerId == target.PlayerId)
        {
            var GetValue = GamerHealth.TryGetValue(target.PlayerId, out var value);
            return GetValue && value > 0 ? Utils.ColorString(GetColor(value, true), $"【{value}/{SelfHealthMax.GetInt()}】") : "";
        }
        else
        {
            var GetValue = PlayerHealth.TryGetValue(target.PlayerId, out var value);
            return GetValue && value > 0 ? Utils.ColorString(GetColor(value), $"【{value}/{HealthMax.GetInt()}】") : "";
        }
    }
    private static Color32 GetColor(float Health, bool self = false)
    {
        var x = (int)(Health / (self ? SelfHealthMax.GetInt() : HealthMax.GetInt()) * 10 * 50);
        int R = 255; int G = 255; int B = 0;
        if (x > 255) R -= (x - 255); else G = x;
        return new Color32((byte)R, (byte)G, (byte)B, byte.MaxValue);
    }
}