using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using TOHTOR.API;
using TOHTOR.Extensions;
using TOHTOR.Gamemodes;
using TOHTOR.Managers;
using TOHTOR.Options;
using TOHTOR.Patches;
using TOHTOR.Roles;
using VentLib.Logging;

namespace TOHTOR
{
    [HarmonyPatch(typeof(AmongUsClient), nameof(AmongUsClient.CoStartGame))]
    class ChangeRoleSettings
    {
        public static void Prefix(AmongUsClient __instance)
        {
            Game.Setup();
            /*GameOptionsManager.Instance.CurrentGameOptions = GameOptionsManager.Instance.normalGameHostOptions.Cast<IGameOptions>();*/
        }

        public static void Postfix(AmongUsClient __instance)
        {
            TOHPlugin.ResetCamPlayerList = new List<byte>();
            StaticOptions.UsedButtonCount = 0;
            TOHPlugin.VisibleTasksCount = true;
            FallFromLadder.Reset();
            AntiBlackout.Reset();

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
            Game.CurrentGamemode.Trigger(GameAction.GameStart);
        }
    }
}