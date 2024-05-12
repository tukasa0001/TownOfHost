using System.Collections.Generic;
using System.Linq;

using UnityEngine;

using AmongUs.GameOptions;
using Hazel;

using TownOfHostForE.Modules;
using TownOfHostForE.Roles.Core;
using TownOfHostForE.Roles.Core.Interfaces;

namespace TownOfHostForE.Roles.Neutral;

// マッドが属性化したらマッド状態時の特別扱いを削除する
public sealed class SchrodingerCat : RoleBase, IAdditionalWinner, IDeathReasonSeeable, IKillFlashSeeable
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(SchrodingerCat),
            player => new SchrodingerCat(player),
            CustomRoles.SchrodingerCat,
            () => RoleTypes.Crewmate,
            CustomRoleTypes.Neutral,
            50300,
            SetupOptionItem,
            "シュレディンガーの猫",
            "#696969",
            introSound: () => GetIntroSound(RoleTypes.Impostor)
        );
    public SchrodingerCat(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        CanWinTheCrewmateBeforeChange = OptionCanWinTheCrewmateBeforeChange.GetBool();
        ChangeTeamWhenExile = OptionChangeTeamWhenExile.GetBool();
        CanSeeKillableTeammate = OptionCanSeeKillableTeammate.GetBool();
    }
    static OptionItem OptionCanWinTheCrewmateBeforeChange;
    static OptionItem OptionChangeTeamWhenExile;
    static OptionItem OptionCanSeeKillableTeammate;

    enum OptionName
    {
        CanBeforeSchrodingerCatWinTheCrewmate,
        SchrodingerCatExiledTeamChanges,
        SchrodingerCatCanSeeKillableTeammate,
    }
    static bool CanWinTheCrewmateBeforeChange;
    static bool ChangeTeamWhenExile;
    static bool CanSeeKillableTeammate;

    /// <summary>
    /// 自分をキルしてきた人のロール
    /// </summary>
    private ISchrodingerCatOwner owner = null;
    private TeamType _team = TeamType.None;
    /// <summary>
    /// 現在の所属陣営<br/>
    /// 変更する際は特段の事情がない限り<see cref="RpcSetTeam"/>を使ってください
    /// </summary>
    public TeamType Team
    {
        get => _team;
        private set
        {
            logger.Info($"{Player.GetRealName()}の陣営を{value}に変更");
            _team = value;
        }
    }
    public bool AmMadmate => Team == TeamType.Mad;
    public Color DisplayRoleColor => GetCatColor(Team);
    private static LogHandler logger = Logger.Handler(nameof(SchrodingerCat));

    public static void SetupOptionItem()
    {
        OptionCanWinTheCrewmateBeforeChange = BooleanOptionItem.Create(RoleInfo, 10, OptionName.CanBeforeSchrodingerCatWinTheCrewmate, false, false);
        OptionChangeTeamWhenExile = BooleanOptionItem.Create(RoleInfo, 11, OptionName.SchrodingerCatExiledTeamChanges, false, false);
        OptionCanSeeKillableTeammate = BooleanOptionItem.Create(RoleInfo, 12, OptionName.SchrodingerCatCanSeeKillableTeammate, false, false);
    }
    public override void ApplyGameOptions(IGameOptions opt)
    {
        owner?.ApplySchrodingerCatOptions(opt);
    }
    /// <summary>
    /// マッド猫用のオプション構築
    /// </summary>
    public static void ApplyMadCatOptions(IGameOptions opt)
    {
        if (Options.MadmateHasImpostorVision.GetBool())
        {
            opt.SetVision(true);
        }
        if (Options.MadmateCanSeeOtherVotes.GetBool())
        {
            opt.SetBool(BoolOptionNames.AnonymousVotes, false);
        }
    }
    public override bool OnCheckMurderAsTarget(MurderInfo info)
    {
        var killer = info.AttemptKiller;

        //自殺ならスルー
        if (info.IsSuicide) return true;

        if (Team == TeamType.None)
        {
            info.CanKill = false;
            ChangeTeamOnKill(killer);
            return false;
        }
        return true;
    }
    /// <summary>
    /// キルしてきた人に応じて陣営の状態を変える
    /// </summary>
    private void ChangeTeamOnKill(PlayerControl killer)
    {
        killer.RpcProtectedMurderPlayer(Player);
        if (killer.GetRoleClass() is ISchrodingerCatOwner catOwner)
        {
            catOwner.OnSchrodingerCatKill(this);
            RpcSetTeam(catOwner.SchrodingerCatChangeTo);
            owner = catOwner;
        }
        else
        {
            logger.Warn($"未知のキル役職からのキル: {killer.GetNameWithRole()}");
        }

        RevealNameColors(killer);

        Utils.NotifyRoles();
        Utils.MarkEveryoneDirtySettings();
    }
    /// <summary>
    /// キルしてきた人とオプションに応じて名前の色を開示する
    /// </summary>
    private void RevealNameColors(PlayerControl killer)
    {
        if (CanSeeKillableTeammate)
        {
            var killerRoleId = killer.GetCustomRole();
            var killerTeam = Main.AllPlayerControls.Where(player => (AmMadmate && player.Is(CustomRoleTypes.Impostor)) || player.Is(killerRoleId));
            foreach (var member in killerTeam)
            {
                NameColorManager.Add(member.PlayerId, Player.PlayerId, RoleInfo.RoleColorCode);
                NameColorManager.Add(Player.PlayerId, member.PlayerId);
            }
        }
        else
        {
            NameColorManager.Add(killer.PlayerId, Player.PlayerId, RoleInfo.RoleColorCode);
            NameColorManager.Add(Player.PlayerId, killer.PlayerId);
        }
    }
    public override void OverrideTrueRoleName(ref Color roleColor, ref string roleText)
    {
        // 陣営変化前なら上書き不要
        if (Team == TeamType.None)
        {
            return;
        }
        roleColor = DisplayRoleColor;
    }
    public override void OnExileWrapUp(GameData.PlayerInfo exiled, ref bool DecidedWinner)
    {
        if (exiled.PlayerId != Player.PlayerId || Team != TeamType.None || !ChangeTeamWhenExile)
        {
            return;
        }
        ChangeTeamRandomly();
    }
    /// <summary>
    /// ゲームに存在している陣営の中からランダムに自分の陣営を変更する
    /// </summary>
    private void ChangeTeamRandomly()
    {
        var rand = IRandom.Instance;
        List<TeamType> candidates = new(4)
        {
            TeamType.Crew,
            TeamType.Mad,
        };
        if (CustomRoles.Egoist.IsPresent())
        {
            candidates.Add(TeamType.Egoist);
        }
        if (CustomRoles.Jackal.IsPresent())
        {
            candidates.Add(TeamType.Jackal);
        }
        if (CustomRoles.Coyote.IsPresent() ||
            CustomRoles.Braki.IsPresent() ||
            CustomRoles.Nyaoha.IsPresent() ||
            CustomRoles.Kraken.IsPresent() ||
            CustomRoles.Leopard.IsPresent())
        {
            candidates.Add(TeamType.Animals);
        }
        if (CustomRoles.DarkHide.IsPresent())
        {
            candidates.Add(TeamType.DarkHide);
        }
        if (CustomRoles.Opportunist.IsPresent())
        {
            candidates.Add(TeamType.Opportunist);
        }
        if (CustomRoles.Gizoku.IsPresent())
        {
            candidates.Add(TeamType.Gizoku);
        }
        var team = candidates[rand.Next(candidates.Count)];
        RpcSetTeam(team);
    }
    public bool CheckWin(ref CustomRoles winnerRole)
    {
        bool? won = Team switch
        {
            TeamType.None => CustomWinnerHolder.WinnerTeam == CustomWinner.Crewmate && CanWinTheCrewmateBeforeChange,
            TeamType.Mad => CustomWinnerHolder.WinnerTeam == CustomWinner.Impostor,
            TeamType.Crew => CustomWinnerHolder.WinnerTeam == CustomWinner.Crewmate,
            TeamType.Jackal => CustomWinnerHolder.WinnerTeam == CustomWinner.Jackal,
            TeamType.Egoist => CustomWinnerHolder.WinnerTeam == CustomWinner.Egoist,
            TeamType.Animals => CustomWinnerHolder.WinnerTeam == CustomWinner.Animals,
            TeamType.DarkHide => CustomWinnerHolder.WinnerTeam == CustomWinner.DarkHide,
            TeamType.Gizoku => CustomWinnerHolder.WinnerTeam == CustomWinner.Gizoku,
            TeamType.Opportunist => CustomWinnerHolder.AdditionalWinnerRoles.Contains(CustomRoles.Opportunist),
            _ => null,
        };
        if (!won.HasValue)
        {
            logger.Warn($"不明な猫の勝利チェック: {Team}");
            return false;
        }
        return won.Value;
    }
    public void RpcSetTeam(TeamType team)
    {
        Team = team;
        if (AmongUsClient.Instance.AmHost)
        {
            using var sender = CreateSender();
            sender.Writer.Write((byte)team);
        }
    }
    public override void ReceiveRPC(MessageReader reader)
    {
        Team = (TeamType)reader.ReadByte();
    }

    // マッド属性化までの間マッド状態時に特別扱いするための応急処置的個別実装
    // マッドが属性化したらマッド状態のシュレ猫にマッド属性を付与することで削除
    // 上にあるApplyMadCatOptions，MeetingHudPatchにある道連れ処理，ShipStatusPatchにあるサボ直しキャンセル処理も同様 - Hyz-sui
    public bool CheckSeeDeathReason(PlayerControl seen) => AmMadmate && Options.MadmateCanSeeDeathReason.GetBool();
    public bool CheckKillFlash(MurderInfo info) => AmMadmate && Options.MadmateCanSeeKillFlash.GetBool();

    /// <summary>
    /// 陣営状態
    /// </summary>
    public enum TeamType : byte
    {
        /// <summary>
        /// どこの陣営にも属していない状態
        /// </summary>
        None = 0,

        // 10-49 シェリフキルオプションを作成しない変化先

        /// <summary>
        /// インポスター陣営に所属する状態
        /// </summary>
        Mad = 10,
        /// <summary>
        /// クルー陣営に所属する状態
        /// </summary>
        Crew,

        // 50- シェリフキルオプションを作成する変化先

        /// <summary>
        /// ジャッカル陣営に所属する状態
        /// </summary>
        Jackal = 50,
        /// <summary>
        /// エゴイスト陣営に所属する状態
        /// </summary>
        Egoist,
        /// <summary>
        /// アニマルズに所属する状態
        /// </summary>
        Animals,
        /// <summary>
        /// ダークハイドに所属する状態
        /// </summary>
        DarkHide,
        /// <summary>
        /// オポチュニストキラーに所属する状態
        /// </summary>
        Opportunist,
        /// <summary>
        /// 義賊に所属する状態
        /// </summary>
        Gizoku,
    }
    public static Color GetCatColor(TeamType catType)
    {
        Color? color = catType switch
        {
            TeamType.None => RoleInfo.RoleColor,
            TeamType.Mad => Utils.GetRoleColor(CustomRoles.Madmate),
            TeamType.Crew => Utils.GetRoleColor(CustomRoles.Crewmate),
            TeamType.Jackal => Utils.GetRoleColor(CustomRoles.Jackal),
            TeamType.Egoist => Utils.GetRoleColor(CustomRoles.Egoist),
            TeamType.Animals => Utils.GetRoleColor(CustomRoles.Coyote),
            TeamType.DarkHide => Utils.GetRoleColor(CustomRoles.DarkHide),
            TeamType.Opportunist => Utils.GetRoleColor(CustomRoles.Opportunist),
            TeamType.Gizoku => Utils.GetRoleColor(CustomRoles.Gizoku),
            _ => null,
        };
        if (!color.HasValue)
        {
            logger.Warn($"不明な猫に対する色の取得: {catType}");
            return Utils.GetRoleColor(CustomRoles.Crewmate);
        }
        return color.Value;
    }
}


//using System.Collections.Generic;
//using System.Linq;
//using AmongUs.GameOptions;

//using TownOfHostForE.Roles.Core;
//using TownOfHostForE.Roles.Core.Interfaces;
//using TownOfHostForE.Roles.Impostor;

//namespace TownOfHostForE.Roles.Neutral;
//public sealed class SchrodingerCat : RoleBase, IAdditionalWinner
//{
//    public static readonly SimpleRoleInfo RoleInfo =
//        SimpleRoleInfo.Create(
//            typeof(SchrodingerCat),
//            player => new SchrodingerCat(player),
//            CustomRoles.SchrodingerCat,
//            () => RoleTypes.Crewmate,
//            CustomRoleTypes.Neutral,
//            50300,
//            SetupOptionItem,
//            "シュレディンガーの猫",
//            "#696969",
//            introSound: () => GetIntroSound(RoleTypes.Impostor)
//        );
//    public SchrodingerCat(PlayerControl player)
//    : base(
//        RoleInfo,
//        player
//    )
//    {
//        CanWinTheCrewmateBeforeChange = OptionCanWinTheCrewmateBeforeChange.GetBool();
//        ChangeTeamWhenExile = OptionChangeTeamWhenExile.GetBool();
//        CanSeeKillableTeammate = OptionCanSeeKillableTeammate.GetBool();
//    }
//    static OptionItem OptionCanWinTheCrewmateBeforeChange;
//    static OptionItem OptionChangeTeamWhenExile;
//    static OptionItem OptionCanSeeKillableTeammate;

//    enum OptionName
//    {
//        CanBeforeSchrodingerCatWinTheCrewmate,
//        SchrodingerCatExiledTeamChanges,
//        SchrodingerCatCanSeeKillableTeammate,
//    }
//    static bool CanWinTheCrewmateBeforeChange;
//    static bool ChangeTeamWhenExile;
//    static bool CanSeeKillableTeammate;

//    /// <summary>
//    /// 自分をキルしてきた人のロール
//    /// </summary>
//    private ISchrodingerCatOwner owner = null;
//    private TeamType _team = TeamType.None;
//    /// <summary>
//    /// 現在の所属陣営<br/>
//    /// 変更する際は特段の事情がない限り<see cref="RpcSetTeam"/>を使ってください
//    /// </summary>
//    public TeamType Team
//    {
//        get => _team;
//        private set
//        {
//            logger.Info($"{Player.GetRealName()}の陣営を{value}に変更");
//            _team = value;
//        }
//    }
//    public bool AmMadmate => Team == TeamType.Mad;
//    public Color DisplayRoleColor => GetCatColor(Team);
//    private static LogHandler logger = Logger.Handler(nameof(SchrodingerCat));

//    public static void SetupOptionItem()
//    {
//        OptionCanWinTheCrewmateBeforeChange = BooleanOptionItem.Create(RoleInfo, 10, OptionName.CanBeforeSchrodingerCatWinTheCrewmate, false, false);
//        OptionChangeTeamWhenExile = BooleanOptionItem.Create(RoleInfo, 11, OptionName.SchrodingerCatExiledTeamChanges, false, false);
//        OptionCanSeeKillableTeammate = BooleanOptionItem.Create(RoleInfo, 12, OptionName.SchrodingerCatCanSeeKillableTeammate, false, false);
//    }
//    public override void ApplyGameOptions(IGameOptions opt)
//    {
//        owner?.ApplySchrodingerCatOptions(opt);
//    }
//    /// <summary>
//    /// マッド猫用のオプション構築
//    /// </summary>
//    public static void ApplyMadCatOptions(IGameOptions opt)
//    {
//        if (Options.MadmateHasImpostorVision.GetBool())
//        {
//            opt.SetVision(true);
//        }
//        if (Options.MadmateCanSeeOtherVotes.GetBool())
//        {
//            opt.SetBool(BoolOptionNames.AnonymousVotes, false);
//        }
//    }
//    public override bool OnCheckMurderAsTarget(MurderInfo info)
//    {
//        if (!Is(info.AttemptTarget) || info.IsSuicide) return true;

//        (var killer, var target) = info.AttemptTuple;
//        // 直接キル出来る役職チェック
//        if (killer.GetCustomRole().IsDirectKillRole()) return true;
//        //既に変化していたらスルー
//        if (!target.Is(CustomRoles.SchrodingerCat)) return true;

//        //シュレディンガーの猫が切られた場合の役職変化スタート
//        killer.RpcProtectedMurderPlayer(target);
//        info.CanKill = false;
//        switch (killer.GetCustomRole())
//        {
//            case CustomRoles.BountyHunter:
//                var bountyHunter = (BountyHunter)killer.GetRoleClass();
//                if (bountyHunter.GetTarget() == target)
//                    bountyHunter.ResetTarget();//ターゲットの選びなおし
//                break;
//            case CustomRoles.SerialKiller:
//                var serialKiller = (SerialKiller)killer.GetRoleClass();
//                serialKiller.SuicideTimer = null;
//                break;
//            case CustomRoles.Sheriff:
//            case CustomRoles.Hunter:
//            case CustomRoles.SillySheriff:
//                target.RpcSetCustomRole(CustomRoles.CSchrodingerCat);
//                break;
//            case CustomRoles.Egoist:
//                target.RpcSetCustomRole(CustomRoles.EgoSchrodingerCat);
//                break;
//            case CustomRoles.Jackal:
//                target.RpcSetCustomRole(CustomRoles.JSchrodingerCat);
//                break;
//            case CustomRoles.DarkHide:
//                target.RpcSetCustomRole(CustomRoles.DSchrodingerCat);
//                break;
//            case CustomRoles.Opportunist:
//                target.RpcSetCustomRole(CustomRoles.OSchrodingerCat);
//                break;
//            case CustomRoles.Gizoku:
//                target.RpcSetCustomRole(CustomRoles.GSchrodingerCat);
//                break;
//            case CustomRoles.Coyote:
//            case CustomRoles.Braki:
//            case CustomRoles.Leopard:
//                target.RpcSetCustomRole(CustomRoles.ASchrodingerCat);
//                break;
//    	}
//        if (killer.Is(CustomRoleTypes.Impostor))
//            target.RpcSetCustomRole(CustomRoles.MSchrodingerCat);

//        if (CanSeeKillableTeammate)
//        {
//            var roleType = killer.GetCustomRole().GetCustomRoleTypes();
//            System.Func<PlayerControl, bool> isTarget = roleType switch
//            {
//                CustomRoleTypes.Impostor => (pc) => pc.GetCustomRole().GetCustomRoleTypes() == roleType,
//                _ => (pc) => pc.GetCustomRole() == killer.GetCustomRole()
//            };
//            ;
//            var killerTeam = Main.AllPlayerControls.Where(pc => isTarget(pc));
//            foreach (var member in killerTeam)
//            {
//                NameColorManager.Add(member.PlayerId, target.PlayerId, RoleInfo.RoleColorCode);
//                NameColorManager.Add(target.PlayerId, member.PlayerId);
//            }
//        }
//        else
//        {
//            NameColorManager.Add(killer.PlayerId, target.PlayerId, RoleInfo.RoleColorCode);
//            NameColorManager.Add(target.PlayerId, killer.PlayerId);
//        }
//        Utils.NotifyRoles();
//        Utils.MarkEveryoneDirtySettings();
//        //シュレディンガーの猫の役職変化処理終了
//        //ニュートラルのキル能力持ちが追加されたら、その陣営を味方するシュレディンガーの猫の役職を作って上と同じ書き方で書いてください
//        return false;
//    }
//    public static void ChangeTeam(PlayerControl player)
//    {
//        if (!(ChangeTeamWhenExile && player.Is(CustomRoles.SchrodingerCat))) return;

//        var rand = IRandom.Instance;
//        List<CustomRoles> Rand = new()
//            {
//                CustomRoles.CSchrodingerCat,
//                CustomRoles.MSchrodingerCat
//            };
//        foreach (var pc in Main.AllAlivePlayerControls)
//        {
//            if (pc.Is(CustomRoles.Egoist) && !Rand.Contains(CustomRoles.EgoSchrodingerCat))
//                Rand.Add(CustomRoles.EgoSchrodingerCat);
//            if (pc.Is(CustomRoles.Jackal) && !Rand.Contains(CustomRoles.JSchrodingerCat))
//                Rand.Add(CustomRoles.JSchrodingerCat);
//            if (pc.Is(CustomRoles.DarkHide) && !Rand.Contains(CustomRoles.DSchrodingerCat))
//                Rand.Add(CustomRoles.DSchrodingerCat);
//            if (pc.Is(CustomRoles.Gizoku) && !Rand.Contains(CustomRoles.GSchrodingerCat))
//                Rand.Add(CustomRoles.GSchrodingerCat);
//            if ((pc.Is(CustomRoles.Coyote) && !Rand.Contains(CustomRoles.ASchrodingerCat)) ||
//                (pc.Is(CustomRoles.Braki) && !Rand.Contains(CustomRoles.ASchrodingerCat)) ||
//                (pc.Is(CustomRoles.Leopard) && !Rand.Contains(CustomRoles.ASchrodingerCat)))
//                Rand.Add(CustomRoles.ASchrodingerCat);
//        }
//        var Role = Rand[rand.Next(Rand.Count)];
//        player.RpcSetCustomRole(Role);
//    }
//    public bool CheckWin(out AdditionalWinners winnerType)
//    {
//        winnerType = AdditionalWinners.SchrodingerCat;
//        return CustomWinnerHolder.WinnerTeam == CustomWinner.Crewmate && CanWinTheCrewmateBeforeChange;
//    }
//}
