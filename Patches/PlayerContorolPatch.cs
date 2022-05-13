using Hazel;
using HarmonyLib;
using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;
using System.Linq;
using static TownOfHost.Translator;

namespace TownOfHost
{
    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.MurderPlayer))]
    class MurderPlayerPatch
    {
        public static void Postfix(PlayerControl __instance, [HarmonyArgument(0)] PlayerControl target)
        {
            Logger.info($"{__instance.getNameWithRole()} => {target.getNameWithRole()}", "MurderPlayer");
            if (!target.Data.IsDead || !AmongUsClient.Instance.AmHost)
                return;
            if (PlayerState.getDeathReason(target.PlayerId) == PlayerState.DeathReason.etc)
            {
                //死因が設定されていない場合は死亡判定
                PlayerState.setDeathReason(target.PlayerId, PlayerState.DeathReason.Kill);
            }
            //When Bait is killed
            if (target.getCustomRole() == CustomRoles.Bait && __instance.PlayerId != target.PlayerId)
            {
                Logger.info(target.Data.PlayerName + "はBaitだった", "MurderPlayer");
                new LateTask(() => __instance.CmdReportDeadBody(target.Data), 0.15f, "Bait Self Report");
            }
            else
            //BountyHunter
            if (__instance.Is(CustomRoles.BountyHunter)) //キルが発生する前にここの処理をしないとバグる
            {
                if (target == __instance.getBountyTarget())
                {//ターゲットをキルした場合
                    main.AllPlayerKillCooldown[__instance.PlayerId] = Options.BountySuccessKillCooldown.GetFloat() * 2;
                    Utils.CustomSyncAllSettings();//キルクール処理を同期
                    main.isTargetKilled.Remove(__instance.PlayerId);
                    main.isTargetKilled.Add(__instance.PlayerId, true);
                    Logger.info($"{__instance.Data.PlayerName}:ターゲットをキル", "BountyHunter");
                }
                else
                {
                    main.AllPlayerKillCooldown[__instance.PlayerId] = Options.BountyFailureKillCooldown.GetFloat();
                    Logger.info($"{__instance.Data.PlayerName}:ターゲット以外をキル", "BountyHunter");
                    Utils.CustomSyncAllSettings();//キルクール処理を同期
                }
            }
            if (__instance.Is(CustomRoles.SerialKiller))
            {
                main.AllPlayerKillCooldown[__instance.PlayerId] = Options.SerialKillerCooldown.GetFloat() * 2;
                __instance.CustomSyncSettings();
            }
            //Terrorist
            if (target.Is(CustomRoles.Terrorist))
            {
                Logger.info(target.Data.PlayerName + "はTerroristだった", "MurderPlayer");
                Utils.CheckTerroristWin(target.Data);
            }
            if (target.Is(CustomRoles.Trapper) && !__instance.Is(CustomRoles.Trapper))
                __instance.TrapperKilled(target);
            if (main.ExecutionerTarget.ContainsValue(target.PlayerId))
            {
                List<byte> RemoveExecutionerKey = new();
                foreach (var ExecutionerTarget in main.ExecutionerTarget)
                {
                    var executioner = Utils.getPlayerById(ExecutionerTarget.Key);
                    if (target.PlayerId == ExecutionerTarget.Value && !executioner.Data.IsDead)
                    {
                        executioner.RpcSetCustomRole(Options.CRoleExecutionerChangeRoles[Options.ExecutionerChangeRolesAfterTargetKilled.GetSelection()]); //対象がキルされたらオプションで設定した役職にする
                        RemoveExecutionerKey.Add(ExecutionerTarget.Key);
                    }
                }
                foreach (var RemoveKey in RemoveExecutionerKey)
                {
                    main.ExecutionerTarget.Remove(RemoveKey);
                    RPC.removeExecutionerKey(RemoveKey);
                }
            }
            if (target.Is(CustomRoles.TimeThief))
                target.ResetThiefVotingTime();
            if (!main.isDeadDoused[target.PlayerId])
                target.RemoveDousePlayer();


            foreach (var pc in PlayerControl.AllPlayerControls)
            {
                if (pc.isLastImpostor())
                    main.AllPlayerKillCooldown[pc.PlayerId] = Options.LastImpostorKillCooldown.GetFloat();
            }
            FixedUpdatePatch.LoversSuicide(target.PlayerId);

            main.LastKiller.Remove(target);

            PlayerState.setDead(target.PlayerId);
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
            if (__instance.Is(CustomRoles.Warlock))
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
                                Logger.info($"{p.Data.PlayerName}の位置{dis}", "Warlock");
                            }
                        }
                        var min = cpdistance.OrderBy(c => c.Value).FirstOrDefault();//一番小さい値を取り出す
                        PlayerControl targetw = min.Key;
                        Logger.info($"{targetw.getNameWithRole()}was killed", "Warlock");
                        cp.RpcMurderPlayer(targetw);//殺す
                        __instance.RpcGuardAndKill(__instance);
                        main.isCurseAndKill[__instance.PlayerId] = false;
                    }
                    main.CursedPlayers[__instance.PlayerId] = (null);
                }
            }
            if (Options.CanMakeMadmateCount.GetFloat() > main.SKMadmateNowCount && !!__instance.Is(CustomRoles.Warlock) && !__instance.Is(CustomRoles.FireWorks) && !main.CheckShapeshift[__instance.PlayerId])
            {//変身したとき一番近い人をマッドメイトにする処理
                Vector2 __instancepos = __instance.transform.position;//変身者の位置
                Dictionary<PlayerControl, float> mpdistance = new Dictionary<PlayerControl, float>();
                float dis;
                foreach (PlayerControl p in PlayerControl.AllPlayerControls)
                {
                    if (!p.Data.IsDead && p.Data.Role.Role != RoleTypes.Shapeshifter && !p.Is(CustomRoles.Impostor) && !p.Is(CustomRoles.BountyHunter) && !p.Is(CustomRoles.Witch) && !p.Is(CustomRoles.SKMadmate))
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
            if (__instance.Is(CustomRoles.FireWorks)) FireWorks.ShapeShiftState(__instance, main.CheckShapeshift[__instance.PlayerId]);
            if (__instance.Is(CustomRoles.Sniper)) Sniper.ShapeShiftCheck(__instance, main.CheckShapeshift[__instance.PlayerId]);

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
            Logger.info("CheckProtect発生: " + __instance.getNameWithRole() + "=>" + target.getNameWithRole(), "CheckProtect");
            if (__instance.Is(CustomRoles.Sheriff))
            {
                if (__instance.Data.IsDead)
                {
                    Logger.info("守護をブロックしました。", "CheckProtect");
                    return false;
                }
            }
            return true;
        }
    }
    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.CheckMurder))]
    class CheckMurderPatch
    {
        public static bool Prefix(PlayerControl __instance, [HarmonyArgument(0)] PlayerControl target)
        {
            if (!AmongUsClient.Instance.AmHost) return false;
            if (main.AirshipMeetingCheck)
            {
                main.AirshipMeetingCheck = false;
                Utils.CustomSyncAllSettings();
            }
            main.LastKiller[target] = __instance;
            Logger.info($"{__instance.getNameWithRole()} => {target.getNameWithRole()}", "CheckMurder");
            if (__instance.PlayerId == target.PlayerId)
            {
                //自殺ならノーチェック
                __instance.RpcMurderPlayer(target);
                return false;
            }
            if (Options.CurrentGameMode == CustomGameMode.HideAndSeek && Options.HideAndSeekKillDelayTimer > 0)
            {
                Logger.info("HideAndSeekの待機時間中だったため、キルをキャンセルしました。", "CheckMurder");
                return false;
            }
            if (__instance.Is(CustomRoles.SKMadmate)) return false;//シェリフがサイドキックされた場合

            if (main.BlockKilling.TryGetValue(__instance.PlayerId, out bool isBlocked) && isBlocked)
            {
                Logger.info("キルをブロックしました。", "CheckMurder");
                return false;
            }

            main.BlockKilling[__instance.PlayerId] = true;

            if (__instance.Is(CustomRoles.FireWorks))
            {
                if (!__instance.CanUseKillButton())
                {
                    main.BlockKilling[__instance.PlayerId] = false;
                    return false;
                }
            }
            if (__instance.Is(CustomRoles.Sniper))
            {
                if (!__instance.CanUseKillButton())
                {
                    main.BlockKilling[__instance.PlayerId] = false;
                    return false;
                }
            }
            if (__instance.Is(CustomRoles.Mafia))
            {
                if (!__instance.CanUseKillButton())
                {
                    Logger.info(__instance.Data.PlayerName + "はMafiaだったので、キルはキャンセルされました。", "CheckMurder");
                    main.BlockKilling[__instance.PlayerId] = false;
                    return false;
                }
                else
                {
                    Logger.info(__instance.Data.PlayerName + "はMafiaですが、他のインポスターがいないのでキルが許可されました。", "CheckMurder");
                }
            }
            if (__instance.Is(CustomRoles.SerialKiller) && !target.Is(CustomRoles.SchrodingerCat))
            {
                __instance.RpcMurderPlayer(target);
                __instance.RpcGuardAndKill(target);
                main.SerialKillerTimer.Remove(__instance.PlayerId);
                main.SerialKillerTimer.Add(__instance.PlayerId, 0f);
                return false;
            }
            if (__instance.Is(CustomRoles.Puppeteer))
            {
                main.PuppeteerList[target.PlayerId] = __instance.PlayerId;
                main.AllPlayerKillCooldown[__instance.PlayerId] = Options.BHDefaultKillCooldown.GetFloat() * 2;
                __instance.CustomSyncSettings(); //負荷軽減のため、__instanceだけがCustomSyncSettingsを実行
                __instance.RpcGuardAndKill(target);
                return false;
            }
            if (__instance.Is(CustomRoles.Sheriff))
            {
                if (__instance.Data.IsDead)
                {
                    main.BlockKilling[__instance.PlayerId] = false;
                    return false;
                }

                if (main.SheriffShotLimit[__instance.PlayerId] == 0)
                {
                    //Logger.info($"シェリフ:{__instance.name}はキル可能回数に達したため、RoleTypeを守護天使に変更しました。");
                    //__instance.RpcSetRoleDesync(RoleTypes.GuardianAngel);
                    //Utils.hasTasks(__instance.Data, false);
                    //Utils.NotifyRoles();
                    return false;
                }

                main.SheriffShotLimit[__instance.PlayerId]--;
                Logger.info($"{__instance.getNameWithRole()} : 残り{main.SheriffShotLimit[__instance.PlayerId]}発", "Sheriff");
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
            if (target.Is(CustomRoles.MadGuardian))
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
            if (__instance.Is(CustomRoles.Witch))
            {
                if (__instance.GetKillOrSpell() && !main.SpelledPlayer.Contains(target))
                {
                    __instance.RpcGuardAndKill(target);
                    main.SpelledPlayer.Add(target);
                    RPC.RpcDoSpell(target.PlayerId);
                }
                main.KillOrSpell[__instance.PlayerId] = !__instance.GetKillOrSpell();
                Utils.NotifyRoles();
                __instance.SyncKillOrSpell();
            }
            if (__instance.Is(CustomRoles.Warlock) && !target.Is(CustomRoles.SchrodingerCat))
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
            if (__instance.Is(CustomRoles.Vampire) && !target.Is(CustomRoles.Bait) && !target.Is(CustomRoles.SchrodingerCat))
            { //キルキャンセル&自爆処理
                Utils.CustomSyncAllSettings();
                __instance.RpcGuardAndKill(target);
                main.BitPlayers.Add(target.PlayerId, (__instance.PlayerId, 0f));
                return false;
            }
            if (__instance.Is(CustomRoles.Arsonist))
            {
                main.AllPlayerKillCooldown[__instance.PlayerId] = 10f;
                Utils.CustomSyncAllSettings();
                __instance.RpcGuardAndKill(target);
                if (!main.isDoused[(__instance.PlayerId, target.PlayerId)]) main.ArsonistTimer.Add(__instance.PlayerId, (target, 0f));
                return false;
            }
            //シュレディンガーの猫が切られた場合の役職変化スタート
            if (target.Is(CustomRoles.SchrodingerCat))
            {
                if (__instance.Is(CustomRoles.Arsonist)) return false;
                __instance.RpcGuardAndKill(target);
                NameColorManager.Instance.RpcAdd(__instance.PlayerId, target.PlayerId, $"{Utils.getRoleColorCode(CustomRoles.SchrodingerCat)}");
                if (__instance.getCustomRole().isImpostor())
                    target.RpcSetCustomRole(CustomRoles.MSchrodingerCat);
                if (__instance.Is(CustomRoles.Sheriff))
                    target.RpcSetCustomRole(CustomRoles.CSchrodingerCat);
                if (__instance.Is(CustomRoles.Egoist))
                    target.RpcSetCustomRole(CustomRoles.EgoSchrodingerCat);
                Utils.NotifyRoles();
                Utils.CustomSyncAllSettings();
                return false;
            }
            //シュレディンガーの猫の役職変化処理終了
            //第三陣営キル能力持ちが追加されたら、その陣営を味方するシュレディンガーの猫の役職を作って上と同じ書き方で書いてください
            if (__instance.Is(CustomRoles.Mare))
            {
                if (!__instance.CanUseKillButton())
                {
                    Logger.info(__instance.Data.PlayerName + "のキルは停電中ではなかったので、キルはキャンセルされました。", "Mare");
                    main.BlockKilling[__instance.PlayerId] = false;
                    return false;
                }
                else
                {
                    Logger.info(__instance.Data.PlayerName + "はMareですが、停電中だったのでキルが許可されました。", "Mare");
                }
            }
            if (__instance.Is(CustomRoles.TimeThief))
            {
                main.TimeThiefKillCount[__instance.PlayerId]++;
                __instance.RpcSetTimeThiefKillCount();
                if (main.DiscussionTime > 0)
                    main.DiscussionTime -= Options.TimeThiefDecreaseDiscussionTime.GetInt();
                else
                    main.VotingTime -= Options.TimeThiefDecreaseVotingTime.GetInt();
                Utils.CustomSyncAllSettings();
            }

            //==キル処理==
            __instance.RpcMurderPlayer(target);
            //============

            if (__instance.Is(CustomRoles.BountyHunter) && target != __instance.getBountyTarget())
            {
                __instance.RpcGuardAndKill(target);
                __instance.ResetBountyTarget();
                main.BountyTimer[__instance.PlayerId] = 0f;
            }

            return false;
        }
    }
    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.ReportDeadBody))]
    class ReportDeadBodyPatch
    {
        public static bool Prefix(PlayerControl __instance, [HarmonyArgument(0)] GameData.PlayerInfo target)
        {
            Logger.info($"{__instance.getNameWithRole()} => {target?.getNameWithRole() ?? "null"}", "ReportDeadBody");
            if (Options.StandardHAS.GetBool() && target != null && __instance == target.Object) return true; //[StandardHAS] ボタンでなく、通報者と死体が同じなら許可
            if (Options.CurrentGameMode == CustomGameMode.HideAndSeek || Options.StandardHAS.GetBool()) return false;
            if (!AmongUsClient.Instance.AmHost) return true;
            main.BountyTimer.Clear();
            main.SerialKillerTimer.Clear();
            if (target != null)
            {
                if (main.IgnoreReportPlayers.Contains(target.PlayerId) && !CheckForEndVotingPatch.recall)
                {
                    Logger.info($"{target.PlayerName}は通報が禁止された死体なのでキャンセルされました", "ReportDeadBody");
                    return false;
                }
            }

            if (Options.SyncButtonMode.GetBool() && target == null)
            {
                Logger.info("最大:" + Options.SyncedButtonCount + ", 現在:" + Options.UsedButtonCount, "ReportDeadBody");
                if (Options.SyncedButtonCount.GetFloat() <= Options.UsedButtonCount)
                {
                    Logger.info("使用可能ボタン回数が最大数を超えているため、ボタンはキャンセルされました。", "ReportDeadBody");
                    return false;
                }
                else Options.UsedButtonCount++;
                if (Options.SyncedButtonCount.GetFloat() == Options.UsedButtonCount)
                {
                    Logger.info("使用可能ボタン回数が最大数に達しました。", "ReportDeadBody");
                }
            }

            foreach (var bp in main.BitPlayers)
            {
                var vampireID = bp.Value.Item1;
                var bitten = Utils.getPlayerById(bp.Key);
                //vampireのキルブロック解除
                main.BlockKilling[vampireID] = false;
                if (!bitten.Data.IsDead)
                {
                    PlayerState.setDeathReason(bitten.PlayerId, PlayerState.DeathReason.Bite);
                    bitten.RpcMurderPlayer(bitten);
                    RPC.PlaySoundRPC(vampireID, Sounds.KillSound);
                    Logger.info("Vampireに噛まれている" + bitten.Data.PlayerName + "を自爆させました。", "ReportDeadBody");
                }
                else
                    Logger.info("Vampireに噛まれている" + bitten.Data.PlayerName + "はすでに死んでいました。", "ReportDeadBody");
            }
            main.BitPlayers = new Dictionary<byte, (byte, float)>();
            main.PuppeteerList.Clear();

            if (__instance.Data.IsDead) return true;
            //=============================================
            //以下、ボタンが押されることが確定したものとする。
            //=============================================

            if (Options.SyncButtonMode.GetBool() && AmongUsClient.Instance.AmHost && PlayerControl.LocalPlayer.Data.IsDead)
            {
                //SyncButtonMode中にホストが死んでいる場合
                ChangeLocalNameAndRevert(
                    "緊急会議ボタンはあと" + (Options.SyncedButtonCount.GetFloat() - Options.UsedButtonCount) + "回使用可能です。",
                    1000
                );
            }

            Utils.CustomSyncAllSettings();
            return true;
        }
        public static async void ChangeLocalNameAndRevert(string name, int time)
        {
            //async Taskじゃ警告出るから仕方ないよね。
            var revertName = PlayerControl.LocalPlayer.name;
            PlayerControl.LocalPlayer.RpcSetNameEx(name);
            await Task.Delay(time);
            PlayerControl.LocalPlayer.RpcSetNameEx(revertName);
        }
    }
    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.FixedUpdate))]
    class FixedUpdatePatch
    {
        public static void Postfix(PlayerControl __instance)
        {
            if (AmongUsClient.Instance.AmHost)
            {//実行クライアントがホストの場合のみ実行
                if (GameStates.isLobby && ModUpdater.hasUpdate && AmongUsClient.Instance.IsGamePublic)
                    AmongUsClient.Instance.ChangeGamePublic(false);
                if (GameStates.isInTask && CustomRoles.Vampire.isEnable())
                {
                    //Vampireの処理
                    if (main.BitPlayers.ContainsKey(__instance.PlayerId))
                    {
                        //__instance:キルされる予定のプレイヤー
                        //main.BitPlayers[__instance.PlayerId].Item1:キルしたプレイヤーのID
                        //main.BitPlayers[__instance.PlayerId].Item2:キルするまでの秒数
                        if (main.BitPlayers[__instance.PlayerId].Item2 >= Options.VampireKillDelay.GetFloat())
                        {
                            byte vampireID = main.BitPlayers[__instance.PlayerId].Item1;
                            var bitten = __instance;
                            //vampireのキルブロック解除
                            main.BlockKilling[vampireID] = false;
                            if (!bitten.Data.IsDead)
                            {
                                PlayerState.setDeathReason(bitten.PlayerId, PlayerState.DeathReason.Bite);
                                __instance.RpcMurderPlayer(bitten);
                                RPC.PlaySoundRPC(vampireID, Sounds.KillSound);
                                Logger.info("Vampireに噛まれている" + bitten.Data.PlayerName + "を自爆させました。", "Vampire");
                                if (bitten.Is(CustomRoles.Trapper))
                                    Utils.getPlayerById(vampireID).TrapperKilled(bitten);
                            }
                            else
                            {
                                Logger.info("Vampireに噛まれている" + bitten.Data.PlayerName + "はすでに死んでいました。", "Vampire");
                            }
                            main.BitPlayers.Remove(bitten.PlayerId);
                        }
                        else
                        {
                            main.BitPlayers[__instance.PlayerId] =
                            (main.BitPlayers[__instance.PlayerId].Item1, main.BitPlayers[__instance.PlayerId].Item2 + Time.fixedDeltaTime);
                        }
                    }
                }
                if (main.SerialKillerTimer.ContainsKey(__instance.PlayerId))
                {
                    if (main.SerialKillerTimer[__instance.PlayerId] >= Options.SerialKillerLimit.GetFloat())
                    {//自滅時間が来たとき
                        if (!__instance.Data.IsDead)
                        {
                            PlayerState.setDeathReason(__instance.PlayerId, PlayerState.DeathReason.Suicide);//死因：自滅
                            __instance.RpcMurderPlayer(__instance);//自滅させる
                            RPC.PlaySoundRPC(__instance.PlayerId, Sounds.KillSound);
                        }
                        else
                            main.SerialKillerTimer.Remove(__instance.PlayerId);
                    }
                    else
                    {
                        main.SerialKillerTimer[__instance.PlayerId] =
                        (main.SerialKillerTimer[__instance.PlayerId] + Time.fixedDeltaTime);//時間をカウント
                    }
                }
                if (GameStates.isInTask && main.WarlockTimer.ContainsKey(__instance.PlayerId))//処理を1秒遅らせる
                {
                    if (main.WarlockTimer[__instance.PlayerId] >= 1f)
                    {
                        __instance.RpcGuardAndKill(__instance);
                        main.isCursed = false;//変身クールを１秒に変更
                        Utils.CustomSyncAllSettings();
                        main.WarlockTimer.Remove(__instance.PlayerId);
                    }
                    else main.WarlockTimer[__instance.PlayerId] = (main.WarlockTimer[__instance.PlayerId] + Time.fixedDeltaTime);//時間をカウント
                }
                //バウハンのキルクールの変換とターゲットのリセット
                if (GameStates.isInTask && main.BountyTimer.ContainsKey(__instance.PlayerId))
                {
                    if (main.BountyTimer[__instance.PlayerId] >= Options.BountyTargetChangeTime.GetFloat() + Options.BountyFailureKillCooldown.GetFloat() - 1f && main.AirshipMeetingCheck)
                    {
                        main.AirshipMeetingCheck = false;
                        Utils.CustomSyncAllSettings();
                    }
                    if (main.BountyTimer[__instance.PlayerId] >= (Options.BountyTargetChangeTime.GetFloat() + Options.BountyFailureKillCooldown.GetFloat()) || main.isTargetKilled[__instance.PlayerId])//時間経過でターゲットをリセットする処理
                    {
                        main.BountyTimer[__instance.PlayerId] = 0f;
                        main.AllPlayerKillCooldown[__instance.PlayerId] = 10;
                        Logger.info($"{__instance.getNameWithRole()}:ターゲットリセット", "BountyHunter");
                        Utils.CustomSyncAllSettings();//ここでの処理をキルクールの変更の処理と同期
                        __instance.RpcGuardAndKill(__instance);//タイマー（変身クールダウン）のリセットと、名前の変更のためのKill
                        __instance.ResetBountyTarget();//ターゲットの選びなおし
                        Utils.NotifyRoles();
                    }
                    if (main.isTargetKilled[__instance.PlayerId])//ターゲットをキルした場合
                    {
                        main.isTargetKilled[__instance.PlayerId] = false;
                    }
                    if (main.BountyTimer[__instance.PlayerId] >= 0)
                        main.BountyTimer[__instance.PlayerId] = (main.BountyTimer[__instance.PlayerId] + Time.fixedDeltaTime);
                }
                /*if (GameStates.isInGame && main.AirshipMeetingTimer.ContainsKey(__instance.PlayerId)) //会議後すぐにここの処理をするため不要になったコードです。今後#465で変更した仕様がバグって、ここの処理が必要になった時のために残してコメントアウトしています
                {
                    if (main.AirshipMeetingTimer[__instance.PlayerId] >= 9f && !main.AirshipMeetingCheck)
                    {
                        main.AirshipMeetingCheck = true;
                        Utils.CustomSyncAllSettings();
                    }
                    if (main.AirshipMeetingTimer[__instance.PlayerId] >= 10f)
                    {
                        Utils.AfterMeetingTasks();
                        main.AirshipMeetingTimer.Remove(__instance.PlayerId);
                    }
                    else
                        main.AirshipMeetingTimer[__instance.PlayerId] = (main.AirshipMeetingTimer[__instance.PlayerId] + Time.fixedDeltaTime);
                    }
                }*/

                if (GameStates.isInGame) LoversSuicide();
                if (GameStates.isInTask && main.ArsonistTimer.ContainsKey(__instance.PlayerId))//アーソニストが誰かを塗っているとき
                {
                    var ArsonistDic = main.DousedPlayerCount[__instance.PlayerId];
                    var ar_target = main.ArsonistTimer[__instance.PlayerId].Item1;//塗られる人
                    if (main.ArsonistTimer[__instance.PlayerId].Item2 >= Options.ArsonistDouseTime.GetFloat())//時間以上一緒にいて塗れた時
                    {
                        main.AllPlayerKillCooldown[__instance.PlayerId] = Options.ArsonistCooldown.GetFloat() * 2;
                        Utils.CustomSyncAllSettings();//同期
                        __instance.RpcGuardAndKill(ar_target);//通知とクールリセット
                        main.ArsonistTimer.Remove(__instance.PlayerId);//塗が完了したのでDictionaryから削除
                        main.isDoused[(__instance.PlayerId, ar_target.PlayerId)] = true;//塗り完了
                        main.DousedPlayerCount[__instance.PlayerId] = ((ArsonistDic.Item1 + 1), ArsonistDic.Item2);//塗った人数を増やす
                        Logger.info($"{__instance.getNameWithRole()} : {main.DousedPlayerCount[__instance.PlayerId]}", "Arsonist");
                        __instance.RpcSendDousedPlayerCount();
                        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetDousedPlayer, SendOption.Reliable, -1);//RPCによる同期
                        writer.Write(__instance.PlayerId);
                        writer.Write(ar_target.PlayerId);
                        writer.Write(true);
                        AmongUsClient.Instance.FinishRpcImmediately(writer);
                        Utils.NotifyRoles();//名前変更
                    }
                    else
                    {
                        float dis;
                        dis = Vector2.Distance(__instance.transform.position, ar_target.transform.position);//距離を出す
                        if (dis <= 1.75f)//一定の距離にターゲットがいるならば時間をカウント
                        {
                            main.ArsonistTimer[__instance.PlayerId] =
                            (main.ArsonistTimer[__instance.PlayerId].Item1, main.ArsonistTimer[__instance.PlayerId].Item2 + Time.fixedDeltaTime);
                        }
                        else//それ以外は削除
                        {
                            main.ArsonistTimer.Remove(__instance.PlayerId);
                        }
                    }
                }
                if (GameStates.isInTask && main.PuppeteerList.ContainsKey(__instance.PlayerId))
                {
                    Vector2 __instancepos = __instance.transform.position;//PuppeteerListのKeyの位置
                    Dictionary<byte, float> targetdistance = new Dictionary<byte, float>();
                    float dis;
                    foreach (var target in PlayerControl.AllPlayerControls)
                    {
                        if (!target.Data.IsDead && !target.getCustomRole().isImpostor() && target != __instance)
                        {
                            dis = Vector2.Distance(__instancepos, target.transform.position);
                            targetdistance.Add(target.PlayerId, dis);
                        }
                    }
                    if (targetdistance.Count() != 0)
                    {
                        var min = targetdistance.OrderBy(c => c.Value).FirstOrDefault();//一番値が小さい
                        PlayerControl targetp = Utils.getPlayerById(min.Key);
                        if (__instance.Data.IsDead)
                            main.PuppeteerList.Remove(__instance.PlayerId);
                        if (min.Value <= 1.75f && !targetp.Data.IsDead)
                        {
                            RPC.PlaySoundRPC(main.PuppeteerList[__instance.PlayerId], Sounds.KillSound);
                            __instance.RpcMurderPlayer(targetp);
                            Utils.CustomSyncAllSettings();
                            main.PuppeteerList.Remove(__instance.PlayerId);
                            Utils.NotifyRoles();
                        }
                    }
                }

                if (GameStates.isInGame && main.RefixCooldownDelay <= 0)
                    foreach (var pc in PlayerControl.AllPlayerControls)
                    {
                        if (pc.Is(CustomRoles.Vampire) || pc.Is(CustomRoles.Warlock))
                            main.AllPlayerKillCooldown[pc.PlayerId] = Options.BHDefaultKillCooldown.GetFloat() * 2;
                    }

                if (__instance.AmOwner) Utils.ApplySuffix();
            }

            //役職テキストの表示
            var RoleTextTransform = __instance.nameText.transform.Find("RoleText");
            var RoleText = RoleTextTransform.GetComponent<TMPro.TextMeshPro>();
            if (RoleText != null && __instance != null)
            {
                if (GameStates.isInGame)
                {
                    var RoleTextData = Utils.GetRoleText(__instance);
                    //if (Options.CurrentGameMode == CustomGameMode.HideAndSeek)
                    //{
                    //    var hasRole = main.AllPlayerCustomRoles.TryGetValue(__instance.PlayerId, out var role);
                    //    if (hasRole) RoleTextData = Utils.GetRoleTextHideAndSeek(__instance.Data.Role.Role, role);
                    //}
                    RoleText.text = RoleTextData.Item1;
                    RoleText.color = RoleTextData.Item2;
                    if (__instance.AmOwner) RoleText.enabled = true; //自分ならロールを表示
                    else if (main.VisibleTasksCount && PlayerControl.LocalPlayer.Data.IsDead && Options.GhostCanSeeOtherRoles.GetBool()) RoleText.enabled = true; //他プレイヤーでVisibleTasksCountが有効なおかつ自分が死んでいるならロールを表示
                    else RoleText.enabled = false; //そうでなければロールを非表示
                    if (!AmongUsClient.Instance.IsGameStarted && AmongUsClient.Instance.GameMode != GameModes.FreePlay)
                    {
                        RoleText.enabled = false; //ゲームが始まっておらずフリープレイでなければロールを非表示
                        if (!__instance.AmOwner) __instance.nameText.text = __instance.Data.PlayerName;
                    }
                    if (main.VisibleTasksCount && Utils.hasTasks(__instance.Data, false)) //他プレイヤーでVisibleTasksCountは有効なおかつタスクがあるなら
                        RoleText.text += $" {Utils.getProgressText(__instance)}"; //ロールの横にタスク表示

                    if (__instance.Is(CustomRoles.Sniper))
                        RoleText.text += $" {Sniper.GetBulletCount(__instance)}";

                    //変数定義
                    var seer = PlayerControl.LocalPlayer;
                    var target = __instance;

                    string RealName;
                    string Mark = "";
                    string Suffix = "";

                    //名前変更
                    RealName = target.Data.PlayerName;


                    //名前色変更処理
                    //自分自身の名前の色を変更
                    if (target.AmOwner && AmongUsClient.Instance.IsGameStarted)
                    { //targetが自分自身
                        RealName = $"<color={target.getRoleColorCode()}>{RealName}</color>"; //名前の色を変更
                        if (target.Is(CustomRoles.Arsonist) && target.isDouseDone())
                            RealName = $"<color={Utils.getRoleColorCode(CustomRoles.Arsonist)}>{getString("EnterVentToWin")}</color>";
                    }
                    //タスクを終わらせたMadSnitchがインポスターを確認できる
                    else if (seer.Is(CustomRoles.MadSnitch) && //seerがMadSnitch
                        target.getCustomRole().isImpostor() && //targetがインポスター
                        seer.getPlayerTaskState().isTaskFinished) //seerのタスクが終わっている
                    {
                        RealName = $"<color={Utils.getRoleColorCode(CustomRoles.Impostor)}>{RealName}</color>"; //targetの名前を赤色で表示
                    }
                    //タスクを終わらせたSnitchがインポスターを確認できる
                    else if (PlayerControl.LocalPlayer.Is(CustomRoles.Snitch) && //LocalPlayerがSnitch
                        PlayerControl.LocalPlayer.getPlayerTaskState().isTaskFinished) //LocalPlayerのタスクが終わっている
                    {
                        var targetCheck = __instance.getCustomRole().isImpostor() || (Options.SnitchCanFindNeutralKiller.GetBool() && __instance.Is(CustomRoles.Egoist));
                        if (targetCheck)//__instanceがターゲット
                        {
                            RealName = $"<color={target.getRoleColorCode()}>{RealName}</color>"; //targetの名前を役職色で表示
                        }
                    }
                    else if (seer.getCustomRole().isImpostor() && //seerがインポスター
                        target.Is(CustomRoles.Egoist) //targetがエゴイスト
                    )
                        RealName = $"<color={Utils.getRoleColorCode(CustomRoles.Egoist)}>{RealName}</color>"; //targetの名前をエゴイスト色で表示
                    else if (seer.Is(CustomRoles.EgoSchrodingerCat) && //seerがエゴイスト陣営のシュレディンガーの猫
                        target.Is(CustomRoles.Egoist) //targetがエゴイスト
                    )
                        RealName = $"<color={Utils.getRoleColorCode(CustomRoles.Egoist)}>{RealName}</color>"; //targetの名前をエゴイスト色で表示
                    else if (target.Is(CustomRoles.Mare) && Utils.isActive(SystemTypes.Electrical))
                        RealName = $"<color={Utils.getRoleColorCode(CustomRoles.Impostor)}>{RealName}</color>"; //targetの赤色で表示
                    else if (seer != null)
                    {//NameColorManager準拠の処理
                        var ncd = NameColorManager.Instance.GetData(seer.PlayerId, target.PlayerId);
                        RealName = ncd.OpenTag + RealName + ncd.CloseTag;
                    }

                    //インポスター/キル可能な第三陣営がタスクが終わりそうなSnitchを確認できる
                    var canFindSnitchRole = seer.getCustomRole().isImpostor() || //LocalPlayerがインポスター
                        (Options.SnitchCanFindNeutralKiller.GetBool() && seer.Is(CustomRoles.Egoist));//or エゴイスト

                    if (canFindSnitchRole && target.Is(CustomRoles.Snitch) && target.getPlayerTaskState().doExpose //targetがタスクが終わりそうなSnitch
                    )
                    {
                        Mark += $"<color={Utils.getRoleColorCode(CustomRoles.Snitch)}>★</color>"; //Snitch警告をつける
                    }
                    if (seer.Is(CustomRoles.Arsonist) && seer.isDousedPlayer(target))
                    {
                        Mark += $"<color={Utils.getRoleColorCode(CustomRoles.Arsonist)}>▲</color>";
                    }
                    foreach (var ExecutionerTarget in main.ExecutionerTarget)
                    {
                        if ((seer.PlayerId == ExecutionerTarget.Key || seer.Data.IsDead) && //seerがKey or Dead
                        target.PlayerId == ExecutionerTarget.Value) //targetがValue
                            Mark += $"<color={Utils.getRoleColorCode(CustomRoles.Executioner)}>♦</color>";
                    }
                    if (seer.Is(CustomRoles.Puppeteer))
                    {
                        if (seer.Is(CustomRoles.Puppeteer) &&
                        main.PuppeteerList.ContainsValue(seer.PlayerId) &&
                        main.PuppeteerList.ContainsKey(target.PlayerId))
                            Mark += $"<color={Utils.getRoleColorCode(CustomRoles.Impostor)}>◆</color>";
                    }
                    if (Sniper.isEnable() && target.AmOwner)
                    {
                        //銃声が聞こえるかチェック
                        Mark += Sniper.GetShotNotify(target.PlayerId);

                    }
                    //タスクが終わりそうなSnitchがいるとき、インポスター/キル可能な第三陣営に警告が表示される
                    if (!GameStates.isMeeting && target.getCustomRole().isImpostor()
                        || (Options.SnitchCanFindNeutralKiller.GetBool() && target.Is(CustomRoles.Egoist)))
                    { //targetがインポスターかつ自分自身
                        var found = false;
                        var update = false;
                        var arrows = "";
                        foreach (var pc in PlayerControl.AllPlayerControls)
                        { //全員分ループ
                            if (!pc.Is(CustomRoles.Snitch) || pc.Data.IsDead || pc.Data.Disconnected) continue; //(スニッチ以外 || 死者 || 切断者)に用はない
                            if (pc.getPlayerTaskState().doExpose)
                            { //タスクが終わりそうなSnitchが見つかった時
                                found = true;
                                //矢印表示しないならこれ以上は不要
                                if (!Options.SnitchEnableTargetArrow.GetBool()) break;
                                update = CheckArrowUpdate(target, pc, update, false);
                                var key = (target.PlayerId, pc.PlayerId);
                                arrows += main.targetArrows[key];
                            }
                        }
                        if (found && target.AmOwner) Mark += $"<color={Utils.getRoleColorCode(CustomRoles.Snitch)}>★{arrows}</color>"; //Snitch警告を表示
                        if (AmongUsClient.Instance.AmHost && seer.PlayerId != target.PlayerId && update)
                        {
                            //更新があったら非Modに通知
                            Utils.NotifyRoles(SpecifySeer: target);
                        }
                    }

                    //ハートマークを付ける(会議中MOD視点)
                    if (__instance.Is(CustomRoles.Lovers) && PlayerControl.LocalPlayer.Is(CustomRoles.Lovers))
                    {
                        Mark += $"<color={Utils.getRoleColorCode(CustomRoles.Lovers)}>♡</color>";
                    }
                    else if (__instance.Is(CustomRoles.Lovers) && PlayerControl.LocalPlayer.Data.IsDead)
                    {
                        Mark += $"<color={Utils.getRoleColorCode(CustomRoles.Lovers)}>♡</color>";
                    }

                    //矢印オプションありならタスクが終わったスニッチはインポスター/キル可能な第三陣営の方角がわかる
                    if (!GameStates.isMeeting && Options.SnitchEnableTargetArrow.GetBool() && target.Is(CustomRoles.Snitch))
                    {
                        var TaskState = target.getPlayerTaskState();
                        if (TaskState.isTaskFinished)
                        {
                            var coloredArrow = Options.SnitchCanGetArrowColor.GetBool();
                            var update = false;
                            foreach (var pc in PlayerControl.AllPlayerControls)
                            {
                                var foundCheck =
                                    pc.getCustomRole().isImpostor() ||
                                    (Options.SnitchCanFindNeutralKiller.GetBool() && pc.Is(CustomRoles.Egoist));

                                //発見対象じゃ無ければ次
                                if (!foundCheck) continue;

                                update = CheckArrowUpdate(target, pc, update, coloredArrow);
                                var key = (target.PlayerId, pc.PlayerId);
                                if (target.AmOwner)
                                {
                                    //MODなら矢印表示
                                    Suffix += main.targetArrows[key];
                                }
                            }
                            if (AmongUsClient.Instance.AmHost && seer.PlayerId != target.PlayerId && update)
                            {
                                //更新があったら非Modに通知
                                Utils.NotifyRoles(SpecifySeer: target);
                            }
                        }
                    }
                    /*if(main.AmDebugger.Value && main.BlockKilling.TryGetValue(target.PlayerId, out var isBlocked)) {
                        Mark = isBlocked ? "(true)" : "(false)";
                    }*/

                    //Mark・Suffixの適用
                    target.nameText.text = $"{RealName}{Mark}";
                    if (Suffix != "")
                    {
                        //名前が2行になると役職テキストを上にずらす必要がある
                        RoleText.transform.SetLocalY(0.35f);
                        target.nameText.text += "\r\n" + Suffix;

                    }
                    else
                    {
                        //役職テキストの座標を初期値に戻す
                        RoleText.transform.SetLocalY(0.175f);
                    }
                }
                else
                {
                    //役職テキストの座標を初期値に戻す
                    RoleText.transform.SetLocalY(0.175f);
                }
            }
        }
        //FIXME: 役職クラス化のタイミングで、このメソッドは移動予定
        public static void LoversSuicide(byte deathId = 0x7f, bool isExiled = false)
        {
            if (CustomRoles.Lovers.isEnable() && main.isLoversDead == false)
            {
                foreach (var loversPlayer in main.LoversPlayers)
                {
                    //生きていて死ぬ予定でなければスキップ
                    if (!loversPlayer.Data.IsDead && loversPlayer.PlayerId != deathId) continue;

                    main.isLoversDead = true;
                    foreach (var partnerPlayer in main.LoversPlayers)
                    {
                        //本人ならスキップ
                        if (loversPlayer.PlayerId == partnerPlayer.PlayerId) continue;

                        //残った恋人を全て殺す(2人以上可)
                        //生きていて死ぬ予定もない場合は心中
                        if (partnerPlayer.PlayerId != deathId && !partnerPlayer.Data.IsDead)
                        {
                            PlayerState.setDeathReason(partnerPlayer.PlayerId, PlayerState.DeathReason.LoversSuicide);
                            if (isExiled)
                            {
                                main.IgnoreReportPlayers.Add(partnerPlayer.PlayerId);   //通報不可な死体にする
                                if (PlayerControl.GameOptions.MapId != 4) //Airship用
                                    CheckForEndVotingPatch.recall = true;
                            }
                            partnerPlayer.CheckMurder(partnerPlayer);
                        }
                    }
                }
            }
        }

        public static bool CheckArrowUpdate(PlayerControl seer, PlayerControl target, bool updateFlag, bool coloredArrow)
        {
            if (!Options.SnitchEnableTargetArrow.GetBool()) return false;

            var key = (seer.PlayerId, target.PlayerId);
            if (target.Data.IsDead)
            {
                //死んでたらリストから削除
                main.targetArrows.Remove(key);
                return updateFlag;
            }
            if (!main.targetArrows.TryGetValue(key, out var oldArrow))
            {
                oldArrow = "";
            }
            //インポスターの方角ベクトルを取る
            var dir = target.transform.position - seer.transform.position;
            byte index;
            if (dir.magnitude < 2)
            {
                //近い時はドット表示
                index = 8;
            }
            else
            {
                //-22.5～22.5度を0とするindexに変換
                var angle = Vector3.SignedAngle(Vector3.down, dir, Vector3.back) + 180 + 22.5;
                index = (byte)(((int)(angle / 45)) % 8);
            }
            var arrow = "↑↗→↘↓↙←↖・"[index].ToString();
            if (coloredArrow)
            {
                arrow = $"<color={target.getRoleColorCode()}>{arrow}</color>";
            }
            if (oldArrow != arrow)
            {
                //前回から変わってたら登録して更新フラグ
                main.targetArrows[key] = arrow;
                updateFlag = true;
                //Logger.info($"{seer.name}->{target.name}:{arrow}");
            }
            return updateFlag;
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
            if (pc.Is(CustomRoles.Mayor))
            {
                pc.MyPhysics.RpcBootFromVent(__instance.Id);
                if (main.MayorUsedButtonCount[pc.PlayerId] < Options.MayorNumOfUseButton.GetFloat())
                {
                    main.MayorUsedButtonCount[pc.PlayerId] += 1;
                    pc.CmdReportDeadBody(null);
                }
            }
        }
    }
    [HarmonyPatch(typeof(PlayerPhysics), nameof(PlayerPhysics.CoEnterVent))]
    class CoEnterVentPatch
    {
        public static bool Prefix(PlayerPhysics __instance, [HarmonyArgument(0)] int id)
        {
            if (AmongUsClient.Instance.AmHost)
            {
                if (main.DousedPlayerCount.ContainsKey(__instance.myPlayer.PlayerId) && AmongUsClient.Instance.IsGameStarted)
                    if (__instance.myPlayer.isDouseDone())
                    {
                        foreach (var pc in PlayerControl.AllPlayerControls)
                        {
                            if (!pc.Data.IsDead)
                            {
                                if (pc != __instance.myPlayer)
                                {
                                    //生存者は焼殺
                                    pc.RpcMurderPlayer(pc);
                                    PlayerState.setDeathReason(pc.PlayerId, PlayerState.DeathReason.Torched);
                                    PlayerState.setDead(pc.PlayerId);
                                }
                                else
                                    RPC.PlaySoundRPC(pc.PlayerId, Sounds.KillSound);
                            }
                        }
                        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.ArsonistWin, Hazel.SendOption.Reliable, -1);
                        writer.Write(__instance.myPlayer.PlayerId);
                        AmongUsClient.Instance.FinishRpcImmediately(writer);
                        RPC.ArsonistWin(__instance.myPlayer.PlayerId);
                        return true;
                    }
                if (__instance.myPlayer.Is(CustomRoles.Sheriff) || __instance.myPlayer.Is(CustomRoles.SKMadmate) || __instance.myPlayer.Is(CustomRoles.Arsonist))
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
    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.CompleteTask))]
    class PlayerControlCompleteTaskPatch
    {
        public static void Postfix(PlayerControl __instance)
        {
            Logger.info($"TaskComplete:{__instance.PlayerId}", "CompleteTask");
            PlayerState.UpdateTask(__instance);
            Utils.NotifyRoles();
        }
    }
}

