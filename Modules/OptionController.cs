using BepInEx;
using BepInEx.Configuration;
using BepInEx.IL2CPP;
using System;
using HarmonyLib;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnhollowerBaseLib;
using TownOfHost;

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
            var RoleOptions = new PageObject(basePage, "Role Options");
            //役職数変更
            var Madmate = new PageObject(RoleOptions, CustomRoles.Madmate);
            var MadGuardian = new PageObject(RoleOptions, CustomRoles.MadGuardian);
            var Mafia = new PageObject(RoleOptions, CustomRoles.Mafia);
            var Vampire = new PageObject(RoleOptions, CustomRoles.Vampire);
            var Jester = new PageObject(RoleOptions, CustomRoles.Jester);
            var Terrorist = new PageObject(RoleOptions, CustomRoles.Terrorist);
            var Opportunist = new PageObject(RoleOptions, CustomRoles.Opportunist);
            var Bait = new PageObject(RoleOptions, CustomRoles.Bait);
            var SabotageMaster = new PageObject(RoleOptions, CustomRoles.SabotageMaster);
            var Mayor = new PageObject(RoleOptions, CustomRoles.Mayor);
            var Snitch = new PageObject(RoleOptions, CustomRoles.Snitch);
            var Sheriff = new PageObject(RoleOptions, CustomRoles.Sheriff);
            var BountyHunter = new PageObject(RoleOptions, CustomRoles.BountyHunter);
            var Witch = new PageObject(RoleOptions, CustomRoles.Witch);
            //役職の詳細設定
            var AdvRoleOptions = new PageObject(RoleOptions, lang.AdvancedRoleOptions);
            var VampireKillDelay = new PageObject(AdvRoleOptions, () => $"<color={main.getRoleColorCode(CustomRoles.Vampire)}>{main.getLang(lang.VampireKillDelay)}</color>(s): {main.VampireKillDelay}{main.TextCursor}", true, () => {main.VampireKillDelay = 0;}, (n) => main.ChangeInt(ref main.VampireKillDelay, n, 999));
            var SabotageMasterSkillLimit = new PageObject(AdvRoleOptions, () => $"<color={main.getRoleColorCode(CustomRoles.SabotageMaster)}>{main.getLang(lang.SabotageMasterSkillLimit)}</color>: {main.SabotageMasterSkillLimit}{main.TextCursor}", true, () => {main.SabotageMasterSkillLimit = 0;}, (n) => main.ChangeInt(ref main.SabotageMasterSkillLimit, n, 999));
            var SabotageMasterFixesDoors = new PageObject(AdvRoleOptions, () => $"<color={main.getRoleColorCode(CustomRoles.SabotageMaster)}>{main.getLang(lang.SabotageMasterFixesDoors)}</color>: {main.getOnOff(main.SabotageMasterFixesDoors)}", true, () => main.SabotageMasterFixesDoors = !main.SabotageMasterFixesDoors);
            var SabotageMasterFixesReactors = new PageObject(AdvRoleOptions, () => $"<color={main.getRoleColorCode(CustomRoles.SabotageMaster)}>{main.getLang(lang.SabotageMasterFixesReactors)}</color>: {main.getOnOff(main.SabotageMasterFixesReactors)}", true, () => main.SabotageMasterFixesReactors = !main.SabotageMasterFixesReactors);
            var SabotageMasterFixesOxygens = new PageObject(AdvRoleOptions, () => $"<color={main.getRoleColorCode(CustomRoles.SabotageMaster)}>{main.getLang(lang.SabotageMasterFixesOxygens)}</color>: {main.getOnOff(main.SabotageMasterFixesOxygens)}", true, () => main.SabotageMasterFixesOxygens = !main.SabotageMasterFixesOxygens);
            var SabotageMasterFixesComms = new PageObject(AdvRoleOptions, () => $"<color={main.getRoleColorCode(CustomRoles.SabotageMaster)}>{main.getLang(lang.SabotageMasterFixesCommunications)}</color>: {main.getOnOff(main.SabotageMasterFixesCommunications)}", true, () => main.SabotageMasterFixesCommunications = !main.SabotageMasterFixesCommunications);
            var SabotageMasterFixesElectrical = new PageObject(AdvRoleOptions, () => $"<color={main.getRoleColorCode(CustomRoles.SabotageMaster)}>{main.getLang(lang.SabotageMasterFixesElectrical)}</color>: {main.getOnOff(main.SabotageMasterFixesElectrical)}", true, () => main.SabotageMasterFixesElectrical = !main.SabotageMasterFixesElectrical);
            var SheriffCanKillJester = new PageObject(AdvRoleOptions, () => $"<color={main.getRoleColorCode(CustomRoles.Sheriff)}>{main.getLang(lang.SheriffCanKillJester)}</color>: {main.getOnOff(main.SheriffCanKillJester)}", true, () => main.SheriffCanKillJester = !main.SheriffCanKillJester);
            var SheriffCanKillTerrorist = new PageObject(AdvRoleOptions, () => $"<color={main.getRoleColorCode(CustomRoles.Sheriff)}>{main.getLang(lang.SheriffCanKillTerrorist)}</color>: {main.getOnOff(main.SheriffCanKillTerrorist)}", true, () => main.SheriffCanKillTerrorist = !main.SheriffCanKillTerrorist);
            var SheriffCanKillOpportunist = new PageObject(AdvRoleOptions, () => $"<color={main.getRoleColorCode(CustomRoles.Sheriff)}>{main.getLang(lang.SheriffCanKillOpportunist)}</color>: {main.getOnOff(main.SheriffCanKillOpportunist)}", true, () => main.SheriffCanKillOpportunist = !main.SheriffCanKillOpportunist);
            var MadmateCanFixLightsOut = new PageObject(AdvRoleOptions, () => $"<color={main.getRoleColorCode(CustomRoles.Madmate)}>{main.getLang(lang.MadmateCanFixLightsOut)}</color>: {main.getOnOff(main.MadmateCanFixLightsOut)}", true, () => {main.MadmateCanFixLightsOut = !main.MadmateCanFixLightsOut;});
            var MadGuardianCanSeeBarrier = new PageObject(AdvRoleOptions, () => $"<color={main.getRoleColorCode(CustomRoles.MadGuardian)}>{main.getLang(lang.MadGuardianCanSeeBarrier)}</color>: {main.getOnOff(main.MadGuardianCanSeeBarrier)}", true, () => {main.MadGuardianCanSeeBarrier = !main.MadGuardianCanSeeBarrier;});
            var MayorAdditionalVote = new PageObject(AdvRoleOptions, () => $"<color={main.getRoleColorCode(CustomRoles.Mayor)}>{main.getLang(lang.MayorAdditionalVote)}</color>: {main.MayorAdditionalVote}{main.TextCursor}", true, () => {main.MayorAdditionalVote = 0;}, (n) => main.ChangeInt(ref main.MayorAdditionalVote, n, 99));

                {OptionPages.modes, new PageObject(
                    "Mode Options",
                    false,
                    () => {SetPage(OptionPages.modes);},
                    new List<OptionPages>(){
                        OptionPages.HideAndSeek,
                        OptionPages.HideAndSeekOptions,
                        OptionPages.SyncButtonMode,
                        OptionPages.DisableTasks,
                        OptionPages.RandomMapsMode,
                        OptionPages.NoGameEnd,
                        OptionPages.WhenSkipVote,
                        OptionPages.WhenNonVote
                    },
                    OptionPages.basepage
                )},
                    {OptionPages.HideAndSeek, new PageObject(
                        () => main.getLang(lang.HideAndSeek) + ": " + main.getOnOff(main.IsHideAndSeek),
                        true,
                        () => {main.IsHideAndSeek = !main.IsHideAndSeek;},
                        new List<OptionPages>(){},
                        OptionPages.modes
                    )},
                    {OptionPages.HideAndSeekOptions, new PageObject(
                        lang.HideAndSeekOptions,
                        false,
                        () => {SetPage(OptionPages.HideAndSeekOptions);},
                        new List<OptionPages>(){
                            OptionPages.AllowCloseDoors,
                            OptionPages.HideAndSeekWaitingTime,
                            OptionPages.IgnoreCosmetics,
                            OptionPages.IgnoreVent,
                            OptionPages.HideAndSeekRoles
                        },
                        OptionPages.modes
                    )},
                        {OptionPages.AllowCloseDoors, new PageObject(
                            () => main.getLang(lang.AllowCloseDoors) + ": " + main.getOnOff(main.AllowCloseDoors),
                            true,
                            () => {main.AllowCloseDoors = !main.AllowCloseDoors;},
                            new List<OptionPages>(){},
                            OptionPages.HideAndSeekOptions
                        )},
                        {OptionPages.HideAndSeekWaitingTime, new PageObject(
                            () => main.getLang(lang.HideAndSeekWaitingTime) + ": " + main.HideAndSeekKillDelay,
                            true,
                            () => {main.HideAndSeekKillDelay = 0;},
                            new List<OptionPages>(){},
                            OptionPages.HideAndSeekOptions,
                            (i) => {
                                var Count = main.HideAndSeekKillDelay * 10;
                                Count += i;
                                var FixedDelay = Math.Clamp(Count,0,180);
                                main.HideAndSeekKillDelay = FixedDelay;
                            }
                        )},
                        {OptionPages.IgnoreCosmetics, new PageObject(
                            () => main.getLang(lang.IgnoreCosmetics) + ": " + main.getOnOff(main.IgnoreCosmetics),
                            true,
                            () => {main.IgnoreCosmetics = !main.IgnoreCosmetics;},
                            new List<OptionPages>(){},
                            OptionPages.HideAndSeekOptions
                        )},
                        {OptionPages.IgnoreVent, new PageObject(
                            () => main.getLang(lang.IgnoreVent) + ": " + main.getOnOff(main.IgnoreVent),
                            true,
                            () => {main.IgnoreVent = !main.IgnoreVent;},
                            new List<OptionPages>(){},
                            OptionPages.HideAndSeekOptions
                        )},
                        {OptionPages.HideAndSeekRoles, new PageObject(
                            lang.HideAndSeekRoles,
                            false,
                            () => {SetPage(OptionPages.HideAndSeekRoles);},
                            new List<OptionPages>(){
                                OptionPages.Fox,
                                OptionPages.Troll
                            },
                            OptionPages.HideAndSeekOptions
                        )},
                            {OptionPages.Fox, new PageObject(
                                () => $"<color=#e478ff>" + main.getRoleName(CustomRoles.Fox) + "</color>: " + main.FoxCount,
                                true,
                                () => {
                                    if(main.FoxCount == 0) main.FoxCount = 1;
                                    else main.FoxCount = 0;
                                },
                                new List<OptionPages>(){},
                                OptionPages.HideAndSeekRoles,
                                (i) => {
                                    var Count = main.FoxCount * 10;
                                    Count += i;
                                    var MaxCount =
                                        GameData.Instance.AllPlayers.Count
                                        - main.TrollCount;
                                    var FixedCount = Math.Clamp(Count,0,MaxCount);
                                    main.FoxCount = FixedCount;
                                }
                            )},
                            {OptionPages.Troll, new PageObject(
                                () => $"<color=#00ff00>" + main.getRoleName(CustomRoles.Troll) + "</color>: " + main.TrollCount,
                                true,
                                () => {
                                    if(main.TrollCount == 0) main.TrollCount = 1;
                                    else main.TrollCount = 0;
                                },
                                new List<OptionPages>(){},
                                OptionPages.HideAndSeekRoles,
                                (i) => {
                                    var Count = main.TrollCount * 10;
                                    Count += i;
                                    var MaxCount =
                                        GameData.Instance.AllPlayers.Count
                                        - main.FoxCount;
                                    var FixedCount = Math.Clamp(Count,0,MaxCount);
                                    main.TrollCount = FixedCount;
                                }
                            )},
                    {OptionPages.SyncButtonMode, new PageObject(
                        lang.SyncButtonMode,
                        false,
                        () => {SetPage(OptionPages.SyncButtonMode);},
                        new List<OptionPages>(){OptionPages.SyncButtonModeEnabled, OptionPages.SyncedButtonCount},
                        OptionPages.modes
                    )},
                        {OptionPages.SyncButtonModeEnabled, new PageObject(
                            () => main.getLang(lang.SyncButtonMode) + ": " + main.getOnOff(main.SyncButtonMode),
                            true,
                            () => {
                                main.SyncButtonMode = !main.SyncButtonMode;
                            },
                            new List<OptionPages>(){},
                            OptionPages.SyncButtonMode
                        )},
                        {OptionPages.SyncedButtonCount, new PageObject(
                            () => main.getLang(lang.SyncedButtonCount) + ": " + main.SyncedButtonCount + main.TextCursor,
                            true,
                            () => {main.SyncedButtonCount = 0;},
                            new List<OptionPages>(){},
                            OptionPages.SyncButtonMode,
                            (i) => {
                                var Count = main.SyncedButtonCount * 10;
                                Count += i;
                                var FixedCount = Math.Clamp(Count,0,100);
                                main.SyncedButtonCount = FixedCount;
                            }
                        )},
                    {OptionPages.DisableTasks, new PageObject(
                        lang.DisableTasks,
                        false,
                        () => {SetPage(OptionPages.DisableTasks);},
                        new List<OptionPages>(){
                            OptionPages.SwipeCard,
                            OptionPages.SubmitScan,
                            OptionPages.UnlockSafe,
                            OptionPages.UploadData,
                            OptionPages.StartReactor
                        },
                        OptionPages.modes
                    )},
                        {OptionPages.SwipeCard, new PageObject(
                            () => main.getLang(lang.DisableSwipeCardTask) + ": " + main.getOnOff(main.DisableSwipeCard),
                            true,
                            () => {main.DisableSwipeCard = !main.DisableSwipeCard;},
                            new List<OptionPages>(){},
                            OptionPages.modes
                        )},
                        {OptionPages.SubmitScan, new PageObject(
                            () => main.getLang(lang.DisableSubmitScanTask) + ": " + main.getOnOff(main.DisableSubmitScan),
                            true,
                            () => {main.DisableSubmitScan = !main.DisableSubmitScan;},
                            new List<OptionPages>(){},
                            OptionPages.modes
                        )},
                        {OptionPages.UnlockSafe, new PageObject(
                            () => main.getLang(lang.DisableUnlockSafeTask) + ": " + main.getOnOff(main.DisableUnlockSafe),
                            true,
                            () => {main.DisableUnlockSafe = !main.DisableUnlockSafe;},
                            new List<OptionPages>(){},
                            OptionPages.modes
                        )},
                        {OptionPages.UploadData, new PageObject(
                            () => main.getLang(lang.DisableUploadDataTask) + ": " + main.getOnOff(main.DisableUploadData),
                            true,
                            () => {main.DisableUploadData = !main.DisableUploadData;},
                            new List<OptionPages>(){},
                            OptionPages.modes
                        )},
                        {OptionPages.StartReactor, new PageObject(
                            () => main.getLang(lang.DisableStartReactorTask) + ": " + main.getOnOff(main.DisableStartReactor),
                            true,
                            () => {main.DisableStartReactor = !main.DisableStartReactor;},
                            new List<OptionPages>(){},
                            OptionPages.modes
                        )},
                    {OptionPages.RandomMapsMode, new PageObject(
                        lang.RandomMapsMode,
                        false,
                        () => {SetPage(OptionPages.RandomMapsMode);},
                        new List<OptionPages>(){
                            OptionPages.RandomMapsModeEnabled,
                            OptionPages.AddedTheSkeld,
                            OptionPages.AddedMIRAHQ,
                            OptionPages.AddedPolus,
                            OptionPages.AddedDleks,
                            OptionPages.AddedTheAirShip
                        },
                        OptionPages.modes
                    )},
                        {OptionPages.RandomMapsModeEnabled, new PageObject(
                            () => main.getLang(lang.RandomMapsMode) + ": " + main.getOnOff(main.RandomMapsMode),
                            true,
                            () => {main.RandomMapsMode = !main.RandomMapsMode;},
                            new List<OptionPages>(){},
                            OptionPages.modes
                        )},
                        {OptionPages.AddedTheSkeld, new PageObject(
                            () => main.getLang(lang.AddedTheSkeld) + ": " + main.getOnOff(main.AddedTheSkeld),
                            true,
                            () => {main.AddedTheSkeld = !main.AddedTheSkeld;},
                            new List<OptionPages>(){},
                            OptionPages.modes
                        )},
                        {OptionPages.AddedMIRAHQ, new PageObject(
                            () => main.getLang(lang.AddedMIRAHQ) + ": " + main.getOnOff(main.AddedMIRAHQ),
                            true,
                            () => {main.AddedMIRAHQ = !main.AddedMIRAHQ;},
                            new List<OptionPages>(){},
                            OptionPages.modes
                        )},
                        {OptionPages.AddedPolus, new PageObject(
                            () => main.getLang(lang.AddedPolus) + ": " + main.getOnOff(main.AddedPolus),
                            true,
                            () => {main.AddedPolus = !main.AddedPolus;},
                            new List<OptionPages>(){},
                            OptionPages.modes
                        )},
                        {OptionPages.AddedDleks, new PageObject(
                            () => main.getLang(lang.AddedDleks) + ": " + main.getOnOff(main.AddedDleks),
                            true,
                            () => {main.AddedDleks = !main.AddedDleks;},
                            new List<OptionPages>(){},
                            OptionPages.modes
                        )},
                        {OptionPages.AddedTheAirShip, new PageObject(
                            () => main.getLang(lang.AddedTheAirShip) + ": " + main.getOnOff(main.AddedTheAirShip),
                            true,
                            () => {main.AddedTheAirShip = !main.AddedTheAirShip;},
                            new List<OptionPages>(){},
                            OptionPages.modes
                        )},
                    {OptionPages.NoGameEnd, new PageObject(
                        () => main.getLang(lang.NoGameEnd) + "<DEBUG>: " + main.getOnOff(main.NoGameEnd),
                        true,
                        () => {main.NoGameEnd = !main.NoGameEnd;},
                        new List<OptionPages>(){},
                        OptionPages.modes
                    )},
                    {OptionPages.WhenSkipVote, new PageObject(
                        () => main.getLang(lang.WhenSkipVote) + ": " + main.whenSkipVote.ToString(),
                        true,
                        () => {
                            var next = main.whenSkipVote + 1;
                            if(next > VoteMode.SelfVote) next = VoteMode.Default;
                            main.whenSkipVote = next;
                        },
                        new List<OptionPages>(){},
                        OptionPages.basepage
                    )},
                    {OptionPages.WhenNonVote, new PageObject(
                        () => main.getLang(lang.WhenNonVote) + ": " + main.whenNonVote.ToString(),
                        true,
                        () => {
                            var next = main.whenNonVote + 1;
                            if(next > VoteMode.SelfVote) next = VoteMode.Default;
                            main.whenNonVote = next;
                        },
                        new List<OptionPages>(){},
                        OptionPages.basepage
                    )},
                {OptionPages.Suffix, new PageObject(
                    () => main.getLang(lang.SuffixMode) + ": " + main.currentSuffix.ToString(),
                    false,
                    () => {
                        var next = main.currentSuffix + 1;
                        if(next > SuffixModes.Recording) next = SuffixModes.None;
                        main.currentSuffix = next;
                    },
                    new List<OptionPages>(){},
                    OptionPages.basepage
                )}
        }
        public static void SetPage(PageObject page)
        {
            currentCursor = 0;
            currentPage = page;
        }
        public static void Up()
        {
            if (currentCursor <= 0) currentCursor = 0;
            else currentCursor--;
        }
        public static void Down()
        {
            if (currentCursor >= currentPage.ChildPages.Count - 1) currentCursor = currentPage.ChildPages.Count - 1;
            else currentCursor++;
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
        public PageObject( //フォルダー2
            PageObject parent,
            lang lang,
            bool isHostOnly = false
        ) {
            this.parent = parent; //親オブジェクト
            this.getName = () => main.getLang(lang); //名前
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
            this.isHostOnly = false; //実行をホストのみに限定するか
            this.onEnter = () => main.SetRoleCountToggle(main.GetCountFromRole(role)); //実行時の動作
            this.onInput = (n) => role.SetCount(n); //入力時の動作

            this.ChildPages = new List<PageObject>(); //子オブジェクトリストを初期化
            parent?.ChildPages.Add(this); //親のリストに自分を追加
        }
    }
    public enum OptionPages
    {
        basepage = 0,
            roles,
                Jester,
                Madmate,
                MadGuardian,
                Bait,
                Terrorist,
                Mafia,
                Vampire,
                SabotageMaster,
                Mayor,
                Sheriff,
                Opportunist,
                Snitch,
                BountyHunter,
                Witch,
                VampireOptions,
                AdvancedRoleOptions,
                    VampireKillDelay,
                    MadmateCanFixLightsOut,
                    MadGuardianCanSeeBarrier,
                    SabotageMasterSkillLimit,
                    SabotageMasterFixesDoors,
                    SabotageMasterFixesReactors,
                    SabotageMasterFixesOxygens,
                    SabotageMasterFixesCommunications,
                    SabotageMasterFixesElectrical,
                    SheriffCanKillJester,
                    SheriffCanKillTerrorist,
                    SheriffCanKillOpportunist,
                    MayorAdditionalVote,
            modes,
                HideAndSeek,
                HideAndSeekOptions,
                    AllowCloseDoors,
                    IgnoreCosmetics,
                    HideAndSeekWaitingTime,
                    IgnoreVent,
                    HideAndSeekRoles,
                        Fox,
                        Troll,
                SyncButtonMode,
                    SyncButtonModeEnabled,
                    SyncedButtonCount,
                DisableTasks,
                    SwipeCard,
                    SubmitScan,
                    UnlockSafe,
                    UploadData,
                    StartReactor,
                RandomMapsMode,
                    RandomMapsModeEnabled,
                    AddedTheSkeld,
                    AddedMIRAHQ,
                    AddedPolus,
                    AddedDleks,
                    AddedTheAirShip,
                NoGameEnd,
                WhenSkipVote,
                WhenNonVote,
            Suffix
    }
}
