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
    //役職表示変更
    [HarmonyPatch(typeof(IntroCutscene), nameof(IntroCutscene.BeginCrewmate))]
    class BeginCrewmatePatch
    {
        public static void Prefix(IntroCutscene __instance, ref Il2CppSystem.Collections.Generic.List<PlayerControl> yourTeam)
        {
            if (main.isJester(PlayerControl.LocalPlayer))
            {
                var soloTeam = new Il2CppSystem.Collections.Generic.List<PlayerControl>();
                soloTeam.Add(PlayerControl.LocalPlayer);
                yourTeam = soloTeam;
            }
        }
        public static void Postfix(IntroCutscene __instance, ref Il2CppSystem.Collections.Generic.List<PlayerControl> yourTeam)
        {
            if (Input.GetKey(KeyCode.RightShift))
            {
                __instance.TeamTitle.text = "Town Of Host";
                __instance.ImpostorText.gameObject.SetActive(true);
                __instance.ImpostorText.text = "https://github.com/tukasa0001/TownOfHost" +
                "\r\nOut Now on Github";
                __instance.TeamTitle.color = Color.cyan;
                __instance.BackgroundBar.material.color = Palette.CrewmateBlue;
                __instance.RoleText.text = "役職名";
                __instance.RoleBlurbText.text = "役職の説明";
            }
            if (main.isJester(PlayerControl.LocalPlayer))
            {
                __instance.TeamTitle.text = main.getLang(lang.Jester);
                __instance.ImpostorText.gameObject.SetActive(true);
                __instance.ImpostorText.text = main.getLang(lang.JesterInfo);
                __instance.TeamTitle.color = main.JesterColor();
                __instance.BackgroundBar.material.color = main.JesterColor();
                __instance.RoleText.text = "ジェスター";
                __instance.RoleBlurbText.text = "投票で追放されて勝利しろ";
            }
            if (main.isMadmate(PlayerControl.LocalPlayer))
            {
                __instance.TeamTitle.text = main.getLang(lang.Madmate);
                __instance.ImpostorText.gameObject.SetActive(true);
                __instance.ImpostorText.text = main.getLang(lang.MadmateInfo);
                __instance.TeamTitle.color = Palette.ImpostorRed;
                __instance.BackgroundBar.material.color = Palette.ImpostorRed;
                __instance.RoleText.text = "狂人";
                __instance.RoleBlurbText.text = "インポスターを助けて勝利しろ";
            }
            if (main.isBait(PlayerControl.LocalPlayer))
            {
                __instance.TeamTitle.text = main.getLang(lang.Bait);
                __instance.ImpostorText.gameObject.SetActive(true);
                __instance.ImpostorText.text = main.getLang(lang.BaitInfo);
                __instance.TeamTitle.color = Color.cyan;
                __instance.BackgroundBar.material.color = Color.yellow;
                __instance.RoleText.text = "ベイト";
                __instance.RoleBlurbText.text = "おとりになってインポスターを探し出せ";
            }
            if (main.isTerrorist(PlayerControl.LocalPlayer))
            {
                __instance.TeamTitle.text = main.getLang(lang.Terrorist);
                __instance.ImpostorText.gameObject.SetActive(true);
                __instance.ImpostorText.text = main.getLang(lang.TerroristInfo);
                __instance.TeamTitle.color = Color.green;
                __instance.BackgroundBar.material.color = Color.green;
                __instance.RoleText.text = "テロリスト";
                __instance.RoleBlurbText.text = "タスクを完了させ、そして死んで勝利しろ";
            }
            if (main.isSidekick(PlayerControl.LocalPlayer))
            {
                __instance.TeamTitle.text = main.getLang(lang.Sidekick);
                __instance.TeamTitle.fontSize -= 0.5f;
                __instance.ImpostorText.gameObject.SetActive(true);
                __instance.ImpostorText.text = main.getLang(lang.SidekickInfo);
                __instance.TeamTitle.color = Palette.ImpostorRed;
                __instance.BackgroundBar.material.color = Color.red;
                __instance.RoleText.text = "相棒";
                __instance.RoleBlurbText.text = "インポスターを助けて勝利しろ";
            }
            if (main.isVampire(PlayerControl.LocalPlayer))
            {
                __instance.TeamTitle.text = main.getLang(lang.Vampire);
                __instance.TeamTitle.fontSize -= 0.5f;
                __instance.ImpostorText.gameObject.SetActive(true);
                __instance.ImpostorText.text = main.getLang(lang.VampireInfo);
                __instance.TeamTitle.color = Palette.ImpostorRed;
                __instance.BackgroundBar.material.color = main.VampireColor;
                __instance.RoleText.text = "吸血鬼";
                __instance.RoleBlurbText.text = "クルーを全員噛み殺せ";
            }
            if(main.IsHideAndSeek) {
                if (main.HideAndSeekRoleList[PlayerControl.LocalPlayer.PlayerId] == HideAndSeekRoles.Fox) {
                    __instance.TeamTitle.text = "Fox";
                    __instance.TeamTitle.fontSize -= 0.5f;
                    __instance.ImpostorText.gameObject.SetActive(true);
                    __instance.ImpostorText.text = "殺されずに逃げきれ";
                    __instance.TeamTitle.color = Color.magenta;
                    __instance.BackgroundBar.material.color = Color.magenta;
                    __instance.RoleText.text = "狐";
                    __instance.RoleBlurbText.text = "殺されずに逃げきれ";
                }
                if (main.HideAndSeekRoleList[PlayerControl.LocalPlayer.PlayerId] == HideAndSeekRoles.Troll) {
                    __instance.TeamTitle.text = "Troll";
                    __instance.TeamTitle.fontSize -= 0.5f;
                    __instance.ImpostorText.gameObject.SetActive(true);
                    __instance.ImpostorText.text = "インポスターにキルされろ";
                    __instance.TeamTitle.color = Color.green;
                    __instance.BackgroundBar.material.color = Color.green;
                    __instance.RoleText.text = "トロール";
                    __instance.RoleBlurbText.text = "インポスターにキルされろ";
                }
            }
        }
    }
    [HarmonyPatch(typeof(IntroCutscene), nameof(IntroCutscene.BeginImpostor))]
    class BeginImpostorPatch
    {
        public static void Prefix(IntroCutscene __instance, ref Il2CppSystem.Collections.Generic.List<PlayerControl> yourTeam)
        {
            BeginCrewmatePatch.Prefix(__instance, ref yourTeam);
        }
        public static void Postfix(IntroCutscene __instance, ref Il2CppSystem.Collections.Generic.List<PlayerControl> yourTeam)
        {
            BeginCrewmatePatch.Postfix(__instance, ref yourTeam);
        }
    }
}
