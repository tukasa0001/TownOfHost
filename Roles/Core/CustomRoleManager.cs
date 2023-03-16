using System;
using System.Linq;
using System.Collections.Generic;
using HarmonyLib;
using Hazel;
using Il2CppSystem.Text;

using AmongUs.GameOptions;
using TownOfHost.Roles.Impostor;
using TownOfHost.Roles.Neutral;
using TownOfHost.Roles.Crewmate;

namespace TownOfHost.Roles.Core;

public static class CustomRoleManager
{
    public static Type[] AllRolesClassType;
    public static Dictionary<CustomRoles, SimpleRoleInfo> AllRolesInfo = new(Enum.GetValues(typeof(CustomRoles)).Length);
    public static List<RoleBase> AllActiveRoles = new(Enum.GetValues(typeof(CustomRoles)).Length);

    public static SimpleRoleInfo GetRoleInfo(this CustomRoles role) => AllRolesInfo.ContainsKey(role) ? AllRolesInfo[role] : null;
    public static RoleBase GetRoleClass(this PlayerControl player) => GetByPlayerId(player.PlayerId);
    public static RoleBase GetByPlayerId(byte playerId) => AllActiveRoles.ToArray().Where(roleClass => roleClass.Player.PlayerId == playerId).FirstOrDefault();
    public static void Do<T>(this List<T> list, Action<T> action) => list.ToArray().Do(action);
    // == CheckMurder関連処理 ==
    public static Dictionary<byte, MurderInfo> CheckMurderInfos = new();
    /// <summary>
    /// 
    /// </summary>
    /// <param name="attemptKiller">実際にキルを行ったプレイヤー 不変</param>
    /// <param name="attemptTarget">>Killerが実際にキルを行おうとしたプレイヤー 不変</param>
    public static void OnCheckMurder(PlayerControl attemptKiller, PlayerControl attemptTarget)
        => OnCheckMurder(attemptKiller, attemptTarget, attemptKiller, attemptTarget);
    /// <summary>
    /// 
    /// </summary>
    /// <param name="attemptKiller">実際にキルを行ったプレイヤー 不変</param>
    /// <param name="attemptTarget">>Killerが実際にキルを行おうとしたプレイヤー 不変</param>
    /// <param name="appearanceKiller">見た目上でキルを行うプレイヤー 可変</param>
    /// <param name="appearanceTarget">見た目上でキルされるプレイヤー 可変</param>
    public static void OnCheckMurder(PlayerControl attemptKiller, PlayerControl attemptTarget, PlayerControl appearanceKiller, PlayerControl appearanceTarget)
    {
        var info = new MurderInfo(attemptKiller, attemptTarget, appearanceKiller, appearanceTarget);

        appearanceKiller.ResetKillCooldown();

        // 無効なキルをブロックする処理 必ず最初に実行する
        if (!CheckMurderPatch.CheckForInvalidMurdering(info)) return;

        var killerRole = attemptKiller.GetRoleClass();
        var targetRole = attemptTarget.GetRoleClass();

        //キラーがキル能力持ちでなければターゲットのキルチェック処理実行
        if ((killerRole?.IsKiller ?? false) || !attemptKiller.Is(CustomRoles.Arsonist))
        {
            if (targetRole != null)
            {
                if (!targetRole.OnCheckMurderAsTarget(info)) return;
            }
            else
            {
                //RoleBase化されていないターゲット処理
                if (!CheckMurderPatch.OnCheckMurderAsTarget(info)) return;
            }

        }
        //キラーのキルチェック処理実行
        if (killerRole != null)
        {
            if (!killerRole.OnCheckMurderAsKiller(info)) return;
        }
        else
        {
            //RoleBase化されていないキラー処理
            if (!CheckMurderPatch.OnCheckMurderAsKiller(info)) return;
        }
        //MurderPlayer用にinfoを保存
        CheckMurderInfos[appearanceKiller.PlayerId] = info;
        appearanceKiller.RpcMurderPlayer(appearanceTarget);
    }
    /// <summary>
    /// 
    /// </summary>
    /// <param name="appearanceKiller">見た目上でキルを行うプレイヤー 可変</param>
    /// <param name="appearanceTarget">見た目上でキルされるプレイヤー 可変</param>
    public static void OnMurderPlayer(PlayerControl appearanceKiller, PlayerControl appearanceTarget)
    {
        //MurderInfoの取得
        //CheckMurderを経由していない場合はappearanceで処理
        CheckMurderInfos.TryGetValue(appearanceKiller.PlayerId, out var info);

        var attemptKiller = info?.AttemptKiller ?? appearanceKiller;
        var attemptTarget = info?.AttemptTarget ?? appearanceTarget;

        Logger.Info($"Real Killer={attemptKiller.GetNameWithRole()}", "MurderPlayer");

        attemptKiller.GetRoleClass()?.OnMurderPlayerAsKiller(info);
        var targetRole = attemptTarget.GetRoleClass();
        if (targetRole != null)
            targetRole.OnMurderPlayerAsTarget(info);
        else
            OnMurderPlayerAsTarget(attemptKiller, attemptTarget, info.IsSuicide);

        //以降共通処理
        if (Main.PlayerStates[attemptTarget.PlayerId].deathReason == PlayerState.DeathReason.etc)
        {
            //死因が設定されていない場合は死亡判定
            Main.PlayerStates[attemptTarget.PlayerId].deathReason = PlayerState.DeathReason.Kill;
        }

        Main.PlayerStates[attemptTarget.PlayerId].SetDead();
        attemptTarget.SetRealKiller(attemptKiller, true); //既に追加されてたらスキップ
        Utils.CountAlivePlayers(true);
        Utils.SyncAllSettings();
        Utils.NotifyRoles();
        Utils.TargetDies(appearanceKiller, attemptTarget);

        CheckMurderInfos.Remove(appearanceKiller.PlayerId);
    }
    public static void OnMurderPlayerAsTarget(PlayerControl attemptKiller, PlayerControl attemptTarget, bool suicide)
    {
        //RoleClass非対応の処理
        if (attemptTarget.Is(CustomRoles.Bait) && !suicide)
        {
            Logger.Info(attemptTarget?.Data?.PlayerName + "はBaitだった", "MurderPlayer");
            new LateTask(() => attemptKiller.CmdReportDeadBody(attemptTarget.Data), 0.15f, "Bait Self Report");
        }
        else if (attemptTarget.Is(CustomRoles.Terrorist))
        {
            Logger.Info(attemptTarget?.Data?.PlayerName + "はTerroristだった", "MurderPlayer");
            Utils.CheckTerroristWin(attemptTarget.Data);
        }
        else if (attemptTarget.Is(CustomRoles.Trapper) && !suicide)
            attemptKiller.TrapperKilled(attemptTarget);
        else if (Executioner.Target.ContainsValue(attemptTarget.PlayerId))
            Executioner.ChangeRoleByTarget(attemptTarget);
        else if (attemptTarget.Is(CustomRoles.Executioner) && Executioner.Target.ContainsKey(attemptTarget.PlayerId))
        {
            Executioner.Target.Remove(attemptTarget.PlayerId);
            Executioner.SendRPC(attemptTarget.PlayerId);
        }

        FixedUpdatePatch.LoversSuicide(attemptTarget.PlayerId);

    }

    // ==初期化関連処理 ==
    public static void Initialize()
    {
        AllRolesInfo.Do(kvp => kvp.Value.IsEnable = kvp.Key.IsEnable());
        AllActiveRoles.Clear();
        MarkOthers.Clear();
        LowerOthers.Clear();
        SuffixOthers.Clear();
        CheckMurderInfos.Clear();
    }
    public static void CreateInstance()
    {
        foreach (var pc in Main.AllPlayerControls)
        {
            CreateInstance(pc.GetCustomRole(), pc);
        }
    }
    public static void CreateInstance(CustomRoles role, PlayerControl player)
    {
        if (AllRolesInfo.TryGetValue(role, out var roleInfo))
        {
            roleInfo.CreateInstance(player).Add();
        }
        else
        {
            OtherRolesAdd(player);
        }
        if (player.Data.Role.Role == RoleTypes.Shapeshifter) Main.CheckShapeshift.Add(player.PlayerId, false);

    }
    public static void OtherRolesAdd(PlayerControl pc)
    {
        switch (pc.GetCustomRole())
        {
            case CustomRoles.SerialKiller:
                SerialKiller.Add(pc.PlayerId);
                break;
            case CustomRoles.Witch:
                Witch.Add(pc.PlayerId);
                break;
            case CustomRoles.Warlock:
                Main.CursedPlayers.Add(pc.PlayerId, null);
                Main.isCurseAndKill.Add(pc.PlayerId, false);
                break;
            case CustomRoles.FireWorks:
                FireWorks.Add(pc.PlayerId);
                break;
            case CustomRoles.TimeThief:
                TimeThief.Add(pc.PlayerId);
                break;
            case CustomRoles.Mare:
                Mare.Add(pc.PlayerId);
                break;
            case CustomRoles.Vampire:
                Vampire.Add(pc.PlayerId);
                break;

            case CustomRoles.Arsonist:
                foreach (var ar in Main.AllPlayerControls)
                    Main.isDoused.Add((pc.PlayerId, ar.PlayerId), false);
                break;
            case CustomRoles.Executioner:
                Executioner.Add(pc.PlayerId);
                break;
            case CustomRoles.Egoist:
                Egoist.Add(pc.PlayerId);
                break;
            case CustomRoles.Jackal:
                Jackal.Add(pc.PlayerId);
                break;

            case CustomRoles.Mayor:
                Main.MayorUsedButtonCount[pc.PlayerId] = 0;
                break;
            case CustomRoles.SabotageMaster:
                SabotageMaster.Add(pc.PlayerId);
                break;
            case CustomRoles.EvilTracker:
                EvilTracker.Add(pc.PlayerId);
                break;
            case CustomRoles.TimeManager:
                TimeManager.Add(pc.PlayerId);
                break;
        }
        foreach (var subRole in pc.GetCustomSubRoles())
        {
            switch (subRole)
            {
                // ここに属性のAddを追加
                default:
                    break;
            }
        }
    }
    /// <summary>
    /// 受信したRPCから送信先を読み取ってRoleClassに配信する
    /// </summary>
    /// <param name="reader"></param>
    /// <param name="rpcType"></param>
    public static void DispatchRpc(MessageReader reader, CustomRPC rpcType)
    {
        var playerId = reader.ReadByte();
        AllActiveRoles.FirstOrDefault(r => r.Player.PlayerId == playerId)?.ReceiveRPC(reader, rpcType);
    }
    //NameSystem
    public static HashSet<Func<PlayerControl, PlayerControl, bool, string>> MarkOthers = new();
    public static HashSet<Func<PlayerControl, PlayerControl, bool, bool, string>> LowerOthers = new();
    public static HashSet<Func<PlayerControl, PlayerControl, bool, string>> SuffixOthers = new();
    /// <summary>
    /// seer,seenが役職であるかに関わらず発動するMark
    /// 登録されたすべてを結合する。
    /// </summary>
    /// <param name="seer">見る側</param>
    /// <param name="seen">見られる側</param>
    /// <param name="isForMeeting">会議中フラグ</param>
    /// <returns>結合したMark</returns>
    public static string GetMarkOthers(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false)
    {
        var sb = new StringBuilder(100);
        foreach (var marker in MarkOthers)
        {
            sb.Append(marker(seer, seen, isForMeeting));
        }
        return sb.ToString();
    }
    /// <summary>
    /// seer,seenが役職であるかに関わらず発動するLowerText
    /// 登録されたすべてを結合する。
    /// </summary>
    /// <param name="seer">見る側</param>
    /// <param name="seen">見られる側</param>
    /// <param name="isForMeeting">会議中フラグ</param>
    /// <param name="isForHud">ModでHudとして表示する場合</param>
    /// <returns>結合したLowerText</returns>
    public static string GetLowerTextOthers(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false, bool isForHud = false)
    {
        var sb = new StringBuilder(100);
        foreach (var lower in LowerOthers)
        {
            sb.Append(lower(seer, seen, isForMeeting, isForHud));
        }
        return sb.ToString();
    }
    /// <summary>
    /// seer,seenが役職であるかに関わらず発動するSuffix
    /// 登録されたすべてを結合する。
    /// </summary>
    /// <param name="seer">見る側</param>
    /// <param name="seen">見られる側</param>
    /// <param name="isForMeeting">会議中フラグ</param>
    /// <returns>結合したSuffix</returns>
    public static string GetSuffixOthers(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false)
    {
        var sb = new StringBuilder(100);
        foreach (var suffix in SuffixOthers)
        {
            sb.Append(suffix(seer, seen, isForMeeting));
        }
        return sb.ToString();
    }
    /// <summary>
    /// オブジェクトの破棄
    /// </summary>
    public static void Dispose()
    {
        MarkOthers.Clear();
        LowerOthers.Clear();
        SuffixOthers.Clear();
        CheckMurderInfos.Clear();
        AllActiveRoles.Do(roleClass => roleClass.Dispose());
    }
}
public class MurderInfo
{
    /// <summary>実際にキルを行ったプレイヤー 不変</summary>
    public PlayerControl AttemptKiller { get; }
    /// <summary>Killerが実際にキルを行おうとしたプレイヤー 不変</summary>
    public PlayerControl AttemptTarget { get; }
    /// <summary>見た目上でキルを行うプレイヤー 可変</summary>
    public PlayerControl AppearanceKiller { get; set; }
    /// <summary>見た目上でキルされるプレイヤー 可変</summary>
    public PlayerControl AppearanceTarget { get; set; }

    // 分解用 (killer, target) = info.AttemptTuple; のような記述でkillerとtargetをまとめて取り出せる
    public (PlayerControl killer, PlayerControl target) AttemptTuple => (AttemptKiller, AttemptTarget);
    public (PlayerControl killer, PlayerControl target) AppearanceTuple => (AppearanceKiller, AppearanceTarget);
    /// <summary>
    /// 本来の自殺
    /// </summary>
    public bool IsSuicide => AttemptKiller.PlayerId == AttemptTarget.PlayerId;
    /// <summary>
    /// 遠距離キル代わりの疑似自殺
    /// </summary>
    public bool IsFakeSuicide => AppearanceKiller.PlayerId == AppearanceTarget.PlayerId;
    public MurderInfo(PlayerControl attemptKiller, PlayerControl attemptTarget, PlayerControl appearanceKiller, PlayerControl appearancetarget)
    {
        AttemptKiller = attemptKiller;
        AttemptTarget = attemptTarget;
        AppearanceKiller = appearanceKiller;
        AppearanceTarget = appearancetarget;
    }
}

public enum CustomRoles
{
    //Default
    Crewmate = 0,
    //Impostor(Vanilla)
    Impostor,
    Shapeshifter,
    //Impostor
    BountyHunter,
    EvilWatcher,
    FireWorks,
    Mafia,
    SerialKiller,
    ShapeMaster,
    Sniper,
    Vampire,
    Witch,
    Warlock,
    Mare,
    Puppeteer,
    TimeThief,
    EvilTracker,
    //Madmate
    MadGuardian,
    Madmate,
    MadSnitch,
    SKMadmate,
    MSchrodingerCat,//インポスター陣営のシュレディンガーの猫
    //両陣営
    Watcher,
    //Crewmate(Vanilla)
    Engineer,
    GuardianAngel,
    Scientist,
    //Crewmate
    Bait,
    Lighter,
    Mayor,
    NiceWatcher,
    SabotageMaster,
    Sheriff,
    Snitch,
    SpeedBooster,
    Trapper,
    Dictator,
    Doctor,
    Seer,
    TimeManager,
    CSchrodingerCat,//クルー陣営のシュレディンガーの猫
    //Neutral
    Arsonist,
    Egoist,
    EgoSchrodingerCat,//エゴイスト陣営のシュレディンガーの猫
    Jester,
    Opportunist,
    SchrodingerCat,//無所属のシュレディンガーの猫
    Terrorist,
    Executioner,
    Jackal,
    JSchrodingerCat,//ジャッカル陣営のシュレディンガーの猫
    //HideAndSeek
    HASFox,
    HASTroll,
    //GM
    GM,
    // Sub-roll after 500
    NotAssigned = 500,
    LastImpostor,
    Lovers,
    Workhorse,
}
public enum CustomRoleTypes
{
    Crewmate,
    Impostor,
    Neutral,
    Madmate
}