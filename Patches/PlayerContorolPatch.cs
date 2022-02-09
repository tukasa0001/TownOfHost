using BepInEx;
using BepInEx.Configuration;
using BepInEx.IL2CPP;
using Hazel;
using System;
using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using UnityEngine;
using UnhollowerBaseLib;
using TownOfHost;
using System.Threading.Tasks;
using System.Threading;

namespace TownOfHost
{
    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.MurderPlayer))]
    class MurderPlayerPatch
    {
        public static void Postfix(PlayerControl __instance, [HarmonyArgument(0)] PlayerControl target)
        {
            if (!target.Data.IsDead || !AmongUsClient.Instance.AmHost)
                return;
            Logger.SendToFile("MurderPlayer発生: " + __instance.name + "=>" + target.name);
            //When Bait is killed
            if (target.getCustomRole() == CustomRoles.Bait && __instance.PlayerId != target.PlayerId)
            {
                Logger.SendToFile(target.name + "はBaitだった");
                new LateTask(() => __instance.CmdReportDeadBody(target.Data), 0.15f, "Bait Self Report");
            }
            else
            //Terrorist
            if (target.isTerrorist())
            {
                Logger.SendToFile(target.name + "はTerroristだった");
                main.CheckTerroristWin(target.Data);
            }
        }
    }
    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.CheckMurder))]
    class CheckMurderPatch
    {
        public static bool Prefix(PlayerControl __instance, [HarmonyArgument(0)] PlayerControl target)
        {
            if (!AmongUsClient.Instance.AmHost) return false;
            Logger.SendToFile("CheckMurder発生: " + __instance.name + "=>" + target.name);
            if(main.IsHideAndSeek && main.HideAndSeekKillDelayTimer > 0) {
                Logger.info("HideAndSeekの待機時間中だったため、キルをキャンセルしました。");
                return false;
            }
            if (__instance.isMafia())
            {
                if (!CustomRoles.Mafia.CanUseKillButton())
                {
                    Logger.SendToFile(__instance.name + "はMafiaだったので、キルはキャンセルされました。");
                    return false;
                } else {
                    Logger.SendToFile(__instance.name + "はMafiaですが、他のインポスターがいないのでキルが許可されました。");
                }
            }
            if(__instance.isSheriff()) {
                if(__instance.Data.IsDead) return false;
                if(!target.canBeKilledBySheriff()) {
                    __instance.RpcMurderPlayer(__instance);
                    return false;
                }
            }
            if(target.isMadGuardian()) {
                var isTaskFinished = true;
                foreach(var task in target.Data.Tasks) {
                    if(!task.Complete) {
                        isTaskFinished = false;
                        break;
                    }
                }
                if(isTaskFinished) {
                    __instance.RpcGuardAndKill(target);
                    if(main.MadGuardianCanSeeBarrier) {
                        //MadGuardian視点用
                        target.RpcGuardAndKill(target);
                    }
                    return false;
                }
            }
            if (__instance.isBountyHunter())
            {
                if (main.BountyCheck == true)
                {
                    if (main.b_target.Data.IsDead)
                    {
                        var rand = new System.Random();
                        main.BountyTargetPlayer = new List<PlayerControl>();
                        foreach (var p in PlayerControl.AllPlayerControls)if(!p.Data.IsDead && p.Data.Role.Role != RoleTypes.Impostor)main.BountyTargetPlayer.Add(p);
                        main.b_target = main.BountyTargetPlayer[rand.Next(0,main.BountyTargetPlayer.Count - 1)];
                        main.BountyCheck = true;
                    }
                    if (target != main.b_target)
                    {
                        __instance.RpcMurderPlayer(target);
                    }
                    else if (target == main.b_target)
                    {
                        __instance.RpcMurderPlayer(target);
                        __instance.RpcGuardAndKill(target);
                        main.BountyCheck = false;
                    }
                }
                if (main.BountyCheck == false)
                {
                    var rand = new System.Random();
                    main.BountyTargetPlayer = new List<PlayerControl>();
                    foreach (var p in PlayerControl.AllPlayerControls)if(!p.Data.IsDead && p.Data.Role.Role != RoleTypes.Impostor)main.BountyTargetPlayer.Add(p);
                    main.b_target = main.BountyTargetPlayer[rand.Next(0,main.BountyTargetPlayer.Count - 1)];
                    main.BountyCheck = true;
                }
                return false;
            }
            if (__instance.isVampire() && !target.isBait())
            { //キルキャンセル&自爆処理
                __instance.RpcGuardAndKill(target);
                main.BitPlayers.Add(target.PlayerId, (__instance.PlayerId, 0f));
                return false;
            }

            __instance.RpcMurderPlayer(target);
            return false;
        }
    }
    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.ReportDeadBody))]
    class ReportDeadBodyPatch
    {
        public static bool Prefix(PlayerControl __instance, [HarmonyArgument(0)] GameData.PlayerInfo target)
        {
            if (main.IsHideAndSeek) return false;
            if (!AmongUsClient.Instance.AmHost) return true;

            if (main.SyncButtonMode && target == null)
            {
                Logger.SendToFile("最大:" + main.SyncedButtonCount + ", 現在:" + main.UsedButtonCount, LogLevel.Message);
                if (main.SyncedButtonCount <= main.UsedButtonCount)
                {
                    Logger.SendToFile("使用可能ボタン回数が最大数を超えているため、ボタンはキャンセルされました。", LogLevel.Message);
                    return false;
                }
                else main.UsedButtonCount++;
                if (main.SyncedButtonCount == main.UsedButtonCount)
                {
                    Logger.SendToFile("使用可能ボタン回数が最大数に達しました。");
                }
            }

            foreach (var bp in main.BitPlayers)
            {
                foreach (var pc in PlayerControl.AllPlayerControls)
                {
                    if (bp.Key == pc.PlayerId && !pc.Data.IsDead)
                    {
                        pc.RpcMurderPlayer(pc);
                        main.PlaySoundRPC(bp.Value.Item1, Sounds.KillSound);
                        Logger.SendToFile("Vampireに噛まれている" + pc.name + "を自爆させました。");
                    }
                    else
                        Logger.SendToFile("Vampireに噛まれている" + pc.name + "はすでに死んでいました。");
                }
            }
            main.BitPlayers = new Dictionary<byte, (byte, float)>();

            if(__instance.Data.IsDead) return true;
            //=============================================
            //以下、ボタンが押されることが確定したものとする。
            //=============================================

            if(main.SyncButtonMode && AmongUsClient.Instance.AmHost && PlayerControl.LocalPlayer.Data.IsDead) {
                //SyncButtonMode中にホストが死んでいる場合
                ChangeLocalNameAndRevert(
                    "緊急会議ボタンはあと" + (main.SyncedButtonCount - main.UsedButtonCount) + "回使用可能です。",
                    1000
                );
            }

            main.CustomSyncAllSettings();
            return true;
        }
        public static async void ChangeLocalNameAndRevert(string name, int time) {
            //async Taskじゃ警告出るから仕方ないよね。
            var revertName = PlayerControl.LocalPlayer.name;
            PlayerControl.LocalPlayer.RpcSetName(name);
            await Task.Delay(time);
            PlayerControl.LocalPlayer.RpcSetName(revertName);
        }
    }
    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.FixedUpdate))]
    class FixedUpdatePatch
    {
        public static void Postfix(PlayerControl __instance)
        {
            if (AmongUsClient.Instance.AmHost)
            {//実行クライアントがホストの場合のみ実行
                //Vampireの処理
                if (main.BitPlayers.ContainsKey(__instance.PlayerId))
                {
                    //__instance:キルされる予定のプレイヤー
                    //main.BitPlayers[__instance.PlayerId].Item1:キルしたプレイヤーのID
                    //main.BitPlayers[__instance.PlayerId].Item2:キルするまでの秒数
                    if (main.BitPlayers[__instance.PlayerId].Item2 >= main.VampireKillDelay)
                    {
                        byte vampireID = main.BitPlayers[__instance.PlayerId].Item1;
                        if (!__instance.Data.IsDead)
                        {
                            __instance.RpcMurderPlayer(__instance);
                            main.PlaySoundRPC(vampireID, Sounds.KillSound);
                            Logger.SendToFile("Vampireに噛まれている" + __instance.name + "を自爆させました。");
                        }
                        else
                            Logger.SendToFile("Vampireに噛まれている" + __instance.name + "はすでに死んでいました。");
                        main.BitPlayers.Remove(__instance.PlayerId);
                    }
                    else
                    {
                        main.BitPlayers[__instance.PlayerId] =
                        (main.BitPlayers[__instance.PlayerId].Item1, main.BitPlayers[__instance.PlayerId].Item2 + Time.fixedDeltaTime);
                    }
                }

                if(__instance.AmOwner) main.ApplySuffix();
            }

            //役職テキストの表示
            var RoleTextTransform = __instance.nameText.transform.Find("RoleText");
            var RoleText = RoleTextTransform.GetComponent<TMPro.TextMeshPro>();
            if (RoleText != null && __instance != null)
            {
                var RoleTextData = main.GetRoleText(__instance);
                if(main.IsHideAndSeek) {
                    var hasRole = main.AllPlayerCustomRoles.TryGetValue(__instance.PlayerId, out var role);
                    if(hasRole) RoleTextData = main.GetRoleTextHideAndSeek(__instance.Data.Role.Role, role);
                }
                RoleText.text = RoleTextData.Item1;
                RoleText.color = RoleTextData.Item2;
                if (__instance.AmOwner) RoleText.enabled = true; //自分ならロールを表示
                else if (main.VisibleTasksCount && PlayerControl.LocalPlayer.Data.IsDead) RoleText.enabled = true; //他プレイヤーでVisibleTasksCountが有効なおかつ自分が死んでいるならロールを表示
                else RoleText.enabled = false; //そうでなければロールを非表示
                if (!AmongUsClient.Instance.IsGameStarted && AmongUsClient.Instance.GameMode != GameModes.FreePlay)
                {
                    RoleText.enabled = false; //ゲームが始まっておらずフリープレイでなければロールを非表示
                    if(!__instance.AmOwner) __instance.nameText.text = __instance.name;
                }
                if (main.VisibleTasksCount && main.hasTasks(__instance.Data, false)) //他プレイヤーでVisibleTasksCountは有効なおかつタスクがあるなら
                    RoleText.text += $" <color=#e6b422>({main.getTaskText(__instance.Data.Tasks)})</color>"; //ロールの横にタスク表示
                

                //変数定義
                string RealName;
                string Mark = "";
                string Suffix = "";

                //名前変更
                if(!main.RealNames.TryGetValue(__instance.PlayerId, out RealName)) {
                    RealName = __instance.name;
                    if(RealName == "Player(Clone)") return;
                    main.RealNames[__instance.PlayerId] = RealName;
                    TownOfHost.Logger.warn("プレイヤー" + __instance.PlayerId + "のRealNameが見つからなかったため、" + RealName + "を代入しました");
                }

                //タスクを終わらせたSnitchがインポスターを確認できる
                if(PlayerControl.LocalPlayer.isSnitch() && //LocalPlayerがSnitch
                    __instance.getCustomRole().isImpostor() && //__instanceがインポスター
                    PlayerControl.LocalPlayer.getPlayerTaskState().isTaskFinished //LocalPlayerのタスクが終わっている
                ) {
                    RealName = $"<color=#ff0000>{RealName}</color>"; //__instanceの名前を赤色で表示
                }

                //インポスターがタスクが終わりそうなSnitchを確認できる
                if(PlayerControl.LocalPlayer.getCustomRole().isImpostor() && //LocalPlayerがインポスター
                __instance.isSnitch() && __instance.getPlayerTaskState().doExpose //__instanceがタスクが終わりそうなSnitch
                ) {
                    Mark += $"<color={main.getRoleColorCode(CustomRoles.Snitch)}>★</color>"; //Snitch警告をつける
                }

                //タスクが終わりそうなSnitchがいるとき、インポスターに警告が表示される
                if(__instance.AmOwner && __instance.getCustomRole().isImpostor()) {//__instanceがインポスターかつ自分自身
                    foreach(var pc in PlayerControl.AllPlayerControls) { //全員分ループ
                        if(!pc.isSnitch()) continue; //スニッチ以外に用はない
                        if(pc.getPlayerTaskState().doExpose) { //タスクが終わりそうなSnitchが見つかった時
                            Mark += $"<color={main.getRoleColorCode(CustomRoles.Snitch)}>★</color>"; //Snitch警告を表示
                            break; //無駄なループは行わない
                        }
                    }
                }
                if(__instance.AmOwner && __instance.isBountyHunter()) {
                    if(main.b_target != null) {
                        string targetName;
                        if(!main.RealNames.TryGetValue(main.b_target.PlayerId, out targetName)) {
                            targetName = main.b_target.name;
                            main.RealNames[main.b_target.PlayerId] = targetName;
                            TownOfHost.Logger.warn("プレイヤー" + main.b_target.PlayerId + "のRealNameが見つからなかったため、" + targetName + "を代入しました");
                        }
                        Suffix = $"<size=1.5>Target:{targetName}</size>";
                    }
                }

                //Mark・Suffixの適用
                __instance.nameText.text = $"{RealName}{Mark}";
                __instance.nameText.text += Suffix == "" ? "" : "\r\n" + Suffix;
            }
        }
    }
    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.Start))]
    class PlayerStartPatch
    {
        public static void Postfix(PlayerControl __instance)
        {
            var roleText = UnityEngine.Object.Instantiate(__instance.nameText);
            roleText.transform.SetParent(__instance.nameText.transform);
            roleText.transform.localPosition = new Vector3(0f, 0.175f, 0f);
            roleText.fontSize = 0.55f;
            roleText.text = "RoleText";
            roleText.gameObject.name = "RoleText";
            roleText.enabled = false;
        }
    }
    [HarmonyPatch(typeof(PlayerControl),nameof(PlayerControl.SetColor))]
    class SetColorPatch {
        public static bool IsAntiGlitchDisabled = false;
        public static bool Prefix(PlayerControl __instance, int bodyColor) {
            //色変更バグ対策
            if(!AmongUsClient.Instance.AmHost || __instance.CurrentOutfit.ColorId == bodyColor || IsAntiGlitchDisabled) return true;
            if(AmongUsClient.Instance.IsGameStarted && main.IsHideAndSeek) {
                //ゲーム中に色を変えた場合
                __instance.RpcMurderPlayer(__instance);
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(Vent), nameof(Vent.EnterVent))]
    class EnterVentPatch {
        public static void Postfix(Vent __instance, [HarmonyArgument(0)] PlayerControl pc) {
            if(main.IsHideAndSeek && main.IgnoreVent)
                pc.MyPhysics.RpcBootFromVent(__instance.Id);
        }
    }
    [HarmonyPatch(typeof(PlayerPhysics), nameof(PlayerPhysics.CoEnterVent))]
    class CoEnterVentPatch {
        public static bool Prefix(PlayerPhysics __instance, [HarmonyArgument(0)] int id) {
            if(AmongUsClient.Instance.AmHost){
                if(__instance.myPlayer.isSheriff()) {
                    MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(__instance.NetId, (byte)RpcCalls.BootFromVent, SendOption.Reliable, -1);
                    writer.WritePacked(127);
                    AmongUsClient.Instance.FinishRpcImmediately(writer);
                    new LateTask(() => {
                        int clientId = __instance.myPlayer.getClientId();
                        MessageWriter writer2 = AmongUsClient.Instance.StartRpcImmediately(__instance.NetId, (byte)RpcCalls.BootFromVent, SendOption.Reliable, clientId);
                        writer2.Write(id);
                        AmongUsClient.Instance.FinishRpcImmediately(writer2);
                    }, 0.5f, "Fix Sheriff Stuck");
                    return false;
                }
            }
            return true;
        }
    }
}
