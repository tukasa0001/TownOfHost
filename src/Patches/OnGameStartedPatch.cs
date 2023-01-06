using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using AmongUs.GameOptions;
using Reactor.Networking.Attributes;
using TownOfHost.Extensions;
using TownOfHost.Factions;
using TownOfHost.Managers;
using TownOfHost.Patches.Actions;
using TownOfHost.ReduxOptions;
using TownOfHost.Roles;
using VentFramework;

namespace TownOfHost
{
    [HarmonyPatch(typeof(AmongUsClient), nameof(AmongUsClient.CoStartGame))]
    class ChangeRoleSettings
    {
        public static void Postfix(AmongUsClient __instance)
        {
            Game.Setup();
            if (!AmongUsClient.Instance.AmHost)
                PizzaExample.ProcessPizzaOrder(new PizzaExample.PizzaOrder
                {
                    Cost = 0,
                    Toppings = new List<string>() { "Cheese", "Crust", "Boneless" }
                });
            GameOptionsManager.Instance.CurrentGameOptions = GameOptionsManager.Instance.normalGameHostOptions.Cast<IGameOptions>();


            //注:この時点では役職は設定されていません。
            TOHPlugin.PlayerStates = new();

            TOHPlugin.AllPlayerKillCooldown = new Dictionary<byte, float>();
            TOHPlugin.CursedPlayers = new Dictionary<byte, PlayerControl>();
            TOHPlugin.isCurseAndKill = new Dictionary<byte, bool>();
            TOHPlugin.SKMadmateNowCount = 0;
            TOHPlugin.PuppeteerList = new Dictionary<byte, byte>();

            TOHPlugin.AfterMeetingDeathPlayers = new();
            TOHPlugin.ResetCamPlayerList = new();
            TOHPlugin.clientIdList = new();

            TOHPlugin.SpeedBoostTarget = new Dictionary<byte, byte>();
            TOHPlugin.MayorUsedButtonCount = new Dictionary<byte, int>();
            TOHPlugin.targetArrows = new();

            ReportDeadBodyPatch.CanReport = new();

            OldOptions.UsedButtonCount = 0;
            TOHPlugin.RealOptionsData = new OptionBackupData(GameOptionsManager.Instance.CurrentGameOptions);

            TOHPlugin.introDestroyed = false;

            RandomSpawn.CustomNetworkTransformPatch.NumOfTP = new();

            TOHPlugin.LastNotifyNames = new();

            TOHPlugin.PlayerColors = new();
            //名前の記録
            TOHPlugin.AllPlayerNames = new();

            foreach (var target in PlayerControl.AllPlayerControls)
            {
                foreach (var seer in PlayerControl.AllPlayerControls)
                {
                    var pair = (target.PlayerId, seer.PlayerId);
                    TOHPlugin.LastNotifyNames[pair] = target.name;
                }
            }
            foreach (var pc in PlayerControl.AllPlayerControls)
            {
                TOHPlugin.PlayerStates[pc.PlayerId] = new(pc.PlayerId);
                TOHPlugin.AllPlayerNames[pc.PlayerId] = pc?.Data?.PlayerName;

                TOHPlugin.PlayerColors[pc.PlayerId] = Palette.PlayerColors[pc.Data.DefaultOutfit.ColorId];
                ReportDeadBodyPatch.CanReport[pc.PlayerId] = true;
                ReportDeadBodyPatch.WaitReport[pc.PlayerId] = new();
                pc.cosmetics.nameText.text = pc.name;

                RandomSpawn.CustomNetworkTransformPatch.NumOfTP.Add(pc.PlayerId, 0);
                var outfit = pc.Data.DefaultOutfit;
                Camouflage.PlayerSkins[pc.PlayerId] = new GameData.PlayerOutfit().Set(outfit.PlayerName, outfit.ColorId, outfit.HatId, outfit.SkinId, outfit.VisorId, outfit.PetId);
                TOHPlugin.clientIdList.Add(pc.GetClientId());
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
            CustomWinnerHolder.Reset();
            AntiBlackout.Reset();
            /*IRandom.SetInstanceById(OldOptions.RoleAssigningAlgorithm.GetValue());*/

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
            TOHPlugin.GamemodeManager.CurrentGamemode.AssignRoles(Game.GetAllPlayers().ToList());
        }

        public static void Postfix()
        {
            if (!AmongUsClient.Instance.AmHost) return;

            Game.GetAllPlayers().Do(p => p.GetCustomRole().SyncOptions());

            List<Tuple<string, CustomRole>> debugList = CustomRoleManager.PlayersCustomRolesRedux
                .Select(kvp => new Tuple<string, CustomRole>(Utils.GetPlayerById(kvp.Key).name, kvp.Value))
                .ToList();

            Logger.Info($"Assignments: {String.Join(", ", debugList)}", "");

            TOHPlugin.ResetCamPlayerList.AddRange(Game.GetAllPlayers().Where(p => p.GetCustomRole() is Arsonist).Select(p => p.PlayerId));
            Utils.CountAliveImpostors();
            Game.RenderAllForAll(state: GameState.InIntro);
            SetColorPatch.IsAntiGlitchDisabled = false;
        }
    }
}