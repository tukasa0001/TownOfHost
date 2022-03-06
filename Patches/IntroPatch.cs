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
            {CustomRoles.Bait, lang.BaitInfo},
            {CustomRoles.Terrorist, lang.TerroristInfo},
            {CustomRoles.Mafia, lang.MafiaInfo},
            {CustomRoles.Vampire, lang.VampireInfo},
            {CustomRoles.SabotageMaster, lang.SabotageMasterInfo},
            {CustomRoles.MadGuardian, lang.MadGuardianInfo},
            {CustomRoles.Mayor, lang.MayorInfo},
            {CustomRoles.Opportunist, lang.OpportunistInfo},
            {CustomRoles.Snitch, lang.SnitchInfo},
            {CustomRoles.Sheriff, lang.SheriffInfo},
            {CustomRoles.BountyHunter, lang.BountyHunterInfo},
            {CustomRoles.Witch, lang.WitchInfo},
            {CustomRoles.Fox, lang.FoxInfo},
            {CustomRoles.Troll, lang.TrollInfo}
        };
        public static void Postfix(IntroCutscene __instance) {
            CustomRoles role = PlayerControl.LocalPlayer.getCustomRole();
            __instance.RoleText.text = main.getRoleName(role);
            if(RoleAndInfo.TryGetValue(role, out var info)) __instance.RoleBlurbText.text = main.getLang(info);
            __instance.RoleText.color = main.getRoleColor(role);
            __instance.RoleBlurbText.color = main.getRoleColor(role);

            if(PlayerControl.LocalPlayer.isSheriff()) __instance.YouAreText.color = Palette.CrewmateBlue; //シェリフ専用
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
                    PlayerControl.LocalPlayer.Data.Role.IntroSound = GetIntroSound(RoleTypes.Impostor);
                    break;
            }
            switch(role) {
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
                    __instance.BackgroundBar.material.color = Palette.CrewmateBlue;
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
        public static bool Prefix(IntroCutscene __instance, ref Il2CppSystem.Collections.Generic.List<PlayerControl> yourTeam)
        {
            if(PlayerControl.LocalPlayer.isSheriff()) {
                //シェリフの場合はキャンセルしてBeginCrewmateに繋ぐ
                yourTeam = new Il2CppSystem.Collections.Generic.List<PlayerControl>();
                yourTeam.Add(PlayerControl.LocalPlayer);
                foreach(var pc in PlayerControl.AllPlayerControls) if(!pc.AmOwner)yourTeam.Add(pc);
                __instance.BeginCrewmate(yourTeam);
                __instance.overlayHandle.color = Palette.CrewmateBlue;
                return false;
            }
            BeginCrewmatePatch.Prefix(__instance, ref yourTeam);
            return true;
        }
        public static void Postfix(IntroCutscene __instance, ref Il2CppSystem.Collections.Generic.List<PlayerControl> yourTeam)
        {
            BeginCrewmatePatch.Postfix(__instance, ref yourTeam);
        }
    }
}
