using System.Collections.Generic;
using System.Linq;
using System.Text;
using AmongUs.GameOptions;
using Hazel;

using TownOfHostForE.Roles.Core;
using TownOfHostForE.Roles.Neutral;
using UnityEngine;
using static Il2CppSystem.Uri;
using static TownOfHostForE.Translator;

namespace TownOfHostForE.Roles.Crewmate;
public sealed class GrudgeSheriff : RoleBase
{
    public static readonly SimpleRoleInfo RoleInfo =
         SimpleRoleInfo.Create(
            typeof(GrudgeSheriff),
            player => new GrudgeSheriff(player),
            CustomRoles.GrudgeSheriff,
            () => RoleTypes.Engineer,
            CustomRoleTypes.Crewmate,
            8300,
            SetupOptionItem,
            "グラージシェリフ",
            "#f8cd46",
            introSound: () => GetIntroSound(RoleTypes.Crewmate)
        );
    public GrudgeSheriff(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        ShotLimit = ShotLimitOpt.GetInt();
        KillCooldown = OptionKillCooldown.GetFloat();

    }

    private static OptionItem OptionKillCooldown;
    private static OptionItem MisfireKillsTarget;
    private static OptionItem ShotLimitOpt;
    private static OptionItem CanKillAllAlive;
    public static OptionItem CanKillNeutrals;
    public static OptionItem CanKillAnimals;
    enum OptionName
    {
        SheriffMisfireKillsTarget,
        SheriffShotLimit,
        SheriffCanKillAllAlive,
        SheriffCanKillNeutrals,
        SheriffCanKillAnimals,
        SheriffCanKill,
    }
    public static Dictionary<CustomRoles, OptionItem> KillTargetOptions = new();
    public static Dictionary<SchrodingerCat.TeamType, OptionItem> SchrodingerCatKillTargetOptions = new();
    PlayerControl KillWaitPlayerSelect = null;
    PlayerControl KillWaitPlayer = null;
    bool IsCoolTimeOn = true;

    public int ShotLimit = 0;
    public static float KillCooldown = 30;
    public static readonly string[] KillOption =
    {
            "SheriffCanKillAll", "SheriffCanKillSeparately"
        };
    private static void SetupOptionItem()
    {
        OptionKillCooldown = FloatOptionItem.Create(RoleInfo, 10, GeneralOption.KillCooldown, new(0f, 180f, 2.5f), 30f, false)
            .SetValueFormat(OptionFormat.Seconds);
        MisfireKillsTarget = BooleanOptionItem.Create(RoleInfo, 11, OptionName.SheriffMisfireKillsTarget, false, false);
        ShotLimitOpt = IntegerOptionItem.Create(RoleInfo, 12, OptionName.SheriffShotLimit, new(1, 15, 1), 15, false)
            .SetValueFormat(OptionFormat.Times);
        CanKillAllAlive = BooleanOptionItem.Create(RoleInfo, 15, OptionName.SheriffCanKillAllAlive, true, false);
        SetUpKillTargetOption(CustomRoles.Madmate, 13);
        CanKillNeutrals = StringOptionItem.Create(RoleInfo, 14, OptionName.SheriffCanKillNeutrals, KillOption, 0, false);
        SetUpNeutralOptions(30);
        CanKillAnimals = StringOptionItem.Create(RoleInfo, 16, OptionName.SheriffCanKillAnimals, KillOption, 0, false);
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
    public static void SetUpAnimalsOptions(int idOffset)
{
    foreach (var animals in CustomRolesHelper.AllRoles.Where(x => x.IsAnimals()).ToArray())
    {
        SetUpKillTargetOption(animals, idOffset, true, CanKillAnimals);
        idOffset++;
    }
    SetUpSchrodingerCatKillTargetOption(SchrodingerCat.TeamType.Animals, idOffset, true, CanKillAnimals);
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
        KillWaitPlayerSelect = null;
        KillWaitPlayer = null;
        IsCoolTimeOn = true;
        ShotLimit = ShotLimitOpt.GetInt();

        Player.AddVentSelect();
    }
    private void SendRPC()
    {
        if (!AmongUsClient.Instance.AmHost) return;
        using var sender = CreateSender();
        sender.Writer.Write(ShotLimit);
    }
    public override void ReceiveRPC(MessageReader reader, CustomRPC rpcType)
    {
        //if (rpcType != CustomRPC.SetGrudgeSheriffShotLimit) return;

        ShotLimit = reader.ReadInt32();
    }

    public override void ApplyGameOptions(IGameOptions opt)
    {
        AURoleOptions.EngineerCooldown = CanUseKillButton() ? (IsCoolTimeOn ? KillCooldown : 0f) : 255f;
        AURoleOptions.EngineerInVentMaxTime = 1f;
    }
    public bool CanUseKillButton()
        => Player.IsAlive()
        && (CanKillAllAlive.GetBool() || GameStates.AlreadyDied)
        && ShotLimit > 0;

    public override bool OnEnterVent(PlayerPhysics physics, int ventId)
    {
        if (!CanUseKillButton()) return false;

        IsCoolTimeOn = false;
        Player.MarkDirtySettings();

        KillWaitPlayerSelect = Player.VentPlayerSelect(() =>
        {
            KillWaitPlayer = KillWaitPlayerSelect;
            IsCoolTimeOn = true;
            Player.MarkDirtySettings();
        });

        Utils.NotifyRoles(SpecifySeer: Player);
        return true;
    }
    public override void OnFixedUpdate(PlayerControl player)
    {
        if (!GameStates.IsInTask) return;
        if (KillWaitPlayer == null) return;

        if (Player == null || !Player.IsAlive())
        {
            KillWaitPlayerSelect = null;
            KillWaitPlayer = null;
            return;
        }

        Vector2 GSpos = Player.transform.position;//GSの位置

        var target = KillWaitPlayer;

        if (target == null || !target.IsAlive())
        {
            KillWaitPlayerSelect = null;
            KillWaitPlayer = null;
            return;
        }

        float targetDistance = Vector2.Distance(GSpos, target.transform.position);

        var KillRange = GameOptionsData.KillDistances[Mathf.Clamp(Main.NormalOptions.KillDistance, 0, 2)];
        if (targetDistance <= KillRange && Player.CanMove && target.CanMove)
        {
            ShotLimit--;
            Logger.Info($"{Player.GetNameWithRole()} : 残り{ShotLimit}発", "GrudgeSheriff");
            Player.RpcResetAbilityCooldown();

            if (!CanBeKilledBy(target))
            {
                PlayerState.GetByPlayerId(Player.PlayerId).DeathReason = CustomDeathReason.Misfire;
                Player.RpcMurderPlayer(Player);
                Utils.MarkEveryoneDirtySettings();
                KillWaitPlayerSelect = null;
                KillWaitPlayer = null;

                if (!MisfireKillsTarget.GetBool())
                {
                    Utils.NotifyRoles(); return;
                }
            }
            target.SetRealKiller(Player);
            Player.RpcMurderPlayer(target);
            Utils.MarkEveryoneDirtySettings();
            KillWaitPlayerSelect = null;
            KillWaitPlayer = null;
            SendRPC();
            Utils.NotifyRoles();
        }
    }
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

    public override string GetLowerText(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false, bool isForHud = false)
    {
        //seenが省略の場合seer
        seen ??= seer;
        //seerおよびseenが自分である場合以外は関係なし
        if (!Is(seer) || !Is(seen) || !CanUseKillButton() || isForMeeting) return string.Empty;

        var str = new StringBuilder();
        if (KillWaitPlayerSelect == null)
            str.Append(GetString(isForHud ? "SelectPlayerTagBefore" : "SelectPlayerTagMiniBefore"));
        else
        {
            str.Append(GetString(isForHud ? "SelectPlayerTag" : "SelectPlayerTagMini"));
            str.Append(KillWaitPlayerSelect.GetRealName());
        }
        return str.ToString();
    }

    public override string GetProgressText(bool comms = false)
        => Utils.ColorString(RoleInfo.RoleColor, $"({ShotLimit})");
    public override string GetAbilityButtonText() => GetString("ChangeButtonText");
}