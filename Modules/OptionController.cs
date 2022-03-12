using System;
using System.Collections.Generic;

namespace TownOfHost
{
    static class CustomOptionController
    {
        public static PageObject basePage;
        public static PageObject currentPage = basePage;
        public static int currentCursor = 0;
        public static void begin() {
            basePage = new PageObject(
                null,
                () => "Town Of Host Settings",
                false,
                () => Logger.SendInGame("このテキストが出るのはバグです。開発者にご報告ください。")
            );
            currentPage = basePage;

            //ページ追加など
            var RoleOptions = new PageObject(basePage, () => main.tr.getString("RoleOptions"));
            //役職数変更
            //陣営＞サイドキック＞アルファベット
            ///インポスター役職
            var BountyHunter = new PageObject(RoleOptions, CustomRoles.BountyHunter);
            var SerialKiller = new PageObject(RoleOptions, CustomRoles.SerialKiller);
            var ShapeMaster = new PageObject(RoleOptions, CustomRoles.ShapeMaster);
            var Vampire = new PageObject(RoleOptions, CustomRoles.Vampire);
            var Warlock = new PageObject(RoleOptions, CustomRoles.Warlock);
            var Witch = new PageObject(RoleOptions, CustomRoles.Witch);
            var Mafia = new PageObject(RoleOptions, CustomRoles.Mafia);
            ///Madmate系役職
            var Madmate = new PageObject(RoleOptions, CustomRoles.Madmate);
            var MadGuardian = new PageObject(RoleOptions, CustomRoles.MadGuardian);
            var MadSnitch = new PageObject(RoleOptions, CustomRoles.MadSnitch);
            ///第三陣営役職
            var Jester = new PageObject(RoleOptions, CustomRoles.Jester);
            var Opportunist = new PageObject(RoleOptions, CustomRoles.Opportunist);
            var Terrorist = new PageObject(RoleOptions, CustomRoles.Terrorist);
            ///クルー役職
            var Bait = new PageObject(RoleOptions, CustomRoles.Bait);
            var Mayor = new PageObject(RoleOptions, CustomRoles.Mayor);
            var SabotageMaster = new PageObject(RoleOptions, CustomRoles.SabotageMaster);
            var Sheriff = new PageObject(RoleOptions, CustomRoles.Sheriff);
            var Snitch = new PageObject(RoleOptions, CustomRoles.Snitch);



            //役職の詳細設定
            var AdvRoleOptions = new PageObject(RoleOptions, () => main.tr.getString("AdvancedRoleOptions"));
            var AdvImpostorRoleOptions = new PageObject(AdvRoleOptions, () => main.tr.getString("AdvancedImpostorRoleOptions"));

            var BountyTargetChangeTime = new PageObject(AdvImpostorRoleOptions, () => $"<color={main.getRoleColorCode(CustomRoles.BountyHunter)}>{main.tr.getString("BountyTargetChangeTime")}</color>(s): {main.BountyTargetChangeTime}{main.TextCursor}", true, () => {main.BountyTargetChangeTime = 0;}, (n) => main.ChangeInt(ref main.BountyTargetChangeTime, n, 999));
            var BountySuccessKillCooldown = new PageObject(AdvImpostorRoleOptions, () => $"<color={main.getRoleColorCode(CustomRoles.BountyHunter)}>{main.tr.getString("BountySuccessKillCooldown")}</color>(s): {main.BountySuccessKillCooldown}{main.TextCursor}", true, () => {main.BountySuccessKillCooldown = 0;}, (n) => main.ChangeInt(ref main.BountySuccessKillCooldown, n, 999));
            var BountyFailureKillCooldown = new PageObject(AdvImpostorRoleOptions, () => $"<color={main.getRoleColorCode(CustomRoles.BountyHunter)}>{main.tr.getString("BountyFailureKillCooldown")}</color>(s): {main.BountyFailureKillCooldown}{main.TextCursor}", true, () => {main.BountyFailureKillCooldown = 0;}, (n) => main.ChangeInt(ref main.BountyFailureKillCooldown, n, 999));
            var BHDefaultKillCooldown = new PageObject(AdvImpostorRoleOptions, () => $"<color={main.getRoleColorCode(CustomRoles.BountyHunter)}>{main.tr.getString("BHDefaultKillCooldown")}</color>(s): {main.BHDefaultKillCooldown}{main.TextCursor}", true, () => {main.BHDefaultKillCooldown = 0;}, (n) => main.ChangeInt(ref main.BHDefaultKillCooldown, n, 999));
            var SerialKillerCooldown = new PageObject(AdvImpostorRoleOptions, () => $"<color={main.getRoleColorCode(CustomRoles.SerialKiller)}>{main.tr.getString("SerialKillerCooldown")}</color>(s): {main.SerialKillerCooldown}{main.TextCursor}", true, () => {main.SerialKillerCooldown = 0;}, (n) => main.ChangeInt(ref main.SerialKillerCooldown, n, 100));
            var SerialKillerLimit = new PageObject(AdvImpostorRoleOptions, () => $"<color={main.getRoleColorCode(CustomRoles.SerialKiller)}>{main.tr.getString("SerialKillerLimit")}</color>(s): {main.SerialKillerLimit}{main.TextCursor}", true, () => {main.SerialKillerLimit = 0;}, (n) => main.ChangeInt(ref main.SerialKillerLimit, n, 999));
            var VampireKillDelay = new PageObject(AdvImpostorRoleOptions, () => $"<color={main.getRoleColorCode(CustomRoles.Vampire)}>{main.tr.getString("VampireKillDelay")}</color>(s): {main.VampireKillDelay}{main.TextCursor}", true, () => {main.VampireKillDelay = 0;}, (n) => main.ChangeInt(ref main.VampireKillDelay, n, 999));
            var ShapeMasterShapeshiftDuration = new PageObject(AdvImpostorRoleOptions, () => $"<color={main.getRoleColorCode(CustomRoles.ShapeMaster)}>{main.tr.getString("ShapeMasterShapeshiftDuration")}</color>(s): {main.ShapeMasterShapeshiftDuration}{main.TextCursor}", true, () => {main.ShapeMasterShapeshiftDuration = 0;}, (n) => main.ChangeInt(ref main.ShapeMasterShapeshiftDuration, n, 100));
            var MadmateCanFixLightsOut = new PageObject(AdvImpostorRoleOptions, () => $"<color={main.getRoleColorCode(CustomRoles.Madmate)}>{main.tr.getString("MadmateCanFixLightsOut")}</color>: {main.getOnOff(main.MadmateCanFixLightsOut)}", true, () => {main.MadmateCanFixLightsOut = !main.MadmateCanFixLightsOut;});
            var MadmateCanFixComms = new PageObject(AdvImpostorRoleOptions, () => $"<color={main.getRoleColorCode(CustomRoles.Madmate)}>{main.tr.getString("MadmateCanFixComms")}</color>: {main.getOnOff(main.MadmateCanFixComms)}", true, () => { main.MadmateCanFixComms = !main.MadmateCanFixComms; });
            var MadGuardianCanSeeBarrier = new PageObject(AdvImpostorRoleOptions, () => $"<color={main.getRoleColorCode(CustomRoles.MadGuardian)}>{main.tr.getString("MadGuardianCanSeeWhoTriedToKill")}</color>: {main.getOnOff(main.MadGuardianCanSeeWhoTriedToKill)}", true, () => {main.MadGuardianCanSeeWhoTriedToKill = !main.MadGuardianCanSeeWhoTriedToKill;});
            var MadmateVisionAsImpostor = new PageObject(AdvImpostorRoleOptions, () => $"<color={main.getRoleColorCode(CustomRoles.Madmate)}>{main.tr.getString("MadmateVisionAsImpostor")}</color>: {main.getOnOff(main.MadmateVisionAsImpostor)}", true, () => {main.MadmateVisionAsImpostor = !main.MadmateVisionAsImpostor;});
            var CanMakeMadmateCount = new PageObject(AdvImpostorRoleOptions, () => $"<color={main.getRoleColorCode(CustomRoles.Madmate)}>{main.tr.getString("CanMakeMadmateCount")}</color>: {main.CanMakeMadmateCount}{main.TextCursor}", true, () => {main.CanMakeMadmateCount = 0;}, (n) => main.ChangeInt(ref main.CanMakeMadmateCount, n, 999));
            var MadSnitchTasks = new PageObject(AdvImpostorRoleOptions, () => $"<color={main.getRoleColorCode(CustomRoles.MadSnitch)}>{main.tr.getString("MadSnitchTasks")}</color>: {main.MadSnitchTasks}{main.TextCursor}", true, () => { main.MadSnitchTasks = 0; }, (n) => main.ChangeInt(ref main.MadSnitchTasks, n, 99));

            var AdvCrewmateRoleOptions = new PageObject(AdvRoleOptions, () => main.tr.getString("AdvancedCrewmateRoleOptions"));
            var MayorAdditionalVote = new PageObject(AdvCrewmateRoleOptions, () => $"<color={main.getRoleColorCode(CustomRoles.Mayor)}>{main.tr.getString("MayorAdditionalVote")}</color>: {main.MayorAdditionalVote}{main.TextCursor}", true, () => {main.MayorAdditionalVote = 0;}, (n) => main.ChangeInt(ref main.MayorAdditionalVote, n, 99));
            var SabotageMasterSkillLimit = new PageObject(AdvCrewmateRoleOptions, () => $"<color={main.getRoleColorCode(CustomRoles.SabotageMaster)}>{main.tr.getString("SabotageMasterSkillLimit")}</color>: {main.SabotageMasterSkillLimit}{main.TextCursor}", true, () => {main.SabotageMasterSkillLimit = 0;}, (n) => main.ChangeInt(ref main.SabotageMasterSkillLimit, n, 999));
            var SabotageMasterFixesDoors = new PageObject(AdvCrewmateRoleOptions, () => $"<color={main.getRoleColorCode(CustomRoles.SabotageMaster)}>{main.tr.getString("SabotageMasterFixesDoors")}</color>: {main.getOnOff(main.SabotageMasterFixesDoors)}", true, () => main.SabotageMasterFixesDoors = !main.SabotageMasterFixesDoors);
            var SabotageMasterFixesReactors = new PageObject(AdvCrewmateRoleOptions, () => $"<color={main.getRoleColorCode(CustomRoles.SabotageMaster)}>{main.tr.getString("SabotageMasterFixesReactors")}</color>: {main.getOnOff(main.SabotageMasterFixesReactors)}", true, () => main.SabotageMasterFixesReactors = !main.SabotageMasterFixesReactors);
            var SabotageMasterFixesOxygens = new PageObject(AdvCrewmateRoleOptions, () => $"<color={main.getRoleColorCode(CustomRoles.SabotageMaster)}>{main.tr.getString("SabotageMasterFixesOxygens")}</color>: {main.getOnOff(main.SabotageMasterFixesOxygens)}", true, () => main.SabotageMasterFixesOxygens = !main.SabotageMasterFixesOxygens);
            var SabotageMasterFixesComms = new PageObject(AdvCrewmateRoleOptions, () => $"<color={main.getRoleColorCode(CustomRoles.SabotageMaster)}>{main.tr.getString("SabotageMasterFixesCommunications")}</color>: {main.getOnOff(main.SabotageMasterFixesCommunications)}", true, () => main.SabotageMasterFixesCommunications = !main.SabotageMasterFixesCommunications);
            var SabotageMasterFixesElectrical = new PageObject(AdvCrewmateRoleOptions, () => $"<color={main.getRoleColorCode(CustomRoles.SabotageMaster)}>{main.tr.getString("SabotageMasterFixesElectrical")}</color>: {main.getOnOff(main.SabotageMasterFixesElectrical)}", true, () => main.SabotageMasterFixesElectrical = !main.SabotageMasterFixesElectrical);
            var SheriffKillCooldown = new PageObject(AdvCrewmateRoleOptions, () => $"<color={main.getRoleColorCode(CustomRoles.Sheriff)}>{main.tr.getString("SheriffKillCooldown")}</color>: {main.SheriffKillCooldown}{main.TextCursor}", true, () => {main.SheriffKillCooldown = 0;}, (n) => main.ChangeInt(ref main.SheriffKillCooldown, n, 180));
            var SheriffCanKillJester = new PageObject(AdvCrewmateRoleOptions, () => $"<color={main.getRoleColorCode(CustomRoles.Sheriff)}>{main.tr.getString("SheriffCanKillJester")}</color>: {main.getOnOff(main.SheriffCanKillJester)}", true, () => main.SheriffCanKillJester = !main.SheriffCanKillJester);
            var SheriffCanKillTerrorist = new PageObject(AdvCrewmateRoleOptions, () => $"<color={main.getRoleColorCode(CustomRoles.Sheriff)}>{main.tr.getString("SheriffCanKillTerrorist")}</color>: {main.getOnOff(main.SheriffCanKillTerrorist)}", true, () => main.SheriffCanKillTerrorist = !main.SheriffCanKillTerrorist);
            var SheriffCanKillOpportunist = new PageObject(AdvCrewmateRoleOptions, () => $"<color={main.getRoleColorCode(CustomRoles.Sheriff)}>{main.tr.getString("SheriffCanKillOpportunist")}</color>: {main.getOnOff(main.SheriffCanKillOpportunist)}", true, () => main.SheriffCanKillOpportunist = !main.SheriffCanKillOpportunist);

            //Mode Options
            var ModeOptions = new PageObject(basePage, () => main.tr.getString("ModeOptions"));
            var HideAndSeek = new PageObject(ModeOptions, () => main.tr.getString("HideAndSeek") + ": " + main.getOnOff(main.IsHideAndSeek), true, () => main.IsHideAndSeek = !main.IsHideAndSeek);
            var HideAndSeekOptions = new PageObject(ModeOptions, () => main.tr.getString("HideAndSeekOptions"));
            var AllowCloseDoors = new PageObject(HideAndSeekOptions, () => main.tr.getString("AllowCloseDoors") + ": " + main.getOnOff(main.AllowCloseDoors), true, () => {main.AllowCloseDoors = !main.AllowCloseDoors;});
            var HideAndSeekWaitingTime = new PageObject(HideAndSeekOptions, () => main.tr.getString("HideAndSeekWaitingTime") + ": " + main.HideAndSeekKillDelay, true, () => {main.HideAndSeekKillDelay = 0;}, i => main.ChangeInt(ref main.HideAndSeekKillDelay, i, 180));
            var IgnoreCosmetics = new PageObject(HideAndSeekOptions, () => main.tr.getString("IgnoreCosmetics") + ": " + main.getOnOff(main.IgnoreCosmetics), true, () => {main.IgnoreCosmetics = !main.IgnoreCosmetics;});
            var IgnoreVent = new PageObject(HideAndSeekOptions, () => main.tr.getString("IgnoreVent") + ": " + main.getOnOff(main.IgnoreVent), true, () => {main.IgnoreVent = !main.IgnoreVent;});
            var HideAndSeekRoles = new PageObject(HideAndSeekOptions, () => main.tr.getString("HideAndSeekRoles"));
            var Fox = new PageObject(HideAndSeekRoles, () => $"<color=#e478ff>" + main.getRoleName(CustomRoles.Fox) + "</color>: " + main.FoxCount,
                true,
                () => {
                    if(main.FoxCount == 0) main.FoxCount = 1;
                    else main.FoxCount = 0;
                },
                i => main.ChangeInt(ref main.FoxCount, i, GameData.Instance.AllPlayers.Count - main.TrollCount)
            );
            var Troll = new PageObject(HideAndSeekRoles, () => $"<color=#00ff00>" + main.getRoleName(CustomRoles.Troll) + "</color>: " + main.TrollCount,
                true,
                () => {
                    if(main.TrollCount == 0) main.TrollCount = 1;
                    else main.TrollCount = 0;
                },
                i => main.ChangeInt(ref main.TrollCount, i, GameData.Instance.AllPlayers.Count - main.FoxCount)
            );

            var SyncButtonMode = new PageObject(ModeOptions, () => main.tr.getString("SyncButtonMode"));
            var SyncButtonModeEnabled = new PageObject(SyncButtonMode, () => main.tr.getString("SyncButtonMode") + ": " + main.getOnOff(main.SyncButtonMode), true, () => main.SyncButtonMode = !main.SyncButtonMode);
            var SyncedButtonCount = new PageObject(SyncButtonMode, () => main.tr.getString("SyncedButtonCount") + ": " + main.SyncedButtonCount + main.TextCursor, true, () => {main.SyncedButtonCount = 0;}, i => main.ChangeInt(ref main.SyncedButtonCount, i, 100));

            var DisableTasks = new PageObject(ModeOptions, () => main.tr.getString("DisableTasks"));
            var dSwipeCard = new PageObject(DisableTasks, () => main.tr.getString("DisableSwipeCardTask") + ": " + main.getOnOff(main.DisableSwipeCard), true, () => {main.DisableSwipeCard = !main.DisableSwipeCard;});
            var dSubmitScan = new PageObject(DisableTasks, () => main.tr.getString("DisableSubmitScanTask") + ": " + main.getOnOff(main.DisableSubmitScan), true, () => {main.DisableSubmitScan = !main.DisableSubmitScan;});
            var dUnlockSafe = new PageObject(DisableTasks, () => main.tr.getString("DisableUnlockSafeTask") + ": " + main.getOnOff(main.DisableUnlockSafe), true, () => {main.DisableUnlockSafe = !main.DisableUnlockSafe;});
            var dUploadData = new PageObject(DisableTasks, () => main.tr.getString("DisableUploadDataTask") + ": " + main.getOnOff(main.DisableUploadData), true, () => {main.DisableUploadData = !main.DisableUploadData;});
            var dStartReactor = new PageObject(DisableTasks, () => main.tr.getString("DisableStartReactorTask") + ": " + main.getOnOff(main.DisableStartReactor), true, () => {main.DisableStartReactor = !main.DisableStartReactor;});
            var dResetBreaker = new PageObject(DisableTasks, () => main.tr.getString("DisableResetBreakerTask") + ": " + main.getOnOff(main.DisableResetBreaker), true, () => {main.DisableResetBreaker = !main.DisableResetBreaker;});

            var RandomMapsMode = new PageObject(ModeOptions, () => main.tr.getString("RandomMapsMode"));
            var RandomMapsModeEnabled = new PageObject(RandomMapsMode, () => main.tr.getString("RandomMapsMode") + ": " + main.getOnOff(main.RandomMapsMode), true, () => main.RandomMapsMode = !main.RandomMapsMode);
            var rmSkeld = new PageObject(RandomMapsMode, () => main.tr.getString("AddedTheSkeld") + ": " + main.getOnOff(main.AddedTheSkeld), true, () => main.AddedTheSkeld = !main.AddedTheSkeld);
            var rmMiraHQ = new PageObject(RandomMapsMode, () => main.tr.getString("AddedMIRAHQ") + ": " + main.getOnOff(main.AddedMIRAHQ), true, () => main.AddedMIRAHQ = !main.AddedMIRAHQ);
            var rmPolus = new PageObject(RandomMapsMode, () => main.tr.getString("AddedPolus") + ": " + main.getOnOff(main.AddedPolus), true, () => main.AddedPolus = !main.AddedPolus);
            //var rmDleks = new PageObject(RandomMapsMode, () => main.tr.getString("AddedDleks") + ": " + main.getOnOff(main.AddedDleks), true, () => main.AddedDleks = !main.AddedDleks);
            var rmAirship = new PageObject(RandomMapsMode, () => main.tr.getString("AddedTheAirShip") + ": " + main.getOnOff(main.AddedTheAirShip), true, () => main.AddedTheAirShip = !main.AddedTheAirShip);
            var NoGameEnd = new PageObject(ModeOptions, () => main.tr.getString("NoGameEnd") + ": " + main.getOnOff(main.NoGameEnd), true, () => main.NoGameEnd = !main.NoGameEnd);

            var voteMode = new PageObject(ModeOptions, () => main.tr.getString("VoteMode"));
            var WhenSkipVote = new PageObject(voteMode, () => main.tr.getString("WhenSkipVote") + ": " + main.tr.getString(Enum.GetName(typeof(VoteMode),main.whenSkipVote)), true, () => {
                var next = main.whenSkipVote + 1;
                if(next > VoteMode.SelfVote) next = VoteMode.Default;
                main.whenSkipVote = next;
            });
            var WhenNonVote = new PageObject(voteMode, () => main.tr.getString("WhenNonVote") + ": " + main.tr.getString(Enum.GetName(typeof(VoteMode),main.whenNonVote)), true, () => {
                var next = main.whenNonVote + 1;
                if(next > VoteMode.SelfVote) next = VoteMode.Default;
                main.whenNonVote = next;
            });
            var canTerroristSuicideWin = new PageObject(voteMode, () => main.tr.getString("CanTerroristSuicideWin") + ": " + main.getOnOff(main.canTerroristSuicideWin), true, () => main.canTerroristSuicideWin = !main.canTerroristSuicideWin);

            var Suffix = new PageObject(basePage, () => main.tr.getString("SuffixMode") + ": " + main.currentSuffix.ToString(), true, () => {
                var next = main.currentSuffix + 1;
                if(next > SuffixModes.Recording) next = SuffixModes.None;
                main.currentSuffix = next;
            });
            Suffix.amVisible = () => AmongUsClient.Instance.AmHost;
            var forceJapanese = new PageObject(basePage, () => main.tr.getString("ForceJapanese") + ": " + main.getOnOff(main.forceJapanese), false, () => main.forceJapanese = !main.forceJapanese);
            var autoPrintLastRoles = new PageObject(basePage, () => main.tr.getString("AutoDisplayLastResult") + ": " + main.getOnOff(main.autoDisplayLastRoles), false, () => main.autoDisplayLastRoles = !main.autoDisplayLastRoles);
            autoPrintLastRoles.amVisible = () => AmongUsClient.Instance.AmHost;
        }
        public static void SetPage(PageObject page)
        {
            currentCursor = 0;
            currentPage = page;
        }
        public static void Up()
        {
            if (currentCursor <= 0) currentCursor = currentPage.ChildPages.Count - 1;
            else currentCursor--;
        }
        public static void Down()
        {
            if (currentCursor >= currentPage.ChildPages.Count - 1) currentCursor = 0;
            else currentCursor++;
            if(!currentPage.ChildPages[currentCursor].amVisible()) currentCursor--;
        }
        public static void Enter()
        {
            var selectingObj = currentPage.ChildPages[currentCursor];

            if (selectingObj.isHostOnly && !AmongUsClient.Instance.AmHost) return;
            selectingObj.onEnter();
            main.SyncCustomSettingsRPC();
        }
        public static void Return()
        {
            if(currentPage.parent != null)
            SetPage(currentPage.parent);
        }
        public static void Input(int num)
        {
            var selectingObj = currentPage.ChildPages[currentCursor];

            if (selectingObj.isHostOnly && !AmongUsClient.Instance.AmHost) return;
            selectingObj.onInput(num);
            main.SyncCustomSettingsRPC();
        }
        public static string GetOptionText()
        {
            string text;
            text = "==" + currentPage.name + "==" + "\r\n";
            for (var i = 0; i < currentPage.ChildPages.Count; i++)
            {
                var obj = currentPage.ChildPages[i];
                if(!obj.amVisible()) continue;
                text += currentCursor == i ? ">" : "";
                text += obj.name + "\r\n";
            }
            return text;
        }
    }

    class PageObject {
        public PageObject parent;
        public string name => getName();
        private Func<string> getName;
        public bool isHostOnly;
        public Action onEnter;
        public Action<int> onInput;
        public List<PageObject> ChildPages;
        public Func<bool> amVisible = () => true;
        public PageObject( //フォルダー
            PageObject parent,
            string text,
            bool isHostOnly = false
        ) {
            this.parent = parent; //親オブジェクト
            this.getName = () => text; //名前
            this.isHostOnly = isHostOnly; //実行をホストのみに限定するか
            this.onEnter = () => CustomOptionController.SetPage(this);
            this.onInput = (i) => {}; //入力時の動作

            this.ChildPages = new List<PageObject>(); //子オブジェクトリストを初期化
            parent?.ChildPages.Add(this); //親のリストに自分を追加
        }
        public PageObject( //フォルダー
            PageObject parent,
            Func<string> name,
            bool isHostOnly = false
        ) {
            this.parent = parent; //親オブジェクト
            this.getName = name; //名前
            this.isHostOnly = isHostOnly; //実行をホストのみに限定するか
            this.onEnter = () => CustomOptionController.SetPage(this);
            this.onInput = (i) => {}; //入力時の動作

            this.ChildPages = new List<PageObject>(); //子オブジェクトリストを初期化
            parent?.ChildPages.Add(this); //親のリストに自分を追加
        }
        public PageObject( //ON・OFF
            PageObject parent,
            Func<string> name,
            bool isHostOnly,
            Action onEnter
        ) {
            this.parent = parent; //親オブジェクト
            this.getName = name; //名前
            this.isHostOnly = isHostOnly; //実行をホストのみに限定するか
            this.onEnter = onEnter; //実行時の動作
            this.onInput = (i) => {}; //入力時の動作

            this.ChildPages = new List<PageObject>(); //子オブジェクトリストを初期化
            parent?.ChildPages.Add(this); //親のリストに自分を追加
        }
        public PageObject( //数値設定
            PageObject parent,
            Func<string> name,
            bool isHostOnly,
            Action onEnter,
            Action<int> onInput
        ) {
            this.parent = parent; //親オブジェクト
            this.getName = name; //名前
            this.isHostOnly = isHostOnly; //実行をホストのみに限定するか
            this.onEnter = onEnter; //実行時の動作
            this.onInput = onInput; //入力時の動作

            this.ChildPages = new List<PageObject>(); //子オブジェクトリストを初期化
            parent?.ChildPages.Add(this); //親のリストに自分を追加
        }
        public PageObject( //役職設定
            PageObject parent,
            CustomRoles role
        ) {
            this.parent = parent; //親オブジェクト
            this.getName = () => $"<color={main.getRoleColorCode(role)}>{main.getRoleName(role)}</color>: {main.GetCountFromRole(role)}";
            this.isHostOnly = true; //実行をホストのみに限定するか
            this.onEnter = () => main.SetRoleCountToggle(role); //実行時の動作
            this.onInput = (n) => role.SetCount(n); //入力時の動作

            this.ChildPages = new List<PageObject>(); //子オブジェクトリストを初期化
            parent?.ChildPages.Add(this); //親のリストに自分を追加
        }
    }
}
