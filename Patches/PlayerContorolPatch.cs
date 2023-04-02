using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AmongUs.GameOptions;
using HarmonyLib;
using Hazel;
using UnityEngine;

using TownOfHost.Modules;
using TownOfHost.Roles;
using TownOfHost.Roles.Core;
using TownOfHost.Roles.Impostor;
using TownOfHost.Roles.Crewmate;
using TownOfHost.Roles.Neutral;
using TownOfHost.Roles.AddOns.Crewmate;
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

            // 処理は全てCustomRoleManager側で行う
            CustomRoleManager.OnCheckMurder(__instance, target);

            return false;
        }

        // 不正キル防止チェック
        public static bool CheckForInvalidMurdering(MurderInfo info)
        {
            (var killer, var target) = info.AttemptTuple;

            // Killerが既に死んでいないかどうか
            if (!killer.IsAlive())
            {
                Logger.Info($"{killer.GetNameWithRole()}は死亡しているためキャンセルされました。", "CheckMurder");
                return false;
            }
            // targetがキル可能な状態か
            if (target.Data == null || //PlayerDataがnullじゃないか確認
                target.inVent || target.inMovingPlat) //targetの状態をチェック
            {
                Logger.Info("targetは現在キルできない状態です。", "CheckMurder");
                return false;
            }
            // targetが既に死んでいないか
            if (!target.IsAlive())
            {
                Logger.Info("targetは既に死んでいたため、キルをキャンセルしました。", "CheckMurder");
                return false;
            }
            // 会議中のキルでないか
            if (MeetingHud.Instance != null)
            {
                Logger.Info("会議が始まっていたため、キルをキャンセルしました。", "CheckMurder");
                return false;
            }

            // 連打キルでないか
            float minTime = Mathf.Max(0.02f, AmongUsClient.Instance.Ping / 1000f * 6f); //※AmongUsClient.Instance.Pingの値はミリ秒(ms)なので÷1000
            //TimeSinceLastKillに値が保存されていない || 保存されている時間がminTime以上 => キルを許可
            //↓許可されない場合
            if (TimeSinceLastKill.TryGetValue(killer.PlayerId, out var time) && time < minTime)
            {
                Logger.Info("前回のキルからの時間が早すぎるため、キルをブロックしました。", "CheckMurder");
                return false;
            }
            TimeSinceLastKill[killer.PlayerId] = 0f;

            // HideAndSeek_キルボタンが使用可能か
            if ((Options.CurrentGameMode == CustomGameMode.HideAndSeek || Options.IsStandardHAS) && Options.HideAndSeekKillDelayTimer > 0)
            {
                Logger.Info("HideAndSeekの待機時間中だったため、キルをキャンセルしました。", "CheckMurder");
                return false;
            }
            // キルが可能なプレイヤーか(遠隔は除く)
            if (!info.IsFakeSuicide && !killer.CanUseKillButton())
            {
                Logger.Info(killer.GetNameWithRole() + "はKillできないので、キルはキャンセルされました。", "CheckMurder");
                return false;
            }
            return true;
        }

        public static void OnCheckMurderAsKiller(MurderInfo info)
        {
            (var killer, var target) = info.AttemptTuple;

            //キル時の特殊判定
            if (killer.PlayerId != target.PlayerId)
            {
                var roleClass = killer.GetRoleClass();
                //自殺でない場合のみ役職チェック
                switch (killer.GetCustomRole())
                {
                    //==========インポスター役職==========//
                    case CustomRoles.SerialKiller:
                        SerialKiller.OnCheckMurder(killer);
                        break;
                    case CustomRoles.Vampire:
                        info.DoKill = Vampire.OnCheckMurder(info);
                        break;
                    case CustomRoles.Warlock:
                        if (!Main.CheckShapeshift[killer.PlayerId] && !Main.isCurseAndKill[killer.PlayerId])
                        { //Warlockが変身時以外にキルしたら、呪われる処理
                            Main.isCursed = true;
                            killer.SetKillCooldown();
                            Main.CursedPlayers[killer.PlayerId] = target;
                            Main.WarlockTimer.Add(killer.PlayerId, 0f);
                            Main.isCurseAndKill[killer.PlayerId] = true;
                            info.DoKill = false;
                            return;
                        }
                        if (Main.CheckShapeshift[killer.PlayerId])
                        {//呪われてる人がいないくて変身してるときに通常キルになる
                            killer.RpcGuardAndKill(target);
                            return;
                        }
                        if (Main.isCurseAndKill[killer.PlayerId]) killer.RpcGuardAndKill(target);
                        info.DoKill = false;
                        break;
                    case CustomRoles.Witch:
                        info.DoKill = Witch.OnCheckMurder(info);
                        break;
                    case CustomRoles.Puppeteer:
                        Main.PuppeteerList[target.PlayerId] = killer.PlayerId;
                        killer.SetKillCooldown();
                        Utils.NotifyRoles(SpecifySeer: killer);
                        info.DoKill = false;
                        break;

                    //==========マッドメイト系役職==========//

                    //==========ニュートラル役職==========//
                    case CustomRoles.Arsonist:
                        Logger.Info("Arsonist start douse", "OnCheckMurderAsKiller");
                        killer.SetKillCooldown(Options.ArsonistDouseTime.GetFloat());
                        if (!Main.isDoused[(killer.PlayerId, target.PlayerId)] && !Main.ArsonistTimer.ContainsKey(killer.PlayerId))
                        {
                            Main.ArsonistTimer.Add(killer.PlayerId, (target, 0f));
                            Utils.NotifyRoles(SpecifySeer: killer);
                            RPC.SetCurrentDousingTarget(killer.PlayerId, target.PlayerId);
                        }
                        info.DoKill = false;
                        break;
                }
            }
        }
        public static bool OnCheckMurderAsTarget(MurderInfo info)
        {
            (var killer, var target) = info.AttemptTuple;

            //キルされた時の特殊判定
            switch (target.GetCustomRole())
            {
                //==========マッドメイト系役職==========//
                case CustomRoles.MadGuardian:
                    //MadGuardianを切れるかの判定処理
                    var taskState = target.GetPlayerTaskState();
                    if (taskState.IsTaskFinished)
                    {
                        info.CanKill = false;
                        var colorCode = Utils.GetRoleColorCode(CustomRoles.MadGuardian);
                        if (!NameColorManager.TryGetData(killer, target, out var value) || value != colorCode)
                        {
                            NameColorManager.Add(killer.PlayerId, target.PlayerId);
                            if (Options.MadGuardianCanSeeWhoTriedToKill.GetBool())
                                NameColorManager.Add(target.PlayerId, killer.PlayerId, colorCode);
                            Utils.NotifyRoles();
                        }
                        return true;
                    }
                    break;
            }
            return true;

        }
    }
    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.MurderPlayer))]
    class MurderPlayerPatch
    {
        public static void Prefix(PlayerControl __instance, [HarmonyArgument(0)] PlayerControl target)
        {
            Logger.Info($"{__instance.GetNameWithRole()} => {target.GetNameWithRole()}{(target.protectedByGuardian ? "(Protected)" : "")}", "MurderPlayer");

            if (RandomSpawn.CustomNetworkTransformPatch.NumOfTP.TryGetValue(__instance.PlayerId, out var num) && num > 2) RandomSpawn.CustomNetworkTransformPatch.NumOfTP[__instance.PlayerId] = 3;
            if (!target.protectedByGuardian)
                Camouflage.RpcSetSkin(target, ForceRevert: true);
        }
        public static void Postfix(PlayerControl __instance, [HarmonyArgument(0)] PlayerControl target)
        {
            if (target.AmOwner) RemoveDisableDevicesPatch.UpdateDisableDevices();
            if (!target.Data.IsDead || !AmongUsClient.Instance.AmHost) return;
            //以降ホストしか処理しない
            // 処理は全てCustomRoleManager側で行う
            CustomRoleManager.OnMurderPlayer(__instance, target);
        }
    }
    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.Shapeshift))]
    class ShapeshiftPatch
    {
        public static void Prefix(PlayerControl __instance, [HarmonyArgument(0)] PlayerControl target)
        {
            Logger.Info($"{__instance?.GetNameWithRole()} => {target?.GetNameWithRole()}", "Shapeshift");

            var shapeshifter = __instance;
            var shapeshifting = shapeshifter.PlayerId != target.PlayerId;

            if (Main.CheckShapeshift.TryGetValue(shapeshifter.PlayerId, out var last) && last == shapeshifting)
            {
                Logger.Info($"{__instance?.GetNameWithRole()}:Cancel Shapeshift.Prefix", "Shapeshift");
                return;
            }

            Main.CheckShapeshift[shapeshifter.PlayerId] = shapeshifting;
            Main.ShapeshiftTarget[shapeshifter.PlayerId] = target.PlayerId;

            shapeshifter.GetRoleClass()?.OnShapeshift(target);

            if (!AmongUsClient.Instance.AmHost) return;

            if (!shapeshifting) Camouflage.RpcSetSkin(__instance);

            switch (shapeshifter.GetCustomRole())
            {
                case CustomRoles.EvilTracker:
                    EvilTracker.OnShapeshift(shapeshifter, target, shapeshifting);
                    break;
                case CustomRoles.FireWorks:
                    FireWorks.ShapeShiftState(shapeshifter, shapeshifting);
                    break;
                case CustomRoles.Warlock:
                    if (Main.CursedPlayers[shapeshifter.PlayerId] != null)//呪われた人がいるか確認
                    {
                        if (shapeshifting && !Main.CursedPlayers[shapeshifter.PlayerId].Data.IsDead)//変身解除の時に反応しない
                        {
                            var cp = Main.CursedPlayers[shapeshifter.PlayerId];
                            Vector2 cppos = cp.transform.position;//呪われた人の位置
                            Dictionary<PlayerControl, float> cpdistance = new();
                            float dis;
                            foreach (PlayerControl p in Main.AllAlivePlayerControls)
                            {
                                if (p != cp)
                                {
                                    dis = Vector2.Distance(cppos, p.transform.position);
                                    cpdistance.Add(p, dis);
                                    Logger.Info($"{p?.Data?.PlayerName}の位置{dis}", "Warlock");
                                }
                            }
                            var min = cpdistance.OrderBy(c => c.Value).FirstOrDefault();//一番小さい値を取り出す
                            PlayerControl targetw = min.Key;
                            targetw.SetRealKiller(shapeshifter);
                            Logger.Info($"{targetw.GetNameWithRole()}was killed", "Warlock");
                            cp.RpcMurderPlayerV2(targetw);//殺す
                            shapeshifter.RpcGuardAndKill(shapeshifter);
                            Main.isCurseAndKill[shapeshifter.PlayerId] = false;
                        }
                        Main.CursedPlayers[shapeshifter.PlayerId] = null;
                    }
                    break;
            }

            if (shapeshifter.CanMakeMadmate() && shapeshifting)
            {//変身したとき一番近い人をマッドメイトにする処理
                Vector2 shapeshifterPosition = shapeshifter.transform.position;//変身者の位置
                Dictionary<PlayerControl, float> mpdistance = new();
                float dis;
                foreach (var p in Main.AllAlivePlayerControls)
                {
                    if (p.Data.Role.Role != RoleTypes.Shapeshifter && !p.Is(CustomRoleTypes.Impostor) && !p.Is(CustomRoles.SKMadmate))
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
                    Utils.MarkEveryoneDirtySettings();
                    Utils.NotifyRoles();
                }
            }

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
        public static Dictionary<byte, bool> CanReport;
        public static Dictionary<byte, List<GameData.PlayerInfo>> WaitReport = new();
        public static bool Prefix(PlayerControl __instance, [HarmonyArgument(0)] GameData.PlayerInfo target)
        {
            if (GameStates.IsMeeting) return false;
            Logger.Info($"{__instance.GetNameWithRole()} => {target?.Object?.GetNameWithRole() ?? "null"}", "ReportDeadBody");
            if (Options.IsStandardHAS && target != null && __instance == target.Object) return true; //[StandardHAS] ボタンでなく、通報者と死体が同じなら許可
            if (Options.CurrentGameMode == CustomGameMode.HideAndSeek || Options.IsStandardHAS) return false;
            if (!CanReport[__instance.PlayerId])
            {
                WaitReport[__instance.PlayerId].Add(target);
                Logger.Warn($"{__instance.GetNameWithRole()}:通報禁止中のため可能になるまで待機します", "ReportDeadBody");
                return false;
            }
            foreach (var kvp in Main.PlayerStates)
            {
                var pc = Utils.GetPlayerById(kvp.Key);
                kvp.Value.LastRoom = pc.GetPlainShipRoom();
            }
            if (!AmongUsClient.Instance.AmHost) return true;

            //通報者が死んでいる場合、本処理で会議がキャンセルされるのでここで止める
            if (__instance.Data.IsDead) return false;

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

            //=============================================
            //以下、ボタンが押されることが確定したものとする。
            //=============================================


            Main.ArsonistTimer.Clear();
            Main.PuppeteerList.Clear();

            foreach (var role in CustomRoleManager.AllActiveRoles)
            {
                role.OnReportDeadBody(__instance, target);
            }

            SerialKiller.OnReportDeadBody();
            Vampire.OnStartMeeting();

            Main.AllPlayerControls
                .Where(pc => Main.CheckShapeshift.ContainsKey(pc.PlayerId))
                .Do(pc => Camouflage.RpcSetSkin(pc, RevertToDefault: true));
            MeetingTimeManager.OnReportDeadBody();

            Utils.NotifyRoles(isForMeeting: true, NoCache: true);

            Utils.SyncAllSettings();
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
        private static StringBuilder Mark = new(20);
        private static StringBuilder Suffix = new(120);
        public static void Postfix(PlayerControl __instance)
        {
            var player = __instance;

            if (!GameStates.IsModHost) return;

            TargetArrow.OnFixedUpdate(player);

            if (GameStates.IsInTask)
                __instance.GetRoleClass()?.OnFixedUpdate();

            if (AmongUsClient.Instance.AmHost)
            {//実行クライアントがホストの場合のみ実行
                if (GameStates.IsLobby && (ModUpdater.hasUpdate || ModUpdater.isBroken || !Main.AllowPublicRoom) && AmongUsClient.Instance.IsGamePublic)
                    AmongUsClient.Instance.ChangeGamePublic(false);

                if (GameStates.IsInTask && ReportDeadBodyPatch.CanReport[__instance.PlayerId] && ReportDeadBodyPatch.WaitReport[__instance.PlayerId].Count > 0)
                {
                    var info = ReportDeadBodyPatch.WaitReport[__instance.PlayerId][0];
                    ReportDeadBodyPatch.WaitReport[__instance.PlayerId].Clear();
                    Logger.Info($"{__instance.GetNameWithRole()}:通報可能になったため通報処理を行います", "ReportDeadbody");
                    __instance.ReportDeadBody(info);
                }

                DoubleTrigger.OnFixedUpdate(player);
                Vampire.OnFixedUpdate(player);

                if (GameStates.IsInTask && CustomRoles.SerialKiller.IsPresent()) SerialKiller.FixedUpdate(player);
                if (GameStates.IsInTask && Main.WarlockTimer.ContainsKey(player.PlayerId))//処理を1秒遅らせる
                {
                    if (player.IsAlive())
                    {
                        if (Main.WarlockTimer[player.PlayerId] >= 1f)
                        {
                            player.RpcResetAbilityCooldown();
                            Main.isCursed = false;//変身クールを１秒に変更
                            player.SyncSettings();
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
                            player.SetKillCooldown();
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
                        foreach (var target in Main.AllAlivePlayerControls)
                        {
                            if (target.PlayerId != player.PlayerId && !target.Is(CountTypes.Impostor))
                            {
                                dis = Vector2.Distance(puppeteerPos, target.transform.position);
                                targetDistance.Add(target.PlayerId, dis);
                            }
                        }
                        if (targetDistance.Count() != 0)
                        {
                            var min = targetDistance.OrderBy(c => c.Value).FirstOrDefault();//一番値が小さい
                            PlayerControl target = Utils.GetPlayerById(min.Key);
                            var KillRange = NormalGameOptionsV07.KillDistances[Mathf.Clamp(Main.NormalOptions.KillDistance, 0, 2)];
                            if (min.Value <= KillRange && player.CanMove && target.CanMove)
                            {
                                var puppeteerId = Main.PuppeteerList[player.PlayerId];
                                RPC.PlaySoundRPC(puppeteerId, Sounds.KillSound);
                                target.SetRealKiller(Utils.GetPlayerById(puppeteerId));
                                player.RpcMurderPlayer(target);
                                Utils.MarkEveryoneDirtySettings();
                                Main.PuppeteerList.Remove(player.PlayerId);
                                Utils.NotifyRoles();
                            }
                        }
                    }
                }
                if (GameStates.IsInTask && player == PlayerControl.LocalPlayer)
                    DisableDevice.FixedUpdate();

                if (GameStates.IsInGame && Main.RefixCooldownDelay <= 0)
                    foreach (var pc in Main.AllPlayerControls)
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
                if (GameStates.IsInTask && !(__instance.Is(CustomRoleTypes.Impostor) || __instance.Is(CustomRoles.Egoist)) && __instance.CanUseKillButton() && !__instance.Data.IsDead)
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
                    var RoleTextData = Utils.GetRoleText(__instance.PlayerId);
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
                    if (!AmongUsClient.Instance.IsGameStarted && AmongUsClient.Instance.NetworkMode != NetworkModes.FreePlay)
                    {
                        RoleText.enabled = false; //ゲームが始まっておらずフリープレイでなければロールを非表示
                        if (!__instance.AmOwner) __instance.cosmetics.nameText.text = __instance?.Data?.PlayerName;
                    }
                    if (Main.VisibleTasksCount) //他プレイヤーでVisibleTasksCountは有効なら
                        RoleText.text += Utils.GetProgressText(__instance); //ロールの横にタスクなど進行状況表示


                    //変数定義
                    var seer = PlayerControl.LocalPlayer;
                    var seerRole = seer.GetRoleClass();
                    var target = __instance;
                    string RealName;
                    Mark.Clear();
                    Suffix.Clear();

                    //名前変更
                    RealName = target.GetRealName();

                    //名前色変更処理
                    //自分自身の名前の色を変更
                    if (target.AmOwner && AmongUsClient.Instance.IsGameStarted)
                    { //targetが自分自身
                        if (target.Is(CustomRoles.Arsonist) && target.IsDouseDone())
                            RealName = Utils.ColorString(Utils.GetRoleColor(CustomRoles.Arsonist), GetString("EnterVentToWin"));
                    }

                    //NameColorManager準拠の処理
                    RealName = RealName.ApplyNameColorData(seer, target, false);

                    //seer役職が対象のMark
                    Mark.Append(seerRole?.GetMark(seer, target, false));
                    //seerに関わらず発動するMark
                    Mark.Append(CustomRoleManager.GetMarkOthers(seer, target, false));

                    if (seer.GetCustomRole().IsImpostor()) //seerがインポスター
                    {
                        if (target.Is(CustomRoles.MadSnitch) && target.GetPlayerTaskState().IsTaskFinished && Options.MadSnitchCanAlsoBeExposedToImpostor.GetBool()) //targetがタスクを終わらせたマッドスニッチ
                            Mark.Append(Utils.ColorString(Utils.GetRoleColor(CustomRoles.MadSnitch), "★")); //targetにマーク付与
                    }

                    if (seer.Is(CustomRoles.Arsonist))
                    {
                        if (seer.IsDousedPlayer(target))
                        {
                            Mark.Append($"<color={Utils.GetRoleColorCode(CustomRoles.Arsonist)}>▲</color>");
                        }
                        else if (
                            Main.currentDousingTarget != 255 &&
                            Main.currentDousingTarget == target.PlayerId
                        )
                        {
                            Mark.Append($"<color={Utils.GetRoleColorCode(CustomRoles.Arsonist)}>△</color>");
                        }
                    }
                    Mark.Append(Executioner.TargetMark(seer, target));
                    if (seer.Is(CustomRoles.Puppeteer))
                    {
                        if (seer.Is(CustomRoles.Puppeteer) &&
                        Main.PuppeteerList.ContainsValue(seer.PlayerId) &&
                        Main.PuppeteerList.ContainsKey(target.PlayerId))
                            Mark.Append($"<color={Utils.GetRoleColorCode(CustomRoles.Impostor)}>◆</color>");
                    }
                    if (seer.Is(CustomRoles.EvilTracker)) Mark.Append(EvilTracker.GetTargetMark(seer, target));

                    //ハートマークを付ける(会議中MOD視点)
                    if (__instance.Is(CustomRoles.Lovers) && PlayerControl.LocalPlayer.Is(CustomRoles.Lovers))
                    {
                        Mark.Append($"<color={Utils.GetRoleColorCode(CustomRoles.Lovers)}>♡</color>");
                    }
                    else if (__instance.Is(CustomRoles.Lovers) && PlayerControl.LocalPlayer.Data.IsDead)
                    {
                        Mark.Append($"<color={Utils.GetRoleColorCode(CustomRoles.Lovers)}>♡</color>");
                    }

                    //seerに関わらず発動するLowerText
                    Suffix.Append(CustomRoleManager.GetLowerTextOthers(seer, target));

                    //seer役職が対象のSuffix
                    Suffix.Append(seerRole?.GetSuffix(seer, target));

                    //seerに関わらず発動するSuffix
                    Suffix.Append(CustomRoleManager.GetSuffixOthers(seer, target));

                    Suffix.Append(EvilTracker.GetTargetArrow(seer, target));

                    /*if(main.AmDebugger.Value && main.BlockKilling.TryGetValue(target.PlayerId, out var isBlocked)) {
                        Mark = isBlocked ? "(true)" : "(false)";
                    }*/
                    if (Utils.IsActive(SystemTypes.Comms) && Options.CommsCamouflage.GetBool())
                        RealName = $"<size=0>{RealName}</size> ";

                    string DeathReason = seer.Data.IsDead && seer.KnowDeathReason(target) ? $"({Utils.ColorString(Utils.GetRoleColor(CustomRoles.Doctor), Utils.GetVitalText(target.PlayerId))})" : "";
                    //Mark・Suffixの適用
                    target.cosmetics.nameText.text = $"{RealName}{DeathReason}{Mark}";

                    if (Suffix.ToString() != "")
                    {
                        //名前が2行になると役職テキストを上にずらす必要がある
                        RoleText.transform.SetLocalY(0.35f);
                        target.cosmetics.nameText.text += "\r\n" + Suffix.ToString();

                    }
                    else
                    {
                        //役職テキストの座標を初期値に戻す
                        RoleText.transform.SetLocalY(0.2f);
                    }
                }
                else
                {
                    //役職テキストの座標を初期値に戻す
                    RoleText.transform.SetLocalY(0.2f);
                }
            }
        }
        //FIXME: 役職クラス化のタイミングで、このメソッドは移動予定
        public static void LoversSuicide(byte deathId = 0x7f, bool isExiled = false)
        {
            if (CustomRoles.Lovers.IsPresent() && Main.isLoversDead == false)
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
                            Main.PlayerStates[partnerPlayer.PlayerId].deathReason = PlayerState.DeathReason.FollowingSuicide;
                            if (isExiled)
                                CheckForEndVotingPatch.TryAddAfterMeetingDeathPlayers(PlayerState.DeathReason.FollowingSuicide, partnerPlayer.PlayerId);
                            else
                                partnerPlayer.RpcMurderPlayer(partnerPlayer);
                        }
                    }
                }
            }
        }
    }
    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.Start))]
    class PlayerStartPatch
    {
        public static void Postfix(PlayerControl __instance)
        {
            var roleText = UnityEngine.Object.Instantiate(__instance.cosmetics.nameText);
            roleText.transform.SetParent(__instance.cosmetics.nameText.transform);
            roleText.transform.localPosition = new Vector3(0f, 0.2f, 0f);
            roleText.fontSize -= 1.2f;
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

            Witch.OnEnterVent(pc);
            if (pc.Is(CustomRoles.Mayor))
            {
                if (pc.GetRoleClass() is Mayor mayor && mayor.UsedButtonCount < Mayor.NumOfUseButton)
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
                    foreach (var pc in Main.AllAlivePlayerControls)
                    {
                        if (pc != __instance.myPlayer)
                        {
                            //生存者は焼殺
                            pc.SetRealKiller(__instance.myPlayer);
                            pc.RpcMurderPlayer(pc);
                            Main.PlayerStates[pc.PlayerId].deathReason = PlayerState.DeathReason.Torched;
                            Main.PlayerStates[pc.PlayerId].SetDead();
                        }
                        else
                            RPC.PlaySoundRPC(pc.PlayerId, Sounds.KillSound);
                    }
                    CustomWinnerHolder.ShiftWinnerAndSetWinner(CustomWinner.Arsonist); //焼殺で勝利した人も勝利させる
                    CustomWinnerHolder.WinnerIds.Add(__instance.myPlayer.PlayerId);
                    return true;
                }
                if ((__instance.myPlayer.Data.Role.Role != RoleTypes.Engineer && //エンジニアでなく
                !__instance.myPlayer.CanUseImpostorVentButton()) || //インポスターベントも使えない
                (__instance.myPlayer.GetRoleClass() is Mayor mayor && mayor.UsedButtonCount >= Mayor.NumOfUseButton)
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
        public static bool Prefix(PlayerControl __instance)
        {
            var pc = __instance;

            Logger.Info($"TaskComplete:{pc.GetNameWithRole()}", "CompleteTask");
            var taskState = pc.GetPlayerTaskState();
            taskState.Update(pc);

            var roleClass = pc.GetRoleClass();
            var ret = true;
            if (roleClass != null)
            {
                ret = roleClass.OnCompleteTask();
            }
            else
            {
                ret = Workhorse.OnCompleteTask(pc);
                var isTaskFinish = taskState.IsTaskFinished;
                if (isTaskFinish && pc.Is(CustomRoles.MadSnitch))
                {
                    foreach (var impostor in Main.AllAlivePlayerControls.Where(pc => pc.Is(CustomRoleTypes.Impostor)))
                    {
                        NameColorManager.Add(pc.PlayerId, impostor.PlayerId);
                    }
                }
                if (pc.Is(CustomRoles.SpeedBooster))
                {
                    //FIXME:SpeedBooster class transplant
                    if (pc.IsAlive()
                    && (isTaskFinish || (taskState.CompletedTasksCount) >= Options.SpeedBoosterTaskTrigger.GetInt())
                    && !Main.SpeedBoostTarget.ContainsKey(pc.PlayerId))
                    {   //ｽﾋﾟﾌﾞが生きていて、全タスク完了orトリガー数までタスクを完了していて、SpeedBoostTargetに登録済みでない場合
                        var rand = IRandom.Instance;
                        List<PlayerControl> targetPlayers = new();
                        //切断者と死亡者を除外
                        foreach (var p in Main.AllAlivePlayerControls)
                        {
                            if (!Main.SpeedBoostTarget.ContainsValue(p.PlayerId)) targetPlayers.Add(p);
                        }
                        //ターゲットが0ならアップ先をプレイヤーをnullに
                        if (targetPlayers.Count >= 1)
                        {
                            PlayerControl target = targetPlayers[rand.Next(0, targetPlayers.Count)];
                            Logger.Info("スピードブースト先:" + target.cosmetics.nameText.text, "SpeedBooster");
                            Main.SpeedBoostTarget.Add(pc.PlayerId, target.PlayerId);
                            Main.AllPlayerSpeed[Main.SpeedBoostTarget[pc.PlayerId]] += Options.SpeedBoosterUpSpeed.GetFloat();
                            target.MarkDirtySettings();
                        }
                        else
                        {
                            Main.SpeedBoostTarget.Add(pc.PlayerId, 255);
                            Logger.SendInGame("Error.SpeedBoosterNullException");
                            Logger.Warn("スピードブースト先がnullです。", "SpeedBooster");
                        }
                    }
                }
                if (isTaskFinish && pc.GetCustomRole() is CustomRoles.Lighter or CustomRoles.Doctor)
                {
                    Utils.MarkEveryoneDirtySettings();
                }
            }
            Utils.NotifyRoles();
            return ret;
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
    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.RpcSetRole))]
    class PlayerControlSetRolePatch
    {
        public static bool Prefix(PlayerControl __instance, ref RoleTypes roleType)
        {
            var target = __instance;
            var targetName = __instance.GetNameWithRole();
            Logger.Info($"{targetName} =>{roleType}", "PlayerControl.RpcSetRole");
            if (!ShipStatus.Instance.enabled) return true;
            if (roleType is RoleTypes.CrewmateGhost or RoleTypes.ImpostorGhost)
            {
                var targetRequireResetCam = target.GetCustomRole().GetRoleInfo()?.RequireResetCam ?? false;
                var targetIsKiller = target.Is(CustomRoleTypes.Impostor) || Main.ResetCamPlayerList.Contains(target.PlayerId) || targetRequireResetCam;
                var ghostRoles = new Dictionary<PlayerControl, RoleTypes>();
                foreach (var seer in Main.AllPlayerControls)
                {
                    var self = seer.PlayerId == target.PlayerId;
                    var seerRequireResetCam = seer.GetCustomRole().GetRoleInfo()?.RequireResetCam ?? false;
                    var seerIsKiller = seer.Is(CustomRoleTypes.Impostor) || Main.ResetCamPlayerList.Contains(seer.PlayerId) || seerRequireResetCam;

                    if ((self && targetIsKiller) || (!seerIsKiller && target.Is(CustomRoleTypes.Impostor)))
                    {
                        ghostRoles[seer] = RoleTypes.ImpostorGhost;
                    }
                    else
                    {
                        ghostRoles[seer] = RoleTypes.CrewmateGhost;
                    }
                }
                if (ghostRoles.All(kvp => kvp.Value == RoleTypes.CrewmateGhost))
                {
                    roleType = RoleTypes.CrewmateGhost;
                }
                else if (ghostRoles.All(kvp => kvp.Value == RoleTypes.ImpostorGhost))
                {
                    roleType = RoleTypes.ImpostorGhost;
                }
                else
                {
                    foreach ((var seer, var role) in ghostRoles)
                    {
                        Logger.Info($"Desync {targetName} =>{role} for{seer.GetNameWithRole()}", "PlayerControl.RpcSetRole");
                        target.RpcSetRoleDesync(role, seer.GetClientId());
                    }
                    return false;
                }
            }
            return true;
        }
    }
}