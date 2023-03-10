using System;
using System.Linq;
using System.Collections.Generic;
using HarmonyLib;

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
    // # 順番ルール #
    // 0_000_001..1_000_000: 先に実行する必要がある特殊処理
    // 1_000_000..2_000_000: Killer側のキルキャンセル処理
    // 2_000_000..3_000_000: Target側のキルキャンセル処理
    // 3_000_000..: その他の処理
    // # 順番メモ #
    // x: orderがxの時に行われる処理 (クラス名)
    // 1_001_000: シェリフのキルキャンセル・自爆処理 (Sheriff)
    // 1_002_000: アーソニストのキルキャンセル・塗り開始処理 (CheckMurderPatch)
    // 1_500_000: クラス化されていないキルされた側の処理 (CheckMurderPatch)
    // 3_001_000: バウンティハンターのターゲット関連の処理 (BountyHunter)
    // 3_500_000: クラス化されていないキルした側の処理 (CheckMurderPatch)

    public static void OnCheckMurder(PlayerControl attemptKiller, PlayerControl attemptTarget)
        => OnCheckMurder(attemptKiller, attemptTarget, attemptKiller, attemptTarget);
    public static void OnCheckMurder(PlayerControl attemptKiller, PlayerControl attemptTarget, PlayerControl appearanceKiller, PlayerControl appearanceTarget)
    {
        attemptKiller.ResetKillCooldown();

        // このリストへは、先のorderが前のorder以上の値になるようにオブジェクトを挿入していく
        LinkedList<(int order, IEnumerator<int> method, RoleBase role)> methods = new();
        CheckMurderInfo info = new(attemptKiller, attemptTarget, appearanceKiller, appearanceTarget);
        foreach (var role in AllActiveRoles)
        {
            //if (attemptKiller != role.Player || attemptTarget != role.Player) continue;
            var m = role.OnCheckMurder(attemptKiller, attemptTarget, info);
            if (m != null)
                methods.AddFirst((0, m, role));
        }

        // クラス化されていないOnCheckMurder処理
        methods.AddFirst((0, CheckMurderPatch.OnCheckMurder(attemptKiller, attemptTarget, info), null));
        // 無効なキルをブロックする処理 必ず最初に実行する
        methods.AddFirst((-1, CheckMurderPatch.CheckForInvalidMurdering(attemptKiller, attemptTarget, info), null));

        while (methods.Count > 0)
        {
            var pair = methods.First.Value; // 最初のオブジェクトは必ず最小のorderを持つ
            methods.RemoveFirst();
            (_, var method, var role) = pair;
            if (method == null) continue;

            try
            {
                // MoveNext() のタイミングで初めて処理が実行される
                // true: 次の処理順がyield returnされた (= まだ別の処理がある)
                // false: yield breakされた (= もう処理がない)
                if (method.MoveNext())
                {
                    var current = new LinkedListNode<(int order, IEnumerator<int> method, RoleBase role)>((method.Current, method, role));
                    var elem = methods.First;

                    while (true)
                    {
                        // current <= elem <= elem.Next
                        if (current.Value.order <= elem.Value.order)
                        {
                            methods.AddBefore(elem, current);
                            break;
                        }
                        // elemが最終オブジェクト || elem < current <= elem.Next
                        else if (elem.Next == null || current.Value.order <= elem.Next.Value.order)
                        {
                            methods.AddAfter(elem, current);
                            break;
                        }
                        // elem <= elem.Next <= current
                        else
                        {
                            elem = elem.Next;
                        }
                    }
                }
            }
            // 例外発生時: プレイヤー名とorderと例外内容を出力してスキップ
            catch (Exception ex)
            {
                var handler = Logger.Handler("CustomRoleManager.OnCheckMurder");
                handler.Error($"OnCheckMurder関数内でエラーが発生しました ({(role != null ? $"player: {role.Player.name}" : "")}, order: {pair.order})");
                handler.Error($"killer: {attemptKiller.name}, target: {attemptTarget.name}");
                handler.Exception(ex);
            }


            if (info.IsAborted) break;
        }
        if (!info.IsCanceled)
        {
            var (killer, target) = info.AppearanceTuple;
            killer.RpcMurderPlayer(target);
        }
    }
    public class CheckMurderInfo
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

        /// <summary><para>この値がtrueの状態で終了すると、実際のキルが行われません。</para>
        /// <para>他のRoleBaseのCheckMurder処理は通常通り行われます。</para></summary>
        public bool IsCanceled { get; set; } = false;
        /// <summary><para>この値がtrueの状態でyield return/breakが行われると、以降の他のRoleBaseのCheckMurderの処理が行われなくなります。</para>
        /// <para>実際のキルが行われるかどうかはIsCanceledの値に依存します。</para></summary>
        public bool IsAborted { get; set; } = false;

        public CheckMurderInfo(PlayerControl killer, PlayerControl target)
        {
            AttemptKiller = AppearanceKiller = killer;
            AttemptTarget = AttemptTarget = target;
        }
        public CheckMurderInfo(PlayerControl attemptKiller, PlayerControl attemptTarget, PlayerControl appearanceKiller, PlayerControl appearancetarget)
        {
            AttemptKiller = attemptKiller;
            AttemptTarget = attemptTarget;
            AppearanceKiller = appearanceKiller;
            AppearanceTarget = appearancetarget;
        }

        public void Cancel() => IsCanceled = true;
        public void Abort() => IsAborted = true;
        public void CancelAndAbort() => IsCanceled = IsAborted = true;
    }
    // ==/CheckMurder関連処理 ==
    public static void Initialize()
    {
        AllRolesInfo.Do(kvp => kvp.Value.IsEnable = kvp.Key.IsEnable());
        AllActiveRoles.Clear();
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
            case CustomRoles.Snitch:
                Snitch.Add(pc.PlayerId);
                break;
            case CustomRoles.SchrodingerCat:
                SchrodingerCat.Add(pc.PlayerId);
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