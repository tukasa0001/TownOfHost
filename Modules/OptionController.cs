using System;
using System.Collections.Generic;
using static TownOfHost.Translator;

namespace TownOfHost
{
    static class CustomOptionController
    {
        public static PageObject basePage;
        public static PageObject currentPage = basePage;
        public static int currentCursor = 0;
        public static void begin()
        {
            basePage = new PageObject(
                null,
                () => "Town Of Host Settings",
                false,
                () => Logger.SendInGame("このテキストが出るのはバグです。開発者にご報告ください。")
            );
            currentPage = basePage;

            //ページ追加など
            var RoleOptions = new PageObject(basePage, () => getString("RoleOptions"));
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
            var Lighter = new PageObject(RoleOptions, CustomRoles.Lighter);
            var Mayor = new PageObject(RoleOptions, CustomRoles.Mayor);
            var SabotageMaster = new PageObject(RoleOptions, CustomRoles.SabotageMaster);
            var Sheriff = new PageObject(RoleOptions, CustomRoles.Sheriff);
            var Snitch = new PageObject(RoleOptions, CustomRoles.Snitch);



            //役職の詳細設定
            var AdvRoleOptions = new PageObject(RoleOptions, () => getString("AdvancedRoleOptions"));
            var AdvImpostorRoleOptions = new PageObject(AdvRoleOptions, () => getString("AdvancedImpostorRoleOptions"));

            var BountyTargetChangeTime = new PageObject(AdvImpostorRoleOptions, () => $"<color={Utils.getRoleColorCode(CustomRoles.BountyHunter)}>{getString("BountyTargetChangeTime")}</color>(s): {Options.BountyTargetChangeTime.GetFloat()}{main.TextCursor}", true, () => { Options.BountyTargetChangeTime.UpdateSelection(0); }, (n) => {});
            var BountySuccessKillCooldown = new PageObject(AdvImpostorRoleOptions, () => $"<color={Utils.getRoleColorCode(CustomRoles.BountyHunter)}>{getString("BountySuccessKillCooldown")}</color>(s): {Options.BountySuccessKillCooldown.GetFloat()}{main.TextCursor}", true, () => { Options.BountySuccessKillCooldown.UpdateSelection(0); }, (n) => {});
            var BountyFailureKillCooldown = new PageObject(AdvImpostorRoleOptions, () => $"<color={Utils.getRoleColorCode(CustomRoles.BountyHunter)}>{getString("BountyFailureKillCooldown")}</color>(s): {Options.BountyFailureKillCooldown.GetFloat()}{main.TextCursor}", true, () => { Options.BountyFailureKillCooldown.UpdateSelection(0); }, (n) => {});
            var BHDefaultKillCooldown = new PageObject(AdvImpostorRoleOptions, () => $"<color={Utils.getRoleColorCode(CustomRoles.BountyHunter)}>{getString("BHDefaultKillCooldown")}</color>(s): {Options.BHDefaultKillCooldown.GetFloat()}{main.TextCursor}", true, () => { Options.BHDefaultKillCooldown.UpdateSelection(0); }, (n) =>{});
            var SerialKillerCooldown = new PageObject(AdvImpostorRoleOptions, () => $"<color={Utils.getRoleColorCode(CustomRoles.SerialKiller)}>{getString("SerialKillerCooldown")}</color>(s): {Options.SerialKillerCooldown.GetFloat()}{main.TextCursor}", true, () => { Options.SerialKillerCooldown.UpdateSelection(0); }, (n) => {});
            var SerialKillerLimit = new PageObject(AdvImpostorRoleOptions, () => $"<color={Utils.getRoleColorCode(CustomRoles.SerialKiller)}>{getString("SerialKillerLimit")}</color>(s): {Options.SerialKillerLimit.GetFloat()}{main.TextCursor}", true, () => { Options.SerialKillerLimit.UpdateSelection(0); }, (n) => {});
            var VampireKillDelay = new PageObject(AdvImpostorRoleOptions, () => $"<color={Utils.getRoleColorCode(CustomRoles.Vampire)}>{getString("VampireKillDelay")}</color>(s): {Options.VampireKillDelay}{main.TextCursor}", true, () => { Options.VampireKillDelay = 0; }, (n) => Utils.ChangeInt(ref Options.VampireKillDelay, n, 999));
            var ShapeMasterShapeshiftDuration = new PageObject(AdvImpostorRoleOptions, () => $"<color={Utils.getRoleColorCode(CustomRoles.ShapeMaster)}>{getString("ShapeMasterShapeshiftDuration")}</color>(s): {Options.ShapeMasterShapeshiftDuration}{main.TextCursor}", true, () => { Options.ShapeMasterShapeshiftDuration = 0; }, (n) => Utils.ChangeInt(ref Options.ShapeMasterShapeshiftDuration, n, 100));
            var MadmateCanFixLightsOut = new PageObject(AdvImpostorRoleOptions, () => $"<color={Utils.getRoleColorCode(CustomRoles.Madmate)}>{getString("MadmateCanFixLightsOut")}</color>: {Utils.getOnOff(Options.MadmateCanFixLightsOut)}", true, () => { Options.MadmateCanFixLightsOut = !Options.MadmateCanFixLightsOut; });
            var MadmateCanFixComms = new PageObject(AdvImpostorRoleOptions, () => $"<color={Utils.getRoleColorCode(CustomRoles.Madmate)}>{getString("MadmateCanFixComms")}</color>: {Utils.getOnOff(Options.MadmateCanFixComms)}", true, () => { Options.MadmateCanFixComms = !Options.MadmateCanFixComms; });
            var MadGuardianCanSeeBarrier = new PageObject(AdvImpostorRoleOptions, () => $"<color={Utils.getRoleColorCode(CustomRoles.MadGuardian)}>{getString("MadGuardianCanSeeWhoTriedToKill")}</color>: {Utils.getOnOff(Options.MadGuardianCanSeeWhoTriedToKill)}", true, () => { Options.MadGuardianCanSeeWhoTriedToKill = !Options.MadGuardianCanSeeWhoTriedToKill; });
            var MadmateHasImpostorVision = new PageObject(AdvImpostorRoleOptions, () => $"<color={Utils.getRoleColorCode(CustomRoles.Madmate)}>{getString("MadmateHasImpostorVision")}</color>: {Utils.getOnOff(Options.MadmateHasImpostorVision)}", true, () => { Options.MadmateHasImpostorVision = !Options.MadmateHasImpostorVision; });
            var CanMakeMadmateCount = new PageObject(AdvImpostorRoleOptions, () => $"<color={Utils.getRoleColorCode(CustomRoles.Madmate)}>{getString("CanMakeMadmateCount")}</color>: {Options.CanMakeMadmateCount}{main.TextCursor}", true, () => { Options.CanMakeMadmateCount = 0; }, (n) => Utils.ChangeInt(ref Options.CanMakeMadmateCount, n, 999));
            var MadSnitchTasks = new PageObject(AdvImpostorRoleOptions, () => $"<color={Utils.getRoleColorCode(CustomRoles.MadSnitch)}>{getString("MadSnitchTasks")}</color>: {Options.MadSnitchTasks}{main.TextCursor}", true, () => { Options.MadSnitchTasks = 0; }, (n) => Utils.ChangeInt(ref Options.MadSnitchTasks, n, 99));

            var AdvCrewmateRoleOptions = new PageObject(AdvRoleOptions, () => getString("AdvancedCrewmateRoleOptions"));
            var MayorAdditionalVote = new PageObject(AdvCrewmateRoleOptions, () => $"<color={Utils.getRoleColorCode(CustomRoles.Mayor)}>{getString("MayorAdditionalVote")}</color>: {Options.MayorAdditionalVote}{main.TextCursor}", true, () => { Options.MayorAdditionalVote = 0; }, (n) => Utils.ChangeInt(ref Options.MayorAdditionalVote, n, 99));
            var SabotageMasterSkillLimit = new PageObject(AdvCrewmateRoleOptions, () => $"<color={Utils.getRoleColorCode(CustomRoles.SabotageMaster)}>{getString("SabotageMasterSkillLimit")}</color>: {Options.SabotageMasterSkillLimit}{main.TextCursor}", true, () => { Options.SabotageMasterSkillLimit = 0; }, (n) => Utils.ChangeInt(ref Options.SabotageMasterSkillLimit, n, 999));
            var SabotageMasterFixesDoors = new PageObject(AdvCrewmateRoleOptions, () => $"<color={Utils.getRoleColorCode(CustomRoles.SabotageMaster)}>{getString("SabotageMasterFixesDoors")}</color>: {Utils.getOnOff(Options.SabotageMasterFixesDoors)}", true, () => Options.SabotageMasterFixesDoors = !Options.SabotageMasterFixesDoors);
            var SabotageMasterFixesReactors = new PageObject(AdvCrewmateRoleOptions, () => $"<color={Utils.getRoleColorCode(CustomRoles.SabotageMaster)}>{getString("SabotageMasterFixesReactors")}</color>: {Utils.getOnOff(Options.SabotageMasterFixesReactors)}", true, () => Options.SabotageMasterFixesReactors = !Options.SabotageMasterFixesReactors);
            var SabotageMasterFixesOxygens = new PageObject(AdvCrewmateRoleOptions, () => $"<color={Utils.getRoleColorCode(CustomRoles.SabotageMaster)}>{getString("SabotageMasterFixesOxygens")}</color>: {Utils.getOnOff(Options.SabotageMasterFixesOxygens)}", true, () => Options.SabotageMasterFixesOxygens = !Options.SabotageMasterFixesOxygens);
            var SabotageMasterFixesComms = new PageObject(AdvCrewmateRoleOptions, () => $"<color={Utils.getRoleColorCode(CustomRoles.SabotageMaster)}>{getString("SabotageMasterFixesCommunications")}</color>: {Utils.getOnOff(Options.SabotageMasterFixesCommunications)}", true, () => Options.SabotageMasterFixesCommunications = !Options.SabotageMasterFixesCommunications);
            var SabotageMasterFixesElectrical = new PageObject(AdvCrewmateRoleOptions, () => $"<color={Utils.getRoleColorCode(CustomRoles.SabotageMaster)}>{getString("SabotageMasterFixesElectrical")}</color>: {Utils.getOnOff(Options.SabotageMasterFixesElectrical)}", true, () => Options.SabotageMasterFixesElectrical = !Options.SabotageMasterFixesElectrical);
            var SheriffKillCooldown = new PageObject(AdvCrewmateRoleOptions, () => $"<color={Utils.getRoleColorCode(CustomRoles.Sheriff)}>{getString("SheriffKillCooldown")}</color>: {Options.SheriffKillCooldown}{main.TextCursor}", true, () => { Options.SheriffKillCooldown = 0; }, (n) => Utils.ChangeInt(ref Options.SheriffKillCooldown, n, 180));
            var SheriffCanKillJester = new PageObject(AdvCrewmateRoleOptions, () => $"<color={Utils.getRoleColorCode(CustomRoles.Sheriff)}>{getString("SheriffCanKillJester")}</color>: {Utils.getOnOff(Options.SheriffCanKillJester)}", true, () => Options.SheriffCanKillJester = !Options.SheriffCanKillJester);
            var SheriffCanKillTerrorist = new PageObject(AdvCrewmateRoleOptions, () => $"<color={Utils.getRoleColorCode(CustomRoles.Sheriff)}>{getString("SheriffCanKillTerrorist")}</color>: {Utils.getOnOff(Options.SheriffCanKillTerrorist)}", true, () => Options.SheriffCanKillTerrorist = !Options.SheriffCanKillTerrorist);
            var SheriffCanKillOpportunist = new PageObject(AdvCrewmateRoleOptions, () => $"<color={Utils.getRoleColorCode(CustomRoles.Sheriff)}>{getString("SheriffCanKillOpportunist")}</color>: {Utils.getOnOff(Options.SheriffCanKillOpportunist)}", true, () => Options.SheriffCanKillOpportunist = !Options.SheriffCanKillOpportunist);

            //Mode Options
            var ModeOptions = new PageObject(basePage, () => getString("ModeOptions"));
            var HideAndSeek = new PageObject(ModeOptions, () => getString("HideAndSeek") + ": " + Utils.getOnOff(Options.CurrentGameMode == CustomGameMode.HideAndSeek), true, () => { /*Options.IsHideAndSeek = !Options.IsHideAndSeek*/ });
            var HideAndSeekOptions = new PageObject(ModeOptions, () => getString("HideAndSeekOptions"));
            var AllowCloseDoors = new PageObject(HideAndSeekOptions, () => getString("AllowCloseDoors") + ": " + Utils.getOnOff(Options.AllowCloseDoors), true, () => { Options.AllowCloseDoors = !Options.AllowCloseDoors; });
            var HideAndSeekWaitingTime = new PageObject(HideAndSeekOptions, () => getString("HideAndSeekWaitingTime") + ": " + Options.HideAndSeekKillDelay, true, () => { Options.HideAndSeekKillDelay = 0; }, i => Utils.ChangeInt(ref Options.HideAndSeekKillDelay, i, 180));
            var IgnoreCosmetics = new PageObject(HideAndSeekOptions, () => getString("IgnoreCosmetics") + ": " + Utils.getOnOff(Options.IgnoreCosmetics), true, () => { Options.IgnoreCosmetics = !Options.IgnoreCosmetics; });
            var IgnoreVent = new PageObject(HideAndSeekOptions, () => getString("IgnoreVent") + ": " + Utils.getOnOff(Options.IgnoreVent), true, () => { Options.IgnoreVent = !Options.IgnoreVent; });
            var HideAndSeekRoles = new PageObject(HideAndSeekOptions, () => getString("HideAndSeekRoles"));


            var Fox = new PageObject(HideAndSeekRoles, CustomRoles.Fox);
            var Troll = new PageObject(HideAndSeekRoles, CustomRoles.Troll);


            var SyncButtonMode = new PageObject(ModeOptions, () => getString("SyncButtonMode"));
            var SyncButtonModeEnabled = new PageObject(SyncButtonMode, () => getString("SyncButtonMode") + ": " + Utils.getOnOff(Options.SyncButtonMode.GetBool()), true, () => {});
            var SyncedButtonCount = new PageObject(SyncButtonMode, () => getString("SyncedButtonCount") + ": " + Options.SyncedButtonCount.GetSelection() + main.TextCursor, true, () => { }, i =>{});

            var DisableTasks = new PageObject(ModeOptions, () => getString("DisableTasks"));
            var dSwipeCard = new PageObject(DisableTasks, () => getString("DisableSwipeCardTask") + ": " + Utils.getOnOff(Options.DisableSwipeCard.GetBool()), true, () => { /* Options.Task_DisableSwipeCard.UpdateSelection()*/ });
            var dSubmitScan = new PageObject(DisableTasks, () => getString("DisableSubmitScanTask") + ": " + Utils.getOnOff(Options.DisableSubmitScan.GetBool()), true, () => { /*Options.DisableSubmitScan = !Options.DisableSubmitScan;*/ });
            var dUnlockSafe = new PageObject(DisableTasks, () => getString("DisableUnlockSafeTask") + ": " + Utils.getOnOff(Options.DisableUnlockSafe.GetBool()), true, () => { /*Options.DisableUnlockSafe = !Options.DisableUnlockSafe;*/ });
            var dUploadData = new PageObject(DisableTasks, () => getString("DisableUploadDataTask") + ": " + Utils.getOnOff(Options.DisableUploadData.GetBool()), true, () => { /*Options.DisableUploadData = !Options.DisableUploadData;*/ });
            var dStartReactor = new PageObject(DisableTasks, () => getString("DisableStartReactorTask") + ": " + Utils.getOnOff(Options.DisableStartReactor.GetBool()), true, () => { /*Options.DisableStartReactor = !Options.DisableStartReactor;*/ });
            var dResetBreaker = new PageObject(DisableTasks, () => getString("DisableResetBreakerTask") + ": " + Utils.getOnOff(Options.DisableResetBreaker.GetBool()), true, () => { /*Options.DisableResetBreaker = !Options.DisableResetBreaker;*/ });

            var RandomMapsMode = new PageObject(ModeOptions, () => getString("RandomMapsMode"));
            var RandomMapsModeEnabled = new PageObject(RandomMapsMode, () => getString("RandomMapsMode") + ": " + Utils.getOnOff(Options.RandomMapsMode.GetBool()), true);
            var rmSkeld = new PageObject(RandomMapsMode, () => getString("AddedTheSkeld") + ": " + Utils.getOnOff(Options.AddedTheSkeld.GetBool()), true);
            var rmMiraHQ = new PageObject(RandomMapsMode, () => getString("AddedMIRAHQ") + ": " + Utils.getOnOff(Options.AddedMiraHQ.GetBool()), true);
            var rmPolus = new PageObject(RandomMapsMode, () => getString("AddedPolus") + ": " + Utils.getOnOff(Options.AddedPolus.GetBool()), true);
            //var rmDleks = new PageObject(RandomMapsMode, () => getString("AddedDleks") + ": " + Utils.getOnOff(Options.AddedDleks.GetBool()), true);
            var rmAirship = new PageObject(RandomMapsMode, () => getString("AddedTheAirShip") + ": " + Utils.getOnOff(Options.AddedTheAirShip.GetBool()), true);
            var NoGameEnd = new PageObject(ModeOptions, () => getString("NoGameEnd") + ": " + Utils.getOnOff(Options.NoGameEnd.GetBool()), true, () =>
            {
                 /* Options.NoGameEnd = !Options.NoGameEnd; */
            });

            var voteMode = new PageObject(ModeOptions, () => getString("VoteMode"));
            var WhenSkipVote = new PageObject(voteMode, () => getString("WhenSkipVote") + ": " + getString(Enum.GetName(typeof(VoteMode), Options.whenSkipVote)), true, () =>
            {
                var next = Options.whenSkipVote + 1;
                if (next > VoteMode.SelfVote) next = VoteMode.Default;
                Options.whenSkipVote = next;
            });
            var WhenNonVote = new PageObject(voteMode, () => getString("WhenNonVote") + ": " + getString(Enum.GetName(typeof(VoteMode), Options.whenNonVote)), true, () =>
            {
                var next = Options.whenNonVote + 1;
                if (next > VoteMode.Skip) next = VoteMode.Default;
                Options.whenNonVote = next;
            });
            var canTerroristSuicideWin = new PageObject(voteMode, () => getString("CanTerroristSuicideWin") + ": " + Utils.getOnOff(Options.canTerroristSuicideWin), true, () => Options.canTerroristSuicideWin = !Options.canTerroristSuicideWin);

            var Suffix = new PageObject(basePage, () => getString("SuffixMode") + ": " + Options.currentSuffix.ToString(), true, () =>
            {
                var next = Options.currentSuffix + 1;
                if (next > SuffixModes.Recording) next = SuffixModes.None;
                Options.currentSuffix = next;
            });
            Suffix.amVisible = () => AmongUsClient.Instance.AmHost;
            var forceJapanese = new PageObject(basePage, () => getString("ForceJapanese") + ": " + Utils.getOnOff(Options.forceJapanese), false, () => Options.forceJapanese = !Options.forceJapanese);
            var autoPrintLastRoles = new PageObject(basePage, () => getString("AutoDisplayLastResult") + ": " + Utils.getOnOff(Options.autoDisplayLastRoles), false, () => Options.autoDisplayLastRoles = !Options.autoDisplayLastRoles);
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
            if (!currentPage.ChildPages[currentCursor].amVisible()) currentCursor--;
        }
        public static void Enter()
        {
            var selectingObj = currentPage.ChildPages[currentCursor];

            if (selectingObj.isHostOnly && !AmongUsClient.Instance.AmHost) return;
            selectingObj.onEnter();
            RPC.SyncCustomSettingsRPC();
        }
        public static void Return()
        {
            if (currentPage.parent != null)
                SetPage(currentPage.parent);
        }
        public static void Input(int num)
        {
            var selectingObj = currentPage.ChildPages[currentCursor];

            if (selectingObj.isHostOnly && !AmongUsClient.Instance.AmHost) return;
            selectingObj.onInput(num);
            RPC.SyncCustomSettingsRPC();
        }
        public static string GetOptionText()
        {
            string text;
            text = "==" + currentPage.name + "==" + "\r\n";
            for (var i = 0; i < currentPage.ChildPages.Count; i++)
            {
                var obj = currentPage.ChildPages[i];
                if (!obj.amVisible()) continue;
                text += currentCursor == i ? ">" : "";
                text += obj.name + "\r\n";
            }
            return text;
        }
    }

    class PageObject
    {
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
        )
        {
            this.parent = parent; //親オブジェクト
            this.getName = () => text; //名前
            this.isHostOnly = isHostOnly; //実行をホストのみに限定するか
            this.onEnter = () => CustomOptionController.SetPage(this);
            this.onInput = (i) => { }; //入力時の動作

            this.ChildPages = new List<PageObject>(); //子オブジェクトリストを初期化
            parent?.ChildPages.Add(this); //親のリストに自分を追加
        }
        public PageObject( //フォルダー2
            PageObject parent,
            Func<string> name,
            bool isHostOnly = false
        )
        {
            this.parent = parent; //親オブジェクト
            this.getName = name; //名前
            this.isHostOnly = isHostOnly; //実行をホストのみに限定するか
            this.onEnter = () => CustomOptionController.SetPage(this);
            this.onInput = (i) => { }; //入力時の動作

            this.ChildPages = new List<PageObject>(); //子オブジェクトリストを初期化
            parent?.ChildPages.Add(this); //親のリストに自分を追加
        }
        public PageObject( //ON・OFF
            PageObject parent,
            Func<string> name,
            bool isHostOnly,
            Action onEnter
        )
        {
            this.parent = parent; //親オブジェクト
            this.getName = name; //名前
            this.isHostOnly = isHostOnly; //実行をホストのみに限定するか
            this.onEnter = onEnter; //実行時の動作
            this.onInput = (i) => { }; //入力時の動作

            this.ChildPages = new List<PageObject>(); //子オブジェクトリストを初期化
            parent?.ChildPages.Add(this); //親のリストに自分を追加
        }
        public PageObject( //数値設定
            PageObject parent,
            Func<string> name,
            bool isHostOnly,
            Action onEnter,
            Action<int> onInput
        )
        {
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
        )
        {
            this.parent = parent; //親オブジェクト
            this.getName = () => $"<color={Utils.getRoleColorCode(role)}>{Utils.getRoleName(role)}</color>: {role.getCount()}";
            this.isHostOnly = true; //実行をホストのみに限定するか
            this.onEnter = () => Utils.SetRoleCountToggle(role); //実行時の動作
            this.onInput = (n) => role.setCount(n); //入力時の動作

            this.ChildPages = new List<PageObject>(); //子オブジェクトリストを初期化
            parent?.ChildPages.Add(this); //親のリストに自分を追加
        }
    }
}
