using System;
using System.Linq;
using System.Threading.Tasks;
using HarmonyLib;
using UnityEngine;
using AmongUs.GameOptions;
using TownOfHost.Extensions;
using TownOfHost.Managers;
using TownOfHost.Options;
using TownOfHost.Roles;
using static TownOfHost.Managers.Translator;
using VentLib.Logging;

namespace TownOfHost
{
    [HarmonyPatch(typeof(IntroCutscene), nameof(IntroCutscene.ShowRole))]
    class SetUpRoleTextPatch
    {
        public static void Postfix(IntroCutscene __instance)
        {
            DTask.Schedule(() =>
            {
                CustomRole role = PlayerControl.LocalPlayer.GetCustomRole();
                if (!role.IsVanilla())
                {
                    __instance.YouAreText.color = Utils.GetRoleColor(role);
                    __instance.RoleText.text = Utils.GetRoleName(role);
                    __instance.RoleText.color = Utils.GetRoleColor(role);
                    __instance.RoleBlurbText.color = Utils.GetRoleColor(role);

                    __instance.RoleBlurbText.text = PlayerControl.LocalPlayer.GetRoleInfo();
                }

                __instance.RoleText.text += Utils.GetSubRolesText(PlayerControl.LocalPlayer.PlayerId);

            }, 0.01f);

        }
    }
    [HarmonyPatch(typeof(IntroCutscene), nameof(IntroCutscene.CoBegin))]
    class CoBeginPatch
    {
        public static void Prefix()
        {
            Game.State = GameState.InIntro;
            VentLogger.Old("------------名前表示------------", "Info");
            foreach (var pc in PlayerControl.AllPlayerControls)
            {
                VentLogger.Old($"{(pc.AmOwner ? "[*]" : ""),-3}{pc.PlayerId,-2}:{pc.name.PadRightV2(20)}:{pc.cosmetics.nameText.text}({Palette.ColorNames[pc.Data.DefaultOutfit.ColorId].ToString().Replace("Color", "")})", "Info");
                pc.cosmetics.nameText.text = pc.name;
            }
            VentLogger.Old("----------役職割り当て----------", "Info");
            foreach (var pc in PlayerControl.AllPlayerControls)
            {
                VentLogger.Old($"{(pc.AmOwner ? "[*]" : ""),-3}{pc.PlayerId,-2}:{pc?.Data?.PlayerName?.PadRightV2(20)}:{pc.GetAllRoleName()}", "Info");
            }
            VentLogger.Old("--------------環境--------------", "Info");
            foreach (var pc in PlayerControl.AllPlayerControls)
            {
                try
                {
                    var text = pc.AmOwner ? "[*]" : "   ";
                    text += $"{pc.PlayerId,-2}:{pc.Data?.PlayerName?.PadRightV2(20)}:{pc.GetClient()?.PlatformData?.Platform.ToString()?.Replace("Standalone", ""),-11}";
                    if (TOHPlugin.playerVersion.TryGetValue(pc.PlayerId, out PlayerVersion pv))
                        text += $":Mod({pv.forkId}/{pv.version}:{pv.tag})";
                    else text += ":Vanilla";
                    VentLogger.Old(text, "Info");
                }
                catch (Exception ex)
                {
                    VentLogger.Error(ex.ToString(), "Platform");
                }
            }
            VentLogger.Old("------------基本設定------------", "Info");
            var tmp = GameOptionsManager.Instance.CurrentGameOptions.ToHudString(GameData.Instance ? GameData.Instance.PlayerCount : 10).Split("\r\n").Skip(1);
            foreach (var t in tmp) VentLogger.Old(t, "Info");
            VentLogger.Old("------------詳細設定------------", "Info");
            VentLogger.Old($"プレイヤー数: {PlayerControl.AllPlayerControls.Count}人", "Info");
            PlayerControl.AllPlayerControls.ToArray().Do(x => TOHPlugin.PlayerStates[x.PlayerId].InitTask(x));

            GameStates.InGame = true;
        }

        public static void Postfix()
        {
            ActionHandle handle = ActionHandle.NoInit();
            Game.TriggerForAll(RoleActionType.RoundStart, ref handle, true);
        }
    }
    [HarmonyPatch(typeof(IntroCutscene), nameof(IntroCutscene.BeginCrewmate))]
    class BeginCrewmatePatch
    {
        public static void Prefix(IntroCutscene __instance, ref Il2CppSystem.Collections.Generic.List<PlayerControl> teamToDisplay)
        {
            if (PlayerControl.LocalPlayer.Is(Roles.RoleType.Neutral))
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
            CustomRole role = PlayerControl.LocalPlayer.GetCustomRole();
            Roles.RoleType roleType = role.GetRoleType();

            switch (roleType)
            {
                case Roles.RoleType.Neutral:
                    __instance.TeamTitle.text = Utils.GetRoleName(role);
                    __instance.TeamTitle.color = Utils.GetRoleColor(role);
                    __instance.ImpostorText.gameObject.SetActive(true);
                    __instance.ImpostorText.text = role switch
                    {
                        Egoist => GetString("TeamEgoist"),
                        Jackal => GetString("TeamJackal"),
                        _ => GetString("NeutralInfo"),
                    };
                    __instance.BackgroundBar.material.color = Utils.GetRoleColor(role);
                    break;
                case Roles.RoleType.Madmate:
                    __instance.TeamTitle.text = GetString("Madmate");
                    __instance.TeamTitle.color = Utils.GetRoleColor(Madmate.Ref<Madmate>());
                    __instance.ImpostorText.text = GetString("TeamImpostor");
                    StartFadeIntro(__instance, Palette.CrewmateBlue, Palette.ImpostorRed);
                    PlayerControl.LocalPlayer.Data.Role.IntroSound = GetIntroSound(RoleTypes.Impostor);
                    break;
            }
            switch (role)
            {
                case Terrorist:
                    var sound = ShipStatus.Instance.CommonTasks.Where(task => task.TaskType == TaskTypes.FixWiring).FirstOrDefault()
                    .MinigamePrefab.OpenSound;
                    PlayerControl.LocalPlayer.Data.Role.IntroSound = sound;
                    break;

                case Executioner:
                    PlayerControl.LocalPlayer.Data.Role.IntroSound = GetIntroSound(RoleTypes.Shapeshifter);
                    break;

                case Vampire:
                    PlayerControl.LocalPlayer.Data.Role.IntroSound = GetIntroSound(RoleTypes.Shapeshifter);
                    break;

                case SabotageMaster:
                    PlayerControl.LocalPlayer.Data.Role.IntroSound = ShipStatus.Instance.SabotageSound;
                    break;

                case Sheriff:
                    PlayerControl.LocalPlayer.Data.Role.IntroSound = GetIntroSound(RoleTypes.Crewmate);
                    __instance.BackgroundBar.material.color = Palette.CrewmateBlue;
                    break;
                case Arsonist:
                    PlayerControl.LocalPlayer.Data.Role.IntroSound = GetIntroSound(RoleTypes.Crewmate);
                    break;

                case SchrodingerCat:
                    PlayerControl.LocalPlayer.Data.Role.IntroSound = GetIntroSound(RoleTypes.Impostor);
                    break;

                case Mayor:
                    PlayerControl.LocalPlayer.Data.Role.IntroSound = GetIntroSound(RoleTypes.Crewmate);
                    break;

                case GM:
                    __instance.TeamTitle.text = Utils.GetRoleName(role);
                    __instance.TeamTitle.color = Utils.GetRoleColor(role);
                    __instance.BackgroundBar.material.color = Utils.GetRoleColor(role);
                    __instance.ImpostorText.gameObject.SetActive(false);
                    break;

            }

            if (Input.GetKey(KeyCode.RightShift))
            {
                __instance.TeamTitle.text = "Town Of Host:\nThe Other Roles";
                __instance.ImpostorText.gameObject.SetActive(true);
                __instance.ImpostorText.text = "https://github.com/music-discussion/TownOfHost-TheOtherRoles--TOH-TOR" +
                    "\r\nv0.9.4 - Out Now on Github";
                __instance.TeamTitle.color = Utils.ConvertHexToColor("#73fa73");
                StartFadeIntro(__instance, Color.cyan, Color.yellow);
            }
            if (Input.GetKey(KeyCode.LeftShift))
            {
                __instance.TeamTitle.text = "Town Of Host:\nThe Other Roles";
                __instance.ImpostorText.gameObject.SetActive(true);
                __instance.ImpostorText.text = "https://github.com/music-discussion/TownOfHost-TheOtherRoles--TOH-TOR" +
                    "\r\nv0.9.4 - Coming Soon on Github";
                __instance.TeamTitle.color = Utils.ConvertHexToColor("#73fa73");
                StartFadeIntro(__instance, Color.cyan, Color.yellow);
            }
            if (Input.GetKey(KeyCode.RightControl))
            {
                __instance.TeamTitle.text = "Discord Server";
                __instance.ImpostorText.gameObject.SetActive(true);
                __instance.ImpostorText.text = "https://discord.gg/tohtor";
                __instance.TeamTitle.color = Utils.ConvertHexToColor("#73fa73");
                StartFadeIntro(__instance, Utils.ConvertHexToColor("#73fa73"), Utils.ConvertHexToColor("#73fa73"));
            }
        }
        private static AudioClip GetIntroSound(RoleTypes roleType)
        {
            return RoleManager.Instance.AllRoles.FirstOrDefault(role => role.Role == roleType)?.IntroSound;
        }
        private static async void StartFadeIntro(IntroCutscene __instance, Color start, Color end)
        {
            await System.Threading.Tasks.Task.Delay(1000);
            int milliseconds = 0;
            while (true)
            {
                await System.Threading.Tasks.Task.Delay(20);
                milliseconds += 20;
                float time = (float)milliseconds / (float)500;
                Color LerpingColor = Color.Lerp(start, end, time);
                if (__instance == null || milliseconds > 500)
                {
                    VentLogger.Old("ループを終了します", "StartFadeIntro");
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
            if (PlayerControl.LocalPlayer.Is(CustomRoleManager.Static.Sheriff))
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
    class IntroCutsceneDestroyPatch
    {
        public static void Postfix(IntroCutscene __instance)
        {
            Game.State = GameState.Roaming;
            if (!GameStates.IsInGame) return;
            if (!AmongUsClient.Instance.AmHost) return;

            if (TOHPlugin.NormalOptions.MapId != 4)
            {
                PlayerControl.AllPlayerControls.ToArray().Do(pc => pc.RpcResetAbilityCooldown());
                if (StaticOptions.FixFirstKillCooldown)
                    DTask.Schedule(() =>
                    {
                        PlayerControl.AllPlayerControls.ToArray().Do(pc =>
                        {
                            if (pc.GetCustomRole() is not Impostor impostor) return;
                            pc.SetKillCooldown(impostor.KillCooldown);
                        });
                    }, 2f);
            }
            DTask.Schedule(() => PlayerControl.AllPlayerControls.ToArray().Do(pc => pc.RpcSetRoleDesync(RoleTypes.Shapeshifter, -3)), 2f);
            if (PlayerControl.LocalPlayer.Is(CustomRoleManager.Special.GM))
            {
                PlayerControl.LocalPlayer.RpcExile();
                TOHPlugin.PlayerStates[PlayerControl.LocalPlayer.PlayerId].SetDead();
            }

            if (StaticOptions.RandomSpawn)
            {
                RandomSpawn.SpawnMap map;
                switch (TOHPlugin.NormalOptions.MapId)
                {
                    case 0:
                        map = new RandomSpawn.SkeldSpawnMap();
                        PlayerControl.AllPlayerControls.ToArray().Do(map.RandomTeleport);
                        break;
                    case 1:
                        map = new RandomSpawn.MiraHQSpawnMap();
                        PlayerControl.AllPlayerControls.ToArray().Do(map.RandomTeleport);
                        break;
                }
            }

            VentLogger.Old("OnDestroy", "IntroCutscene");
        }
    }
}