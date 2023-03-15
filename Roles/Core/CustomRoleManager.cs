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
    public static List<Func<PlayerControl, PlayerControl, bool>> OnCheckMurderAsTargets = new();
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
        attemptKiller.ResetKillCooldown();

        // 無効なキルをブロックする処理 必ず最初に実行する
        CheckMurderPatch.CheckForInvalidMurdering(attemptKiller, attemptTarget);

        //キラーがキル能力持ちでなければターゲットのキルチェック処理実行
        var killerRole = appearanceKiller.GetRoleClass();
        if (killerRole?.IsKiller ?? false || !appearanceKiller.Is(CustomRoles.Arsonist))
        {
            foreach (var asTarget in OnCheckMurderAsTargets)
            {
                if (!asTarget(appearanceKiller, attemptTarget))
                    return;
            }
            //RoleBase化されていないターゲット処理
            if (!CheckMurderPatch.OnCheckMurderAsTarget(appearanceKiller, attemptTarget)) return;

        }
        //キラーのキルチェック処理実行
        if (killerRole != null)
        {
            if (!killerRole.OnCheckMurderAsKiller(appearanceKiller, attemptTarget))
            {
                return;
            }
        }
        else
        {
            //RoleBase化されていないキラー処理
            if (!CheckMurderPatch.OnCheckMurderAsKiller(appearanceKiller, attemptTarget))
            {
                return;
            }
        }
        attemptKiller.RpcMurderPlayer(attemptTarget);
    }
    // ==初期化関連処理 ==
    public static void Initialize()
    {
        AllRolesInfo.Do(kvp => kvp.Value.IsEnable = kvp.Key.IsEnable());
        AllActiveRoles.Clear();
        OnCheckMurderAsTargets.Clear();
        MarkOthers.Clear();
        LowerOthers.Clear();
        SuffixOthers.Clear();
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
            case CustomRoles.Sniper:
                Sniper.Add(pc.PlayerId);
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
        AllActiveRoles.Do(roleClass => roleClass.Dispose());
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