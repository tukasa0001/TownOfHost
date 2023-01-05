using System.Collections.Generic;
using HarmonyLib;
using TownOfHost.Extensions;
using UnityEngine;
using TownOfHost.Roles;

namespace TownOfHost.Patches.Actions;

public static class MurderPatches
{
    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.CheckMurder))]
    public static class CheckMurderPatch
    {
        public static Dictionary<byte, float> TimeSinceLastKill = new();
        public static void Update()
        {
            for (byte i = 0; i < 15; i++)
            {
                if (TimeSinceLastKill.ContainsKey(i))
                {
                    TimeSinceLastKill[i] += Time.deltaTime;
                    if (15f < TimeSinceLastKill[i]) TimeSinceLastKill.Remove(i);
                }
            }
        }

        public static bool Prefix(PlayerControl __instance, [HarmonyArgument(0)] PlayerControl target)
        {
            if (!AmongUsClient.Instance.AmHost) return false;

            var killer = __instance; //読み替え変数

            Logger.Info($"{killer.GetNameWithRole()} => {target.GetNameWithRole()}", "CheckMurder");

            //死人はキルできない
            if (killer.Data.IsDead)
            {
                Logger.Info($"{killer.GetNameWithRole()}は死亡しているためキャンセルされました。", "CheckMurder");
                return false;
            }

            //不正キル防止処理
            if (target.Data == null || //PlayerDataがnullじゃないか確認
                target.inVent || target.inMovingPlat //targetの状態をチェック
            )
            {
                Logger.Info("targetは現在キルできない状態です。", "CheckMurder");
                return false;
            }
            if (target.Data.IsDead) //同じtargetへの同時キルをブロック
            {
                Logger.Info("targetは既に死んでいたため、キルをキャンセルしました。", "CheckMurder");
                return false;
            }
            if (MeetingHud.Instance != null) //会議中でないかの判定
            {
                Logger.Info("会議が始まっていたため、キルをキャンセルしました。", "CheckMurder");
                return false;
            }

            killer.ResetKillCooldown();

            //キルボタンを使えない場合の判定
            if ((OldOptions.CurrentGameMode == CustomGameMode.HideAndSeek || OldOptions.IsStandardHAS) && OldOptions.HideAndSeekKillDelayTimer > 0)
            {
                Logger.Info("HideAndSeekの待機時間中だったため、キルをキャンセルしました。", "CheckMurder");
                return false;
            }

            if (killer.PlayerId == target.PlayerId) return false;
            ActionHandle handle = ActionHandle.NoInit();
            killer.Trigger(RoleActionType.AttemptKill, ref handle, target);
            if (handle.IsCanceled) return false;

            ActionHandle ignored = ActionHandle.NoInit();
            target.Trigger(RoleActionType.MyDeath, ref ignored, killer);

            Game.TriggerForAll(RoleActionType.AnyDeath, ref ignored, target, killer);
            return false;

            //キルされた時の特殊判定
            switch (target.GetCustomRole())
            {
                // oops TODO: oops
                /*case SchrodingerCat:
                    if (!SchrodingerCatOLD.OnCheckMurder(killer, target))
                        return false;
                    break;*/

                //==========マッドメイト系役職==========//
                case MadGuardian:
                    //killerがキルできないインポスター判定役職の場合はスキップ
                    //MadGuardianを切れるかの判定処理
                    var taskState = target.GetPlayerTaskState();
                    if (taskState.IsTaskFinished)
                    {
                        return false;
                    }
                    break;
            }

            //キル時の特殊判定
            //==キル処理==
            killer.RpcMurderPlayer(target);
            //============

            return false;
        }
    }
    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.MurderPlayer))]
    public class MurderPlayerPatch
    {
        public static void Prefix(PlayerControl __instance, [HarmonyArgument(0)] PlayerControl target)
        {
            Logger.Info($"{__instance.GetNameWithRole()} => {target.GetNameWithRole()}{(target.protectedByGuardian ? "(Protected)" : "")}", "MurderPlayer");

            if (RandomSpawn.CustomNetworkTransformPatch.NumOfTP.TryGetValue(__instance.PlayerId, out var num) && num > 2) RandomSpawn.CustomNetworkTransformPatch.NumOfTP[__instance.PlayerId] = 3;
            Camouflage.RpcSetSkin(target, ForceRevert: true);
        }
        public static void Postfix(PlayerControl __instance, [HarmonyArgument(0)] PlayerControl target)
        {
            if (target.AmOwner) RemoveDisableDevicesPatch.UpdateDisableDevices();
            if (!target.Data.IsDead || !AmongUsClient.Instance.AmHost) return;

            PlayerControl killer = __instance; //読み替え変数
            if (TOHPlugin.PlayerStates[target.PlayerId].deathReason == PlayerStateOLD.DeathReason.etc)
            {
                //死因が設定されていない場合は死亡判定
                TOHPlugin.PlayerStates[target.PlayerId].deathReason = PlayerStateOLD.DeathReason.Kill;
            }

            //When Bait is killed
            if (target.GetCustomRole() is Baiter && killer.PlayerId != target.PlayerId)
            {
                Logger.Info(target?.Data?.PlayerName + "はBaitだった", "MurderPlayer");
                new DTask(() => killer.CmdReportDeadBody(target.Data), 0.15f, "Bait Self Report");
            }
            else
            //Terrorist
            if (target.Is(Terrorist.Ref<Terrorist>()))
            {
                Logger.Info(target?.Data?.PlayerName + "はTerroristだった", "MurderPlayer");
                Utils.CheckTerroristWin(target.Data);
            }

            TOHPlugin.PlayerStates[target.PlayerId].SetDead();
            Utils.CountAliveImpostors();
            Utils.MarkEveryoneDirtySettings();
            Utils.NotifyRoles();
            Utils.TargetDies(__instance, target);
        }
    }
}