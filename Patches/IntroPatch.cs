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
    //役職表示変更
    [HarmonyPatch(typeof(IntroCutscene), nameof(IntroCutscene.BeginCrewmate))]
    class BeginCrewmatePatch {
        public static void Prefix(IntroCutscene __instance, ref  Il2CppSystem.Collections.Generic.List<PlayerControl> yourTeam) {
            if(main.isJester(PlayerControl.LocalPlayer)) {
                var soloTeam = new Il2CppSystem.Collections.Generic.List<PlayerControl>();
                soloTeam.Add(PlayerControl.LocalPlayer);
                yourTeam = soloTeam;
            }
        }
        public static void Postfix(IntroCutscene __instance, ref  Il2CppSystem.Collections.Generic.List<PlayerControl> yourTeam) {
            if(Input.GetKey(KeyCode.LeftShift)) {
                __instance.TeamTitle.text = "チーム";
                __instance.ImpostorText.gameObject.SetActive(true);
                __instance.ImpostorText.text = "チームの説明";
                __instance.TeamTitle.color = Color.magenta;
                __instance.BackgroundBar.material.color = Color.green;
                __instance.Foreground.material.color = Palette.ImpostorRed;
                __instance.RoleText.text = "役職名";
                __instance.RoleBlurbText.text = "役職の説明";
            }
            if(main.isJester(PlayerControl.LocalPlayer)) {
                __instance.TeamTitle.text = main.getLang(lang.Jester);
                __instance.ImpostorText.gameObject.SetActive(true);
                __instance.ImpostorText.text = main.getLang(lang.JesterInfo);
                __instance.TeamTitle.color = main.JesterColor();
                __instance.BackgroundBar.material.color = main.JesterColor();
                __instance.RoleText.text = "てるてる";
                __instance.RoleBlurbText.text = "投票で追放されて勝利しよう";
            }
            if(main.isMadmate(PlayerControl.LocalPlayer)) {
                __instance.TeamTitle.text = main.getLang(lang.Madmate);
                __instance.ImpostorText.gameObject.SetActive(true);
                __instance.ImpostorText.text = main.getLang(lang.MadmateInfo);
                __instance.TeamTitle.color = Palette.ImpostorRed;
                __instance.BackgroundBar.material.color = Palette.ImpostorRed;
                __instance.RoleText.text = "狂人";
                __instance.RoleBlurbText.text = "インポスターを助けて勝利しよう";
            }
            if(main.isBait(PlayerControl.LocalPlayer)) {
                __instance.TeamTitle.text = main.getLang(lang.Bait);
                __instance.ImpostorText.gameObject.SetActive(true);
                __instance.ImpostorText.text = main.getLang(lang.BaitInfo);
                __instance.TeamTitle.color = Color.cyan;
                __instance.BackgroundBar.material.color = Color.yellow;
                __instance.RoleText.text = "ベイト";
                __instance.RoleBlurbText.text = "おとりになってインポスターを暴け";
            }
            if(main.isTerrorist(PlayerControl.LocalPlayer)) {
                __instance.TeamTitle.text = main.getLang(lang.Terrorist);
                __instance.ImpostorText.gameObject.SetActive(true);
                __instance.ImpostorText.text = main.getLang(lang.TerroristInfo);
                __instance.TeamTitle.color = Color.green;
                __instance.BackgroundBar.material.color = Color.green;
                __instance.RoleText.text = "テロリスト";
                __instance.RoleBlurbText.text = "タスクを終えて、そして死んで勝利しよう";
            }
        }
    }
}