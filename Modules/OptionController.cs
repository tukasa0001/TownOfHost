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
                new List<OptionPages>(){
                    OptionPages.roles,
                    OptionPages.modes,
                    OptionPages.Suffix
                },
                OptionPages.basepage
            )},
                {OptionPages.roles, new PageObject(
                    "Role Options",
                    false,
                    () => {SetPage(OptionPages.roles);},
                    new List<OptionPages>(){
                        OptionPages.Vampire,
                        OptionPages.BountyHunter,
                        OptionPages.Witch,
                        OptionPages.Mafia,
                        OptionPages.Madmate,
                        OptionPages.MadGuardian,
                        OptionPages.Jester,
                        OptionPages.Opportunist,
                        OptionPages.Terrorist,
                        OptionPages.Bait,
                        OptionPages.Mayor,
                        OptionPages.SabotageMaster,
                        OptionPages.Snitch,
                        OptionPages.Sheriff,
                        OptionPages.AdvancedRoleOptions
                    },
                    OptionPages.basepage
                )},
                    {OptionPages.Madmate, new PageObject(
                        () => $"<color={main.getRoleColorCode(CustomRoles.Madmate)}>{main.getRoleName(CustomRoles.Madmate)}</color>: {main.MadmateCount}",
                        true,
                        () => {main.SetRoleCountToggle(CustomRoles.Madmate);},
                        new List<OptionPages>(){},
                        OptionPages.roles,
                        i => main.SetRoleCount(CustomRoles.Madmate, i)
                    )},
                    {OptionPages.MadGuardian, new PageObject(
                        () => $"<color={main.getRoleColorCode(CustomRoles.MadGuardian)}>{main.getRoleName(CustomRoles.MadGuardian)}</color>: {main.MadGuardianCount}",
                        true,
                        () => {main.SetRoleCountToggle(CustomRoles.MadGuardian);},
                        new List<OptionPages>(){},
                        OptionPages.roles,
                        i => main.SetRoleCount(CustomRoles.MadGuardian, i)
                    )},
                    {OptionPages.Mafia, new PageObject(
                        () => $"<color={main.getRoleColorCode(CustomRoles.Mafia)}>{main.getRoleName(CustomRoles.Mafia)}</color>: {main.MafiaCount}",
                        true,
                        () => {main.SetRoleCountToggle(CustomRoles.Mafia);},
                        new List<OptionPages>(){},
                        OptionPages.roles,
                        i => main.SetRoleCount(CustomRoles.Mafia, i)
                    )},
                    {OptionPages.Vampire, new PageObject(
                        () => $"<color={main.getRoleColorCode(CustomRoles.Vampire)}>{main.getRoleName(CustomRoles.Vampire)}</color>: {main.VampireCount}",
                        true,
                        () => {main.SetRoleCountToggle(CustomRoles.Vampire);},
                        new List<OptionPages>(){},
                        OptionPages.roles,
                        i => main.SetRoleCount(CustomRoles.Vampire, i)
                    )},
                    {OptionPages.Jester, new PageObject(
                        () => $"<color={main.getRoleColorCode(CustomRoles.Jester)}>{main.getRoleName(CustomRoles.Jester)}</color>: {main.JesterCount}",
                        true,
                        () => {main.SetRoleCountToggle(CustomRoles.Jester);},
                        new List<OptionPages>(){},
                        OptionPages.roles,
                        i => main.SetRoleCount(CustomRoles.Jester, i)
                    )},
                    {OptionPages.Terrorist, new PageObject(
                        () => $"<color={main.getRoleColorCode(CustomRoles.Terrorist)}>{main.getRoleName(CustomRoles.Terrorist)}</color>: {main.TerroristCount}",
                        true,
                        () => {main.SetRoleCountToggle(CustomRoles.Terrorist);},
                        new List<OptionPages>(){},
                        OptionPages.roles,
                        i => main.SetRoleCount(CustomRoles.Terrorist, i)
                    )},
                    {OptionPages.Opportunist, new PageObject(
                        () => $"<color={main.getRoleColorCode(CustomRoles.Opportunist)}>{main.getRoleName(CustomRoles.Opportunist)}</color>: {main.OpportunistCount}",
                        true,
                        () => {main.SetRoleCountToggle(CustomRoles.Opportunist);},
                        new List<OptionPages>(){},
                        OptionPages.roles,
                        i => main.SetRoleCount(CustomRoles.Opportunist, i)
                    )},
                    {OptionPages.Bait, new PageObject(
                        () => $"<color={main.getRoleColorCode(CustomRoles.Bait)}>{main.getRoleName(CustomRoles.Bait)}</color>: {main.BaitCount}",
                        true,
                        () => {main.SetRoleCountToggle(CustomRoles.Bait);},
                        new List<OptionPages>(){},
                        OptionPages.roles,
                        i => main.SetRoleCount(CustomRoles.Bait, i)
                    )},
                    {OptionPages.SabotageMaster, new PageObject(
                        () => $"<color={main.getRoleColorCode(CustomRoles.SabotageMaster)}>{main.getRoleName(CustomRoles.SabotageMaster)}</color>: {main.SabotageMasterCount}",
                        true,
                        () => {main.SetRoleCountToggle(CustomRoles.SabotageMaster);},
                        new List<OptionPages>(){},
                        OptionPages.roles,
                        i => main.SetRoleCount(CustomRoles.SabotageMaster, i)
                    )},
                    {OptionPages.Mayor, new PageObject(
                        () => $"<color={main.getRoleColorCode(CustomRoles.Mayor)}>{main.getRoleName(CustomRoles.Mayor)}</color>: {main.MayorCount}",
                        true,
                        () => {main.SetRoleCountToggle(CustomRoles.Mayor);},
                        new List<OptionPages>(){},
                        OptionPages.roles,
                        i => main.SetRoleCount(CustomRoles.Mayor, i)
                    )},
                    {OptionPages.Snitch, new PageObject(
                        () => $"<color={main.getRoleColorCode(CustomRoles.Snitch)}>{main.getRoleName(CustomRoles.Snitch)}</color>: {main.SnitchCount}",
                        true,
                        () => {main.SetRoleCountToggle(CustomRoles.Snitch);},
                        new List<OptionPages>(){},
                        OptionPages.roles,
                        i => main.SetRoleCount(CustomRoles.Snitch, i)
                    )},
                    {OptionPages.Sheriff, new PageObject(
                        () => "<color=#ffff00>" + main.getRoleName(CustomRoles.Sheriff) + "</color>: " + main.SheriffCount,
                        true,
                        () => {main.SetRoleCountToggle(CustomRoles.Sheriff);},
                        new List<OptionPages>(){},
                        OptionPages.roles,
                        i => main.SetRoleCount(CustomRoles.Sheriff, i)
                    )},
                    {OptionPages.BountyHunter, new PageObject(
                        () => $"<color={main.getRoleColorCode(CustomRoles.BountyHunter)}>{main.getRoleName(CustomRoles.BountyHunter)}</color>: {main.BountyHunterCount}",
                        true,
                        () => {main.SetRoleCountToggle(CustomRoles.BountyHunter);},
                        new List<OptionPages>(){},
                        OptionPages.roles,
                        i => main.SetRoleCount(CustomRoles.BountyHunter, i)
                    )},
                    {OptionPages.Witch, new PageObject(
                        () => $"<color={main.getRoleColorCode(CustomRoles.Witch)}>{main.getRoleName(CustomRoles.Witch)}</color>: {main.WitchCount}",
                        true,
                        () => {main.SetRoleCountToggle(CustomRoles.Witch);},
                        new List<OptionPages>(){},
                        OptionPages.roles,
                        i => main.SetRoleCount(CustomRoles.Witch, i)
                    )},
                    {OptionPages.AdvancedRoleOptions, new PageObject(
                        lang.AdvancedRoleOptions,
                        false,
                        () => {SetPage(OptionPages.AdvancedRoleOptions);},
                        new List<OptionPages>(){
                            OptionPages.VampireKillDelay,
                            OptionPages.MadmateCanFixLightsOut,
                            OptionPages.MadGuardianCanSeeBarrier,
                            OptionPages.MayorAdditionalVote,
                            OptionPages.SabotageMasterSkillLimit,
                            OptionPages.SabotageMasterFixesDoors,
                            OptionPages.SabotageMasterFixesReactors,
                            OptionPages.SabotageMasterFixesOxygens,
                            OptionPages.SabotageMasterFixesCommunications,
                            OptionPages.SabotageMasterFixesElectrical,
                            OptionPages.SheriffCanKillJester,
                            OptionPages.SheriffCanKillTerrorist,
                            OptionPages.SheriffCanKillOpportunist,
                        },
                        OptionPages.roles
                    )},
                        {OptionPages.VampireKillDelay, new PageObject(
                            () => $"<color={main.getRoleColorCode(CustomRoles.Vampire)}>{main.getLang(lang.VampireKillDelay)}</color>(s): {main.VampireKillDelay}{main.TextCursor}",
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
                        {OptionPages.SabotageMasterSkillLimit, new PageObject(
                            () => $"<color={main.getRoleColorCode(CustomRoles.SabotageMaster)}>{main.getLang(lang.SabotageMasterSkillLimit)}</color>: {main.SabotageMasterSkillLimit}{main.TextCursor}",
                            true,
                            () => {main.SabotageMasterSkillLimit = 0;},
                            new List<OptionPages>(){},
                            OptionPages.AdvancedRoleOptions,
                            (i) => {
                                var SkillLimit = main.SabotageMasterSkillLimit * 10;
                                SkillLimit += i;
                                var FixedSkillLimit = Math.Clamp(SkillLimit,0,999);
                                main.SabotageMasterSkillLimit = FixedSkillLimit;
                            }
                        )},
                        {OptionPages.SabotageMasterFixesDoors, new PageObject(
                            () => $"<color={main.getRoleColorCode(CustomRoles.SabotageMaster)}>{main.getLang(lang.SabotageMasterFixesDoors)}</color>: {main.getOnOff(main.SabotageMasterFixesDoors)}",
                            true,
                            () => {main.SabotageMasterFixesDoors = !main.SabotageMasterFixesDoors;},
                            new List<OptionPages>(){},
                            OptionPages.AdvancedRoleOptions
                        )},
                        {OptionPages.SabotageMasterFixesReactors, new PageObject(
                            () => $"<color={main.getRoleColorCode(CustomRoles.SabotageMaster)}>{main.getLang(lang.SabotageMasterFixesReactors)}</color>: {main.getOnOff(main.SabotageMasterFixesReactors)}",
                            true,
                            () => {main.SabotageMasterFixesReactors = !main.SabotageMasterFixesReactors;},
                            new List<OptionPages>(){},
                            OptionPages.AdvancedRoleOptions
                        )},
                        {OptionPages.SabotageMasterFixesOxygens, new PageObject(
                            () => $"<color={main.getRoleColorCode(CustomRoles.SabotageMaster)}>{main.getLang(lang.SabotageMasterFixesOxygens)}</color>: {main.getOnOff(main.SabotageMasterFixesOxygens)}",
                            true,
                            () => {main.SabotageMasterFixesOxygens = !main.SabotageMasterFixesOxygens;},
                            new List<OptionPages>(){},
                            OptionPages.AdvancedRoleOptions
                        )},
                        {OptionPages.SabotageMasterFixesCommunications, new PageObject(
                            () => $"<color={main.getRoleColorCode(CustomRoles.SabotageMaster)}>{main.getLang(lang.SabotageMasterFixesCommunications)}</color>: {main.getOnOff(main.SabotageMasterFixesCommunications)}",
                            true,
                            () => {main.SabotageMasterFixesCommunications = !main.SabotageMasterFixesCommunications;},
                            new List<OptionPages>(){},
                            OptionPages.AdvancedRoleOptions
                        )},
                        {OptionPages.SabotageMasterFixesElectrical, new PageObject(
                            () => $"<color={main.getRoleColorCode(CustomRoles.SabotageMaster)}>{main.getLang(lang.SabotageMasterFixesElectrical)}</color>: {main.getOnOff(main.SabotageMasterFixesElectrical)}",
                            true,
                            () => {main.SabotageMasterFixesElectrical = !main.SabotageMasterFixesElectrical;},
                            new List<OptionPages>(){},
                            OptionPages.AdvancedRoleOptions
                        )},
                        {OptionPages.SheriffCanKillJester, new PageObject(
                            () => $"<color={main.getRoleColorCode(CustomRoles.Sheriff)}>{main.getLang(lang.SheriffCanKillJester)}</color>: {main.getOnOff(main.SheriffCanKillJester)}",
                            true,
                            () => {main.SheriffCanKillJester = !main.SheriffCanKillJester;},
                            new List<OptionPages>(){},
                            OptionPages.AdvancedRoleOptions
                        )},
                                                {OptionPages.SheriffCanKillTerrorist, new PageObject(
                            () => $"<color={main.getRoleColorCode(CustomRoles.Sheriff)}>{main.getLang(lang.SheriffCanKillTerrorist)}</color>: {main.getOnOff(main.SheriffCanKillTerrorist)}",
                            true,
                            () => {main.SheriffCanKillTerrorist = !main.SheriffCanKillTerrorist;},
                            new List<OptionPages>(){},
                            OptionPages.AdvancedRoleOptions
                        )},
                                                                        {OptionPages.SheriffCanKillOpportunist, new PageObject(
                            () => $"<color={main.getRoleColorCode(CustomRoles.Sheriff)}>{main.getLang(lang.SheriffCanKillOpportunist)}</color>: {main.getOnOff(main.SheriffCanKillOpportunist)}",
                            true,
                            () => {main.SheriffCanKillOpportunist = !main.SheriffCanKillOpportunist;},
                            new List<OptionPages>(){},
                            OptionPages.AdvancedRoleOptions
                        )},
                        {OptionPages.MadmateCanFixLightsOut, new PageObject(
                            () => $"<color={main.getRoleColorCode(CustomRoles.Madmate)}>{main.getLang(lang.MadmateCanFixLightsOut)}</color>: {main.getOnOff(main.MadmateCanFixLightsOut)}",
                            true,
                            () => {main.MadmateCanFixLightsOut = !main.MadmateCanFixLightsOut;},
                            new List<OptionPages>(){},
                            OptionPages.AdvancedRoleOptions
                        )},
                        {OptionPages.MadGuardianCanSeeBarrier, new PageObject(
                            () => $"<color={main.getRoleColorCode(CustomRoles.MadGuardian)}>{main.getLang(lang.MadGuardianCanSeeBarrier)}</color>: {main.getOnOff(main.MadGuardianCanSeeBarrier)}",
                            true,
                            () => {main.MadGuardianCanSeeBarrier = !main.MadGuardianCanSeeBarrier;},
                            new List<OptionPages>(){},
                            OptionPages.AdvancedRoleOptions
                        )},
                        {OptionPages.MayorAdditionalVote, new PageObject(
                            () => $"<color={main.getRoleColorCode(CustomRoles.Mayor)}>{main.getLang(lang.MayorAdditionalVote)}</color>: {main.MayorAdditionalVote}{main.TextCursor}",
                            true,
                            () => {main.MayorAdditionalVote = 0;},
                            new List<OptionPages>(){},
                            OptionPages.AdvancedRoleOptions,
                            (i) => {
                                var Count = main.MayorAdditionalVote * 10;
                                Count += i;
                                var FixedCount = Math.Clamp(Count,0,99);
                                main.MayorAdditionalVote = FixedCount;
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
                )},
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
            if(AmongUsClient.Instance.AmHost && !PageObjects[OptionPages.basepage].PagesInThis.Contains(OptionPages.Suffix)) {
                //ホストの設定にSuffixを入れる
                PageObjects[OptionPages.basepage].PagesInThis.Add(OptionPages.Suffix);
            }
            if(!AmongUsClient.Instance.AmHost && PageObjects[OptionPages.basepage].PagesInThis.Contains(OptionPages.Suffix)) {
                //ホストの設定にSuffixを入れる
                PageObjects[OptionPages.basepage].PagesInThis.Remove(OptionPages.Suffix);
            }
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
    class PageObject {
        public string name => getName();
        private Func<string> getName;
        public bool isHostOnly;
        public Action onEnter;
        public List<OptionPages> PagesInThis;
        public OptionPages pageToReturn;
        public Action<int> onInput;
        public PageObject(string name, bool isHostOnly, Action onEnter, List<OptionPages> PageInThis, OptionPages PageToReturn, Action<int> onInput = null)
        {
            this.getName = () => name;
            this.isHostOnly = isHostOnly;
            this.onEnter = onEnter;
            this.PagesInThis = PageInThis;
            this.pageToReturn = PageToReturn;
            this.onInput = onInput == null ? (i) => {return;} : onInput;
        }
        public PageObject(Func<string> getName, bool isHostOnly, Action onEnter, List<OptionPages> PageInThis, OptionPages PageToReturn, Action<int> onInput = null)
        {
            this.getName = getName;
            this.isHostOnly = isHostOnly;
            this.onEnter = onEnter;
            this.PagesInThis = PageInThis;
            this.pageToReturn = PageToReturn;
            this.onInput = onInput == null ? (i) => {return;} : onInput;
        }
        public PageObject(lang lang, bool isHostOnly, Action onEnter, List<OptionPages> PageInThis, OptionPages PageToReturn, Action<int> onInput = null)
        {
            this.getName = () => main.getLang(lang);
            this.isHostOnly = isHostOnly;
            this.onEnter = onEnter;
            this.PagesInThis = PageInThis;
            this.pageToReturn = PageToReturn;
            this.onInput = onInput == null ? (i) => {return;} : onInput;
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
