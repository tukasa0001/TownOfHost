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

            float minTime = Mathf.Max(0.02f, AmongUsClient.Instance.Ping / 1000f * 6f); //※AmongUsClient.Instance.Pingの値はミリ秒(ms)なので÷1000
            //TimeSinceLastKillに値が保存されていない || 保存されている時間がminTime以上 => キルを許可
            //↓許可されない場合
            if (TimeSinceLastKill.TryGetValue(killer.PlayerId, out var time) && time < minTime)
            {
                Logger.Info("前回のキルからの時間が早すぎるため、キルをブロックしました。", "CheckMurder");
                return false;
            }
            TimeSinceLastKill[killer.PlayerId] = 0f;

            killer.ResetKillCooldown();

            //キルボタンを使えない場合の判定
            if ((Options.CurrentGameMode == CustomGameMode.HideAndSeek || Options.IsStandardHAS) && Options.HideAndSeekKillDelayTimer > 0)
            {
                Logger.Info("HideAndSeekの待機時間中だったため、キルをキャンセルしました。", "CheckMurder");
                return false;
            }

            //キル可能判定
            if (killer.PlayerId != target.PlayerId && !killer.CanUseKillButton())
            {
                Logger.Info(killer.GetNameWithRole() + "はKillできないので、キルはキャンセルされました。", "CheckMurder");
                return false;
            }

            //キルされた時の特殊判定
            switch (target.GetCustomRole())
            {
                case SchrodingerCat:
                    if (!SchrodingerCatOLD.OnCheckMurder(killer, target))
                        return false;
                    break;

                //==========マッドメイト系役職==========//
                case MadGuardian:
                    //killerがキルできないインポスター判定役職の場合はスキップ
                    if (killer.Is(Arsonist.Ref<Arsonist>()) //アーソニスト
                    ) break;

                    //MadGuardianを切れるかの判定処理
                    var taskState = target.GetPlayerTaskState();
                    if (taskState.IsTaskFinished)
                    {
                        int dataCountBefore = NameColorManager.Instance.NameColors.Count;
                        NameColorManager.Instance.RpcAdd(killer.PlayerId, target.PlayerId, "#ff0000");
                        if (Options.MadGuardianCanSeeWhoTriedToKill.GetBool())
                            NameColorManager.Instance.RpcAdd(target.PlayerId, killer.PlayerId, "#ff0000");

                        if (dataCountBefore != NameColorManager.Instance.NameColors.Count)
                            Utils.NotifyRoles();
                        return false;
                    }
                    break;
            }

            //キル時の特殊判定
            if (killer.PlayerId != target.PlayerId)
            {
                //自殺でない場合のみ役職チェック
                switch (killer.GetCustomRole())
                {
                    //==========インポスター役職==========//
                    case BountyHunter: //キルが発生する前にここの処理をしないとバグる
                        BountyHunterOLD.OnCheckMurder(killer, target);
                        break;
                    case SerialKiller:
                        SerialKillerOLD.OnCheckMurder(killer);
                        break;
                    case Vampire:
                        if (!target.Is(Baiter.Ref<Baiter>()))
                        { //キルキャンセル&自爆処理
                            Utils.MarkEveryoneDirtySettings();
                            Main.AllPlayerKillCooldown[killer.PlayerId] = Options.DefaultKillCooldown * 2;
                            killer.MarkDirtySettings(); //負荷軽減のため、killerだけがCustomSyncSettingsを実行
                            killer.RpcGuardAndKill(target);
                            Main.BitPlayers.Add(target.PlayerId, (killer.PlayerId, 0f));
                            return false;
                        }
                        break;
                    case Warlock:
                        if (!Main.CheckShapeshift[killer.PlayerId] && !Main.isCurseAndKill[killer.PlayerId])
                        { //Warlockが変身時以外にキルしたら、呪われる処理
                            Main.isCursed = true;
                            Utils.MarkEveryoneDirtySettings();
                            killer.RpcGuardAndKill(target);
                            Main.CursedPlayers[killer.PlayerId] = target;
                            Main.WarlockTimer.Add(killer.PlayerId, 0f);
                            Main.isCurseAndKill[killer.PlayerId] = true;
                            return false;
                        }
                        if (Main.CheckShapeshift[killer.PlayerId])
                        {//呪われてる人がいないくて変身してるときに通常キルになる
                            killer.RpcMurderPlayer(target);
                            killer.RpcGuardAndKill(target);
                            return false;
                        }
                        if (Main.isCurseAndKill[killer.PlayerId]) killer.RpcGuardAndKill(target);
                        return false;
                    case Witch:
                        if (!WitchOLD.OnCheckMurder(killer, target))
                        {
                            //Spellモードの場合は終了
                            return false;
                        }
                        break;
                    case Puppeteer:
                        Main.PuppeteerList[target.PlayerId] = killer.PlayerId;
                        Main.AllPlayerKillCooldown[killer.PlayerId] = Options.DefaultKillCooldown * 2;
                        killer.MarkDirtySettings(); //負荷軽減のため、killerだけがCustomSyncSettings,NotifyRolesを実行
                        Utils.NotifyRoles(SpecifySeer: killer);
                        killer.RpcGuardAndKill(target);
                        return false;
                    case TimeThief:
                        TimeThiefOLD.OnCheckMurder(killer);
                        break;

                    //==========マッドメイト系役職==========//

                    //==========第三陣営役職==========//
                    case Arsonist:
                        Main.AllPlayerKillCooldown[killer.PlayerId] = 10f;
                        Utils.MarkEveryoneDirtySettings();
                        if (!Main.isDoused[(killer.PlayerId, target.PlayerId)] && !Main.ArsonistTimer.ContainsKey(killer.PlayerId))
                        {
                            Main.ArsonistTimer.Add(killer.PlayerId, (target, 0f));
                            Utils.NotifyRoles(SpecifySeer: __instance);
                            OldRPC.SetCurrentDousingTarget(killer.PlayerId, target.PlayerId);
                        }
                        return false;

                    //==========クルー役職==========//
                    case Sheriff:
                        if (!SheriffOLD.OnCheckMurder(killer, target))
                            return false;
                        break;
                }
            }

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
            if (Main.PlayerStates[target.PlayerId].deathReason == PlayerStateOLD.DeathReason.Sniped)
            {
                killer = Utils.GetPlayerById(SniperOLD.GetSniper(target.PlayerId));
            }
            if (Main.PlayerStates[target.PlayerId].deathReason == PlayerStateOLD.DeathReason.etc)
            {
                //死因が設定されていない場合は死亡判定
                Main.PlayerStates[target.PlayerId].deathReason = PlayerStateOLD.DeathReason.Kill;
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
            if (target.Is(Trapper.Ref<Trapper>()) && !killer.Is(Trapper.Ref<Trapper>()))
                killer.TrapperKilled(target);
            if (ExecutionerOLD.Target.ContainsValue(target.PlayerId))
                ExecutionerOLD.ChangeRoleByTarget(target);
            if (target.Is(Executioner.Ref<Executioner>()) && ExecutionerOLD.Target.ContainsKey(target.PlayerId))
            {
                ExecutionerOLD.Target.Remove(target.PlayerId);
                ExecutionerOLD.SendRPC(target.PlayerId);
            }
            if (target.Is(TimeThief.Ref<TimeThief>()))
                target.ResetVotingTime();

            LastImpostor.SetKillCooldown();
            FixedUpdatePatch.LoversSuicide(target.PlayerId);

            Main.PlayerStates[target.PlayerId].SetDead();
            target.SetRealKiller(__instance, true); //既に追加されてたらスキップ
            Utils.CountAliveImpostors();
            Utils.MarkEveryoneDirtySettings();
            Utils.NotifyRoles();
            Utils.TargetDies(__instance, target);
        }
    }
}