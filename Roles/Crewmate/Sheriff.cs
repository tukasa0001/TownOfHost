using Hazel;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TOHE.Roles.Crewmate;

public static class Sheriff
{
    private static readonly int Id = 20400;
    public static List<byte> playerIdList = new();

    private static OptionItem KillCooldown;
    private static OptionItem MisfireKillsTarget;
    private static OptionItem ShotLimitOpt;
    private static OptionItem CanKillAllAlive;
    public static OptionItem CanKillNeutrals;
    public static OptionItem CanKillMadmate;
    public static Dictionary<CustomRoles, OptionItem> KillTargetOptions = new();
    public static Dictionary<byte, float> ShotLimit = new();
    public static Dictionary<byte, float> CurrentKillCooldown = new();
    public static readonly string[] KillOption =
    {
        "SheriffCanKillAll", "SheriffCanKillSeparately"
    };
    public static void SetupCustomOption()
    {
        Options.SetupRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.Sheriff);
        KillCooldown = FloatOptionItem.Create(Id + 10, "KillCooldown", new(0f, 990f, 1f), 15f, TabGroup.CrewmateRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Sheriff])
            .SetValueFormat(OptionFormat.Seconds);
        MisfireKillsTarget = BooleanOptionItem.Create(Id + 11, "SheriffMisfireKillsTarget", false, TabGroup.CrewmateRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Sheriff]);
        ShotLimitOpt = IntegerOptionItem.Create(Id + 12, "SheriffShotLimit", new(1, 15, 1), 6, TabGroup.CrewmateRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Sheriff])
            .SetValueFormat(OptionFormat.Times);
        CanKillAllAlive = BooleanOptionItem.Create(Id + 15, "SheriffCanKillAllAlive", true, TabGroup.CrewmateRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Sheriff]);
        CanKillMadmate = BooleanOptionItem.Create(Id + 17, "SheriffCanKillMadmate", true, TabGroup.CrewmateRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Sheriff]);
        CanKillNeutrals = StringOptionItem.Create(Id + 14, "SheriffCanKillNeutrals", KillOption, 0, TabGroup.CrewmateRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Sheriff]);
        SetUpNeutralOptions(Id + 30);
    }
    public static void SetUpNeutralOptions(int Id)
    {
        foreach (var neutral in Enum.GetValues(typeof(CustomRoles)).Cast<CustomRoles>().Where(x => x.IsNeutral()))
        {
            SetUpKillTargetOption(neutral, Id, true, CanKillNeutrals);
            Id++;
        }
    }
    public static void SetUpKillTargetOption(CustomRoles role, int Id, bool defaultValue = true, OptionItem parent = null)
    {
        if (parent == null) parent = Options.CustomRoleSpawnChances[CustomRoles.Sheriff];
        var roleName = Utils.GetRoleName(role);
        Dictionary<string, string> replacementDic = new() { { "%role%", Utils.ColorString(Utils.GetRoleColor(role), roleName) } };
        KillTargetOptions[role] = BooleanOptionItem.Create(Id, "SheriffCanKill%role%", defaultValue, TabGroup.CrewmateRoles, false).SetParent(parent);
        KillTargetOptions[role].ReplacementDictionary = replacementDic;
    }
    public static void Init()
    {
        playerIdList = new();
        ShotLimit = new();
        CurrentKillCooldown = new();
    }
    public static void Add(byte playerId)
    {
        playerIdList.Add(playerId);
        CurrentKillCooldown.Add(playerId, KillCooldown.GetFloat());

        if (!Main.ResetCamPlayerList.Contains(playerId))
            Main.ResetCamPlayerList.Add(playerId);

        ShotLimit.TryAdd(playerId, ShotLimitOpt.GetFloat());
        Logger.Info($"{Utils.GetPlayerById(playerId)?.GetNameWithRole()} : 残り{ShotLimit[playerId]}発", "Sheriff");
    }
    public static bool IsEnable => playerIdList.Count > 0;
    private static void SendRPC(byte playerId)
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetSheriffShotLimit, SendOption.Reliable, -1);
        writer.Write(playerId);
        writer.Write(ShotLimit[playerId]);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public static void ReceiveRPC(MessageReader reader)
    {
        byte SheriffId = reader.ReadByte();
        float Limit = reader.ReadSingle();
        if (ShotLimit.ContainsKey(SheriffId))
            ShotLimit[SheriffId] = Limit;
        else
            ShotLimit.Add(SheriffId, ShotLimitOpt.GetFloat());
    }
    public static void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = CanUseKillButton(id) ? CurrentKillCooldown[id] : 0f;
    public static bool CanUseKillButton(byte playerId)
        => !Main.PlayerStates[playerId].IsDead
        && (CanKillAllAlive.GetBool() || GameStates.AlreadyDied)
        && ShotLimit[playerId] > 0;

    public static bool OnCheckMurder(PlayerControl killer, PlayerControl target)
    {
        ShotLimit[killer.PlayerId]--;
        Logger.Info($"{killer.GetNameWithRole()} : 残り{ShotLimit[killer.PlayerId]}発", "Sheriff");
        SendRPC(killer.PlayerId);
        if (!killer.Is(CustomRoles.Madmate) && !target.CanBeKilledBySheriff())
        {
            Main.PlayerStates[killer.PlayerId].deathReason = PlayerState.DeathReason.Misfire;
            killer.RpcMurderPlayer(killer);
            return MisfireKillsTarget.GetBool();
        }
        SetKillCooldown(killer.PlayerId);
        return true;
    }
    public static string GetShotLimit(byte playerId) => Utils.ColorString(CanUseKillButton(playerId) ? Color.yellow : Color.gray, ShotLimit.TryGetValue(playerId, out var shotLimit) ? $"({shotLimit})" : "Invalid");
    public static bool CanBeKilledBySheriff(this PlayerControl player)
    {
        var cRole = player.GetCustomRole();
        var subRole = player.GetCustomSubRoles();
        bool IsMadmate = false;
        foreach (var role in subRole)
        {
            if (role == CustomRoles.Madmate)
                IsMadmate = CanKillMadmate.GetBool();
        }

        return cRole.GetCustomRoleTypes() switch
        {
            CustomRoleTypes.Impostor => true,
            CustomRoleTypes.Neutral => CanKillNeutrals.GetValue() == 0 || !KillTargetOptions.TryGetValue(cRole, out var option) || option.GetBool(),
            _ => IsMadmate,//それでもない場合マッドが切れるand重複マッドか調べる
        };
    }
}