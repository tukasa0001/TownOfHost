using HarmonyLib;
using System.Threading.Tasks;
using UnityEngine;
using System.Linq;
using static TownOfHost.Translator;
using System;

namespace TownOfHost
{
    [HarmonyPatch(typeof(IntroCutscene), nameof(IntroCutscene.ShowRole))]
    class SetUpRoleTextPatch
    {
        public static void Postfix(IntroCutscene __instance)
        {
            new LateTask(() =>
            {
                CustomRoles role = PlayerControl.LocalPlayer.getCustomRole();
                if (role.isVanilla()) return;
                __instance.RoleText.text = Utils.getRoleName(role);
                __instance.RoleText.color = Utils.getRoleColor(role);
                __instance.RoleBlurbText.color = Utils.getRoleColor(role);
                __instance.YouAreText.color = Utils.getRoleColor(role);

                if (PlayerControl.LocalPlayer.Is(CustomRoles.EvilWatcher) || PlayerControl.LocalPlayer.Is(CustomRoles.NiceWatcher))
                    __instance.RoleBlurbText.text = getString("WatcherInfo");
                else
                    __instance.RoleBlurbText.text = getString(role.ToString() + "Info");

                __instance.RoleText.text += Utils.GetShowLastSubRolesText(PlayerControl.LocalPlayer.PlayerId);

            }, 0.01f, "Override Role Text");

        }
    }
    [HarmonyPatch(typeof(IntroCutscene), nameof(IntroCutscene.CoBegin))]
    class CoBeginPatch
    {
        public static void Prefix(IntroCutscene __instance)
        {
            Logger.info("------------名前表示------------", "Info");
            foreach (var pc in PlayerControl.AllPlayerControls)
            {
                Logger.info(String.Format("{0,-2}:{1}:{2}", pc.PlayerId, pc.name.padRight(20), pc.nameText.text), "Info");
                main.RealNames[pc.PlayerId] = pc.name;
                pc.nameText.text = pc.name;
            }
            Logger.info("----------役職割り当て----------", "Info");
            foreach (var pc in PlayerControl.AllPlayerControls)
            {
                Logger.info(String.Format("{0,-2}:{1}:{2}", pc.PlayerId, pc.Data.PlayerName.padRight(20), pc.getAllRoleName()), "Info");
            }
            Logger.info("--------------環境--------------", "Info");
            foreach (var pc in PlayerControl.AllPlayerControls)
            {
                var text = pc.AmOwner ? "[*]" : "   ";
                text += String.Format("{0,-2}:{1}:{2,-11}", pc.PlayerId, pc.Data.PlayerName.padRight(20), pc.getClient().PlatformData.Platform.ToString().Replace("Standalone", ""));
                if (main.playerVersion.TryGetValue(pc.PlayerId, out PlayerVersion pv))
                    text += $":Mod({pv.version}:{pv.tag})";
                else text += ":Vanilla";
                Logger.info(text, "Info");
            }
            Logger.info("------------基本設定------------", "Info");
            var tmp = PlayerControl.GameOptions.ToHudString(GameData.Instance ? GameData.Instance.PlayerCount : 10).Split("\r\n").Skip(1);
            foreach (var t in tmp) Logger.info(t, "Info");
            Logger.info("------------詳細設定------------", "Info");
            foreach (var o in CustomOption.Options)
                if (!o.IsHidden(Options.CurrentGameMode) && (o.Parent == null ? !o.GetString().Equals("0%") : o.Parent.Enabled))
                    Logger.info(String.Format("{0}:{1}", (o.Parent == null ? o.Name : $"┗ {o.Name}").padRight(40), o.GetString()), "Info");
            Logger.info("-------------その他-------------", "Info");
            Logger.info($"プレイヤー数: {PlayerControl.AllPlayerControls.Count}人", "Info");

            GameStates.InGame = true;
        }
    }
    [HarmonyPatch(typeof(IntroCutscene), nameof(IntroCutscene.BeginCrewmate))]
    class BeginCrewmatePatch
    {
        public static void Prefix(IntroCutscene __instance, ref Il2CppSystem.Collections.Generic.List<PlayerControl> teamToDisplay)
        {
            if (PlayerControl.LocalPlayer.Is(RoleType.Neutral))
            {
                //ぼっち役職
                var soloTeam = new Il2CppSystem.Collections.Generic.List<PlayerControl>();
                soloTeam.Add(PlayerControl.LocalPlayer);
                teamToDisplay = soloTeam;
            }
        }
        public static void Postfix(IntroCutscene __instance, ref Il2CppSystem.Collections.Generic.List<PlayerControl> teamToDisplay)
        {
            //チーム表示変更
            var rand = new System.Random();
            CustomRoles role = PlayerControl.LocalPlayer.getCustomRole();
            RoleType roleType = role.getRoleType();

            switch (roleType)
            {
                case RoleType.Neutral:
                    __instance.TeamTitle.text = Utils.getRoleName(role);
                    __instance.TeamTitle.color = Utils.getRoleColor(role);
                    __instance.BackgroundBar.material.color = Utils.getRoleColor(role);
                    break;
                case RoleType.Madmate:
                    __instance.TeamTitle.text = getString("Madmate");
                    __instance.TeamTitle.color = Utils.getRoleColor(CustomRoles.Madmate);
                    __instance.ImpostorText.text = getString("TeamImpostor");
                    StartFadeIntro(__instance, Palette.CrewmateBlue, Palette.ImpostorRed);
                    PlayerControl.LocalPlayer.Data.Role.IntroSound = GetIntroSound(RoleTypes.Impostor);
                    break;
            }
            switch (role)
            {
                case CustomRoles.Terrorist:
                    var sound = ShipStatus.Instance.CommonTasks.Where(task => task.TaskType == TaskTypes.FixWiring).FirstOrDefault()
                    .MinigamePrefab.OpenSound;
                    PlayerControl.LocalPlayer.Data.Role.IntroSound = sound;
                    break;

                case CustomRoles.Executioner:
                    PlayerControl.LocalPlayer.Data.Role.IntroSound = GetIntroSound(RoleTypes.Shapeshifter);
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
                case CustomRoles.Arsonist:
                    PlayerControl.LocalPlayer.Data.Role.IntroSound = GetIntroSound(RoleTypes.Crewmate);
                    break;

                case CustomRoles.SchrodingerCat:
                    PlayerControl.LocalPlayer.Data.Role.IntroSound = GetIntroSound(RoleTypes.Impostor);
                    break;

                case CustomRoles.Mayor:
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
        private static AudioClip GetIntroSound(RoleTypes roleType)
        {
            return RoleManager.Instance.AllRoles.Where((role) => role.Role == roleType).FirstOrDefault().IntroSound;
        }
        private static async void StartFadeIntro(IntroCutscene __instance, Color start, Color end)
        {
            await Task.Delay(1000);
            int milliseconds = 0;
            while (true)
            {
                await Task.Delay(20);
                milliseconds += 20;
                float time = (float)milliseconds / (float)500;
                Color LerpingColor = Color.Lerp(start, end, time);
                if (__instance == null || milliseconds > 500)
                {
                    Logger.info("ループを終了します", "StartFadeIntro");
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
            if (PlayerControl.LocalPlayer.Is(CustomRoles.Sheriff))
            {
                //シェリフの場合はキャンセルしてBeginCrewmateに繋ぐ
                yourTeam = new Il2CppSystem.Collections.Generic.List<PlayerControl>();
                yourTeam.Add(PlayerControl.LocalPlayer);
                foreach (var pc in PlayerControl.AllPlayerControls)
                {
                    if (!pc.AmOwner) yourTeam.Add(pc);
                }
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
    [HarmonyPatch(typeof(IntroCutscene), nameof(IntroCutscene.OnDestroy))]
    class IntroCutsceneDestoryPatch
    {
        public static void Postfix(IntroCutscene __instance)
        {
            main.introDestroyed = true;
        }
    }
}
