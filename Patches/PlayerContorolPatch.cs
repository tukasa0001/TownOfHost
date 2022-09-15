using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HarmonyLib;
using Hazel;
using UnityEngine;
using static TownOfHost.Translator;

namespace TownOfHost
{
    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.CheckProtect))]
    class CheckProtectPatch
    {
        public static bool Prefix(PlayerControl __instance, [HarmonyArgument(0)] PlayerControl target)
        {
            if (!AmongUsClient.Instance.AmHost) return false;
            Logger.Info("CheckProtect発生: " + __instance.GetNameWithRole() + "=>" + target.GetNameWithRole(), "CheckProtect");
            if (__instance.Is(CustomRoles.Sheriff))
            {
                if (__instance.Data.IsDead)
                {
                    Logger.Info("守護をブロックしました。", "CheckProtect");
                    return false;
                }
            }
            return true;
        }
    }
    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.CheckMurder))]
    class CheckMurderPatch
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
            if (killer.PlayerId != target.PlayerId)
            {
                //自殺でない場合のみ役職チェック
                switch (killer.GetCustomRole())
                {
                    //==========インポスター役職==========//
                    case CustomRoles.Mafia:
                        if (!killer.CanUseKillButton())
                        {
                            Logger.Info(killer?.Data?.PlayerName + "はMafiaだったので、キルはキャンセルされました。", "CheckMurder");
                            return false;
                        }
                        else
                        {
                            Logger.Info(killer?.Data?.PlayerName + "はMafiaですが、他のインポスターがいないのでキルが許可されました。", "CheckMurder");
                        }
                        break;
                    case CustomRoles.FireWorks:
                        if (!killer.CanUseKillButton())
                        {
                            return false;
                        }
                        break;
                    case CustomRoles.Sniper:
                        if (!killer.CanUseKillButton())
                        {
                            return false;
                        }
                        break;
                    case CustomRoles.Mare:
                        if (!killer.CanUseKillButton())
                            return false;
                        break;

                    //==========マッドメイト系役職==========//
                    case CustomRoles.SKMadmate:
                        //キル可能職がサイドキックされた場合
                        return false;

                    //==========第三陣営役職==========//

                    //==========クルー役職==========//
                    case CustomRoles.Sheriff:
                        if (!Sheriff.CanUseKillButton(killer))
                            return false;
                        break;
                }
            }


            //キルされた時の特殊判定
            switch (target.GetCustomRole())
            {
                case CustomRoles.SchrodingerCat:
                    //シュレディンガーの猫が切られた場合の役職変化スタート
                    //直接キル出来る役職チェック
                    // Sniperなど自殺扱いのものもあるので追加するときは注意
                    var canDirectKill = !killer.Is(CustomRoles.Arsonist);
                    if (canDirectKill)
                    {
                        killer.RpcGuardAndKill(target);
                        if (PlayerState.GetDeathReason(target.PlayerId) == PlayerState.DeathReason.Sniped)
                        {
                            //スナイプされた時
                            target.RpcSetCustomRole(CustomRoles.MSchrodingerCat);
                            var sniperId = Sniper.GetSniper(target.PlayerId);
                            NameColorManager.Instance.RpcAdd(sniperId, target.PlayerId, $"{Utils.GetRoleColorCode(CustomRoles.SchrodingerCat)}");
                        }
                        else
                        {
                            if (killer.Is(CustomRoles.BountyHunter) && BountyHunter.GetTarget(killer) == target)
                                BountyHunter.ResetTarget(killer);//ターゲットの選びなおし
                            SerialKiller.OnCheckMurder(killer, isKilledSchrodingerCat: true);
                            if (killer.GetCustomRole().IsImpostor())
                                target.RpcSetCustomRole(CustomRoles.MSchrodingerCat);
                            if (killer.Is(CustomRoles.Sheriff))
                                target.RpcSetCustomRole(CustomRoles.CSchrodingerCat);
                            if (killer.Is(CustomRoles.Egoist))
                            {
                                TeamEgoist.Add(target.PlayerId);
                                target.RpcSetCustomRole(CustomRoles.EgoSchrodingerCat);
                            }
                            if (killer.Is(CustomRoles.Jackal))
                                target.RpcSetCustomRole(CustomRoles.JSchrodingerCat);

                            NameColorManager.Instance.RpcAdd(killer.PlayerId, target.PlayerId, $"{Utils.GetRoleColorCode(CustomRoles.SchrodingerCat)}");
                        }
                        Utils.NotifyRoles();
                        Utils.CustomSyncAllSettings();
                        return false;
                        //シュレディンガーの猫の役職変化処理終了
                        //第三陣営キル能力持ちが追加されたら、その陣営を味方するシュレディンガーの猫の役職を作って上と同じ書き方で書いてください
                    }
                    break;

                //==========マッドメイト系役職==========//
                case CustomRoles.MadGuardian:
                    //killerがキルできないインポスター判定役職の場合はスキップ
                    if (killer.Is(CustomRoles.Arsonist) //アーソニスト
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
                    case CustomRoles.BountyHunter: //キルが発生する前にここの処理をしないとバグる
                        BountyHunter.OnCheckMurder(killer, target);
                        break;
                    case CustomRoles.SerialKiller:
                        SerialKiller.OnCheckMurder(killer);
                        break;
                    case CustomRoles.Vampire:
                        if (!target.Is(CustomRoles.Bait))
                        { //キルキャンセル&自爆処理
                            Utils.CustomSyncAllSettings();
                            Main.AllPlayerKillCooldown[killer.PlayerId] = Options.DefaultKillCooldown * 2;
                            killer.CustomSyncSettings(); //負荷軽減のため、killerだけがCustomSyncSettingsを実行
                            killer.RpcGuardAndKill(target);
                            Main.BitPlayers.Add(target.PlayerId, (killer.PlayerId, 0f));
                            return false;
                        }
                        break;
                    case CustomRoles.Warlock:
                        if (!Main.CheckShapeshift[killer.PlayerId] && !Main.isCurseAndKill[killer.PlayerId])
                        { //Warlockが変身時以外にキルしたら、呪われる処理
                            Main.isCursed = true;
                            Utils.CustomSyncAllSettings();
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
                    case CustomRoles.Witch:
                        if (killer.IsSpellMode() && !Main.SpelledPlayer.Contains(target))
                        {
                            killer.RpcGuardAndKill(target);
                            Main.SpelledPlayer.Add(target);
                            RPC.RpcDoSpell(target.PlayerId);
                        }
                        Main.KillOrSpell[killer.PlayerId] = !killer.IsSpellMode();
                        Utils.NotifyRoles();
                        killer.SyncKillOrSpell();
                        if (!killer.IsSpellMode()) return false;
                        break;
                    case CustomRoles.Puppeteer:
                        Main.PuppeteerList[target.PlayerId] = killer.PlayerId;
                        Main.AllPlayerKillCooldown[killer.PlayerId] = Options.DefaultKillCooldown * 2;
                        killer.CustomSyncSettings(); //負荷軽減のため、killerだけがCustomSyncSettings,NotifyRolesを実行
                        Utils.NotifyRoles(SpecifySeer: killer);
                        killer.RpcGuardAndKill(target);
                        return false;
                    case CustomRoles.TimeThief:
                        TimeThief.OnCheckMurder(killer);
                        break;

                    //==========マッドメイト系役職==========//

                    //==========第三陣営役職==========//
                    case CustomRoles.Arsonist:
                        Main.AllPlayerKillCooldown[killer.PlayerId] = 10f;
                        Utils.CustomSyncAllSettings();
                        if (!Main.isDoused[(killer.PlayerId, target.PlayerId)] && !Main.ArsonistTimer.ContainsKey(killer.PlayerId))
                        {
                            Main.ArsonistTimer.Add(killer.PlayerId, (target, 0f));
                            Utils.NotifyRoles(SpecifySeer: __instance);
                            RPC.SetCurrentDousingTarget(killer.PlayerId, target.PlayerId);
                        }
                        return false;

                    //==========クルー役職==========//
                    case CustomRoles.Sheriff:
                        Sheriff.OnCheckMurder(killer, target, Process: "RemoveShotLimit");

                        if (!Sheriff.OnCheckMurder(killer, target, Process: "Suicide"))
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
    class MurderPlayerPatch
    {
        public static void Prefix(PlayerControl __instance, [HarmonyArgument(0)] PlayerControl target)
        {
            Logger.Info($"{__instance.GetNameWithRole()} => {target.GetNameWithRole()}{(target.protectedByGuardian ? "(Protected)" : "")}", "MurderPlayer");

            if (RandomSpawn.CustomNetworkTransformPatch.NumOfTP.TryGetValue(__instance.PlayerId, out var num) && num > 2) RandomSpawn.CustomNetworkTransformPatch.NumOfTP[__instance.PlayerId] = 3;
        }
        public static void Postfix(PlayerControl __instance, [HarmonyArgument(0)] PlayerControl target)
        {
            if (!target.Data.IsDead || !AmongUsClient.Instance.AmHost) return;

            PlayerControl killer = __instance; //読み替え変数
            if (PlayerState.GetDeathReason(target.PlayerId) == PlayerState.DeathReason.Sniped)
            {
                killer = Utils.GetPlayerById(Sniper.GetSniper(target.PlayerId));
            }
            if (PlayerState.GetDeathReason(target.PlayerId) == PlayerState.DeathReason.etc)
            {
                //死因が設定されていない場合は死亡判定
                PlayerState.SetDeathReason(target.PlayerId, PlayerState.DeathReason.Kill);
            }

            //When Bait is killed
            if (target.GetCustomRole() == CustomRoles.Bait && killer.PlayerId != target.PlayerId)
            {
                Logger.Info(target?.Data?.PlayerName + "はBaitだった", "MurderPlayer");
                new LateTask(() => killer.CmdReportDeadBody(target.Data), 0.15f, "Bait Self Report");
            }
            else
            //Terrorist
            if (target.Is(CustomRoles.Terrorist))
            {
                Logger.Info(target?.Data?.PlayerName + "はTerroristだった", "MurderPlayer");
                Utils.CheckTerroristWin(target.Data);
            }
            if (target.Is(CustomRoles.Trapper) && !killer.Is(CustomRoles.Trapper))
                killer.TrapperKilled(target);
            if (Executioner.Target.ContainsValue(target.PlayerId))
                Executioner.ChangeRoleByTarget(target);
            if (target.Is(CustomRoles.Executioner) && Executioner.Target.ContainsKey(target.PlayerId))
            {
                Executioner.Target.Remove(target.PlayerId);
                Executioner.SendRPC(target.PlayerId);
            }
            if (target.Is(CustomRoles.TimeThief))
                target.ResetVotingTime();


            foreach (var pc in PlayerControl.AllPlayerControls)
            {
                if (pc.IsLastImpostor())
                    Main.AllPlayerKillCooldown[pc.PlayerId] = Options.LastImpostorKillCooldown.GetFloat();
            }
            FixedUpdatePatch.LoversSuicide(target.PlayerId);

            PlayerState.SetDead(target.PlayerId);
            Utils.CountAliveImpostors();
            Utils.CustomSyncAllSettings();
            Utils.NotifyRoles();
            Utils.TargetDies(__instance, target, PlayerState.GetDeathReason(target.PlayerId));
        }
    }
    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.Shapeshift))]
    class ShapeshiftPatch
    {
        public static void Prefix(PlayerControl __instance, [HarmonyArgument(0)] PlayerControl target)
        {
            Logger.Info($"{__instance?.GetNameWithRole()} => {target?.GetNameWithRole()}", "Shapeshift");
            if (!AmongUsClient.Instance.AmHost) return;

            var shapeshifter = __instance;
            var shapeshifting = shapeshifter.PlayerId != target.PlayerId;

            Main.CheckShapeshift[shapeshifter.PlayerId] = shapeshifting;
            if (shapeshifter.Is(CustomRoles.Warlock))
            {
                if (Main.CursedPlayers[shapeshifter.PlayerId] != null)//呪われた人がいるか確認
                {
                    if (shapeshifting && !Main.CursedPlayers[shapeshifter.PlayerId].Data.IsDead)//変身解除の時に反応しない
                    {
                        var cp = Main.CursedPlayers[shapeshifter.PlayerId];
                        Vector2 cppos = cp.transform.position;//呪われた人の位置
                        Dictionary<PlayerControl, float> cpdistance = new();
                        float dis;
                        foreach (PlayerControl p in PlayerControl.AllPlayerControls)
                        {
                            if (!p.Data.IsDead && p != cp)
                            {
                                dis = Vector2.Distance(cppos, p.transform.position);
                                cpdistance.Add(p, dis);
                                Logger.Info($"{p?.Data?.PlayerName}の位置{dis}", "Warlock");
                            }
                        }
                        var min = cpdistance.OrderBy(c => c.Value).FirstOrDefault();//一番小さい値を取り出す
                        PlayerControl targetw = min.Key;
                        Logger.Info($"{targetw.GetNameWithRole()}was killed", "Warlock");
                        cp.RpcMurderPlayerV2(targetw);//殺す
                        shapeshifter.RpcGuardAndKill(shapeshifter);
                        Main.isCurseAndKill[shapeshifter.PlayerId] = false;
                    }
                    Main.CursedPlayers[shapeshifter.PlayerId] = null;
                }
            }
            if (shapeshifter.Is(CustomRoles.EvilTracker)) EvilTracker.Shapeshift(shapeshifter, target, shapeshifting);

            if (shapeshifter.CanMakeMadmate() && shapeshifting)
            {//変身したとき一番近い人をマッドメイトにする処理
                Vector2 shapeshifterPosition = shapeshifter.transform.position;//変身者の位置
                Dictionary<PlayerControl, float> mpdistance = new();
                float dis;
                foreach (PlayerControl p in PlayerControl.AllPlayerControls)
                {
                    if (!p.Data.IsDead && p.Data.Role.Role != RoleTypes.Shapeshifter && !p.Is(RoleType.Impostor) && !p.Is(CustomRoles.SKMadmate))
                    {
                        dis = Vector2.Distance(shapeshifterPosition, p.transform.position);
                        mpdistance.Add(p, dis);
                    }
                }
                if (mpdistance.Count() != 0)
                {
                    var min = mpdistance.OrderBy(c => c.Value).FirstOrDefault();//一番値が小さい
                    PlayerControl targetm = min.Key;
                    targetm.RpcSetCustomRole(CustomRoles.SKMadmate);
                    Logger.Info($"Make SKMadmate:{targetm.name}", "Shapeshift");
                    Main.SKMadmateNowCount++;
                    Utils.CustomSyncAllSettings();
                    Utils.NotifyRoles();
                }
            }
            if (shapeshifter.Is(CustomRoles.FireWorks)) FireWorks.ShapeShiftState(shapeshifter, shapeshifting);
            if (shapeshifter.Is(CustomRoles.Sniper)) Sniper.ShapeShiftCheck(shapeshifter, shapeshifting);

            //変身解除のタイミングがずれて名前が直せなかった時のために強制書き換え
            if (!shapeshifting)
            {
                new LateTask(() =>
                {
                    Utils.NotifyRoles(NoCache: true);
                },
                1.2f, "ShapeShiftNotify");
            }
        }
    }
    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.ReportDeadBody))]
    class ReportDeadBodyPatch
    {
        public static bool Prefix(PlayerControl __instance, [HarmonyArgument(0)] GameData.PlayerInfo target)
        {
            if (GameStates.IsMeeting) return false;
            Logger.Info($"{__instance.GetNameWithRole()} => {target?.GetNameWithRole() ?? "null"}", "ReportDeadBody");
            if (Options.IsStandardHAS && target != null && __instance == target.Object) return true; //[StandardHAS] ボタンでなく、通報者と死体が同じなら許可
            if (Options.CurrentGameMode == CustomGameMode.HideAndSeek || Options.IsStandardHAS) return false;
            if (!AmongUsClient.Instance.AmHost) return true;
            BountyHunter.OnReportDeadBody();
            SerialKiller.OnReportDeadBody();
            Main.ArsonistTimer.Clear();
            if (target == null) //ボタン
            {
                if (__instance.Is(CustomRoles.Mayor))
                {
                    Main.MayorUsedButtonCount[__instance.PlayerId] += 1;
                }
            }

            if (Options.SyncButtonMode.GetBool() && target == null)
            {
                Logger.Info("最大:" + Options.SyncedButtonCount.GetInt() + ", 現在:" + Options.UsedButtonCount, "ReportDeadBody");
                if (Options.SyncedButtonCount.GetFloat() <= Options.UsedButtonCount)
                {
                    Logger.Info("使用可能ボタン回数が最大数を超えているため、ボタンはキャンセルされました。", "ReportDeadBody");
                    return false;
                }
                else Options.UsedButtonCount++;
                if (Options.SyncedButtonCount.GetFloat() == Options.UsedButtonCount)
                {
                    Logger.Info("使用可能ボタン回数が最大数に達しました。", "ReportDeadBody");
                }
            }

            foreach (var bp in Main.BitPlayers)
            {
                var vampireID = bp.Value.Item1;
                var bitten = Utils.GetPlayerById(bp.Key);

                if (bitten != null && !bitten.Data.IsDead)
                {
                    PlayerState.SetDeathReason(bitten.PlayerId, PlayerState.DeathReason.Bite);
                    //Protectは強制的にはがす
                    if (bitten.protectedByGuardian)
                        bitten.RpcMurderPlayer(bitten);
                    bitten.RpcMurderPlayer(bitten);
                    RPC.PlaySoundRPC(vampireID, Sounds.KillSound);
                    Logger.Info("Vampireに噛まれている" + bitten?.Data?.PlayerName + "を自爆させました。", "ReportDeadBody");
                }
                else
                    Logger.Info("Vampireに噛まれている" + bitten?.Data?.PlayerName + "はすでに死んでいました。", "ReportDeadBody");
            }
            Main.BitPlayers = new Dictionary<byte, (byte, float)>();
            Main.PuppeteerList.Clear();
            Sniper.OnStartMeeting();

            if (__instance.Data.IsDead) return true;
            //=============================================
            //以下、ボタンが押されることが確定したものとする。
            //=============================================

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
            var player = __instance;

            if (AmongUsClient.Instance.AmHost)
            {//実行クライアントがホストの場合のみ実行
                if (GameStates.IsLobby && (ModUpdater.hasUpdate || ModUpdater.isBroken || !Main.AllowPublicRoom) && AmongUsClient.Instance.IsGamePublic)
                    AmongUsClient.Instance.ChangeGamePublic(false);

                if (GameStates.IsInTask && CustomRoles.Vampire.IsEnable())
                {
                    //Vampireの処理
                    if (Main.BitPlayers.ContainsKey(player.PlayerId))
                    {
                        //__instance:キルされる予定のプレイヤー
                        //main.BitPlayers[__instance.PlayerId].Item1:キルしたプレイヤーのID
                        //main.BitPlayers[__instance.PlayerId].Item2:キルするまでの秒数
                        byte vampireID = Main.BitPlayers[player.PlayerId].Item1;
                        float killTimer = Main.BitPlayers[player.PlayerId].Item2;
                        if (killTimer >= Options.VampireKillDelay.GetFloat())
                        {
                            var bitten = player;
                            if (!bitten.Data.IsDead)
                            {
                                PlayerState.SetDeathReason(bitten.PlayerId, PlayerState.DeathReason.Bite);
                                bitten.RpcMurderPlayer(bitten);
                                var vampirePC = Utils.GetPlayerById(vampireID);
                                Logger.Info("Vampireに噛まれている" + bitten?.Data?.PlayerName + "を自爆させました。", "Vampire");
                                if (vampirePC.IsAlive())
                                {
                                    RPC.PlaySoundRPC(vampireID, Sounds.KillSound);
                                    if (bitten.Is(CustomRoles.Trapper))
                                        vampirePC.TrapperKilled(bitten);
                                }
                            }
                            else
                            {
                                Logger.Info("Vampireに噛まれている" + bitten?.Data?.PlayerName + "はすでに死んでいました。", "Vampire");
                            }
                            Main.BitPlayers.Remove(bitten.PlayerId);
                        }
                        else
                        {
                            Main.BitPlayers[player.PlayerId] =
                            (vampireID, killTimer + Time.fixedDeltaTime);
                        }
                    }
                }
                SerialKiller.FixedUpdate(player);
                if (GameStates.IsInTask && Main.WarlockTimer.ContainsKey(player.PlayerId))//処理を1秒遅らせる
                {
                    if (player.IsAlive())
                    {
                        if (Main.WarlockTimer[player.PlayerId] >= 1f)
                        {
                            player.RpcResetAbilityCooldown();
                            Main.isCursed = false;//変身クールを１秒に変更
                            Utils.CustomSyncAllSettings();
                            Main.WarlockTimer.Remove(player.PlayerId);
                        }
                        else Main.WarlockTimer[player.PlayerId] = Main.WarlockTimer[player.PlayerId] + Time.fixedDeltaTime;//時間をカウント
                    }
                    else
                    {
                        Main.WarlockTimer.Remove(player.PlayerId);
                    }
                }
                //ターゲットのリセット
                BountyHunter.FixedUpdate(player);
                EvilTracker.FixedUpdate(player);
                if (GameStates.IsInTask && player.IsAlive() && Options.LadderDeath.GetBool())
                {
                    FallFromLadder.FixedUpdate(player);
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

                if (GameStates.IsInGame) LoversSuicide();

                if (GameStates.IsInTask && Main.ArsonistTimer.ContainsKey(player.PlayerId))//アーソニストが誰かを塗っているとき
                {
                    if (!player.IsAlive())
                    {
                        Main.ArsonistTimer.Remove(player.PlayerId);
                        Utils.NotifyRoles(SpecifySeer: __instance);
                        RPC.ResetCurrentDousingTarget(player.PlayerId);
                    }
                    else
                    {
                        var ar_target = Main.ArsonistTimer[player.PlayerId].Item1;//塗られる人
                        var ar_time = Main.ArsonistTimer[player.PlayerId].Item2;//塗った時間
                        if (!ar_target.IsAlive())
                        {
                            Main.ArsonistTimer.Remove(player.PlayerId);
                        }
                        else if (ar_time >= Options.ArsonistDouseTime.GetFloat())//時間以上一緒にいて塗れた時
                        {
                            Main.AllPlayerKillCooldown[player.PlayerId] = Options.ArsonistCooldown.GetFloat() * 2;
                            Utils.CustomSyncAllSettings();//同期
                            player.RpcGuardAndKill(ar_target);//通知とクールリセット
                            Main.ArsonistTimer.Remove(player.PlayerId);//塗が完了したのでDictionaryから削除
                            Main.isDoused[(player.PlayerId, ar_target.PlayerId)] = true;//塗り完了
                            player.RpcSetDousedPlayer(ar_target, true);
                            Utils.NotifyRoles();//名前変更
                            RPC.ResetCurrentDousingTarget(player.PlayerId);
                        }
                        else
                        {
                            float dis;
                            dis = Vector2.Distance(player.transform.position, ar_target.transform.position);//距離を出す
                            if (dis <= 1.75f)//一定の距離にターゲットがいるならば時間をカウント
                            {
                                Main.ArsonistTimer[player.PlayerId] = (ar_target, ar_time + Time.fixedDeltaTime);
                            }
                            else//それ以外は削除
                            {
                                Main.ArsonistTimer.Remove(player.PlayerId);
                                Utils.NotifyRoles(SpecifySeer: __instance);
                                RPC.ResetCurrentDousingTarget(player.PlayerId);

                                Logger.Info($"Canceled: {__instance.GetNameWithRole()}", "Arsonist");
                            }
                        }

                    }
                }
                if (GameStates.IsInTask && Main.PuppeteerList.ContainsKey(player.PlayerId))
                {
                    if (!player.IsAlive())
                    {
                        Main.PuppeteerList.Remove(player.PlayerId);
                    }
                    else
                    {
                        Vector2 puppeteerPos = player.transform.position;//PuppeteerListのKeyの位置
                        Dictionary<byte, float> targetDistance = new();
                        float dis;
                        foreach (var target in PlayerControl.AllPlayerControls)
                        {
                            if (!target.IsAlive()) continue;
                            if (target.PlayerId != player.PlayerId && !target.GetCustomRole().IsImpostor())
                            {
                                dis = Vector2.Distance(puppeteerPos, target.transform.position);
                                targetDistance.Add(target.PlayerId, dis);
                            }
                        }
                        if (targetDistance.Count() != 0)
                        {
                            var min = targetDistance.OrderBy(c => c.Value).FirstOrDefault();//一番値が小さい
                            PlayerControl target = Utils.GetPlayerById(min.Key);
                            var KillRange = GameOptionsData.KillDistances[Mathf.Clamp(PlayerControl.GameOptions.KillDistance, 0, 2)];
                            if (min.Value <= KillRange && player.CanMove && target.CanMove)
                            {
                                RPC.PlaySoundRPC(Main.PuppeteerList[player.PlayerId], Sounds.KillSound);
                                player.RpcMurderPlayer(target);
                                Utils.CustomSyncAllSettings();
                                Main.PuppeteerList.Remove(player.PlayerId);
                                Utils.NotifyRoles();
                            }
                        }
                    }
                }
                if (GameStates.IsInTask && player == PlayerControl.LocalPlayer)
                    DisableDevice.FixedUpdate();

                if (GameStates.IsInGame && Main.RefixCooldownDelay <= 0)
                    foreach (var pc in PlayerControl.AllPlayerControls)
                    {
                        if (pc.Is(CustomRoles.Vampire) || pc.Is(CustomRoles.Warlock))
                            Main.AllPlayerKillCooldown[pc.PlayerId] = Options.DefaultKillCooldown * 2;
                    }

                if (__instance.AmOwner) Utils.ApplySuffix();
            }
            //LocalPlayer専用
            if (__instance.AmOwner)
            {
                //キルターゲットの上書き処理
                if (GameStates.IsInTask && (__instance.Is(CustomRoles.Sheriff) || __instance.Is(CustomRoles.Arsonist) || __instance.Is(CustomRoles.Jackal)) && !__instance.Data.IsDead)
                {
                    var players = __instance.GetPlayersInAbilityRangeSorted(false);
                    PlayerControl closest = players.Count <= 0 ? null : players[0];
                    HudManager.Instance.KillButton.SetTarget(closest);
                }
            }

            //役職テキストの表示
            var RoleTextTransform = __instance.cosmetics.nameText.transform.Find("RoleText");
            var RoleText = RoleTextTransform.GetComponent<TMPro.TextMeshPro>();
            if (RoleText != null && __instance != null)
            {
                if (GameStates.IsLobby)
                {
                    if (Main.playerVersion.TryGetValue(__instance.PlayerId, out var ver))
                    {
                        if (Main.ForkId != ver.forkId) // フォークIDが違う場合
                            __instance.cosmetics.nameText.text = $"<color=#ff0000><size=1.2>{ver.forkId}</size>\n{__instance?.name}</color>";
                        else if (Main.version.CompareTo(ver.version) == 0)
                            __instance.cosmetics.nameText.text = ver.tag == $"{ThisAssembly.Git.Commit}({ThisAssembly.Git.Branch})" ? $"<color=#87cefa>{__instance.name}</color>" : $"<color=#ffff00><size=1.2>{ver.tag}</size>\n{__instance?.name}</color>";
                        else __instance.cosmetics.nameText.text = $"<color=#ff0000><size=1.2>v{ver.version}</size>\n{__instance?.name}</color>";
                    }
                    else __instance.cosmetics.nameText.text = __instance?.Data?.PlayerName;
                }
                if (GameStates.IsInGame)
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
                    else if (Main.VisibleTasksCount && PlayerControl.LocalPlayer.Data.IsDead && Options.GhostCanSeeOtherRoles.GetBool()) RoleText.enabled = true; //他プレイヤーでVisibleTasksCountが有効なおかつ自分が死んでいるならロールを表示
                    else RoleText.enabled = false; //そうでなければロールを非表示
                    if (!AmongUsClient.Instance.IsGameStarted && AmongUsClient.Instance.GameMode != GameModes.FreePlay)
                    {
                        RoleText.enabled = false; //ゲームが始まっておらずフリープレイでなければロールを非表示
                        if (!__instance.AmOwner) __instance.cosmetics.nameText.text = __instance?.Data?.PlayerName;
                    }
                    if (Main.VisibleTasksCount) //他プレイヤーでVisibleTasksCountは有効なら
                        RoleText.text += $" {Utils.GetProgressText(__instance)}"; //ロールの横にタスクなど進行状況表示


                    //変数定義
                    var seer = PlayerControl.LocalPlayer;
                    var target = __instance;

                    string RealName;
                    string Mark = "";
                    string Suffix = "";

                    //名前変更
                    RealName = target.GetRealName();

                    //名前色変更処理
                    //自分自身の名前の色を変更
                    if (target.AmOwner && AmongUsClient.Instance.IsGameStarted)
                    { //targetが自分自身
                        RealName = Helpers.ColorString(target.GetRoleColor(), RealName); //名前の色を変更
                        if (target.Is(CustomRoles.Arsonist) && target.IsDouseDone())
                            RealName = Helpers.ColorString(Utils.GetRoleColor(CustomRoles.Arsonist), GetString("EnterVentToWin"));
                    }
                    //タスクを終わらせたMadSnitchがインポスターを確認できる
                    else if (seer.Is(CustomRoles.MadSnitch) && //seerがMadSnitch
                        target.GetCustomRole().IsImpostor() && //targetがインポスター
                        seer.GetPlayerTaskState().IsTaskFinished) //seerのタスクが終わっている
                    {
                        RealName = Helpers.ColorString(Utils.GetRoleColor(CustomRoles.Impostor), RealName); //targetの名前を赤色で表示
                    }
                    //タスクを終わらせたJクライアントがジャッカルを確認できる
                    else if (seer.Is(CustomRoles.JClient) && //seerがJクライアント
                        target.Is(CustomRoles.Jackal) && //targetがジャッカル
                        seer.GetPlayerTaskState().IsTaskFinished) //seerのタスクが終わっている
                    {
                        RealName = Helpers.ColorString(Utils.GetRoleColor(CustomRoles.Jackal), RealName); //targetの名前を赤色で表示
                    }
                    //タスクを終わらせたSnitchがインポスターを確認できる
                    else if (PlayerControl.LocalPlayer.Is(CustomRoles.Snitch) && //LocalPlayerがSnitch
                        PlayerControl.LocalPlayer.GetPlayerTaskState().IsTaskFinished) //LocalPlayerのタスクが終わっている
                    {
                        var targetCheck = target.GetCustomRole().IsImpostor() || (Options.SnitchCanFindNeutralKiller.GetBool() && target.IsNeutralKiller());
                        if (targetCheck)//__instanceがターゲット
                        {
                            RealName = Helpers.ColorString(target.GetRoleColor(), RealName); //targetの名前を役職色で表示
                        }
                    }
                    else if (seer.GetCustomRole().IsImpostor()) //seerがインポスター
                    {
                        if (target.Is(CustomRoles.Egoist)) //targetがエゴイスト
                            RealName = Helpers.ColorString(Utils.GetRoleColor(CustomRoles.Egoist), RealName); //targetの名前をエゴイスト色で表示
                        else if (target.Is(CustomRoles.MadSnitch) && target.GetPlayerTaskState().IsTaskFinished) //targetがタスクを終わらせたマッドスニッチ
                            Mark += Helpers.ColorString(Utils.GetRoleColor(CustomRoles.MadSnitch), "★"); //targetにマーク付与
                    }
                    //seerがジャッカル
                    else if (seer.Is(CustomRoles.Jackal) && //seerがジャッカル
                        target.Is(CustomRoles.JClient) && //targetがJクライアント
                        Options.CanSeeTaskFinishedJClientFromJackal.GetBool() &&
                        target.GetPlayerTaskState().IsTaskFinished) //targetのタスクが終わっている
                        RealName = Helpers.ColorString(Utils.GetRoleColor(CustomRoles.Jackal), RealName); //targetの名前をジャッカル色で表示

                    else if ((seer.Is(CustomRoles.EgoSchrodingerCat) && target.Is(CustomRoles.Egoist)) || //エゴ猫 --> エゴイスト
                             (seer.Is(CustomRoles.JSchrodingerCat) && target.Is(CustomRoles.Jackal)) //J猫 --> ジャッカル
                    )
                        RealName = Helpers.ColorString(target.GetRoleColor(), RealName); //targetの名前をtargetの役職の色で表示
                    else if (target.Is(CustomRoles.Mare) && Utils.IsActive(SystemTypes.Electrical))
                        RealName = Helpers.ColorString(Utils.GetRoleColor(CustomRoles.Impostor), RealName); //targetの赤色で表示
                    else if (seer != null)
                    {//NameColorManager準拠の処理
                        var ncd = NameColorManager.Instance.GetData(seer.PlayerId, target.PlayerId);
                        RealName = ncd.OpenTag + RealName + ncd.CloseTag;
                    }

                    //インポスター/キル可能な第三陣営がタスクが終わりそうなSnitchを確認できる
                    var canFindSnitchRole = seer.GetCustomRole().IsImpostor() || //LocalPlayerがインポスター
                        (Options.SnitchCanFindNeutralKiller.GetBool() && seer.IsNeutralKiller());//or キル可能な第三陣営

                    if (canFindSnitchRole && target.Is(CustomRoles.Snitch) && target.GetPlayerTaskState().DoExpose //targetがタスクが終わりそうなSnitch
                    )
                    {
                        Mark += $"<color={Utils.GetRoleColorCode(CustomRoles.Snitch)}>★</color>"; //Snitch警告をつける
                    }
                    if (seer.Is(CustomRoles.Arsonist))
                    {
                        if (seer.IsDousedPlayer(target))
                        {
                            Mark += $"<color={Utils.GetRoleColorCode(CustomRoles.Arsonist)}>▲</color>";
                        }
                        else if (
                            Main.currentDousingTarget != 255 &&
                            Main.currentDousingTarget == target.PlayerId
                        )
                        {
                            Mark += $"<color={Utils.GetRoleColorCode(CustomRoles.Arsonist)}>△</color>";
                        }
                    }
                    Mark += Executioner.TargetMark(seer, target);
                    if (seer.Is(CustomRoles.Puppeteer))
                    {
                        if (seer.Is(CustomRoles.Puppeteer) &&
                        Main.PuppeteerList.ContainsValue(seer.PlayerId) &&
                        Main.PuppeteerList.ContainsKey(target.PlayerId))
                            Mark += $"<color={Utils.GetRoleColorCode(CustomRoles.Impostor)}>◆</color>";
                    }
                    if (Sniper.IsEnable() && target.AmOwner)
                    {
                        //銃声が聞こえるかチェック
                        Mark += Sniper.GetShotNotify(target.PlayerId);

                    }
                    if (seer.Is(CustomRoles.EvilTracker)) Mark += EvilTracker.GetTargetMark(seer, target);
                    //タスクが終わりそうなSnitchがいるとき、インポスター/キル可能な第三陣営に警告が表示される
                    if (!GameStates.IsMeeting && (target.GetCustomRole().IsImpostor()
                        || (Options.SnitchCanFindNeutralKiller.GetBool() && target.IsNeutralKiller())))
                    { //targetがインポスターかつ自分自身
                        var found = false;
                        var update = false;
                        var arrows = "";
                        foreach (var pc in PlayerControl.AllPlayerControls)
                        { //全員分ループ
                            if (!pc.Is(CustomRoles.Snitch) || pc.Data.IsDead || pc.Data.Disconnected) continue; //(スニッチ以外 || 死者 || 切断者)に用はない
                            if (pc.GetPlayerTaskState().DoExpose)
                            { //タスクが終わりそうなSnitchが見つかった時
                                found = true;
                                //矢印表示しないならこれ以上は不要
                                if (!Options.SnitchEnableTargetArrow.GetBool()) break;
                                update = CheckArrowUpdate(target, pc, update, false);
                                var key = (target.PlayerId, pc.PlayerId);
                                arrows += Main.targetArrows[key];
                            }
                        }
                        if (found && target.AmOwner) Mark += $"<color={Utils.GetRoleColorCode(CustomRoles.Snitch)}>★{arrows}</color>"; //Snitch警告を表示
                        if (AmongUsClient.Instance.AmHost && seer.PlayerId != target.PlayerId && update)
                        {
                            //更新があったら非Modに通知
                            Utils.NotifyRoles(SpecifySeer: target);
                        }
                    }

                    //ハートマークを付ける(会議中MOD視点)
                    if (__instance.Is(CustomRoles.Lovers) && PlayerControl.LocalPlayer.Is(CustomRoles.Lovers))
                    {
                        Mark += $"<color={Utils.GetRoleColorCode(CustomRoles.Lovers)}>♡</color>";
                    }
                    else if (__instance.Is(CustomRoles.Lovers) && PlayerControl.LocalPlayer.Data.IsDead)
                    {
                        Mark += $"<color={Utils.GetRoleColorCode(CustomRoles.Lovers)}>♡</color>";
                    }

                    //矢印オプションありならタスクが終わったスニッチはインポスター/キル可能な第三陣営の方角がわかる
                    if (GameStates.IsInTask && Options.SnitchEnableTargetArrow.GetBool() && target.Is(CustomRoles.Snitch))
                    {
                        var TaskState = target.GetPlayerTaskState();
                        if (TaskState.IsTaskFinished)
                        {
                            var coloredArrow = Options.SnitchCanGetArrowColor.GetBool();
                            var update = false;
                            foreach (var pc in PlayerControl.AllPlayerControls)
                            {
                                var foundCheck =
                                    pc.GetCustomRole().IsImpostor() ||
                                    (Options.SnitchCanFindNeutralKiller.GetBool() && pc.IsNeutralKiller());

                                //発見対象じゃ無ければ次
                                if (!foundCheck) continue;

                                update = CheckArrowUpdate(target, pc, update, coloredArrow);
                                var key = (target.PlayerId, pc.PlayerId);
                                if (target.AmOwner)
                                {
                                    //MODなら矢印表示
                                    Suffix += Main.targetArrows[key];
                                }
                            }
                            if (AmongUsClient.Instance.AmHost && seer.PlayerId != target.PlayerId && update)
                            {
                                //更新があったら非Modに通知
                                Utils.NotifyRoles(SpecifySeer: target);
                            }
                        }
                    }
                    if (GameStates.IsInTask && target.Is(CustomRoles.EvilTracker)) Suffix += EvilTracker.PCGetTargetArrow(seer, target);

                    /*if(main.AmDebugger.Value && main.BlockKilling.TryGetValue(target.PlayerId, out var isBlocked)) {
                        Mark = isBlocked ? "(true)" : "(false)";
                    }*/

                    //Mark・Suffixの適用
                    target.cosmetics.nameText.text = $"{RealName}{Mark}";

                    if (Suffix != "")
                    {
                        //名前が2行になると役職テキストを上にずらす必要がある
                        RoleText.transform.SetLocalY(0.35f);
                        target.cosmetics.nameText.text += "\r\n" + Suffix;

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
            if (CustomRoles.Lovers.IsEnable() && Main.isLoversDead == false)
            {
                foreach (var loversPlayer in Main.LoversPlayers)
                {
                    //生きていて死ぬ予定でなければスキップ
                    if (!loversPlayer.Data.IsDead && loversPlayer.PlayerId != deathId) continue;

                    Main.isLoversDead = true;
                    foreach (var partnerPlayer in Main.LoversPlayers)
                    {
                        //本人ならスキップ
                        if (loversPlayer.PlayerId == partnerPlayer.PlayerId) continue;

                        //残った恋人を全て殺す(2人以上可)
                        //生きていて死ぬ予定もない場合は心中
                        if (partnerPlayer.PlayerId != deathId && !partnerPlayer.Data.IsDead)
                        {
                            PlayerState.SetDeathReason(partnerPlayer.PlayerId, PlayerState.DeathReason.LoversSuicide);
                            if (isExiled)
                                Main.AfterMeetingDeathPlayers.TryAdd(partnerPlayer.PlayerId, PlayerState.DeathReason.LoversSuicide);
                            else
                                partnerPlayer.RpcMurderPlayer(partnerPlayer);
                        }
                    }
                }
            }
        }

        public static bool CheckArrowUpdate(PlayerControl seer, PlayerControl target, bool updateFlag, bool coloredArrow)
        {
            var key = (seer.PlayerId, target.PlayerId);
            if (!Main.targetArrows.TryGetValue(key, out var oldArrow))
            {
                //初回は必ず被らないもの
                oldArrow = "_";
            }
            //初期値は死んでる場合の空白にしておく
            var arrow = "";
            if (!PlayerState.isDead[seer.PlayerId] && !PlayerState.isDead[target.PlayerId])
            {
                //対象の方角ベクトルを取る
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
                arrow = "↑↗→↘↓↙←↖・"[index].ToString();
                if (coloredArrow)
                {
                    arrow = $"<color={target.GetRoleColorCode()}>{arrow}</color>";
                }
            }
            if (oldArrow != arrow)
            {
                //前回から変わってたら登録して更新フラグ
                Main.targetArrows[key] = arrow;
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
            var roleText = UnityEngine.Object.Instantiate(__instance.cosmetics.nameText);
            roleText.transform.SetParent(__instance.cosmetics.nameText.transform);
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
                if (Main.MayorUsedButtonCount.TryGetValue(pc.PlayerId, out var count) && count < Options.MayorNumOfUseButton.GetInt())
                {
                    pc?.MyPhysics?.RpcBootFromVent(__instance.Id);
                    pc?.ReportDeadBody(null);
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
                if (AmongUsClient.Instance.IsGameStarted &&
                    __instance.myPlayer.IsDouseDone())
                {
                    foreach (var pc in PlayerControl.AllPlayerControls)
                    {
                        if (!pc.Data.IsDead)
                        {
                            if (pc != __instance.myPlayer)
                            {
                                //生存者は焼殺
                                pc.RpcMurderPlayer(pc);
                                PlayerState.SetDeathReason(pc.PlayerId, PlayerState.DeathReason.Torched);
                                PlayerState.SetDead(pc.PlayerId);
                            }
                            else
                                RPC.PlaySoundRPC(pc.PlayerId, Sounds.KillSound);
                        }
                    }
                    CustomWinnerHolder.WinnerTeam = CustomWinner.Arsonist;
                    CustomWinnerHolder.WinnerIds.Add(__instance.myPlayer.PlayerId);
                    return true;
                }
                if (__instance.myPlayer.Is(CustomRoles.Sheriff) ||
                __instance.myPlayer.Is(CustomRoles.SKMadmate) ||
                __instance.myPlayer.Is(CustomRoles.Arsonist) ||
                (__instance.myPlayer.Is(CustomRoles.Mayor) && Main.MayorUsedButtonCount.TryGetValue(__instance.myPlayer.PlayerId, out var count) && count >= Options.MayorNumOfUseButton.GetInt()) ||
                (__instance.myPlayer.Is(CustomRoles.Jackal) && !Options.JackalCanVent.GetBool())
                )
                {
                    MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(__instance.NetId, (byte)RpcCalls.BootFromVent, SendOption.Reliable, -1);
                    writer.WritePacked(127);
                    AmongUsClient.Instance.FinishRpcImmediately(writer);
                    new LateTask(() =>
                    {
                        int clientId = __instance.myPlayer.GetClientId();
                        MessageWriter writer2 = AmongUsClient.Instance.StartRpcImmediately(__instance.NetId, (byte)RpcCalls.BootFromVent, SendOption.Reliable, clientId);
                        writer2.Write(id);
                        AmongUsClient.Instance.FinishRpcImmediately(writer2);
                    }, 0.5f, "Fix DesyncImpostor Stuck");
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
        }
    }
    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.CompleteTask))]
    class PlayerControlCompleteTaskPatch
    {
        public static void Postfix(PlayerControl __instance)
        {
            var pc = __instance;
            Logger.Info($"TaskComplete:{pc.PlayerId}", "CompleteTask");
            PlayerState.UpdateTask(pc);
            Utils.NotifyRoles();
            if ((pc.GetPlayerTaskState().IsTaskFinished &&
                pc.GetCustomRole() is CustomRoles.Lighter or CustomRoles.Doctor) ||
                pc.GetCustomRole() is CustomRoles.SpeedBooster)
            {
                //ライターもしくはスピードブースターもしくはドクターがいる試合のみタスク終了時にCustomSyncAllSettingsを実行する
                Utils.CustomSyncAllSettings();
            }

        }
    }
    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.ProtectPlayer))]
    class PlayerControlProtectPlayerPatch
    {
        public static void Postfix(PlayerControl __instance, [HarmonyArgument(0)] PlayerControl target)
        {
            Logger.Info($"{__instance.GetNameWithRole()} => {target.GetNameWithRole()}", "ProtectPlayer");
        }
    }
    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.RemoveProtection))]
    class PlayerControlRemoveProtectionPatch
    {
        public static void Postfix(PlayerControl __instance)
        {
            Logger.Info($"{__instance.GetNameWithRole()}", "RemoveProtection");
        }
    }
}