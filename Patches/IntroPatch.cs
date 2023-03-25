using AmongUs.GameOptions;
using HarmonyLib;
using System;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using static TOHE.Translator;

namespace TOHE;

[HarmonyPatch(typeof(IntroCutscene), nameof(IntroCutscene.ShowRole))]
class SetUpRoleTextPatch
{
    public static void Postfix(IntroCutscene __instance)
    {
        if (!GameStates.IsModHost) return;
        new LateTask(() =>
        {
            CustomRoles role = PlayerControl.LocalPlayer.GetCustomRole();
            if (!role.IsVanilla())
            {
                __instance.YouAreText.color = Utils.GetRoleColor(role);
                __instance.RoleText.text = Utils.GetRoleName(role);
                __instance.RoleText.color = Utils.GetRoleColor(role);
                __instance.RoleBlurbText.color = Utils.GetRoleColor(role);

                __instance.RoleBlurbText.text = PlayerControl.LocalPlayer.GetRoleInfo();
            }

            foreach (var subRole in Main.PlayerStates[PlayerControl.LocalPlayer.PlayerId].SubRoles)
                __instance.RoleBlurbText.text += "\n" + Utils.ColorString(Utils.GetRoleColor(subRole), GetString($"{subRole}Info"));
            if (!PlayerControl.LocalPlayer.Is(CustomRoles.Lovers) && !PlayerControl.LocalPlayer.Is(CustomRoles.Ntr) && CustomRolesHelper.RoleExist(CustomRoles.Ntr))
                __instance.RoleBlurbText.text += "\n" + Utils.ColorString(Utils.GetRoleColor(CustomRoles.Lovers), GetString($"{CustomRoles.Lovers}Info"));
            __instance.RoleText.text += Utils.GetSubRolesText(PlayerControl.LocalPlayer.PlayerId, false, true);

        }, 0.01f, "Override Role Text");

    }
}
[HarmonyPatch(typeof(IntroCutscene), nameof(IntroCutscene.CoBegin))]
class CoBeginPatch
{
    public static void Prefix()
    {
        var logger = Logger.Handler("Info");
        logger.Info("------------显示名称------------");
        foreach (var pc in Main.AllPlayerControls)
        {
            logger.Info($"{(pc.AmOwner ? "[*]" : ""),-3}{pc.PlayerId,-2}:{pc.name.PadRightV2(20)}:{pc.cosmetics.nameText.text}({Palette.ColorNames[pc.Data.DefaultOutfit.ColorId].ToString().Replace("Color", "")})");
            pc.cosmetics.nameText.text = pc.name;
        }
        logger.Info("------------职业分配------------");
        foreach (var pc in Main.AllPlayerControls)
        {
            logger.Info($"{(pc.AmOwner ? "[*]" : ""),-3}{pc.PlayerId,-2}:{pc?.Data?.PlayerName?.PadRightV2(20)}:{pc.GetAllRoleName().RemoveHtmlTags()}");
        }
        logger.Info("------------运行环境------------");
        foreach (var pc in Main.AllPlayerControls)
        {
            try
            {
                var text = pc.AmOwner ? "[*]" : "   ";
                text += $"{pc.PlayerId,-2}:{pc.Data?.PlayerName?.PadRightV2(20)}:{pc.GetClient()?.PlatformData?.Platform.ToString()?.Replace("Standalone", ""),-11}";
                if (Main.playerVersion.TryGetValue(pc.PlayerId, out PlayerVersion pv))
                    text += $":Mod({pv.forkId}/{pv.version}:{pv.tag})";
                else text += ":Vanilla";
                logger.Info(text);
            }
            catch (Exception ex)
            {
                Logger.Exception(ex, "Platform");
            }
        }
        logger.Info("------------基本设置------------");
        var tmp = GameOptionsManager.Instance.CurrentGameOptions.ToHudString(GameData.Instance ? GameData.Instance.PlayerCount : 10).Split("\r\n").Skip(1);
        foreach (var t in tmp) logger.Info(t);
        logger.Info("------------详细设置------------");
        foreach (var o in OptionItem.AllOptions)
            if (!o.IsHiddenOn(Options.CurrentGameMode) && (o.Parent == null ? !o.GetString().Equals("0%") : o.Parent.GetBool()))
                logger.Info($"{(o.Parent == null ? o.GetName(true).RemoveHtmlTags().PadRightV2(40) : $"┗ {o.GetName(true).RemoveHtmlTags()}".PadRightV2(41))}:{o.GetString().RemoveHtmlTags()}");
        logger.Info("-------------其它信息-------------");
        logger.Info($"玩家人数: {Main.AllPlayerControls.Count()}");
        Main.AllPlayerControls.Do(x => Main.PlayerStates[x.PlayerId].InitTask(x));
        GameData.Instance.RecomputeTaskCounts();
        TaskState.InitialTotalTasks = GameData.Instance.TotalTasks;

        Utils.NotifyRoles();

        GameStates.InGame = true;
    }
}
[HarmonyPatch(typeof(IntroCutscene), nameof(IntroCutscene.BeginCrewmate))]
class BeginCrewmatePatch
{
    public static void Prefix(IntroCutscene __instance, ref Il2CppSystem.Collections.Generic.List<PlayerControl> teamToDisplay)
    {
        if (PlayerControl.LocalPlayer.Is(CustomRoleTypes.Neutral))
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
        CustomRoles role = PlayerControl.LocalPlayer.GetCustomRole();

        switch (role.GetCustomRoleTypes())
        {
            case CustomRoleTypes.Neutral:
                __instance.TeamTitle.text = Utils.GetRoleName(role);
                __instance.TeamTitle.color = Utils.GetRoleColor(role);
                __instance.ImpostorText.gameObject.SetActive(true);
                __instance.ImpostorText.text = role switch
                {
                    CustomRoles.Jackal => GetString("TeamJackal"),
                    CustomRoles.Pelican => GetString("TeamPelican"),
                    CustomRoles.Gamer => GetString("TeamGamer"),
                    _ => GetString("NeutralInfo"),
                };
                __instance.BackgroundBar.material.color = Utils.GetRoleColor(role);
                PlayerControl.LocalPlayer.Data.Role.IntroSound = GetIntroSound(RoleTypes.Shapeshifter);
                break;
            case CustomRoleTypes.Crewmate:
                PlayerControl.LocalPlayer.Data.Role.IntroSound = GetIntroSound(RoleTypes.Crewmate);
                break;
            case CustomRoleTypes.Impostor:
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

            case CustomRoles.Workaholic:
                PlayerControl.LocalPlayer.Data.Role.IntroSound = DestroyableSingleton<HudManager>.Instance.TaskCompleteSound;
                break;

            case CustomRoles.Opportunist:
            case CustomRoles.FFF:
            case CustomRoles.Revolutionist:
                PlayerControl.LocalPlayer.Data.Role.IntroSound = GetIntroSound(RoleTypes.Crewmate);
                break;

            case CustomRoles.SabotageMaster:
            case CustomRoles.Provocateur:
                PlayerControl.LocalPlayer.Data.Role.IntroSound = ShipStatus.Instance.SabotageSound;
                break;

            case CustomRoles.Sheriff:
            case CustomRoles.SwordsMan:
            case CustomRoles.Medicaler:
                PlayerControl.LocalPlayer.Data.Role.IntroSound = GetIntroSound(role == CustomRoles.Medicaler ? RoleTypes.Scientist : RoleTypes.Crewmate);
                __instance.BackgroundBar.material.color = Palette.CrewmateBlue;
                __instance.ImpostorText.gameObject.SetActive(true);
                var numImpostors = Main.NormalOptions.NumImpostors;
                var text = numImpostors == 1
                    ? GetString(StringNames.NumImpostorsS)
                    : string.Format(GetString(StringNames.NumImpostorsP), numImpostors);
                __instance.ImpostorText.text = text.Replace("[FF1919FF]", "<color=#FF1919FF>").Replace("[]", "</color>");
                break;

            case CustomRoles.Doctor:
                PlayerControl.LocalPlayer.Data.Role.IntroSound = GetIntroSound(RoleTypes.Scientist);
                break;

            case CustomRoles.GM:
                __instance.TeamTitle.text = Utils.GetRoleName(role);
                __instance.TeamTitle.color = Utils.GetRoleColor(role);
                __instance.BackgroundBar.material.color = Utils.GetRoleColor(role);
                __instance.ImpostorText.gameObject.SetActive(false);
                PlayerControl.LocalPlayer.Data.Role.IntroSound = DestroyableSingleton<HudManager>.Instance.TaskCompleteSound;
                break;
        }

        if (PlayerControl.LocalPlayer.Is(CustomRoles.Madmate))
        {
            __instance.TeamTitle.text = GetString("Madmate");
            __instance.TeamTitle.color = Utils.GetRoleColor(CustomRoles.Madmate);
            __instance.ImpostorText.text = GetString("TeamImpostor");
            StartFadeIntro(__instance, Palette.CrewmateBlue, Palette.ImpostorRed);
            PlayerControl.LocalPlayer.Data.Role.IntroSound = GetIntroSound(RoleTypes.Impostor);
        }

        if (Input.GetKey(KeyCode.RightShift))
        {
            __instance.TeamTitle.text = "明天就跑路啦";
            __instance.ImpostorText.gameObject.SetActive(true);
            __instance.ImpostorText.text = "嘿嘿嘿嘿嘿嘿";
            __instance.TeamTitle.color = Color.cyan;
            StartFadeIntro(__instance, Color.cyan, Color.yellow);
        }
        if (Input.GetKey(KeyCode.RightControl))
        {
            __instance.TeamTitle.text = "警告";
            __instance.ImpostorText.gameObject.SetActive(true);
            __instance.ImpostorText.text = "请远离无知的玩家";
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
            float time = milliseconds / (float)500;
            Color LerpingColor = Color.Lerp(start, end, time);
            if (__instance == null || milliseconds > 500)
            {
                Logger.Info("ループを終了します", "StartFadeIntro");
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
        var role = PlayerControl.LocalPlayer.GetCustomRole();
        if (role is CustomRoles.Sheriff or CustomRoles.SwordsMan or CustomRoles.Medicaler)
        {
            //シェリフの場合はキャンセルしてBeginCrewmateに繋ぐ
            yourTeam = new Il2CppSystem.Collections.Generic.List<PlayerControl>();
            yourTeam.Add(PlayerControl.LocalPlayer);
            foreach (var pc in Main.AllPlayerControls)
            {
                if (!pc.AmOwner) yourTeam.Add(pc);
            }
            __instance.BeginCrewmate(yourTeam);
            __instance.overlayHandle.color = Palette.CrewmateBlue;
            return false;
        }
        if (PlayerControl.LocalPlayer.Is(CustomRoles.Madmate))
        {
            yourTeam = new Il2CppSystem.Collections.Generic.List<PlayerControl>();
            yourTeam.Add(PlayerControl.LocalPlayer);
            foreach (var pc in Main.AllPlayerControls)
                if (!pc.AmOwner && pc.GetCustomRole().IsImpostorTeam())
                    yourTeam.Add(pc);
            __instance.BeginCrewmate(yourTeam);
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
        if (!GameStates.IsInGame) return;
        Main.introDestroyed = true;
        if (AmongUsClient.Instance.AmHost)
        {
            if (Main.NormalOptions.MapId != 4)
            {
                Main.AllPlayerControls.Do(pc => pc.RpcResetAbilityCooldown());
                if (Options.FixFirstKillCooldown.GetBool())
                    new LateTask(() =>
                    {
                        Main.AllPlayerControls.Where(x => (Main.AllPlayerKillCooldown[x.PlayerId] - 2f) > 0f).Do(pc => pc.SetKillCooldown(Main.AllPlayerKillCooldown[pc.PlayerId] - 2f));
                    }, 2f, "FixKillCooldownTask");
            }
            new LateTask(() => Main.AllPlayerControls.Do(pc => pc.RpcSetRoleDesync(RoleTypes.Shapeshifter, -3)), 2f, "SetImpostorForServer");
            if (PlayerControl.LocalPlayer.Is(CustomRoles.GM))
            {
                PlayerControl.LocalPlayer.RpcExile();
                Main.PlayerStates[PlayerControl.LocalPlayer.PlayerId].SetDead();
            }
            if (Options.RandomSpawn.GetBool())
            {
                RandomSpawn.SpawnMap map;
                switch (Main.NormalOptions.MapId)
                {
                    case 0:
                        map = new RandomSpawn.SkeldSpawnMap();
                        Main.AllPlayerControls.Do(map.RandomTeleport);
                        break;
                    case 1:
                        map = new RandomSpawn.MiraHQSpawnMap();
                        Main.AllPlayerControls.Do(map.RandomTeleport);
                        break;
                }
            }
        }
        Logger.Info("OnDestroy", "IntroCutscene");
    }
}