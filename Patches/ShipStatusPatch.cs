using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using UnityEngine;

using TownOfHostForE.Roles.Core;
using TownOfHostForE.Roles.Crewmate;
using TownOfHostForE.GameMode;
using Hazel;
using TownOfHostForE.Patches;

namespace TownOfHostForE
{
    [HarmonyPatch(typeof(ShipStatus), nameof(ShipStatus.FixedUpdate))]
    class ShipFixedUpdatePatch
    {
        public static void Postfix(ShipStatus __instance)
        {
            //ここより上、全員が実行する
            if (!AmongUsClient.Instance.AmHost) return;
            //ここより下、ホストのみが実行する
            if ((Options.CurrentGameMode == CustomGameMode.HideAndSeek || Options.IsStandardHAS) && Main.introDestroyed)
            {
                if (Options.HideAndSeekKillDelayTimer > 0)
                {
                    Options.HideAndSeekKillDelayTimer -= Time.fixedDeltaTime;
                }
                else if (!float.IsNaN(Options.HideAndSeekKillDelayTimer))
                {
                    Utils.MarkEveryoneDirtySettings();
                    Options.HideAndSeekKillDelayTimer = float.NaN;
                    Logger.Info("キル能力解禁", "HideAndSeek");
                }
            }
            else if (Options.CurrentGameMode == CustomGameMode.SuperBombParty)
            {
                SuperBakuretsuBros.OnFixedUpdate();
            }
        }
    }
    [HarmonyPatch(typeof(ShipStatus), nameof(ShipStatus.UpdateSystem), typeof(SystemTypes), typeof(PlayerControl), typeof(MessageReader))]
    public static class MessageReaderUpdateSystemPatch
    {
        public static void Postfix(ShipStatus __instance, [HarmonyArgument(0)] SystemTypes systemType, [HarmonyArgument(1)] PlayerControl player, [HarmonyArgument(2)] MessageReader reader)
        {
            ShipStatusUpdateSystemPatch.Postfix();
        }
    }
    [HarmonyPatch(typeof(ShipStatus), nameof(ShipStatus.UpdateSystem), typeof(SystemTypes), typeof(PlayerControl), typeof(byte))]
    class ShipStatusUpdateSystemPatch
    {
        public static bool Prefix(ShipStatus __instance,
            [HarmonyArgument(0)] SystemTypes systemType,
            [HarmonyArgument(1)] PlayerControl player,
            [HarmonyArgument(2)] byte amount)
        {
            Logger.Info("SystemType: " + systemType.ToString() + ", PlayerName: " + player.GetNameWithRole() + ", amount: " + amount, "RepairSystem");

            if (RepairSender.enabled && AmongUsClient.Instance.NetworkMode != NetworkModes.OnlineGame)
            {
                Logger.SendInGame("SystemType: " + systemType.ToString() + ", PlayerName: " + player.GetNameWithRole() + ", amount: " + amount);
            }
            return true;
        }
        public static void Postfix()
        {
            Camouflage.CheckCamouflage();
        }
        public static void CheckAndOpenDoorsRange(ShipStatus __instance, int amount, int min, int max)
        {
            var Ids = new List<int>();
            for (var i = min; i <= max; i++)
            {
                Ids.Add(i);
            }
            CheckAndOpenDoors(__instance, amount, Ids.ToArray());
        }
        private static void CheckAndOpenDoors(ShipStatus __instance, int amount, params int[] DoorIds)
        {
            if (DoorIds.Contains(amount)) foreach (var id in DoorIds)
                {
                    __instance.RpcUpdateSystem(SystemTypes.Doors, (byte)id);
                }
        }
        private static bool CanSabotage(PlayerControl player, SystemTypes systemType)
        {
            //サボタージュ出来ないキラー役職はサボタージュ自体をキャンセル
            if (!player.Is(CustomRoleTypes.Impostor))
            {
                return false;
            }
            return true;
        }
        private static bool IsClumsy(PlayerControl player)
        {
            return player.Is(CustomRoles.Clumsy) ||
                (player.Is(CustomRoles.Sheriff) && Sheriff.IsClumsy.GetBool()) ||
                (player.Is(CustomRoles.SillySheriff) && SillySheriff.IsClumsy.GetBool());
        }
        public static bool OnSabotage(PlayerControl player, SystemTypes systemType, byte amount)
        {
            return true;
        }
    }
    [HarmonyPatch(typeof(ShipStatus), nameof(ShipStatus.CloseDoorsOfType))]
    class CloseDoorsPatch
    {
        public static bool Prefix(ShipStatus __instance)
        {
            return !(Options.CurrentGameMode == CustomGameMode.HideAndSeek || Options.IsStandardHAS) || Options.AllowCloseDoors.GetBool();
        }
    }
    [HarmonyPatch(typeof(ShipStatus), nameof(ShipStatus.Start))]
    class StartPatch
    {
        public static void Postfix()
        {
            Logger.CurrentMethod();

            BGMSettings.SetTaskBGM();
            Logger.Info("-----------ゲーム開始-----------", "Phase");
            PetSettings.CheckNotHasPetPlayers();
            Utils.CountAlivePlayers(true);
        }
    }
    [HarmonyPatch(typeof(ShipStatus), nameof(ShipStatus.StartMeeting))]
    class StartMeetingPatch
    {
        public static void Prefix(ShipStatus __instance, PlayerControl reporter, GameData.PlayerInfo target)
        {
            MeetingStates.ReportTarget = target;
            MeetingStates.DeadBodies = UnityEngine.Object.FindObjectsOfType<DeadBody>();
        }
        public static void Postfix()
        {
            // 全プレイヤーを湧いてない状態にする
            foreach (var state in PlayerState.AllPlayerStates.Values)
            {
                state.HasSpawned = false;
            }
        }
    }
    [HarmonyPatch(typeof(ShipStatus), nameof(ShipStatus.Begin))]
    class BeginPatch
    {
        public static void Postfix()
        {
            Logger.CurrentMethod();

            //ホストの役職初期設定はここで行うべき？
        }
    }
    [HarmonyPatch(typeof(GameManager), nameof(GameManager.CheckTaskCompletion))]
    class CheckTaskCompletionPatch
    {
        public static bool Prefix(ref bool __result)
        {
            if (Options.DisableTaskWin.GetBool() || Options.NoGameEnd.GetBool() || TaskState.InitialTotalTasks == 0)
            {
                __result = false;
                return false;
            }
            return true;
        }
    }
}