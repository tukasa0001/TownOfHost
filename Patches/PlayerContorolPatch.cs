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
            main.BountyMeetingCheck = false;
            //BountyHunter
            if(__instance.isBountyHunter()) //キルが発生する前にここの処理をしないとバグる
            {
                if(target != __instance.getBountyTarget() && main.isBountyKillSuccess){
                    main.isBountyKillSuccess = false;
                    main.isBountyDoubleKill = true;
                }
                if(target == __instance.getBountyTarget()) {//ターゲットをキルした場合
                    main.isBountyKillSuccess = true;
                    main.isTargetKilled.Remove(__instance.PlayerId);
                    main.isTargetKilled.Add(__instance.PlayerId, true);
                }
            }
            //Terrorist
            if (target.isTerrorist())
            {
                Logger.SendToFile(target.name + "はTerroristだった");
                main.CheckTerroristWin(target.Data);
            }
            if(!__instance.isBountyHunter())main.OtherImpostorsKillCheck = true;
            main.CustomSyncAllSettings();//キルクール処理を同期
        }
    }
    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.Shapeshift))]
    class ShapeshiftPatch
    {
        public static void Prefix(PlayerControl __instance, [HarmonyArgument(0)] PlayerControl target)
        {
            if(__instance.isWarlock())
            {
                if(main.FirstCursedCheck[__instance.PlayerId])//呪われた人がいるか確認
                {
                    if(main.CursedPlayers[__instance.PlayerId].Data.IsDead){//のろわれた人が死んだ場合
                        main.CursedPlayers.Remove(__instance.PlayerId);
                        main.FirstCursedCheck.Remove(__instance.PlayerId);
                        main.FirstCursedCheck.Add(__instance.PlayerId, false);
                    }
                    if(main.CursedPlayers[__instance.PlayerId] != null && !main.CheckShapeshift[__instance.PlayerId])//変身解除の時に反応しない
                    {
                        var cp = main.CursedPlayers[__instance.PlayerId];
                        Vector2 cppos = cp.transform.position;//呪われた人の位置
                        Dictionary<PlayerControl, float> cpdistance = new Dictionary<PlayerControl, float>();
                        float dis;
                        foreach(PlayerControl p in PlayerControl.AllPlayerControls)
                        {
                            if(!p.Data.IsDead && p != cp)
                            {
                                dis = Vector2.Distance(cppos,p.transform.position);
                                cpdistance.Add(p,dis);
                                Logger.info($"{p.name}の位置{dis}");
                            }
                        }
                        var min = cpdistance.OrderBy(c => c.Value).FirstOrDefault();//一番小さい値を取り出す
                        PlayerControl targetw = min.Key;
                        Logger.info($"{targetw.name}was killed");
                        cp.RpcMurderPlayer(targetw);//殺す
                    }
                }
            }
            if(main.CanMakeMadmateCount > main.SKMadmateNowCount && !__instance.isWarlock() && !main.CheckShapeshift[__instance.PlayerId])
            {//変身したとき一番近い人をマッドメイトにする処理
                Vector2 __instancepos = __instance.transform.position;//変身者の位置
                Dictionary<PlayerControl, float> mpdistance = new Dictionary<PlayerControl, float>();
                float dis;
                foreach(PlayerControl p in PlayerControl.AllPlayerControls)
                {
                    if(!p.Data.IsDead && p.Data.Role.Role != RoleTypes.Shapeshifter && !p.isImpostor() && !p.isBountyHunter() && !p.isWitch() && !p.isSKMadmate())
                    {
                        dis = Vector2.Distance(__instancepos,p.transform.position);
                        mpdistance.Add(p,dis);
                    }
                }
                //対象が見つかった時のみ処理
                if (mpdistance.Count() != 0)
                {
                    var min = mpdistance.OrderBy(c => c.Value).FirstOrDefault();//一番値が小さい
                    PlayerControl targetm = min.Key;
                    targetm.SetCustomRole(CustomRoles.SKMadmate);
                    main.SKMadmateNowCount++;
                    main.CustomSyncAllSettings();
                    main.NotifyRoles();
                }
            }
            bool check = main.CheckShapeshift[__instance.PlayerId];//変身、変身解除のスイッチ
            main.CheckShapeshift.Remove(__instance.PlayerId);
            main.CheckShapeshift.Add(__instance.PlayerId, !check);
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
            if(__instance.isSKMadmate())return false;//シェリフがサイドキックされた場合
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
            if(__instance.isSerialKiller())
            {
                __instance.RpcMurderPlayer(target);
                __instance.RpcGuardAndKill(target);
                main.SerialKillerTimer.Remove(__instance.PlayerId);
                main.SerialKillerTimer.Add(__instance.PlayerId,0f);
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
            if (__instance.isWitch())
            {
                if(__instance.GetKillOrSpell() && !main.SpelledPlayer.Contains(target))
                {
                    __instance.RpcGuardAndKill(target);
                    main.SpelledPlayer.Add(target);
                }
                main.KillOrSpell[__instance.PlayerId] = !__instance.GetKillOrSpell();
                main.NotifyRoles();
                __instance.SyncKillOrSpell();
            }
            if (__instance.isWarlock())
            {
                if (!main.CheckShapeshift[__instance.PlayerId] && !main.FirstCursedCheck[__instance.PlayerId])
                { //Warlockが変身時以外にキルしたら、呪われる処理
                    __instance.RpcGuardAndKill(target);
                    main.CursedPlayers.Add(__instance.PlayerId,target);
                    main.CursedPlayerDie.Add(target);
                    main.FirstCursedCheck.Remove(__instance.PlayerId);
                    main.FirstCursedCheck.Add(__instance.PlayerId, true);
                    return false;
                }
                if (main.CheckShapeshift[__instance.PlayerId] && !main.FirstCursedCheck[__instance.PlayerId]){//呪われてる人がいないくて変身してるときに通常キルになる
                    __instance.RpcMurderPlayer(target);
                    __instance.RpcGuardAndKill(target);
                    return false;
                }
                //Warlockが誰かを呪った時にキルできなくなる処理
                if (main.FirstCursedCheck[__instance.PlayerId])return false;
            }
            if (__instance.isVampire() && !target.isBait())
            { //キルキャンセル&自爆処理
                __instance.RpcGuardAndKill(target);
                main.BitPlayers.Add(target.PlayerId, (__instance.PlayerId, 0f));
                return false;
            }


            //==キル処理==
            __instance.RpcMurderPlayer(target);
            //============
            main.OtherImpostorsKillCheck = false;
            if(target != __instance.getBountyTarget() && main.isBountyDoubleKill){
                main.isBountyKillSuccess = true;
                main.isBountyDoubleKill = false;
            }
            main.CustomSyncAllSettings();

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
            main.BountyTimer.Clear();
            main.SerialKillerTimer.Clear();
            if (target != null)
            {
                Logger.info($"{__instance.name} => {target.PlayerName}");
                foreach (var sd in main.SpelledPlayer) if (target.PlayerId == sd.Data.PlayerId)
                {
                    return false;
                }
                foreach(var cp in main.CursedPlayerDie) if (target.PlayerId == cp.Data.PlayerId)return false;
            }

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
            foreach(var sp in main.SpelledPlayer) {
                sp.RpcSetName("<color=#ff0000>†</color>" + sp.getRealName());
            }
            foreach(var cp in main.CursedPlayerDie){
                cp.RpcSetName("<color=#ff0000>†</color>" + cp.getRealName());
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
                if(main.SerialKillerTimer.ContainsKey(__instance.PlayerId))
                {
                    if (main.SerialKillerTimer[__instance.PlayerId] >= main.SerialKillerLimit)
                    {
                        if(!__instance.Data.IsDead)
                        {
                            __instance.RpcMurderPlayer(__instance);
                            main.PlaySoundRPC(__instance.PlayerId, Sounds.KillSound);
                        }
                        else
                        main.SerialKillerTimer.Remove(__instance.PlayerId);
                    }
                    else
                    {
                        main.SerialKillerTimer[__instance.PlayerId] =
                        (main.SerialKillerTimer[__instance.PlayerId] + Time.fixedDeltaTime);
                    }
                }
                //バウハンのキルクールの変換とターゲットのリセット
                if(main.BountyTimer.ContainsKey(__instance.PlayerId))
                {
                    if(main.BountyTimer[__instance.PlayerId] >= main.BountyTargetChangeTime)//時間経過でターゲットをリセットする処理
                    {
                        __instance.RpcGuardAndKill(__instance);//タイマー（変身クールダウン）のリセットと、名前の変更のためのKill
                        main.BountyTimer.Remove(__instance.PlayerId);//時間リセット
                        main.BountyTimer.Add(__instance.PlayerId ,0f);
                        main.BountyTimerCheck = true;//キルクールを０にする処理に行かせるための処理
                    }
                    if(main.isTargetKilled[__instance.PlayerId])//ターゲットをキルした場合
                    {
                        __instance.RpcGuardAndKill(__instance.getBountyTarget());//守護天使バグ対策で上の処理のターゲットをキル対象に変更
                        main.BountyTimer.Remove(__instance.PlayerId);//それ以外上に同じ
                        main.BountyTimer.Add(__instance.PlayerId ,0f);
                        main.BountyTimerCheck = true;
                        main.isTargetKilled.Remove(__instance.PlayerId);
                        main.isTargetKilled.Add(__instance.PlayerId, false);
                    }
                    if(main.BountyTimer[__instance.PlayerId] <= 0.5f && main.BountyTimerCheck){//キルクールを変化させないようにする処理
                        main.BountyTimerCheck = false;
                        main.CustomSyncAllSettings();//ここでの処理をキルクールの変更の処理と同期
                        __instance.ResetBountyTarget();//ターゲットの選びなおし
                    }
                    if(main.BountyTimer[__instance.PlayerId] >= main.BountySuccessKillCoolDown+1 && !main.BountyTimerCheck){//選びなおしてから０．５秒後の処理
                        main.BountyTimerCheck = true;//キルクール変化させないようにする処理をオフ
                        main.isBountyKillSuccess = false;
                        main.CustomSyncAllSettings();//ここでの処理をキルクール変更処理と同期
                    }
                    else//時間を計る処理
                    {
                        main.BountyTimer[__instance.PlayerId] =
                        (main.BountyTimer[__instance.PlayerId] + Time.fixedDeltaTime);
                    }
                }

                if(__instance.AmOwner) main.ApplySuffix();
                if(main.PluginVersionType == VersionTypes.Beta && AmongUsClient.Instance.IsGamePublic) AmongUsClient.Instance.ChangeGamePublic(false);
            }

            //役職テキストの表示
            //TODO:この辺のコードのせいでTOH表示とかが消される
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
                RealName = __instance.getRealName();

                //タスクを終わらせたMadSnitchがインポスターを確認できる
                if(PlayerControl.LocalPlayer.isMadSnitch() && //LocalPlayerがMadSnitch
                    __instance.getCustomRole().isImpostor() && //__instanceがインポスター
                    PlayerControl.LocalPlayer.getPlayerTaskState().isTaskFinished) //LocalPlayerのタスクが終わっている
                {
                    RealName = $"<color={main.getRoleColorCode(CustomRoles.Impostor)}>{RealName}</color>"; //__instanceの名前を赤色で表示
                }

                //タスクを終わらせたSnitchがインポスターを確認できる
                if(PlayerControl.LocalPlayer.isSnitch() && //LocalPlayerがSnitch
                    __instance.getCustomRole().isImpostor() && //__instanceがインポスター
                    PlayerControl.LocalPlayer.getPlayerTaskState().isTaskFinished) //LocalPlayerのタスクが終わっている
                {
                    RealName = $"<color={main.getRoleColorCode(CustomRoles.Impostor)}>{RealName}</color>"; //__instanceの名前を赤色で表示
                }

                //インポスターがタスクが終わりそうなSnitchを確認できる
                if(PlayerControl.LocalPlayer.getCustomRole().isImpostor() && //LocalPlayerがインポスター
                __instance.isSnitch() && __instance.getPlayerTaskState().doExpose //__instanceがタスクが終わりそうなSnitch
                ) {
                    Mark += $"<color={main.getRoleColorCode(CustomRoles.Snitch)}>★</color>"; //Snitch警告をつける
                }

                //タスクが終わりそうなSnitchがいるとき、インポスターに警告が表示される
                if(__instance.AmOwner && __instance.getCustomRole().isImpostor()) { //__instanceがインポスターかつ自分自身
                    foreach(var pc in PlayerControl.AllPlayerControls) { //全員分ループ
                        if(!pc.isSnitch() || pc.Data.IsDead || pc.Data.Disconnected) continue; //(スニッチ以外 || 死者 || 切断者)に用はない 
                        if(pc.getPlayerTaskState().doExpose) { //タスクが終わりそうなSnitchが見つかった時
                            Mark += $"<color={main.getRoleColorCode(CustomRoles.Snitch)}>★</color>"; //Snitch警告を表示
                            break; //無駄なループは行わない
                        }
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
                if(__instance.myPlayer.isSheriff() || __instance.myPlayer.isSKMadmate()) {
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

    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.SetName))]
    class SetNamePatch {
        public static void Postfix(PlayerControl __instance, [HarmonyArgument(0)] string name) {
            main.RealNames[__instance.PlayerId] = name;
        }
    }
}

