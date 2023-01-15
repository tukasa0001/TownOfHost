using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using AmongUs.GameOptions;
using TownOfHost.Extensions;
using TownOfHost.Managers;
using TownOfHost.Options;
using TownOfHost.Patches.Actions;
using TownOfHost.Roles;
using VentLib.Logging;

namespace TownOfHost
{
    [HarmonyPatch(typeof(AmongUsClient), nameof(AmongUsClient.CoStartGame))]
    class ChangeRoleSettings
    {
        public static void Prefix(AmongUsClient __instance)
        {
            Game.Setup();
            GameOptionsManager.Instance.CurrentGameOptions = GameOptionsManager.Instance.normalGameHostOptions.Cast<IGameOptions>();
        }

        public static void Postfix(AmongUsClient __instance)
        {

            TOHPlugin.AllPlayerKillCooldown = new Dictionary<byte, float>();
            TOHPlugin.SKMadmateNowCount = 0;

            TOHPlugin.ResetCamPlayerList = new();

            OldOptions.UsedButtonCount = 0;


            RandomSpawn.CustomNetworkTransformPatch.NumOfTP = new();

            TOHPlugin.PlayerColors = new();
            //名前の記録
            TOHPlugin.AllPlayerNames = new();

            foreach (var pc in PlayerControl.AllPlayerControls)
            {
                TOHPlugin.AllPlayerNames[pc.PlayerId] = pc?.Data?.PlayerName;

                TOHPlugin.PlayerColors[pc.PlayerId] = Palette.PlayerColors[pc.Data.DefaultOutfit.ColorId];
                pc.cosmetics.nameText.text = pc.name;

                RandomSpawn.CustomNetworkTransformPatch.NumOfTP.Add(pc.PlayerId, 0);
            }
            TOHPlugin.VisibleTasksCount = true;
            if (__instance.AmHost)
            {
                TOHPlugin.RefixCooldownDelay = 0;
                if (OldOptions.CurrentGameMode == CustomGameMode.HideAndSeek)
                {
                    OldOptions.HideAndSeekKillDelayTimer = OldOptions.KillDelay.GetFloat();
                }
                if (OldOptions.IsStandardHAS)
                {
                    OldOptions.HideAndSeekKillDelayTimer = OldOptions.StandardHASWaitingTime.GetFloat();
                }
            }
            FallFromLadder.Reset();
            AntiBlackout.Reset();

            MeetingStates.MeetingCalled = false;
            MeetingStates.FirstMeeting = true;
            GameStates.AlreadyDied = false;

            Game.State = GameState.InIntro;
            DesyncOptions.OriginalHostOptions = GameOptionsManager.Instance.CurrentGameOptions;
            Game.GetAllPlayers().Do(p => CustomRoleManager.PlayersCustomRolesRedux[p.PlayerId] = CustomRoleManager.Default);
        }
    }
    [HarmonyPatch(typeof(RoleManager), nameof(RoleManager.SelectRoles))]
    class SelectRolesPatch
    {
        public static void Prefix()
        {
            if (!AmongUsClient.Instance.AmHost) return;
            Game.CurrentGamemode.AssignRoles(Game.GetAllPlayers().ToList());
        }

        public static void Postfix()
        {
            if (!AmongUsClient.Instance.AmHost) return;

            Game.GetAllPlayers().Do(p => p.GetCustomRole().SyncOptions());

            List<Tuple<string, CustomRole>> debugList = CustomRoleManager.PlayersCustomRolesRedux
                .Select(kvp => new Tuple<string, CustomRole>(Utils.GetPlayerById(kvp.Key).GetRawName(), kvp.Value))
                .ToList();

            VentLogger.Old($"Assignments: {String.Join(", ", debugList)}", "");

            TOHPlugin.ResetCamPlayerList.AddRange(Game.GetAllPlayers().Where(p => p.GetCustomRole() is Arsonist).Select(p => p.PlayerId));
            Game.RenderAllForAll(state: GameState.InIntro);
        }
    }
}