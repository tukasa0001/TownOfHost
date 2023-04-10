using Hazel;
using System.Collections.Generic;
using UnityEngine;
using static TOHE.Translator;

namespace TOHE.Roles.Impostor;

public static class Gangster
{
    private static readonly int Id = 5054525;
    private static List<byte> playerIdList = new();

    private static OptionItem RecruitLimitOpt;
    public static OptionItem KillCooldown;

    public static OptionItem SheriffCanBeMadmate;
    public static OptionItem MayorCanBeMadmate;
    public static OptionItem NGuesserCanBeMadmate;
    public static OptionItem JudgeCanBeMadmate;

    public static Dictionary<byte, int> RecruitLimit = new();
    public static void SetupCustomOption()
    {
        Options.SetupRoleOptions(Id, TabGroup.ImpostorRoles, CustomRoles.Gangster);
        KillCooldown = FloatOptionItem.Create(Id + 10, "GangsterRecruitCooldown", new(0f, 180f, 2.5f), 20f, TabGroup.ImpostorRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Gangster])
            .SetValueFormat(OptionFormat.Seconds);
        RecruitLimitOpt = IntegerOptionItem.Create(Id + 12, "GangsterRecruitLimit", new(1, 15, 1), 2, TabGroup.ImpostorRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Gangster])
            .SetValueFormat(OptionFormat.Times);

        SheriffCanBeMadmate = BooleanOptionItem.Create(Id + 14, "GanSheriffCanBeMadmate", false, TabGroup.ImpostorRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Gangster]);
        MayorCanBeMadmate = BooleanOptionItem.Create(Id + 15, "GanMayorCanBeMadmate", false, TabGroup.ImpostorRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Gangster]);
        NGuesserCanBeMadmate = BooleanOptionItem.Create(Id + 16, "GanNGuesserCanBeMadmate", false, TabGroup.ImpostorRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Gangster]);
        JudgeCanBeMadmate = BooleanOptionItem.Create(Id + 17, "GanJudgeCanBeMadmate", false, TabGroup.ImpostorRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Gangster]);

    }
    public static void Init()
    {
        playerIdList = new();
        RecruitLimit = new();
    }
    public static void Add(byte playerId)
    {
        playerIdList.Add(playerId);
        RecruitLimit.TryAdd(playerId, RecruitLimitOpt.GetInt());
    }
    public static bool IsEnable => playerIdList.Count > 0;
    private static void SendRPC(byte playerId)
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetGangsterRecruitLimit, SendOption.Reliable, -1);
        writer.Write(playerId);
        writer.Write(RecruitLimit[playerId]);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public static void ReceiveRPC(MessageReader reader)
    {
        byte PlayerId = reader.ReadByte();
        int Limit = reader.ReadInt32();
        if (RecruitLimit.ContainsKey(PlayerId))
            RecruitLimit[PlayerId] = Limit;
        else
            RecruitLimit.Add(PlayerId, RecruitLimitOpt.GetInt());
    }
    public static void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = CanRecruit(id) ? KillCooldown.GetFloat() : Options.DefaultKillCooldown;
    public static bool CanRecruit(byte id) => RecruitLimit.TryGetValue(id, out var x) && x > 0;
    public static void SetKillButtonText(byte plaeryId)
    {
        if (CanRecruit(plaeryId))
            HudManager.Instance.KillButton.OverrideText($"{GetString("GangsterButtonText")}");
        else
            HudManager.Instance.KillButton.OverrideText($"{GetString("KillButtonText")}");
    }
    public static bool OnCheckMurder(PlayerControl killer, PlayerControl target)
    {
        if (RecruitLimit[killer.PlayerId] < 1) return false;
        if (CanBeMadmate(target))
        {
            RecruitLimit[killer.PlayerId]--;
            SendRPC(killer.PlayerId);
            target.RpcSetCustomRole(CustomRoles.Madmate);
            NameColorManager.Add(killer.PlayerId, target.PlayerId, "#ff1919");
            killer.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Gangster), GetString("GangsterSuccessfullyRecruited")));
            target.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Gangster), GetString("BeRecruitedByGangster")));
            killer.RpcGuardAndKill(target);
            target.RpcGuardAndKill(killer);
            target.RpcGuardAndKill(target);
            SetKillCooldown(killer.PlayerId);
            Logger.Info("设置职业:" + target?.Data?.PlayerName + " = " + target.GetCustomRole().ToString() + " + " + CustomRoles.Madmate.ToString(), "Assign " + CustomRoles.Madmate.ToString());
            if (RecruitLimit[killer.PlayerId] < 0)
                HudManager.Instance.KillButton.OverrideText($"{GetString("KillButtonText")}");
            Logger.Info($"{killer.GetNameWithRole()} : 剩余{RecruitLimit[killer.PlayerId]}次招募机会", "Gangster");
            return true;
        }
        if (RecruitLimit[killer.PlayerId] < 0)
            HudManager.Instance.KillButton.OverrideText($"{GetString("KillButtonText")}");
        killer.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Gangster), GetString("GangsterRecruitmentFailure")));
        Logger.Info($"{killer.GetNameWithRole()} : 剩余{RecruitLimit[killer.PlayerId]}次招募机会", "Gangster");
        return false;
    }
    public static string GetRecruitLimit(byte playerId) => Utils.ColorString(CanRecruit(playerId) ? Color.red : Color.gray, RecruitLimit.TryGetValue(playerId, out var recruitLimit) ? $"({recruitLimit})" : "Invalid");

    public static bool CanBeMadmate(this PlayerControl pc)
    {
        return pc != null && pc.GetCustomRole().IsCrewmate() && !pc.Is(CustomRoles.Madmate)
        && !(
            (pc.Is(CustomRoles.Sheriff) && !SheriffCanBeMadmate.GetBool()) ||
            (pc.Is(CustomRoles.Mayor) && !MayorCanBeMadmate.GetBool()) ||
            (pc.Is(CustomRoles.NiceGuesser) && !NGuesserCanBeMadmate.GetBool()) ||
            (pc.Is(CustomRoles.Judge) && !JudgeCanBeMadmate.GetBool()) ||
            pc.Is(CustomRoles.Snitch) ||
            pc.Is(CustomRoles.Needy) ||
            pc.Is(CustomRoles.CyberStar) ||
            pc.Is(CustomRoles.Egoist) ||
            pc.Is(CustomRoles.DualPersonality)
            );
    }
}