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
        public static Dictionary<OptionPages, PageObject> PageObjects = new Dictionary<OptionPages, PageObject>(){
            {OptionPages.basepage, new PageObject(
                "Town Of Host Options",
                false,
                () => {},
                new List<OptionPages>(){OptionPages.roles, OptionPages.modes},
                OptionPages.basepage
            )},
                {OptionPages.roles, new PageObject(
                    "Role Options",
                    false,
                    () => {SetPage(OptionPages.roles);},
                    new List<OptionPages>(){
                        OptionPages.Sidekick,
                        OptionPages.Vampire,
                        OptionPages.Madmate,
                        OptionPages.Jester,
                        OptionPages.Terrorist,
                        OptionPages.Bait,
                        OptionPages.AdvancedRoleOptions
                    },
                    OptionPages.basepage
                )},
                    {OptionPages.Madmate, new PageObject(
                        () => "<color=#ff0000>Madmate</color>: " + main.getOnOff(main.currentEngineer == EngineerRole.Madmate),
                        true,
                        () => {main.ToggleRole(EngineerRole.Madmate);},
                        new List<OptionPages>(){},
                        OptionPages.roles
                    )},
                    {OptionPages.Sidekick, new PageObject(
                        () => "<color=#ff0000>Sidekick</color>: " + main.getOnOff(main.currentShapeshifter == ShapeshifterRoles.Sidekick),
                        true,
                        () => {main.ToggleRole(ShapeshifterRoles.Sidekick);},
                        new List<OptionPages>(){},
                        OptionPages.roles
                    )},
                    {OptionPages.Vampire, new PageObject(
                        () => "<color=#a757a8>Vampire</color>: " + main.getOnOff(main.currentImpostor == ImpostorRoles.Vampire),
                        true,
                        () => {main.ToggleRole(ImpostorRoles.Vampire);},
                        new List<OptionPages>(){},
                        OptionPages.roles
                    )},
                    {OptionPages.Jester, new PageObject(
                        () => "<color=#d161a4>Jester</color>: " + main.getOnOff(main.currentScientist == ScientistRole.Jester),
                        true,
                        () => {main.ToggleRole(ScientistRole.Jester);},
                        new List<OptionPages>(){},
                        OptionPages.roles
                    )},
                    {OptionPages.Terrorist, new PageObject(
                        () => "<color=#00ff00>Terrorist</color>: " + main.getOnOff(main.currentEngineer == EngineerRole.Terrorist),
                        true,
                        () => {main.ToggleRole(EngineerRole.Terrorist);},
                        new List<OptionPages>(){},
                        OptionPages.roles
                    )},
                    {OptionPages.Bait, new PageObject(
                        () => "<color=#00bfff>Bait</color>: " + main.getOnOff(main.currentScientist == ScientistRole.Bait),
                        true,
                        () => {main.ToggleRole(ScientistRole.Bait);},
                        new List<OptionPages>(){},
                        OptionPages.roles
                    )},
                    {OptionPages.AdvancedRoleOptions, new PageObject(
                        "Advanced Options",
                        false,
                        () => {SetPage(OptionPages.AdvancedRoleOptions);},
                        new List<OptionPages>(){OptionPages.VampireKillDelay},
                        OptionPages.roles
                    )},
                        {OptionPages.VampireKillDelay, new PageObject(
                            () => "<color=#a757a8>Vampire Kill Delay</color>(s): " + main.VampireKillDelay + main.TextCursor,
                            true,
                            () => {main.VampireKillDelay = 0;},
                            new List<OptionPages>(){},
                            OptionPages.AdvancedRoleOptions,
                            (i) => {
                                var KillDelay = main.VampireKillDelay * 10;
                                KillDelay += i;
                                var FixedKillDelay = Math.Clamp(KillDelay,0,999);
                                main.VampireKillDelay = FixedKillDelay;
                            }
                        )},
                {OptionPages.modes, new PageObject(
                    "Mode Options",
                    false,
                    () => {SetPage(OptionPages.modes);},
                    new List<OptionPages>(){
                        OptionPages.HideAndSeek,
                        OptionPages.HideAndSeekOptions,
                        OptionPages.SyncButtonMode,
                        OptionPages.DisableTasks,
                        OptionPages.NoGameEnd
                    },
                    OptionPages.basepage
                )},
                    {OptionPages.HideAndSeek, new PageObject(
                        () => "HideAndSeek<BETA>: " + main.getOnOff(main.IsHideAndSeek),
                        true,
                        () => {main.IsHideAndSeek = !main.IsHideAndSeek;},
                        new List<OptionPages>(){},
                        OptionPages.modes
                    )},
                    {OptionPages.HideAndSeekOptions, new PageObject(
                        "HideAndSeek Options",
                        false,
                        () => {SetPage(OptionPages.HideAndSeekOptions);},
                        new List<OptionPages>(){
                            OptionPages.AllowCloseDoors,
                            OptionPages.HideAndSeekWaitingTime,
                            OptionPages.HideAndSeekRoles
                        },
                        OptionPages.modes
                    )},
                        {OptionPages.AllowCloseDoors, new PageObject(
                            () => "Allow Close Doors: " + main.getOnOff(main.AllowCloseDoors),
                            true,
                            () => {main.AllowCloseDoors = !main.AllowCloseDoors;},
                            new List<OptionPages>(){},
                            OptionPages.HideAndSeekOptions
                        )},
                        {OptionPages.HideAndSeekWaitingTime, new PageObject(
                            () => "Impostor waiting time: " + main.HideAndSeekKillDelay,
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
                        {OptionPages.HideAndSeekRoles, new PageObject(
                            "HideAndSeekRoles",
                            false,
                            () => {SetPage(OptionPages.HideAndSeekRoles);},
                            new List<OptionPages>(){
                                OptionPages.Fox,
                                OptionPages.Troll
                            },
                            OptionPages.HideAndSeekOptions
                        )},
                            {OptionPages.Fox, new PageObject(
                                () => "<color=#e478ff>Fox</color>: " + main.FoxCount,
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
                                () => "<color=#00ff00>Troll</color>: " + main.TrollCount,
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
                        "Sync Button Mode",
                        false,
                        () => {SetPage(OptionPages.SyncButtonMode);},
                        new List<OptionPages>(){OptionPages.SyncButtonModeEnabled, OptionPages.SyncedButtonCount},
                        OptionPages.modes
                    )},
                        {OptionPages.SyncButtonModeEnabled, new PageObject(
                            () => "Sync Button Mode: " + main.getOnOff(main.SyncButtonMode),
                            true,
                            () => {
                                main.SyncButtonMode = !main.SyncButtonMode;
                                //一人当たりのボタン数を9に設定
                                //PlayerControl.GameOptions.NumEmergencyMeetings = 9;
                                PlayerControl.LocalPlayer.RpcSyncSettings(PlayerControl.GameOptions);
                            },
                            new List<OptionPages>(){},
                            OptionPages.SyncButtonMode
                        )},
                        {OptionPages.SyncedButtonCount, new PageObject(
                            () => "Synced Buttons Count: " + main.SyncedButtonCount + main.TextCursor,
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
                        "Disable Tasks",
                        false,
                        () => {SetPage(OptionPages.DisableTasks);},
                        new List<OptionPages>(){OptionPages.SwipeCard, OptionPages.SubmitScan, OptionPages.UnlockSafe, OptionPages.UploadData, OptionPages.StartReactor},
                        OptionPages.modes
                    )},
                        {OptionPages.SwipeCard, new PageObject(
                            () => "Disable SwipeCard Task: " + main.getOnOff(main.DisableSwipeCard),
                            true,
                            () => {main.DisableSwipeCard = !main.DisableSwipeCard;},
                            new List<OptionPages>(){},
                            OptionPages.modes
                        )},
                        {OptionPages.SubmitScan, new PageObject(
                            () => "Disable SubmitScan Task: " + main.getOnOff(main.DisableSubmitScan),
                            true,
                            () => {main.DisableSubmitScan = !main.DisableSubmitScan;},
                            new List<OptionPages>(){},
                            OptionPages.modes
                        )},
                        {OptionPages.UnlockSafe, new PageObject(
                            () => "Disable UnlockSafe Task: " + main.getOnOff(main.DisableUnlockSafe),
                            true,
                            () => {main.DisableUnlockSafe = !main.DisableUnlockSafe;},
                            new List<OptionPages>(){},
                            OptionPages.modes
                        )},
                        {OptionPages.UploadData, new PageObject(
                            () => "Disable UploadData Task: " + main.getOnOff(main.DisableUploadData),
                            true,
                            () => {main.DisableUploadData = !main.DisableUploadData;},
                            new List<OptionPages>(){},
                            OptionPages.modes
                        )},
                        {OptionPages.StartReactor, new PageObject(
                            () => "Disable StartReactor Task: " + main.getOnOff(main.DisableStartReactor),
                            true,
                            () => {main.DisableStartReactor = !main.DisableStartReactor;},
                            new List<OptionPages>(){},
                            OptionPages.modes
                        )},
                    {OptionPages.NoGameEnd, new PageObject(
                        () => "NoGameEnd<DEBUG>: " + main.getOnOff(main.NoGameEnd),
                        true,
                        () => {main.NoGameEnd = !main.NoGameEnd;},
                        new List<OptionPages>(){},
                        OptionPages.modes
                    )}
        };
        public static OptionPages currentPage = OptionPages.basepage;
        public static int currentCursor = 0;
        public static void SetPage(OptionPages page)
        {
            currentCursor = 0;
            currentPage = page;
        }
        public static void Up()
        {
            var currentPageObj = PageObjects[currentPage];
            if (currentCursor <= 0) currentCursor = 0;
            else currentCursor--;
        }
        public static void Down()
        {
            var currentPageObj = PageObjects[currentPage];
            if (currentCursor >= currentPageObj.PagesInThis.Count - 1) currentCursor = currentPageObj.PagesInThis.Count - 1;
            else currentCursor++;
        }
        public static void Enter()
        {
            var currentPageObj = PageObjects[currentPage];
            var selectingObj = PageObjects[currentPageObj.PagesInThis[currentCursor]];

            if (selectingObj.isHostOnly && !AmongUsClient.Instance.AmHost) return;
            selectingObj.onEnter();
            main.SyncCustomSettingsRPC();
        }
        public static void Return()
        {
            var currentPageObj = PageObjects[currentPage];
            SetPage(currentPageObj.pageToReturn);
        }
        public static void Input(int num)
        {
            var currentPageObj = PageObjects[currentPage];
            var selectingObj = PageObjects[currentPageObj.PagesInThis[currentCursor]];

            if (selectingObj.isHostOnly && !AmongUsClient.Instance.AmHost) return;
            selectingObj.onInput(num);
            main.SyncCustomSettingsRPC();
        }
        public static string GetOptionText()
        {
            string text;
            var currentPageObj = PageObjects[currentPage];

            text = "==" + currentPageObj.name + "==" + "\r\n";
            for (var i = 0; i < currentPageObj.PagesInThis.Count; i++)
            {
                var obj = PageObjects[currentPageObj.PagesInThis[i]];

                text += currentCursor == i ? ">" : "";
                text += obj.name + "\r\n";
            }

            return text;
        }
    }
    class PageObject
    {
        public string name => getName();
        private Func<string> getName;
        public bool isHostOnly;
        public Action onEnter;
        public List<OptionPages> PagesInThis;
        public OptionPages pageToReturn;
        public Action<int> onInput;
        public PageObject(string name, bool isHostOnly, Action onEnter, List<OptionPages> PageInThis, OptionPages PageToReturn)
        {
            this.getName = () => name;
            this.isHostOnly = isHostOnly;
            this.onEnter = onEnter;
            this.PagesInThis = PageInThis;
            this.pageToReturn = PageToReturn;
            this.onInput = (i) => { return; };
        }
        public PageObject(Func<string> getName, bool isHostOnly, Action onEnter, List<OptionPages> PageInThis, OptionPages PageToReturn)
        {
            this.getName = getName;
            this.isHostOnly = isHostOnly;
            this.onEnter = onEnter;
            this.PagesInThis = PageInThis;
            this.pageToReturn = PageToReturn;
            this.onInput = (i) => { return; };
        }
        public PageObject(string name, bool isHostOnly, Action onEnter, List<OptionPages> PageInThis, OptionPages PageToReturn, Action<int> onInput)
        {
            this.getName = () => name;
            this.isHostOnly = isHostOnly;
            this.onEnter = onEnter;
            this.PagesInThis = PageInThis;
            this.pageToReturn = PageToReturn;
            this.onInput = onInput;
        }
        public PageObject(Func<string> getName, bool isHostOnly, Action onEnter, List<OptionPages> PageInThis, OptionPages PageToReturn, Action<int> onInput)
        {
            this.getName = getName;
            this.isHostOnly = isHostOnly;
            this.onEnter = onEnter;
            this.PagesInThis = PageInThis;
            this.pageToReturn = PageToReturn;
            this.onInput = onInput;
        }
    }
    public enum OptionPages
    {
        basepage = 0,
            roles,
                Jester,
                Madmate,
                Bait,
                Terrorist,
                Sidekick,
                Vampire,
                VampireOptions,
                AdvancedRoleOptions,
                    VampireKillDelay,
            modes,
                HideAndSeek,
                HideAndSeekOptions,
                    AllowCloseDoors,
                    IgnoreCosmetics,
                    HideAndSeekWaitingTime,
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
                NoGameEnd
    }
}
