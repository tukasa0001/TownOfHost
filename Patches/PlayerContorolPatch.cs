using Hazel;
using HarmonyLib;
using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;
using System.Linq;

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
            if (PlayerState.getDeathReason(target.PlayerId) == PlayerState.DeathReason.etc)
            {
                //死因が設定されていない場合は死亡判定
                PlayerState.setDeathReason(target.PlayerId, PlayerState.DeathReason.Kill);
            }
            //When Bait is killed
            if (target.getCustomRole() == CustomRoles.Bait && __instance.PlayerId != target.PlayerId)
            {
                Logger.SendToFile(target.name + "はBaitだった");
                new LateTask(() => __instance.CmdReportDeadBody(target.Data), 0.15f, "Bait Self Report");
            }
            else
            //BountyHunter
            if (__instance.isBountyHunter()) //キルが発生する前にここの処理をしないとバグる
            {
                main.BountyMeetingCheck = false;//会議後ではないのでキルクールをデフォルトから変更
                if (target == __instance.getBountyTarget())
                {//ターゲットをキルした場合
                    main.isBountyKillSuccess = true;//キルクール減少処理に変換
                    Utils.CustomSyncAllSettings();//キルクール処理を同期
                    main.isTargetKilled.Remove(__instance.PlayerId);
                    main.isTargetKilled.Add(__instance.PlayerId, true);
                }
            }
            if (__instance.isVampire() && CustomRoles.BountyHunter.isEnable()) main.BountyMeetingCheck = false;//会議後ではないのでキルクールをデフォルトから変更
            //Terrorist
            if (target.isTerrorist())
            {
                Logger.SendToFile(target.name + "はTerroristだった");
                Utils.CheckTerroristWin(target.Data);
            }
            Utils.CountAliveImpostors();
            Utils.CustomSyncAllSettings();
            Utils.NotifyRoles();
        }
    }
    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.Shapeshift))]
    class ShapeshiftPatch
    {
        public static void Prefix(PlayerControl __instance, [HarmonyArgument(0)] PlayerControl target)
        {
            if (!AmongUsClient.Instance.AmHost) return;
            if (__instance.isWarlock())
            {
                if (main.CursedPlayers[__instance.PlayerId] != null)//呪われた人がいるか確認
                {
                    if (!main.CheckShapeshift[__instance.PlayerId] && !main.CursedPlayers[__instance.PlayerId].Data.IsDead)//変身解除の時に反応しない
                    {
                        var cp = main.CursedPlayers[__instance.PlayerId];
                        Vector2 cppos = cp.transform.position;//呪われた人の位置
                        Dictionary<PlayerControl, float> cpdistance = new Dictionary<PlayerControl, float>();
                        float dis;
                        foreach (PlayerControl p in PlayerControl.AllPlayerControls)
                        {
                            if (!p.Data.IsDead && p != cp)
                            {
                                dis = Vector2.Distance(cppos, p.transform.position);
                                cpdistance.Add(p, dis);
                                Logger.info($"{p.name}の位置{dis}");
                            }
                        }
                        var min = cpdistance.OrderBy(c => c.Value).FirstOrDefault();//一番小さい値を取り出す
                        PlayerControl targetw = min.Key;
                        Logger.info($"{targetw.name}was killed");
                        cp.RpcMurderPlayer(targetw);//殺す
                        __instance.RpcGuardAndKill(__instance);
                        main.isCurseAndKill[__instance.PlayerId] = false;
                    }
                    main.CursedPlayers[__instance.PlayerId] = (null);
                }
            }
            if (Options.CanMakeMadmateCount.GetSelection() > main.SKMadmateNowCount && !__instance.isWarlock() && !main.CheckShapeshift[__instance.PlayerId])
            {//変身したとき一番近い人をマッドメイトにする処理
                Vector2 __instancepos = __instance.transform.position;//変身者の位置
                Dictionary<PlayerControl, float> mpdistance = new Dictionary<PlayerControl, float>();
                float dis;
                foreach (PlayerControl p in PlayerControl.AllPlayerControls)
                {
                    if (!p.Data.IsDead && p.Data.Role.Role != RoleTypes.Shapeshifter && !p.isImpostor() && !p.isBountyHunter() && !p.isWitch() && !p.isSKMadmate())
                    {
                        dis = Vector2.Distance(__instancepos, p.transform.position);
                        mpdistance.Add(p, dis);
                    }
                }
                if (mpdistance.Count() != 0)
                {
                    var min = mpdistance.OrderBy(c => c.Value).FirstOrDefault();//一番値が小さい
                    PlayerControl targetm = min.Key;
                    targetm.RpcSetCustomRole(CustomRoles.SKMadmate);
                    main.SKMadmateNowCount++;
                    Utils.CustomSyncAllSettings();
                    Utils.NotifyRoles();
                }
            }
            bool check = main.CheckShapeshift[__instance.PlayerId];//変身、変身解除のスイッチ
            main.CheckShapeshift.Remove(__instance.PlayerId);
            main.CheckShapeshift.Add(__instance.PlayerId, !check);
        }
    }
    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.CheckProtect))]
    class CheckProtectPatch
    {
        public static bool Prefix(PlayerControl __instance, [HarmonyArgument(0)] PlayerControl target)
        {
            if (!AmongUsClient.Instance.AmHost) return false;
            Logger.SendToFile("CheckProtect発生: " + __instance.name + "=>" + target.name);
            if (__instance.isSheriff())
            {
                if (__instance.Data.IsDead)
                {
                    Logger.info("守護をブロックしました。");
                    return false;
                }
            }
            return false;
        }
    }
    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.CheckMurder))]
    class CheckMurderPatch
    {
        public static bool Prefix(PlayerControl __instance, [HarmonyArgument(0)] PlayerControl target)
        {
            if (!AmongUsClient.Instance.AmHost) return false;
            Logger.SendToFile("CheckMurder発生: " + __instance.name + "=>" + target.name);
            if (Options.CurrentGameMode == CustomGameMode.HideAndSeek && Options.HideAndSeekKillDelayTimer > 0)
            {
                Logger.info("HideAndSeekの待機時間中だったため、キルをキャンセルしました。");
                return false;
            }

            if (__instance.isSKMadmate()) return false;//シェリフがサイドキックされた場合

            if (main.BlockKilling.TryGetValue(__instance.PlayerId, out bool isBlocked) && isBlocked)
            {
                Logger.info("キルをブロックしました。");
                return false;
            }

            main.BlockKilling[__instance.PlayerId] = true;

            if (__instance.isMafia())
            {
                if (!__instance.CanUseKillButton())
                {
                    Logger.SendToFile(__instance.name + "はMafiaだったので、キルはキャンセルされました。");
                    main.BlockKilling[__instance.PlayerId] = false;
                    return false;
                }
                else
                {
                    Logger.SendToFile(__instance.name + "はMafiaですが、他のインポスターがいないのでキルが許可されました。");
                }
            }
            if (__instance.isSerialKiller() && !target.isSchrodingerCat())
            {
                __instance.RpcMurderPlayer(target);
                __instance.RpcGuardAndKill(target);
                main.SerialKillerTimer.Remove(__instance.PlayerId);
                main.SerialKillerTimer.Add(__instance.PlayerId, 0f);
                return false;
            }
            if (__instance.isSheriff())
            {
                if (__instance.Data.IsDead)
                {
                    main.BlockKilling[__instance.PlayerId] = false;
                    return false;
                }

                if (main.SheriffShotLimit[__instance.PlayerId] == 0)
                {
                    //Logger.info($"シェリフ:{__instance.getRealName()}はキル可能回数に達したため、RoleTypeを守護天使に変更しました。");
                    //__instance.RpcSetRoleDesync(RoleTypes.GuardianAngel);
                    //Utils.hasTasks(__instance.Data, false);
                    //Utils.NotifyRoles();
                    return false;
                }

                main.SheriffShotLimit[__instance.PlayerId]--;
                Logger.info($"{__instance.getRealName()} : 残り{main.SheriffShotLimit[__instance.PlayerId]}発");
                __instance.RpcSetSheriffShotLimit();

                if (!target.canBeKilledBySheriff())
                {
                    PlayerState.setDeathReason(__instance.PlayerId, PlayerState.DeathReason.Misfire);
                    __instance.RpcMurderPlayer(__instance);
                    if (Options.SheriffCanKillCrewmatesAsIt.GetBool())
                        __instance.RpcMurderPlayer(target);

                    return false;
                }
            }
            if (target.isMadGuardian())
            {
                var taskState = target.getPlayerTaskState();
                if (taskState.isTaskFinished)
                {
                    int dataCountBefore = NameColorManager.Instance.NameColors.Count;
                    NameColorManager.Instance.RpcAdd(__instance.PlayerId, target.PlayerId, "#ff0000");
                    if (Options.MadGuardianCanSeeWhoTriedToKill.GetBool())
                        NameColorManager.Instance.RpcAdd(target.PlayerId, __instance.PlayerId, "#ff0000");

                    main.BlockKilling[__instance.PlayerId] = false;
                    if (dataCountBefore != NameColorManager.Instance.NameColors.Count)
                        Utils.NotifyRoles();
                    return false;
                }
            }
            if (__instance.isWitch())
            {
                if (__instance.GetKillOrSpell() && !main.SpelledPlayer.Contains(target))
                {
                    __instance.RpcGuardAndKill(target);
                    main.SpelledPlayer.Add(target);
                }
                main.KillOrSpell[__instance.PlayerId] = !__instance.GetKillOrSpell();
                Utils.NotifyRoles();
                __instance.SyncKillOrSpell();
            }
            if (__instance.isWarlock() && !target.isSchrodingerCat())
            {
                if (!main.CheckShapeshift[__instance.PlayerId] && !main.isCurseAndKill[__instance.PlayerId])
                { //Warlockが変身時以外にキルしたら、呪われる処理
                    main.isCursed = true;
                    Utils.CustomSyncAllSettings();
                    __instance.RpcGuardAndKill(target);
                    main.CursedPlayers[__instance.PlayerId] = (target);
                    main.WarlockTimer.Add(__instance.PlayerId, 0f);
                    main.isCurseAndKill[__instance.PlayerId] = true;
                    return false;
                }
                if (main.CheckShapeshift[__instance.PlayerId])
                {//呪われてる人がいないくて変身してるときに通常キルになる
                    __instance.RpcMurderPlayer(target);
                    __instance.RpcGuardAndKill(target);
                    return false;
                }
                if (main.isCurseAndKill[__instance.PlayerId]) __instance.RpcGuardAndKill(target);
                return false;
            }
            if (__instance.isVampire() && !target.isBait() && !target.isSchrodingerCat())
            { //キルキャンセル&自爆処理
                __instance.RpcGuardAndKill(target);
                main.BitPlayers.Add(target.PlayerId, (__instance.PlayerId, 0f));
                return false;
            }
            if (__instance.isArsonist())
            {
                main.ArsonistKillCooldownCheck = true;
                Utils.CustomSyncAllSettings();
                __instance.RpcGuardAndKill(target);
                if (!main.isDoused[(__instance.PlayerId, target.PlayerId)]) main.ArsonistTimer.Add(__instance.PlayerId, (target, 0f));
                return false;
            }
            //シュレディンガーの猫が切られた場合の役職変化スタート
            if (target.isSchrodingerCat())
            {
                if (__instance.isArsonist()) return false;
                __instance.RpcGuardAndKill(target);
                NameColorManager.Instance.RpcAdd(__instance.PlayerId, target.PlayerId, $"{Utils.getRoleColorCode(CustomRoles.SchrodingerCat)}");
                if (__instance.getCustomRole().isImpostor())
                    target.RpcSetCustomRole(CustomRoles.MSchrodingerCat);
                if (__instance.isSheriff())
                    target.RpcSetCustomRole(CustomRoles.CSchrodingerCat);
                if (__instance.isEgoist())
                    target.RpcSetCustomRole(CustomRoles.EgoSchrodingerCat);
                Utils.NotifyRoles();
                Utils.CustomSyncAllSettings();
                return false;
            }
            //シュレディンガーの猫の役職変化処理終了
            //第三陣営キル能力持ちが追加されたら、その陣営を味方するシュレディンガーの猫の役職を作って上と同じ書き方で書いてください


            //==キル処理==
            __instance.RpcMurderPlayer(target);
            //============
            return false;
        }
    }
    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.ReportDeadBody))]
    class ReportDeadBodyPatch
    {
        public static bool Prefix(PlayerControl __instance, [HarmonyArgument(0)] GameData.PlayerInfo target)
        {
            if (Options.CurrentGameMode == CustomGameMode.HideAndSeek) return false;
            if (!AmongUsClient.Instance.AmHost) return true;
            main.BountyTimer.Clear();
            main.SerialKillerTimer.Clear();
            if (target != null)
            {
                Logger.info($"{__instance.name} => {target.PlayerName}");
                if (main.IgnoreReportPlayers.Contains(target.PlayerId))
                {
                    Logger.info($"{target.PlayerName}は通報が禁止された死体なのでキャンセルされました");
                    return false;
                }
            }

            if (Options.SyncButtonMode.GetBool() && target == null)
            {
                Logger.SendToFile("最大:" + Options.SyncedButtonCount + ", 現在:" + Options.UsedButtonCount, LogLevel.Message);
                if (Options.SyncedButtonCount.GetSelection() <= Options.UsedButtonCount)
                {
                    Logger.SendToFile("使用可能ボタン回数が最大数を超えているため、ボタンはキャンセルされました。", LogLevel.Message);
                    return false;
                }
                else Options.UsedButtonCount++;
                if (Options.SyncedButtonCount.GetSelection() == Options.UsedButtonCount)
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
                        PlayerState.setDeathReason(pc.PlayerId, PlayerState.DeathReason.Bite);
                        pc.RpcMurderPlayer(pc);
                        RPC.PlaySoundRPC(bp.Value.Item1, Sounds.KillSound);
                        Logger.SendToFile("Vampireに噛まれている" + pc.name + "を自爆させました。");
                    }
                    else
                        Logger.SendToFile("Vampireに噛まれている" + pc.name + "はすでに死んでいました。");
                }
            }
            main.BitPlayers = new Dictionary<byte, (byte, float)>();

            if (__instance.Data.IsDead) return true;
            //=============================================
            //以下、ボタンが押されることが確定したものとする。
            //=============================================

            if (Options.SyncButtonMode.GetBool() && AmongUsClient.Instance.AmHost && PlayerControl.LocalPlayer.Data.IsDead)
            {
                //SyncButtonMode中にホストが死んでいる場合
                ChangeLocalNameAndRevert(
                    "緊急会議ボタンはあと" + (Options.SyncedButtonCount.GetSelection() - Options.UsedButtonCount) + "回使用可能です。",
                    1000
                );
            }
            foreach (var sp in main.SpelledPlayer)
            {
                sp.RpcSetName("<color=#ff0000>†</color>" + sp.getRealName());
            }

            Utils.CustomSyncAllSettings();
            return true;
        }
        public static async void ChangeLocalNameAndRevert(string name, int time)
        {
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
                    if (main.BitPlayers[__instance.PlayerId].Item2 >= Options.VampireKillDelay.GetFloat())
                    {
                        byte vampireID = main.BitPlayers[__instance.PlayerId].Item1;
                        if (!__instance.Data.IsDead)
                        {
                            PlayerState.setDeathReason(__instance.PlayerId, PlayerState.DeathReason.Bite);
                            __instance.RpcMurderPlayer(__instance);
                            RPC.PlaySoundRPC(vampireID, Sounds.KillSound);
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
                if (main.SerialKillerTimer.ContainsKey(__instance.PlayerId))
                {
                    if (main.SerialKillerTimer[__instance.PlayerId] >= Options.SerialKillerLimit.GetFloat())
                    {
                        if (!__instance.Data.IsDead)
                        {
                            PlayerState.setDeathReason(__instance.PlayerId, PlayerState.DeathReason.Suicide);
                            __instance.RpcMurderPlayer(__instance);
                            RPC.PlaySoundRPC(__instance.PlayerId, Sounds.KillSound);
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
                if (main.WarlockTimer.ContainsKey(__instance.PlayerId))
                {
                    if (main.WarlockTimer[__instance.PlayerId] >= 1f)
                    {
                        __instance.RpcGuardAndKill(__instance);
                        main.isCursed = false;
                        Utils.CustomSyncAllSettings();
                        main.WarlockTimer.Remove(__instance.PlayerId);
                    }
                    else main.WarlockTimer[__instance.PlayerId] = (main.WarlockTimer[__instance.PlayerId] + Time.fixedDeltaTime);
                }
                //バウハンのキルクールの変換とターゲットのリセット
                if (main.BountyTimer.ContainsKey(__instance.PlayerId))
                {
                    if (main.BountyTimer[__instance.PlayerId] >= Options.BountyTargetChangeTime.GetFloat())//時間経過でターゲットをリセットする処理
                    {
                        main.BountyMeetingCheck = false;
                        __instance.RpcGuardAndKill(__instance);//タイマー（変身クールダウン）のリセットと、名前の変更のためのKill
                        main.BountyTimer.Remove(__instance.PlayerId);//時間リセット
                        main.BountyTimer.Add(__instance.PlayerId, 0f);
                        main.BountyTimerCheck = true;//キルクールを０にする処理に行かせるための処理
                    }
                    if (main.isTargetKilled[__instance.PlayerId])//ターゲットをキルした場合
                    {
                        __instance.RpcGuardAndKill(__instance);//守護天使バグ対策で上の処理のターゲットをキル対象に変更
                        main.BountyTimer.Remove(__instance.PlayerId);//それ以外上に同じ
                        main.BountyTimer.Add(__instance.PlayerId, 0f);
                        main.BountyTimerCheck = true;
                        main.isTargetKilled.Remove(__instance.PlayerId);
                        main.isTargetKilled.Add(__instance.PlayerId, false);
                    }
                    if (main.BountyTimer[__instance.PlayerId] <= 1 && main.BountyTimerCheck)
                    {//キルクールを変化させないようにする処理
                        main.BountyTimerCheck = false;
                        Utils.CustomSyncAllSettings();//ここでの処理をキルクールの変更の処理と同期
                        __instance.ResetBountyTarget();//ターゲットの選びなおし
                    }
                    if (main.BountyTimer[__instance.PlayerId] >= 1 && !main.BountyTimerCheck)
                    {//選びなおしてから１秒後の処理
                        main.BountyTimerCheck = true;//キルクール変化させないようにする処理をオフ
                        main.isBountyKillSuccess = false;//キルクールをターゲット以外をキルした時の場合に変更
                        Utils.CustomSyncAllSettings();//ここでの処理をキルクール変更処理と同期
                    }
                    else//時間を計る処理
                    {
                        main.BountyTimer[__instance.PlayerId] =
                        (main.BountyTimer[__instance.PlayerId] + Time.fixedDeltaTime);
                    }
                }
                if (main.ArsonistTimer.ContainsKey(__instance.PlayerId))
                {
                    var artarget = main.ArsonistTimer[__instance.PlayerId].Item1;
                    if (main.ArsonistTimer[__instance.PlayerId].Item2 >= Options.ArsonistDouseTime.GetFloat())
                    {
                        __instance.RpcGuardAndKill(artarget);
                        main.ArsonistKillCooldownCheck = false;
                        Utils.CustomSyncAllSettings();
                        main.ArsonistTimer.Remove(__instance.PlayerId);
                        main.isDoused[(__instance.PlayerId, artarget.PlayerId)] = true;
                        main.DousedPlayerCount[__instance.PlayerId]--;
                        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetDousedPlayer, SendOption.Reliable, -1);
                        writer.Write(__instance.PlayerId);
                        writer.Write(artarget.PlayerId);
                        writer.Write(true);
                        AmongUsClient.Instance.FinishRpcImmediately(writer);
                        Utils.NotifyRoles();
                    }
                    else
                    {
                        float dis;
                        dis = Vector2.Distance(__instance.transform.position, artarget.transform.position);
                        if (dis <= 1.75f)
                        {
                            main.ArsonistTimer[__instance.PlayerId] =
                            (main.ArsonistTimer[__instance.PlayerId].Item1, main.ArsonistTimer[__instance.PlayerId].Item2 + Time.fixedDeltaTime);
                        }
                        else
                        {
                            main.ArsonistTimer.Remove(__instance.PlayerId);
                        }
                    }
                }
                if (main.DousedPlayerCount.ContainsKey(__instance.PlayerId) && AmongUsClient.Instance.IsGameStarted)
                {
                    if (main.DousedPlayerCount[__instance.PlayerId] == 0)
                    {
                        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.ArsonistWin, Hazel.SendOption.Reliable, -1);
                        writer.Write(__instance.PlayerId);
                        AmongUsClient.Instance.FinishRpcImmediately(writer);
                        RPC.ArsonistWin(__instance.PlayerId);
                        main.DousedPlayerCount[__instance.PlayerId] = 1;
                    }
                    else
                    {
                        foreach (var pc in PlayerControl.AllPlayerControls)
                        {
                            if ((pc.Data.IsDead || pc.Data.Disconnected) && !main.isDoused[(__instance.PlayerId, pc.PlayerId)])
                            {
                                main.DousedPlayerCount[__instance.PlayerId]--;
                                main.isDoused[(__instance.PlayerId, pc.PlayerId)] = true;
                            }
                        }
                    }
                }

                if (__instance.AmOwner) Utils.ApplySuffix();
                if (main.PluginVersionType == VersionTypes.Beta && AmongUsClient.Instance.IsGamePublic) AmongUsClient.Instance.ChangeGamePublic(false);
            }

            //役職テキストの表示
            var RoleTextTransform = __instance.nameText.transform.Find("RoleText");
            var RoleText = RoleTextTransform.GetComponent<TMPro.TextMeshPro>();
            if (RoleText != null && __instance != null)
            {
                var RoleTextData = Utils.GetRoleText(__instance);
                if (Options.CurrentGameMode == CustomGameMode.HideAndSeek)
                {
                    var hasRole = main.AllPlayerCustomRoles.TryGetValue(__instance.PlayerId, out var role);
                    if (hasRole) RoleTextData = Utils.GetRoleTextHideAndSeek(__instance.Data.Role.Role, role);
                }
                RoleText.text = RoleTextData.Item1;
                RoleText.color = RoleTextData.Item2;
                if (__instance.AmOwner) RoleText.enabled = true; //自分ならロールを表示
                else if (main.VisibleTasksCount && PlayerControl.LocalPlayer.Data.IsDead) RoleText.enabled = true; //他プレイヤーでVisibleTasksCountが有効なおかつ自分が死んでいるならロールを表示
                else RoleText.enabled = false; //そうでなければロールを非表示
                if (!AmongUsClient.Instance.IsGameStarted && AmongUsClient.Instance.GameMode != GameModes.FreePlay)
                {
                    RoleText.enabled = false; //ゲームが始まっておらずフリープレイでなければロールを非表示
                    if (!__instance.AmOwner) __instance.nameText.text = __instance.name;
                }
                if (main.VisibleTasksCount && Utils.hasTasks(__instance.Data, false)) //他プレイヤーでVisibleTasksCountは有効なおかつタスクがあるなら
                    RoleText.text += $" <color=#e6b422>({Utils.getTaskText(__instance)})</color>"; //ロールの横にタスク表示


                //変数定義
                string RealName;
                string Mark = "";
                string Suffix = "";

                //名前変更
                RealName = __instance.getRealName();


                //名前色変更処理
                //自分自身の名前の色を変更
                if (__instance.AmOwner && AmongUsClient.Instance.IsGameStarted)
                { //__instanceが自分自身
                    RealName = $"<color={__instance.getRoleColorCode()}>{RealName}</color>"; //名前の色を変更
                }
                //タスクを終わらせたMadSnitchがインポスターを確認できる
                else if (PlayerControl.LocalPlayer.isMadSnitch() && //LocalPlayerがMadSnitch
                    __instance.getCustomRole().isImpostor() && //__instanceがインポスター
                    PlayerControl.LocalPlayer.getPlayerTaskState().isTaskFinished) //LocalPlayerのタスクが終わっている
                {
                    RealName = $"<color={Utils.getRoleColorCode(CustomRoles.Impostor)}>{RealName}</color>"; //__instanceの名前を赤色で表示
                }
                //タスクを終わらせたSnitchがインポスターを確認できる
                else if (PlayerControl.LocalPlayer.isSnitch() && //LocalPlayerがSnitch
                    __instance.getCustomRole().isImpostor() && //__instanceがインポスター
                    PlayerControl.LocalPlayer.getPlayerTaskState().isTaskFinished) //LocalPlayerのタスクが終わっている
                {
                    RealName = $"<color={Utils.getRoleColorCode(CustomRoles.Impostor)}>{RealName}</color>"; //__instanceの名前を赤色で表示
                }
                else if (PlayerControl.LocalPlayer.getCustomRole().isImpostor() && //LocalPlayerがインポスター
                    __instance.isEgoist() //__instanceがエゴイスト
                )
                    RealName = $"<color={Utils.getRoleColorCode(CustomRoles.Egoist)}>{RealName}</color>"; //__instanceの名前をエゴイスト色で表示
                else if (PlayerControl.LocalPlayer.isEgoSchrodingerCat() && //LocalPlayerがエゴイスト陣営のシュレディンガーの猫
                    __instance.isEgoist() //__instanceがエゴイスト
                )
                    RealName = $"<color={Utils.getRoleColorCode(CustomRoles.Egoist)}>{RealName}</color>"; //__instanceの名前をエゴイスト色で表示
                else if (PlayerControl.LocalPlayer != null)
                {//NameColorManager準拠の処理
                    var ncd = NameColorManager.Instance.GetData(PlayerControl.LocalPlayer.PlayerId, __instance.PlayerId);
                    RealName = ncd.OpenTag + RealName + ncd.CloseTag;
                }

                //インポスターがタスクが終わりそうなSnitchを確認できる
                if (PlayerControl.LocalPlayer.getCustomRole().isImpostor() && //LocalPlayerがインポスター
                __instance.isSnitch() && __instance.getPlayerTaskState().doExpose //__instanceがタスクが終わりそうなSnitch
                )
                {
                    Mark += $"<color={Utils.getRoleColorCode(CustomRoles.Snitch)}>★</color>"; //Snitch警告をつける
                }
                if (PlayerControl.LocalPlayer.isArsonist() && PlayerControl.LocalPlayer.isDousedPlayer(__instance))
                {
                    Mark += $"<color={Utils.getRoleColorCode(CustomRoles.Arsonist)}>▲</color>";
                }

                //タスクが終わりそうなSnitchがいるとき、インポスターに警告が表示される
                if (__instance.AmOwner && __instance.getCustomRole().isImpostor())
                { //__instanceがインポスターかつ自分自身
                    foreach (var pc in PlayerControl.AllPlayerControls)
                    { //全員分ループ
                        if (!pc.isSnitch() || pc.Data.IsDead || pc.Data.Disconnected) continue; //(スニッチ以外 || 死者 || 切断者)に用はない 
                        if (pc.getPlayerTaskState().doExpose)
                        { //タスクが終わりそうなSnitchが見つかった時
                            Mark += $"<color={Utils.getRoleColorCode(CustomRoles.Snitch)}>★</color>"; //Snitch警告を表示
                            break; //無駄なループは行わない
                        }
                    }
                }

                /*if(main.AmDebugger.Value && main.BlockKilling.TryGetValue(__instance.PlayerId, out var isBlocked)) {
                    Mark = isBlocked ? "(true)" : "(false)";
                }*/

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
    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.SetColor))]
    class SetColorPatch
    {
        public static bool IsAntiGlitchDisabled = false;
        public static bool Prefix(PlayerControl __instance, int bodyColor)
        {
            //色変更バグ対策
            if (!AmongUsClient.Instance.AmHost || __instance.CurrentOutfit.ColorId == bodyColor || IsAntiGlitchDisabled) return true;
            if (AmongUsClient.Instance.IsGameStarted && Options.CurrentGameMode == CustomGameMode.HideAndSeek)
            {
                //ゲーム中に色を変えた場合
                __instance.RpcMurderPlayer(__instance);
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(Vent), nameof(Vent.EnterVent))]
    class EnterVentPatch
    {
        public static void Postfix(Vent __instance, [HarmonyArgument(0)] PlayerControl pc)
        {
            if (Options.CurrentGameMode == CustomGameMode.HideAndSeek && Options.IgnoreVent.GetBool())
                pc.MyPhysics.RpcBootFromVent(__instance.Id);
        }
    }
    [HarmonyPatch(typeof(PlayerPhysics), nameof(PlayerPhysics.CoEnterVent))]
    class CoEnterVentPatch
    {
        public static bool Prefix(PlayerPhysics __instance, [HarmonyArgument(0)] int id)
        {
            if (AmongUsClient.Instance.AmHost)
            {
                if (__instance.myPlayer.isSheriff() || __instance.myPlayer.isSKMadmate() || __instance.myPlayer.isArsonist())
                {
                    MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(__instance.NetId, (byte)RpcCalls.BootFromVent, SendOption.Reliable, -1);
                    writer.WritePacked(127);
                    AmongUsClient.Instance.FinishRpcImmediately(writer);
                    new LateTask(() =>
                    {
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
    class SetNamePatch
    {
        public static void Postfix(PlayerControl __instance, [HarmonyArgument(0)] string name)
        {
            main.RealNames[__instance.PlayerId] = name;
        }
    }
}

