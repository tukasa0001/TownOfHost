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

namespace TownOfHost {
    static class CustomOptionController {
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
                        OptionPages.Bait
                    },
                    OptionPages.basepage
                )},
                    {OptionPages.Madmate, new PageObject(
                        "<color=#ff0000>Madmate</color>: $MadmateEnabled",
                        true,
                        () => {main.ToggleRole(EngineerRole.Madmate);},
                        new List<OptionPages>(){},
                        OptionPages.roles
                    )},
                    {OptionPages.Sidekick, new PageObject(
                        "<color=#ff0000>Sidekick</color>: $SidekickEnabled",
                        true,
                        () => {main.ToggleRole(ShapeshifterRoles.Sidekick);},
                        new List<OptionPages>(){},
                        OptionPages.roles
                    )},
                    {OptionPages.Vampire, new PageObject(
                        "<color=#a757a8>Vampire</color>: $VampireEnabled",
                        true,
                        () => {main.ToggleRole(ImpostorRoles.Vampire);},
                        new List<OptionPages>(){},
                        OptionPages.roles
                    )},
                    {OptionPages.Jester, new PageObject(
                        "<color=#d161a4>Jester</color>: $JesterEnabled",
                        true,
                        () => {main.ToggleRole(ScientistRole.Jester);},
                        new List<OptionPages>(){},
                        OptionPages.roles
                    )},
                    {OptionPages.Terrorist, new PageObject(
                        "<color=#00ff00>Terrorist</color>: $TerroristEnabled",
                        true,
                        () => {main.ToggleRole(EngineerRole.Terrorist);},
                        new List<OptionPages>(){},
                        OptionPages.roles
                    )},
                    {OptionPages.Bait, new PageObject(
                        "<color=#00bfff>Bait</color>: $BaitEnabled",
                        true,
                        () => {main.ToggleRole(ScientistRole.Bait);},
                        new List<OptionPages>(){},
                        OptionPages.roles
                    )},
                {OptionPages.modes, new PageObject(
                    "Mode Options",
                    false,
                    () => {SetPage(OptionPages.modes);},
                    new List<OptionPages>(){OptionPages.HideAndSeek, OptionPages.NoGameEnd},
                    OptionPages.basepage
                )},
                    {OptionPages.HideAndSeek, new PageObject(
                        "HideAndSeek: $HideAndSeekEnabled",
                        true,
                        () => {main.IsHideAndSeek = !main.IsHideAndSeek;},
                        new List<OptionPages>(){},
                        OptionPages.roles
                    )},
                    {OptionPages.NoGameEnd, new PageObject(
                        "NoGameEnd<DEBUG>: $NoGameEndEnabled",
                        true,
                        () => {main.NoGameEnd = !main.NoGameEnd;},
                        new List<OptionPages>(){},
                        OptionPages.roles
                    )}
        };
        public static OptionPages currentPage = OptionPages.basepage;
        public static int currentCursor = 0;
        public static void SetPage(OptionPages page) {
            currentCursor = 0;
            currentPage = page;
        }
        public static void Up() {
            var currentPageObj = PageObjects[currentPage];
            if(currentCursor <= 0) currentCursor = 0;
            else currentCursor--;
        }
        public static void Down() {
            var currentPageObj = PageObjects[currentPage];
            if(currentCursor >= currentPageObj.PagesInThis.Count - 1) currentCursor = currentPageObj.PagesInThis.Count - 1;
            else currentCursor++;
        }
        public static void Enter() {
            var currentPageObj = PageObjects[currentPage];
            var selectingObj = PageObjects[currentPageObj.PagesInThis[currentCursor]];

            if(selectingObj.isHostOnly && !AmongUsClient.Instance.AmHost) return;
            selectingObj.onEnter();
            main.SyncCustomSettingsRPC();
        }
        public static void Return() {
            var currentPageObj = PageObjects[currentPage];
            SetPage(currentPageObj.pageToReturn);
        }
        public static string GetOptionText() {
            string text;
            var currentPageObj = PageObjects[currentPage];

            text = "==" + currentPageObj.name + "==" + "\r\n";
            for(var i = 0; i < currentPageObj.PagesInThis.Count; i++) {
                var obj = PageObjects[currentPageObj.PagesInThis[i]];

                text += currentCursor == i ? ">" : "";
                text += obj.name + "\r\n";
            }

            //置き換え
            text = text.Replace("$JesterEnabled", ChatCommands.getOnOff(main.currentScientist == ScientistRole.Jester));
            text = text.Replace("$MadmateEnabled", ChatCommands.getOnOff(main.currentEngineer == EngineerRole.Madmate));
            text = text.Replace("$BaitEnabled", ChatCommands.getOnOff(main.currentScientist == ScientistRole.Bait));
            text = text.Replace("$TerroristEnabled", ChatCommands.getOnOff(main.currentEngineer == EngineerRole.Terrorist));
            text = text.Replace("$SidekickEnabled", ChatCommands.getOnOff(main.currentShapeshifter == ShapeshifterRoles.Sidekick));
            text = text.Replace("$VampireEnabled", ChatCommands.getOnOff(main.currentImpostor == ImpostorRoles.Vampire));
            text = text.Replace("$HideAndSeekEnabled", ChatCommands.getOnOff(main.IsHideAndSeek));
            text = text.Replace("$NoGameEndEnabled", ChatCommands.getOnOff(main.NoGameEnd));

            return text;
        }
    }
    class PageObject {
        public string name;
        public bool isHostOnly;
        public Action onEnter;
        public List<OptionPages> PagesInThis;
        public OptionPages pageToReturn;
        public PageObject(string name, bool isHostOnly, Action onEnter, List<OptionPages> PageInThis, OptionPages PageToReturn) {
            this.name = name;
            this.isHostOnly = isHostOnly;
            this.onEnter = onEnter;
            this.PagesInThis = PageInThis;
            this.pageToReturn = PageToReturn;
        }
    }
    public enum OptionPages {
        basepage = 0,
            roles,
                Jester,
                Madmate,
                Bait,
                Terrorist,
                Sidekick,
                Vampire,
            modes,
                HideAndSeek,
                NoGameEnd
    }
}