using BepInEx;
using BepInEx.Configuration;
using BepInEx.IL2CPP;
using System;
using HarmonyLib;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.IO;
using UnityEngine;
using UnhollowerBaseLib;
using TownOfHost;
using System.Linq;

namespace TownOfHost
{
    [HarmonyPatch(typeof(IntroCutscene), nameof(IntroCutscene.SetUpRoleText))]
    class SetUpRoleTextPatch {
        static Dictionary<CustomRoles, lang> RoleAndInfo = new Dictionary<CustomRoles, lang>() {
            {CustomRoles.Jester, lang.JesterInfo},
            {CustomRoles.Madmate, lang.MadmateInfo},
            {CustomRoles.SKMadmate, lang.SKMadmateInfo},
            {CustomRoles.Bait, lang.BaitInfo},
            {CustomRoles.Terrorist, lang.TerroristInfo},
            {CustomRoles.Mafia, lang.MafiaInfo},
            {CustomRoles.Vampire, lang.VampireInfo},
            {CustomRoles.SabotageMaster, lang.SabotageMasterInfo},
            {CustomRoles.MadGuardian, lang.MadGuardianInfo},
            {CustomRoles.MadSnitch, lang.MadSnitchInfo},
            {CustomRoles.BlackCat, lang.BlackCatInfo},
            {CustomRoles.Mayor, lang.MayorInfo},
            {CustomRoles.Opportunist, lang.OpportunistInfo},
            {CustomRoles.Snitch, lang.SnitchInfo},
            {CustomRoles.Sheriff, lang.SheriffInfo},
            {CustomRoles.BountyHunter, lang.BountyHunterInfo},
            {CustomRoles.Witch, lang.WitchInfo},
            {CustomRoles.ShapeMaster, lang.ShapeMasterInfo},
            {CustomRoles.Warlock, lang.WarlockInfo},
            {CustomRoles.SerialKiller, lang.SerialKillerInfo},
            {CustomRoles.Fox, lang.FoxInfo},
            {CustomRoles.Troll, lang.TrollInfo}
        };
        public static void Postfix(IntroCutscene __instance) {
            CustomRoles role = PlayerControl.LocalPlayer.getCustomRole();
            __instance.RoleText.text = main.getRoleName(role);
            if(RoleAndInfo.TryGetValue(role, out var info)) __instance.RoleBlurbText.text = main.getLang(info);
            __instance.RoleText.color = main.getRoleColor(role);
            __instance.RoleBlurbText.color = main.getRoleColor(role);
        }
    }
    [HarmonyPatch(typeof(IntroCutscene), nameof(IntroCutscene.BeginCrewmate))]
    class BeginCrewmatePatch
    {
        public static void Prefix(IntroCutscene __instance, ref Il2CppSystem.Collections.Generic.List<PlayerControl> yourTeam)
        {
            var role = PlayerControl.LocalPlayer.getCustomRole();
            if (role.GetIntroType() == IntroTypes.Neutral) {
                //ぼっち役職
                var soloTeam = new Il2CppSystem.Collections.Generic.List<PlayerControl>();
                soloTeam.Add(PlayerControl.LocalPlayer);
                yourTeam = soloTeam;
            }
        }
        public static void Postfix(IntroCutscene __instance, ref Il2CppSystem.Collections.Generic.List<PlayerControl> yourTeam)
        {
            //チーム表示変更
            var rand = new System.Random();
            CustomRoles role = PlayerControl.LocalPlayer.getCustomRole();
            IntroTypes introType = role.GetIntroType();

            switch(introType) {
                case IntroTypes.Neutral:
                    __instance.TeamTitle.text = main.getRoleName(role);
                    __instance.TeamTitle.color = main.getRoleColor(role);
                    __instance.BackgroundBar.material.color = main.getRoleColor(role);
                    break;
                case IntroTypes.Madmate:
                    StartFadeIntro(__instance, Palette.CrewmateBlue, Palette.ImpostorRed);
                    break;
            }
            switch(role) {
                case CustomRoles.Madmate:
                case CustomRoles.MadGuardian:
                case CustomRoles.MadSnitch:
                case CustomRoles.BlackCat:
                    PlayerControl.LocalPlayer.Data.Role.IntroSound = GetIntroSound(RoleTypes.Impostor);
                    break;

                case CustomRoles.Terrorist:
                    var sound = ShipStatus.Instance.CommonTasks.Where(task => task.TaskType == TaskTypes.FixWiring).FirstOrDefault()
                    .MinigamePrefab.OpenSound;
                    PlayerControl.LocalPlayer.Data.Role.IntroSound = sound;
                    break;

                case CustomRoles.Vampire:
                    PlayerControl.LocalPlayer.Data.Role.IntroSound = GetIntroSound(RoleTypes.Shapeshifter);
                    break;

                case CustomRoles.SabotageMaster:
                    PlayerControl.LocalPlayer.Data.Role.IntroSound = ShipStatus.Instance.SabotageSound;
                    break;

                case CustomRoles.Sheriff:
                    PlayerControl.LocalPlayer.Data.Role.IntroSound = GetIntroSound(RoleTypes.Crewmate);
                    break;

            }

            if (Input.GetKey(KeyCode.RightShift))
            {
                __instance.TeamTitle.text = "Town Of Host";
                __instance.ImpostorText.gameObject.SetActive(true);
                __instance.ImpostorText.text = "https://github.com/tukasa0001/TownOfHost" +
                "\r\nOut Now on Github";
                __instance.TeamTitle.color = Color.cyan;
                StartFadeIntro(__instance, Color.cyan, Color.yellow);
            }
            if (Input.GetKey(KeyCode.RightControl))
            {
                __instance.TeamTitle.text = "Discord Server";
                __instance.ImpostorText.gameObject.SetActive(true);
                __instance.ImpostorText.text = "https://discord.gg/v8SFfdebpz";
                __instance.TeamTitle.color = Color.magenta;
                StartFadeIntro(__instance, Color.magenta, Color.magenta);
            }
        }
        private static AudioClip GetIntroSound(RoleTypes roleType) {
            return RoleManager.Instance.AllRoles.Where((role) => role.Role == roleType).FirstOrDefault().IntroSound;
        }
        private static async void StartFadeIntro(IntroCutscene __instance, Color start, Color end) {
            await Task.Delay(1000);
            int miliseconds = 0;
            while(true) {
                await Task.Delay(20);
                miliseconds += 20;
                float time = (float)miliseconds / (float)500;
                Color LerpingColor = Color.Lerp(start, end, time);
                if(__instance == null || miliseconds > 500) {
                    Logger.info("ループを終了します");
                    break;
                }
                __instance.BackgroundBar.material.color = LerpingColor;
            }
        }
    }
    [HarmonyPatch(typeof(IntroCutscene), nameof(IntroCutscene.BeginImpostor))]
    class BeginImpostorPatch
    {
        public static void Prefix(IntroCutscene __instance, ref Il2CppSystem.Collections.Generic.List<PlayerControl> yourTeam)
        {
            //TODO:シェリフ時にプレイヤーの間隔とかをクルーの場合と同じにする
            BeginCrewmatePatch.Prefix(__instance, ref yourTeam);
        }
        public static void Postfix(IntroCutscene __instance, ref Il2CppSystem.Collections.Generic.List<PlayerControl> yourTeam)
        {
            BeginCrewmatePatch.Postfix(__instance, ref yourTeam);
        }
    }
}
