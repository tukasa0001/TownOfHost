using System.Collections.Generic;
using System.Linq;
using Hazel;
using UnityEngine;
using AmongUs.GameOptions;

using TownOfHostForE.Roles.Core;
using TownOfHostForE.Roles.Core.Interfaces;
using TownOfHostForE.Roles.Neutral;
using static TownOfHostForE.Translator;

namespace TownOfHostForE.Roles.Crewmate;
public sealed class SillySheriff : RoleBase, IKiller
{
    public static readonly SimpleRoleInfo RoleInfo =
         SimpleRoleInfo.Create(
            typeof(SillySheriff),
            player => new SillySheriff(player),
            CustomRoles.SillySheriff,
            () => RoleTypes.Impostor,
            CustomRoleTypes.Crewmate,
            8200,
            SetupOptionItem,
            "バカシェリフ",
            "#f8cd46",
            true,
            introSound: () => GetIntroSound(RoleTypes.Crewmate)
        );
    public SillySheriff(PlayerControl player)
    : base(
        RoleInfo,
        player,
        () => HasTask.False
    )
    {
        ShotLimit = ShotLimitOpt.GetInt();
        CurrentKillCooldown = KillCooldown.GetFloat();
    }

    private static OptionItem KillCooldown;
    private static OptionItem MisfireKillsTarget;
    private static OptionItem ShotLimitOpt;
    public static OptionItem IsInfoPoor;
    public static OptionItem IsClumsy;
    public static OptionItem Probability;
    private static OptionItem CanKillAllAlive;
    public static OptionItem CanKillNeutrals;
    public static OptionItem CanKillAnimals;
    enum OptionName
    {
        SheriffMisfireKillsTarget,
        SheriffShotLimit,
        SheriffIsInfoPoor,
        SheriffIsClumsy,
        SillySheriffProbability,
        SheriffCanKillAllAlive,
        SheriffCanKillNeutrals,
        SheriffCanKillAnimals,
        SheriffCanKill,
    }
    public static Dictionary<CustomRoles, OptionItem> KillTargetOptions = new();
    public static Dictionary<SchrodingerCat.TeamType, OptionItem> SchrodingerCatKillTargetOptions = new();
    public int ShotLimit = 0;
    public float CurrentKillCooldown = 30;
    public static readonly string[] KillOption =
    {
            "SheriffCanKillAll", "SheriffCanKillSeparately"
    };
    public static readonly string[] rates =
    {
            "Rate0",  "Rate5",  "Rate10", "Rate20", "Rate30", "Rate40",
            "Rate50", "Rate60", "Rate70", "Rate80", "Rate90", "Rate100",
    };

    private static void SetupOptionItem()
    {
        KillCooldown = FloatOptionItem.Create(RoleInfo, 10, GeneralOption.KillCooldown, new(0f, 180f, 2.5f), 30f, false)
            .SetValueFormat(OptionFormat.Seconds);
        MisfireKillsTarget = BooleanOptionItem.Create(RoleInfo, 11, OptionName.SheriffMisfireKillsTarget, false, false);
        ShotLimitOpt = IntegerOptionItem.Create(RoleInfo, 12, OptionName.SheriffShotLimit, new(1, 15, 1), 15, false)
            .SetValueFormat(OptionFormat.Times);
        IsInfoPoor = BooleanOptionItem.Create(RoleInfo, 16, OptionName.SheriffIsInfoPoor, false, false);
        IsClumsy = BooleanOptionItem.Create(RoleInfo, 17, OptionName.SheriffIsClumsy, false, false);
        Probability = StringOptionItem.Create(RoleInfo, 20, OptionName.SillySheriffProbability, rates[1..], 0, false);
        CanKillAllAlive = BooleanOptionItem.Create(RoleInfo, 15, OptionName.SheriffCanKillAllAlive, true, false);
        SetUpKillTargetOption(CustomRoles.Madmate, 13);
        CanKillNeutrals = StringOptionItem.Create(RoleInfo, 14, OptionName.SheriffCanKillNeutrals, KillOption, 0, false);
        SetUpNeutralOptions(30);
        CanKillAnimals = StringOptionItem.Create(RoleInfo, 21, OptionName.SheriffCanKillAnimals, KillOption, 0, false);
        SetUpAnimalsOptions(70);
    }
    public static void SetUpNeutralOptions(int idOffset)
    {
        foreach (var neutral in CustomRolesHelper.AllRoles.Where(x => x.IsNeutral()).ToArray())
        {
            if (neutral is CustomRoles.SchrodingerCat
                        or CustomRoles.HASFox
                        or CustomRoles.HASTroll
                        or CustomRoles.BAKURETSUKI) continue;
            SetUpKillTargetOption(neutral, idOffset, true, CanKillNeutrals);
            idOffset++;
        }
        foreach (var catType in EnumHelper.GetAllValues<SchrodingerCat.TeamType>())
        {
            //アニマルズは専用の方で処理
            if ((byte)catType < 50 || catType == SchrodingerCat.TeamType.Animals)
            {
                continue;
            }
            SetUpSchrodingerCatKillTargetOption(catType, idOffset, true, CanKillNeutrals);
            idOffset++;
        }
    }
    public static void SetUpAnimalsOptions(int idOffset)
    {
        foreach (var animals in CustomRolesHelper.AllRoles.Where(x => x.IsAnimals()).ToArray())
        {
            SetUpKillTargetOption(animals, idOffset, true, CanKillAnimals);
            idOffset++;
        }
        SetUpSchrodingerCatKillTargetOption(SchrodingerCat.TeamType.Animals, idOffset, true, CanKillAnimals);
    }

    public static void SetUpSchrodingerCatKillTargetOption(SchrodingerCat.TeamType catType, int idOffset, bool defaultValue = true, OptionItem parent = null)
    {
        var id = RoleInfo.ConfigId + idOffset;
        parent ??= RoleInfo.RoleOption;
        // (%team%陣営)
        var inTeam = GetString("In%team%", new Dictionary<string, string>() { ["%team%"] = GetRoleString(catType.ToString()) });
        // シュレディンガーの猫(%team%陣営)
        var catInTeam = Utils.ColorString(SchrodingerCat.GetCatColor(catType), Utils.GetRoleName(CustomRoles.SchrodingerCat) + inTeam);
        Dictionary<string, string> replacementDic = new() { ["%role%"] = catInTeam };
        SchrodingerCatKillTargetOptions[catType] = BooleanOptionItem.Create(id, OptionName.SheriffCanKill + "%role%", defaultValue, RoleInfo.Tab, false).SetParent(parent);
        SchrodingerCatKillTargetOptions[catType].ReplacementDictionary = replacementDic;
    }

    public static void SetUpKillTargetOption(CustomRoles role, int idOffset, bool defaultValue = true, OptionItem parent = null)
    {
        var id = RoleInfo.ConfigId + idOffset;
        if (parent == null) parent = RoleInfo.RoleOption;
        var roleName = Utils.GetRoleName(role);
        Dictionary<string, string> replacementDic = new() { { "%role%", Utils.ColorString(Utils.GetRoleColor(role), roleName) } };
        KillTargetOptions[role] = BooleanOptionItem.Create(id, OptionName.SheriffCanKill + "%role%", defaultValue, RoleInfo.Tab, false).SetParent(parent);
        KillTargetOptions[role].ReplacementDictionary = replacementDic;
    }
    public override void Add()
    {
        var playerId = Player.PlayerId;
        CurrentKillCooldown = KillCooldown.GetFloat();

        ShotLimit = ShotLimitOpt.GetInt();
        Logger.Info($"{Utils.GetPlayerById(playerId)?.GetNameWithRole()} : 残り{ShotLimit}発", "SillySheriff");
    }
    private void SendRPC()
    {
        if (!AmongUsClient.Instance.AmHost) return;
        using var sender = CreateSender(CustomRPC.SetSSheriffShotLimit);
        sender.Writer.Write(ShotLimit);
    }
    public override void ReceiveRPC(MessageReader reader, CustomRPC rpcType)
    {
        if (rpcType != CustomRPC.SetSSheriffShotLimit) return;

        ShotLimit = reader.ReadInt32();
    }
    public float CalculateKillCooldown() => CanUseKillButton() ? CurrentKillCooldown : 0f;
    public bool CanUseKillButton()
        => Player.IsAlive()
        && (CanKillAllAlive.GetBool() || GameStates.AlreadyDied)
        && ShotLimit > 0;
    public bool CanUseImpostorVentButton() => false;
    public bool CanUseSabotageButton() => false;
    public override void ApplyGameOptions(IGameOptions opt)
    {
        opt.SetVision(false);
    }
    public void OnCheckMurderAsKiller(MurderInfo info)
    {
        if (!Is(info.AttemptKiller) || info.IsSuicide || !info.CanKill) return;
        (var killer, var target) = info.AttemptTuple;

        int Chance = (Probability as StringOptionItem).GetChance();
        int chance = IRandom.Instance.Next(1, 101);

        if (ShotLimit <= 0)
        {
            info.DoKill = false;
            return;
        }
        ShotLimit--;
        Logger.Info($"{killer.GetNameWithRole()} : 残り{ShotLimit}発", "SillySheriff");
        SendRPC();
        if ((CanBeKilledBy(target) && chance <= Chance) || (!CanBeKilledBy(target) && chance >= Chance))
        {
            // 自殺
            killer.RpcMurderPlayer(killer);
            PlayerState.GetByPlayerId(killer.PlayerId).DeathReason = CustomDeathReason.Misfire;
            if (!MisfireKillsTarget.GetBool())
            {
                info.DoKill = false;
                return;
            }
        }
        killer.ResetKillCooldown();
    }
    public override string GetProgressText(bool comms = false) => Utils.ColorString(CanUseKillButton() ? Color.yellow : Color.gray, $"({ShotLimit})");
    public static bool CanBeKilledBy(PlayerControl player)
    {
        if (player.GetRoleClass() is SchrodingerCat schrodingerCat)
        {
            if (schrodingerCat.Team == SchrodingerCat.TeamType.None)
            {
                Logger.Warn($"シェリフ({player.GetRealName()})にキルされたシュレディンガーの猫のロールが変化していません", nameof(Sheriff));
                return false;
            }
            return schrodingerCat.Team switch
            {
                SchrodingerCat.TeamType.Mad => KillTargetOptions.TryGetValue(CustomRoles.Madmate, out var option) && option.GetBool(),
                SchrodingerCat.TeamType.Crew => false,
                _ => CanKillNeutrals.GetValue() == 0 || (SchrodingerCatKillTargetOptions.TryGetValue(schrodingerCat.Team, out var option) && option.GetBool()),
            };
        }

        var cRole = player.GetCustomRole();
        return cRole.GetCustomRoleTypes() switch
        {
            CustomRoleTypes.Impostor => true,
            CustomRoleTypes.Madmate => KillTargetOptions.TryGetValue(CustomRoles.Madmate, out var option) && option.GetBool(),
            CustomRoleTypes.Neutral => CanKillNeutrals.GetValue() == 0 || !KillTargetOptions.TryGetValue(cRole, out var option) || option.GetBool(),
            CustomRoleTypes.Animals => CanKillAnimals.GetValue() == 0 || !KillTargetOptions.TryGetValue(cRole, out var option) || option.GetBool(),
            _ => false,
        };
    }
}