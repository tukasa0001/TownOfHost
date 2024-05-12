using System.Collections.Generic;
using System.Linq;
using Hazel;
using UnityEngine;
using AmongUs.GameOptions;

using TownOfHostForE.Roles.Neutral;
using TownOfHostForE.Roles.Core;
using TownOfHostForE.Roles.Core.Interfaces;
using static TownOfHostForE.Translator;
using static UnityEngine.GraphicsBuffer;

namespace TownOfHostForE.Roles.Crewmate;
public sealed class DogSheriff : RoleBase, ISchrodingerCatOwner
{
    /// <summary>
    ///  20000:TOH4E役職
    ///   1000:陣営 1:crew 2:imp 3:Third 4:Animals
    ///    100:役職ID
    /// </summary>
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(DogSheriff),
            player => new DogSheriff(player),
            CustomRoles.DogSheriff,
            () => RoleTypes.Crewmate,
            CustomRoleTypes.Crewmate,
            21800,
            SetupOptionItem,
            "ドッグシェリフ",
            "#f8cd46",
            introSound: () => GetIntroSound(RoleTypes.Crewmate)
        );
    public DogSheriff(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        ShotLimit = ShotLimitOpt.GetInt();
        CurrentKillCooldown = KillCooldown.GetFloat();
        killRadius = KillRadius.GetFloat();
    }

    private static OptionItem KillCooldown;
    private static OptionItem MisfireKillsTarget;
    private static OptionItem ShotLimitOpt;
    public static OptionItem IsInfoPoor;
    public static OptionItem IsClumsy;
    private static OptionItem CanKillAllAlive;
    public static OptionItem CanKillNeutrals;
    public static OptionItem CanKillAnimals;
    public static OptionItem KillRadius;

    private nowState NowState = nowState.ready;
    private float UpdateTime = 60f;
    private string nowString = "";
    enum OptionName
    {
        SheriffMisfireKillsTarget,
        SheriffShotLimit,
        SheriffIsInfoPoor,
        SheriffIsClumsy,
        SheriffCanKillAllAlive,
        SheriffCanKillNeutrals,
        SheriffCanKillAnimals,
        SheriffCanKill,
        DogSheriffRadius,
    }
    enum nowState
    {
        ready,
        GO
    }

    public static Dictionary<CustomRoles, OptionItem> KillTargetOptions = new();
    public static Dictionary<SchrodingerCat.TeamType, OptionItem> SchrodingerCatKillTargetOptions = new();
    public int ShotLimit = 0;
    public float CurrentKillCooldown = 30;
    public float killRadius = 0.5f;
    public static readonly string[] KillOption =
    {
            "SheriffCanKillAll", "SheriffCanKillSeparately"
        };
    private static void SetupOptionItem()
    {
        KillRadius = FloatOptionItem.Create(RoleInfo, 10, OptionName.DogSheriffRadius, new(0.5f, 3f, 0.5f), 1f, false)
            .SetValueFormat(OptionFormat.Multiplier);
        KillCooldown = FloatOptionItem.Create(RoleInfo, 11, GeneralOption.KillCooldown, new(0f, 180f, 2.5f), 30f, false)
            .SetValueFormat(OptionFormat.Seconds);
        MisfireKillsTarget = BooleanOptionItem.Create(RoleInfo, 12, OptionName.SheriffMisfireKillsTarget, false, false);
        ShotLimitOpt = IntegerOptionItem.Create(RoleInfo, 13, OptionName.SheriffShotLimit, new(1, 15, 1), 15, false)
            .SetValueFormat(OptionFormat.Times);
        IsInfoPoor = BooleanOptionItem.Create(RoleInfo, 14, OptionName.SheriffIsInfoPoor, false, false);
        IsClumsy = BooleanOptionItem.Create(RoleInfo, 15, OptionName.SheriffIsClumsy, false, false);
        CanKillAllAlive = BooleanOptionItem.Create(RoleInfo, 16, OptionName.SheriffCanKillAllAlive, true, false);
        SetUpKillTargetOption(CustomRoles.Madmate, 17);
        CanKillNeutrals = StringOptionItem.Create(RoleInfo, 18, OptionName.SheriffCanKillNeutrals, KillOption, 0, false);
        SetUpNeutralOptions(30);
        CanKillAnimals = StringOptionItem.Create(RoleInfo, 19, OptionName.SheriffCanKillAnimals, KillOption, 0, false);
        SetUpAnimalsOptions(70);
    }
    public SchrodingerCat.TeamType SchrodingerCatChangeTo => SchrodingerCat.TeamType.Crew;
    public static void SetUpNeutralOptions(int idOffset)
    {
        foreach (var neutral in CustomRolesHelper.AllStandardRoles.Where(x => x.IsNeutral()).ToArray())
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
        foreach (var animals in CustomRolesHelper.AllStandardRoles.Where(x => x.IsAnimals()).ToArray())
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
    public override void Add()
    {
        var playerId = Player.PlayerId;
        CurrentKillCooldown = KillCooldown.GetFloat();
        UpdateTime = CurrentKillCooldown;

        ShotLimit = ShotLimitOpt.GetInt();
        NowState = nowState.ready;
        nowString = "Wait";
        Logger.Info($"{Utils.GetPlayerById(playerId)?.GetNameWithRole()} : 残り{ShotLimit}発", "Sheriff");
    }

    public override void AfterMeetingTasks()
    {
        NowState = nowState.ready;
        nowString = "Wait";
        Utils.NotifyRoles(Player);
    }
    private void SendRPC()
    {
        if (!AmongUsClient.Instance.AmHost) return;
        using var sender = CreateSender();
        sender.Writer.Write(ShotLimit);
    }
    public override void ReceiveRPC(MessageReader reader, CustomRPC rpcType)
    {
        ShotLimit = reader.ReadInt32();
    }
    public bool CanUseKillButton()
        => Player.IsAlive()
        && (CanKillAllAlive.GetBool() || GameStates.AlreadyDied)
        && NowState == nowState.GO
        && ShotLimit > 0;
    public override void ApplyGameOptions(IGameOptions opt)
    {
        opt.SetVision(false);
    }


    public override void OnTouchPet(PlayerControl player)
    {
        if (!CanUseKillButton()) return;
        ShotLimit--;
        Logger.Info($"{Player.GetNameWithRole()} : 残り{ShotLimit}発", "DogSheriff");

        Dictionary<float, PlayerControl> KillDic = new();

        //範囲に入っている人算出
        foreach (var pc in Main.AllAlivePlayerControls)
        {
            if (!pc.IsAlive()) continue;

            if (pc == Player) continue;

            float targetDistance = Vector2.Distance(Player.transform.position, pc.transform.position); ;

            bool checker = targetDistance <= killRadius && pc.CanMove;

            if (!checker) continue;

            KillDic.Add(targetDistance, pc);

        }

        if (KillDic.Count == 0) return;
        //距離が一番近い人算出
        var killTargetKeys = KillDic.Keys.OrderBy(x => x).FirstOrDefault();
        var target = KillDic[killTargetKeys];

        if (!CanBeKilledBy(target))
        {
            PlayerState.GetByPlayerId(Player.PlayerId).DeathReason = CustomDeathReason.Misfire;
            Player.RpcMurderPlayer(Player);
            Utils.MarkEveryoneDirtySettings();

            if (!MisfireKillsTarget.GetBool())
            {
                Utils.NotifyRoles();
                return;
            }
        }
        target.SetRealKiller(Player);
        target.RpcMurderPlayer(target);
        Utils.MarkEveryoneDirtySettings();
        SendRPC();
        nowString = ShotLimit <= 0 ? "" : "Wait";
        NowState = nowState.ready;
        Utils.NotifyRoles();
    }

    public override string GetProgressText(bool comms = false) => Utils.ColorString(CanUseKillButton() ? Color.yellow : Color.gray, $"({ShotLimit})");

    public override string GetSuffix(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false)
    {
        if (isForMeeting) return "";
        seen ??= seer;
        if (seer != seen) return "";

        return nowString;
    }

    public static bool CanBeKilledBy(PlayerControl player)
    {
        if (player.GetRoleClass() is SchrodingerCat schrodingerCat)
        {
            if (schrodingerCat.Team == SchrodingerCat.TeamType.None)
            {
                Logger.Warn($"シェリフ({player.GetRealName()})にキルされたシュレディンガーの猫のロールが変化していません", nameof(DogSheriff));
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
    public override void OnFixedUpdate(PlayerControl player)
    {
        if (NowState == nowState.GO || ShotLimit <= 0) return;

        UpdateTime -= Time.fixedDeltaTime;

        if (UpdateTime < 0) UpdateTime = CurrentKillCooldown; //キルクールごとの更新

        if (UpdateTime == CurrentKillCooldown)
        {
            NowState = nowState.GO;
            nowString = "GO!";
            Utils.NotifyRoles();
        }
    }
}