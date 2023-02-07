using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AmongUs.GameOptions;
using HarmonyLib;
using Hazel;
using MS.Internal.Xml.XPath;
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
            if (killer.PlayerId != target.PlayerId && !killer.CanUseKillButton())
            {
                Logger.Info(killer.GetNameWithRole() + "はKillできないので、キルはキャンセルされました。", "CheckMurder");
                return false;
            }

            //キルされた時の特殊判定
            switch (target.GetCustomRole())
            {
                case CustomRoles.Luckey:
                    if (killer.Is(CustomRoles.ChivalrousExpert) && ChivalrousExpert.isKilled(killer.PlayerId)) return false;
                    System.Random rd = Utils.RandomSeedByGuid();
                    if (rd.Next(0, 100) < Options.LuckeyProbability.GetInt())
                    {
                        killer.RpcGuardAndKill(target);
                        return false;
                    }
                    break;

                case CustomRoles.SchrodingerCat:
                    if (!SchrodingerCat.OnCheckMurder(killer, target))
                        return false;
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
                            killer.SetKillCooldown();
                            Main.BitPlayers.Add(target.PlayerId, (killer.PlayerId, 0f));
                            return false;
                        }
                        break;
                    case CustomRoles.Warlock:
                        if (!Main.CheckShapeshift[killer.PlayerId] && !Main.isCurseAndKill[killer.PlayerId])
                        { //Warlockが変身時以外にキルしたら、呪われる処理
                            if (target.Is(CustomRoles.Needy)) return false;
                            Main.isCursed = true;
                            killer.SetKillCooldown();
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
                    case CustomRoles.Assassin:
                        if (!Main.CheckShapeshift[killer.PlayerId] && !Main.isMarkAndKill[killer.PlayerId])
                        { //Assassinが変身時以外にキルしたら
                            Main.isMarked = true;
                            killer.SetKillCooldown();
                            Main.MarkedPlayers[killer.PlayerId] = target;
                            Main.AssassinTimer.Add(killer.PlayerId, 0f);
                            Main.isMarkAndKill[killer.PlayerId] = true;
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
                        if (!Witch.OnCheckMurder(killer, target))
                        {
                            //Spellモードの場合は終了
                            return false;
                        }
                        break;
                    case CustomRoles.Puppeteer:
                        if (target.Is(CustomRoles.Needy)) return false;
                        Main.PuppeteerList[target.PlayerId] = killer.PlayerId;
                        killer.SetKillCooldown();
                        Utils.NotifyRoles(SpecifySeer: killer);
                        return false;
                    case CustomRoles.TimeThief:
                        TimeThief.OnCheckMurder(killer);
                        break;

                    //==========マッドメイト系役職==========//

                    //==========第三陣営役職==========//
                    case CustomRoles.Arsonist:
                        killer.SetKillCooldown(Options.ArsonistDouseTime.GetFloat());
                        if (!Main.isDoused[(killer.PlayerId, target.PlayerId)] && !Main.ArsonistTimer.ContainsKey(killer.PlayerId))
                        {
                            Main.ArsonistTimer.Add(killer.PlayerId, (target, 0f));
                            Utils.NotifyRoles(SpecifySeer: __instance);
                            RPC.SetCurrentDousingTarget(killer.PlayerId, target.PlayerId);
                        }
                        return false;

                    //==========クルー役職==========//
                    case CustomRoles.Sheriff:
                        if (!Sheriff.OnCheckMurder(killer, target))
                            return false;
                        break;
                    case CustomRoles.ChivalrousExpert:
                        if (!ChivalrousExpert.isKilled(killer.PlayerId)) ChivalrousExpert.killed.Add(killer.PlayerId);
                        else return false;
                        break;
                    case CustomRoles.Bomber:
                        return false;
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
            Camouflage.RpcSetSkin(target, ForceRevert: true);
        }
        public static void Postfix(PlayerControl __instance, [HarmonyArgument(0)] PlayerControl target)
        {
            if (target.AmOwner) RemoveDisableDevicesPatch.UpdateDisableDevices();
            if (!target.Data.IsDead || !AmongUsClient.Instance.AmHost) return;

            PlayerControl killer = __instance; //読み替え変数
            if (Main.PlayerStates[target.PlayerId].deathReason == PlayerState.DeathReason.Sniped)
            {
                killer = Utils.GetPlayerById(Sniper.GetSniper(target.PlayerId));
            }
            if (Main.PlayerStates[target.PlayerId].deathReason == PlayerState.DeathReason.etc)
            {
                //死因が設定されていない場合は死亡判定
                Main.PlayerStates[target.PlayerId].deathReason = PlayerState.DeathReason.Kill;
            }

            //骇客击杀
            bool hackKilled = false;
            if (killer.GetCustomRole() == CustomRoles.Hacker && Main.HackerUsedCount.TryGetValue(killer.PlayerId, out var count) && count < Options.HackUsedMaxTime.GetInt())
            {
                hackKilled = true;
                Main.HackerUsedCount[killer.PlayerId] += 1;
                List<PlayerControl> playerList = new();
                foreach (PlayerControl pc in PlayerControl.AllPlayerControls)
                    if (!pc.Data.IsDead && !(pc.GetCustomRole() == CustomRoles.Hacker) && !(pc.GetCustomRole() == CustomRoles.Needy)) playerList.Add(pc);
                if (playerList.Count < 1)
                {
                    Logger.Info(target?.Data?.PlayerName + "被骇客击杀，但无法找到骇入目标", "MurderPlayer");
                    new LateTask(() => killer.CmdReportDeadBody(target.Data), 0.15f, "Hacker Self Report");
                }
                else
                {
                    System.Random rd = new();
                    int hackinPlayer = rd.Next(0, playerList.Count);
                    if (playerList[hackinPlayer] == null) hackinPlayer = 0;
                    Logger.Info(target?.Data?.PlayerName + "被骇客击杀，随机报告者：" + playerList[hackinPlayer]?.Data?.PlayerName, "MurderPlayer");
                    new LateTask(() => playerList[hackinPlayer].CmdReportDeadBody(target.Data), 0.15f, "Hacker Hackin Report");
                }
            }

            //When Bait is killed
            if (target.GetCustomRole() == CustomRoles.Bait && killer.PlayerId != target.PlayerId && !hackKilled)
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
            if (target.Is(CustomRoles.CyberStar) && Main.CyberStarDead.Contains(target.PlayerId))
                Main.CyberStarDead.Add(target.PlayerId);
            if (killer.Is(CustomRoles.Sans))
            {
                if (!Main.SansKillCooldown.ContainsKey(killer.PlayerId))
                    Main.SansKillCooldown.Add(killer.PlayerId, Options.SansDefaultKillCooldown.GetFloat());
                Main.SansKillCooldown[killer.PlayerId] -= Options.SansReduceKillCooldown.GetFloat();
                if (Main.SansKillCooldown[killer.PlayerId] < Options.SansMinKillCooldown.GetFloat())
                    Main.SansKillCooldown[killer.PlayerId] = Options.SansMinKillCooldown.GetFloat();
            }
            if (killer.Is(CustomRoles.BoobyTrap) && killer != target)
            {
                if (!Main.BoobyTrapBody.Contains(target.PlayerId)) Main.BoobyTrapBody.Add(target.PlayerId);
                if (!Main.KillerOfBoobyTrapBody.ContainsKey(target.PlayerId)) Main.KillerOfBoobyTrapBody.Add(target.PlayerId, killer.PlayerId);
                Main.PlayerStates[killer.PlayerId].deathReason = PlayerState.DeathReason.Misfire;
                killer.RpcMurderPlayer(killer);
            }

            FixedUpdatePatch.LoversSuicide(target.PlayerId);

            Main.PlayerStates[target.PlayerId].SetDead();
            target.SetRealKiller(__instance, true); //既に追加されてたらスキップ
            Utils.CountAliveImpostors();
            Utils.SyncAllSettings();
            Utils.NotifyRoles();
            Utils.TargetDies(__instance, target);
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
            Main.ShapeshiftTarget[shapeshifter.PlayerId] = target.PlayerId;

            if (!shapeshifting) Camouflage.RpcSetSkin(__instance);

            //逃逸者的变形
            if (shapeshifter.Is(CustomRoles.Escapee))
            {
                if (shapeshifting)
                {
                    if (Main.EscapeeLocation.ContainsKey(shapeshifter.PlayerId))
                    {
                        var position = Main.EscapeeLocation[shapeshifter.PlayerId];
                        Main.EscapeeLocation.Remove(shapeshifter.PlayerId);
                        Logger.Msg($"{shapeshifter.GetNameWithRole()}:{position}", "EscapeeTeleport");
                        Utils.TP(shapeshifter.NetTransform, new Vector2(position.x, position.y + 0.3636f));
                    }
                    else
                    {
                        Main.EscapeeLocation.Add(shapeshifter.PlayerId, shapeshifter.GetTruePosition());
                    }
                }
            }

            //矿工传送
            if (shapeshifter.Is(CustomRoles.Miner))
            {
                if (Main.LastEnteredVent.ContainsKey(shapeshifter.PlayerId))
                {
                    int ventId = Main.LastEnteredVent[shapeshifter.PlayerId].Id;
                    var vent = Main.LastEnteredVent[shapeshifter.PlayerId];
                    var position = Main.LastEnteredVentLocation[shapeshifter.PlayerId];
                    Logger.Msg($"{shapeshifter.GetNameWithRole()}:{position}", "MinerTeleport");
                    Utils.TP(shapeshifter.NetTransform, new Vector2(position.x, position.y + 0.3636f));
                }
            }

            //自爆兵自爆了
            if (shapeshifter.Is(CustomRoles.Bomber) && shapeshifting)
            {
                Logger.Info("炸弹爆炸了", "Boom");
                foreach (var tg in Main.AllPlayerControls)
                {
                    tg.KillFlash();
                    var pos = shapeshifter.transform.position;
                    var dis = Vector2.Distance(pos, tg.transform.position);

                    if (!tg.IsAlive()) continue;
                    if (dis > Options.BomberRadius.GetFloat()) continue;
                    if (tg == shapeshifter) continue;

                    Main.PlayerStates[tg.PlayerId].deathReason = PlayerState.DeathReason.Bombed;
                    tg.SetRealKiller(shapeshifter);
                    tg.RpcMurderPlayer(tg);
                }
                new LateTask(() =>
                {
                    var totalAlive = Main.AllAlivePlayerControls.Count();
                    //自分が最後の生き残りの場合は勝利のために死なない
                    if (totalAlive != 1)
                    {
                        Main.PlayerStates[shapeshifter.PlayerId].deathReason = PlayerState.DeathReason.Misfire;
                        shapeshifter.RpcMurderPlayer(shapeshifter);
                    }
                    Utils.NotifyRoles();
                }, 1f, "Bomber Suiscide");
            }

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
                        foreach (PlayerControl p in Main.AllAlivePlayerControls)
                        {
                            if (!Options.WarlockCanKillSelf.GetBool() && cp == p) continue;
                            if (!Options.WarlockCanKillAllies.GetBool() && p.Is(CustomRoles.Impostor)) continue;
                            dis = Vector2.Distance(cppos, p.transform.position);
                            cpdistance.Add(p, dis);
                            Logger.Info($"{p?.Data?.PlayerName}の位置{dis}", "Warlock");
                        }
                        var min = cpdistance.OrderBy(c => c.Value).FirstOrDefault();//一番小さい値を取り出す
                        PlayerControl targetw = min.Key;
                        targetw.SetRealKiller(shapeshifter);
                        Logger.Info($"{targetw.GetNameWithRole()} was killed", "Warlock");
                        cp.RpcMurderPlayerV2(targetw);//殺す
                        shapeshifter.RpcGuardAndKill(shapeshifter);
                        Main.isCurseAndKill[shapeshifter.PlayerId] = false;
                    }
                    Main.CursedPlayers[shapeshifter.PlayerId] = null;
                }
            }

            //刺客刺杀
            if (shapeshifter.Is(CustomRoles.Assassin))
            {
                if (Main.MarkedPlayers[shapeshifter.PlayerId] != null)//确认被标记的人
                {
                    if (shapeshifting && !Main.MarkedPlayers[shapeshifter.PlayerId].Data.IsDead)//解除变形时不执行操作
                    {
                        PlayerControl targetw = Main.MarkedPlayers[shapeshifter.PlayerId];
                        targetw.SetRealKiller(shapeshifter);
                        Logger.Info($"{targetw.GetNameWithRole()} was killed", "Assassin");
                        new LateTask(() =>
                        {
                            shapeshifter.RpcMurderPlayer(targetw);//殺す
                        }, 1f, "Assassin Kill");
                        Main.isMarkAndKill[shapeshifter.PlayerId] = false;
                    }
                    Main.MarkedPlayers[shapeshifter.PlayerId] = null;
                }
            }

            if (shapeshifter.Is(CustomRoles.EvilTracker)) EvilTracker.Shapeshift(shapeshifter, target, shapeshifting);

            if (shapeshifter.CanMakeMadmate() && shapeshifting)
            {//変身したとき一番近い人をマッドメイトにする処理
                Vector2 shapeshifterPosition = shapeshifter.transform.position;//変身者の位置
                Dictionary<PlayerControl, float> mpdistance = new();
                float dis;
                foreach (var p in Main.AllAlivePlayerControls)
                {
                    if (p.Data.Role.Role != RoleTypes.Shapeshifter && !p.Is(RoleType.Impostor) && !p.Is(CustomRoles.SKMadmate))
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

            //杀戮机器无法报告或拍灯
            if (__instance.Is(CustomRoles.Minimalism)) return false;

            if (target == null) //拍灯事件
            {
                if (__instance.Is(CustomRoles.Mayor)) Main.MayorUsedButtonCount[__instance.PlayerId] += 1;
                if (__instance.Is(CustomRoles.Jester) && !Options.JesterCanUseButton.GetBool()) return false;
            }
            else //报告尸体事件
            {
                if (Main.BoobyTrapBody.Contains(target.PlayerId) && __instance.IsAlive()) //报告了诡雷尸体
                {
                    var killerID = Main.KillerOfBoobyTrapBody[target.PlayerId];
                    Main.PlayerStates[__instance.PlayerId].deathReason = PlayerState.DeathReason.Bombed;
                    __instance.SetRealKiller(Utils.GetPlayerById(killerID));

                    __instance.RpcMurderPlayer(__instance);
                    RPC.PlaySoundRPC(killerID, Sounds.KillSound);

                    if (!Main.BoobyTrapBody.Contains(__instance.PlayerId)) Main.BoobyTrapBody.Add(__instance.PlayerId); 
                    if (!Main.KillerOfBoobyTrapBody.ContainsKey(__instance.PlayerId)) Main.KillerOfBoobyTrapBody.Add(__instance.PlayerId, killerID);
                    return false;
                }
                if (__instance.Is(CustomRoles.Detective))
                {
                    var tpc = Utils.GetPlayerById(target.PlayerId);
                    string msg;
                    msg = string.Format(GetString("DetectiveNoticeVictim"), tpc.GetRealName(), tpc.GetDisplayRoleName());
                    if (Options.DetectiveCanknowKiller.GetBool()) msg += "；" + string.Format(GetString("DetectiveNoticeKiller"), tpc.GetRealKiller().GetDisplayRoleName());
                    Main.DetectiveNotify.Add(__instance.PlayerId, msg);
                }
            }
            foreach (var kvp in Main.PlayerStates)
            {
                var pc = Utils.GetPlayerById(kvp.Key);
                kvp.Value.LastRoom = pc.GetPlainShipRoom();
            }
            if (!AmongUsClient.Instance.AmHost) return true;
            BountyHunter.OnReportDeadBody();
            SerialKiller.OnReportDeadBody();
            Main.ArsonistTimer.Clear();

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
                    Main.PlayerStates[bitten.PlayerId].deathReason = PlayerState.DeathReason.Bite;
                    bitten.SetRealKiller(Utils.GetPlayerById(vampireID));
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


            Main.AllPlayerControls
                .Where(pc => Main.CheckShapeshift.ContainsKey(pc.PlayerId))
                .Do(pc => Camouflage.RpcSetSkin(pc, RevertToDefault: true));

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
        public static void Postfix(PlayerControl __instance)
        {
            if (!GameStates.IsModHost) return;
            var player = __instance;

            if (AmongUsClient.Instance.AmHost)
            {//実行クライアントがホストの場合のみ実行
                if (GameStates.IsLobby && ((ModUpdater.hasUpdate && ModUpdater.forceUpdate) || ModUpdater.isBroken || !Main.AllowPublicRoom) && AmongUsClient.Instance.IsGamePublic)
                    AmongUsClient.Instance.ChangeGamePublic(false);

                if (GameStates.IsInTask && ReportDeadBodyPatch.CanReport[__instance.PlayerId] && ReportDeadBodyPatch.WaitReport[__instance.PlayerId].Count > 0)
                {
                    var info = ReportDeadBodyPatch.WaitReport[__instance.PlayerId][0];
                    ReportDeadBodyPatch.WaitReport[__instance.PlayerId].Clear();
                    Logger.Info($"{__instance.GetNameWithRole()}:通報可能になったため通報処理を行います", "ReportDeadbody");
                    __instance.ReportDeadBody(info);
                }

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
                                Main.PlayerStates[bitten.PlayerId].deathReason = PlayerState.DeathReason.Bite;
                                var vampirePC = Utils.GetPlayerById(vampireID);
                                bitten.SetRealKiller(vampirePC);
                                bitten.RpcMurderPlayer(bitten);
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
                if (GameStates.IsInTask && CustomRoles.SerialKiller.IsEnable()) SerialKiller.FixedUpdate(player);
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
                if (GameStates.IsInTask && Main.AssassinTimer.ContainsKey(player.PlayerId))//処理を1秒遅らせる
                {
                    if (player.IsAlive())
                    {
                        if (Main.AssassinTimer[player.PlayerId] >= 1f)
                        {
                            player.RpcResetAbilityCooldown();
                            Main.isMarked = false;//変身クールを１秒に変更
                            player.SyncSettings();
                            Main.AssassinTimer.Remove(player.PlayerId);
                        }
                        else Main.AssassinTimer[player.PlayerId] = Main.AssassinTimer[player.PlayerId] + Time.fixedDeltaTime;//時間をカウント
                    }
                    else
                    {
                        Main.AssassinTimer.Remove(player.PlayerId);
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
                if (GameStates.IsInTask && player == PlayerControl.LocalPlayer)
                    AntiAdminer.FixedUpdate();

                if (GameStates.IsInGame && Main.RefixCooldownDelay <= 0)
                    foreach (var pc in Main.AllPlayerControls)
                    {
                        if (pc.Is(CustomRoles.Vampire) || pc.Is(CustomRoles.Warlock) || pc.Is(CustomRoles.Assassin))
                            Main.AllPlayerKillCooldown[pc.PlayerId] = Options.DefaultKillCooldown * 2;
                    }

                if (__instance.AmOwner) Utils.ApplySuffix();
            }
            //LocalPlayer専用
            if (__instance.AmOwner)
            {
                //キルターゲットの上書き処理
                if (GameStates.IsInTask && !(__instance.Is(RoleType.Impostor) || __instance.Is(CustomRoles.Egoist)) && __instance.CanUseKillButton() && !__instance.Data.IsDead)
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
                    else if (__instance.GetCustomRole().IsImpostor() && PlayerControl.LocalPlayer.GetCustomRole().IsImpostor() && !PlayerControl.LocalPlayer.Data.IsDead && Options.ImpKnowAlliesRole.GetBool()) RoleText.enabled = true;
                    else if (PlayerControl.LocalPlayer.Is(CustomRoles.God) && !PlayerControl.LocalPlayer.Data.IsDead) RoleText.enabled = true;
                    else RoleText.enabled = false; //そうでなければロールを非表示
                    if (!AmongUsClient.Instance.IsGameStarted && AmongUsClient.Instance.NetworkMode != NetworkModes.FreePlay)
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
                        RealName = Utils.ColorString(target.GetRoleColor(), RealName); //名前の色を変更
                        if (target.Is(CustomRoles.Arsonist) && target.IsDouseDone())
                            RealName = Utils.ColorString(Utils.GetRoleColor(CustomRoles.Arsonist), GetString("EnterVentToWin"));
                    }
                    //タスクを終わらせたMadSnitchがインポスターを確認できる
                    else if (seer.Is(CustomRoles.MadSnitch) && //seerがMadSnitch
                        target.GetCustomRole().IsImpostor() && //targetがインポスター
                        seer.GetPlayerTaskState().IsTaskFinished) //seerのタスクが終わっている
                    {
                        RealName = Utils.ColorString(Utils.GetRoleColor(CustomRoles.Impostor), RealName); //targetの名前を赤色で表示
                    }
                    //タスクを終わらせたSnitchがインポスターを確認できる
                    else if (PlayerControl.LocalPlayer.Is(CustomRoles.Snitch) && //LocalPlayerがSnitch
                        PlayerControl.LocalPlayer.GetPlayerTaskState().IsTaskFinished) //LocalPlayerのタスクが終わっている
                    {
                        var targetCheck = target.GetCustomRole().IsImpostor() || (Options.SnitchCanFindNeutralKiller.GetBool() && target.IsNeutralKiller());
                        if (targetCheck)//__instanceがターゲット
                        {
                            RealName = Utils.ColorString(target.GetRoleColor(), RealName); //targetの名前を役職色で表示
                        }
                    }
                    else if (seer.GetCustomRole().IsImpostor()) //seerがインポスター
                    {
                        if (target.Is(CustomRoles.Egoist)) //targetがエゴイスト
                            RealName = Utils.ColorString(Utils.GetRoleColor(CustomRoles.Egoist), RealName); //targetの名前をエゴイスト色で表示
                        else if (target.Is(CustomRoles.MadSnitch) && target.GetPlayerTaskState().IsTaskFinished && Options.MadSnitchCanAlsoBeExposedToImpostor.GetBool()) //targetがタスクを終わらせたマッドスニッチ
                            Mark += Utils.ColorString(Utils.GetRoleColor(CustomRoles.MadSnitch), "★"); //targetにマーク付与
                    }

                    else if ((seer.Is(CustomRoles.EgoSchrodingerCat) && target.Is(CustomRoles.Egoist)) || //エゴ猫 --> エゴイスト
                             (seer.Is(CustomRoles.JSchrodingerCat) && target.Is(CustomRoles.Jackal)) || //J猫 --> ジャッカル
                             (seer.Is(CustomRoles.MSchrodingerCat) && target.Is(RoleType.Impostor)) //M猫 --> インポスター
                    )
                        RealName = Utils.ColorString(target.GetRoleColor(), RealName); //targetの名前をtargetの役職の色で表示
                    else if (target.Is(CustomRoles.Mare) && Utils.IsActive(SystemTypes.Electrical))
                        RealName = Utils.ColorString(Utils.GetRoleColor(CustomRoles.Impostor), RealName); //targetの赤色で表示

                    //NameColorManager準拠の処理
                    var ncd = NameColorManager.Instance.GetData(seer.PlayerId, target.PlayerId);
                    if (ncd.color != null) RealName = ncd.OpenTag + RealName + ncd.CloseTag;

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
                        foreach (var pc in Main.AllAlivePlayerControls)
                        { //全員分ループ
                            if (!pc.Is(CustomRoles.Snitch)) continue; //スニッチ以外に用はない
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

                    if (target.Is(CustomRoles.SuperStar) && Options.EveryOneKnowSuperStar.GetBool())
                        Mark += Utils.ColorString(Utils.GetRoleColor(CustomRoles.SuperStar), "★");

                    //ハートマークを付ける(会議中MOD視点)
                    if (__instance.Is(CustomRoles.Lovers) && PlayerControl.LocalPlayer.Is(CustomRoles.Lovers))
                    {
                        Mark += $"<color={Utils.GetRoleColorCode(CustomRoles.Lovers)}>♡</color>";
                    }
                    else if (__instance.Is(CustomRoles.Lovers) && PlayerControl.LocalPlayer.Data.IsDead)
                    {
                        Mark += $"<color={Utils.GetRoleColorCode(CustomRoles.Lovers)}>♡</color>";
                    }
                    else if (__instance.Is(CustomRoles.Ntr) || PlayerControl.LocalPlayer.Is(CustomRoles.Ntr))
                    {
                        Mark += $"<color={Utils.GetRoleColorCode(CustomRoles.Lovers)}>♡</color>";
                    }
                    else if (__instance == PlayerControl.LocalPlayer && CustomRolesHelper.RoleExist(CustomRoles.Ntr))
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
                            foreach (var pc in Main.AllPlayerControls)
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

                    if (GameStates.IsInTask && seer.Is(CustomRoles.AntiAdminer))
                    {
                        AntiAdminer.FixedUpdate();
                        if (target.AmOwner)
                        {
                            if (AntiAdminer.IsAdminWatch) Suffix += "★" + GetString("AntiAdminerAD");
                            if (AntiAdminer.IsVitalWatch) Suffix += "★" + GetString("AntiAdminerVI");
                            if (AntiAdminer.IsDoorLogWatch) Suffix += "★" + GetString("AntiAdminerDL");
                            if (AntiAdminer.IsCameraWatch) Suffix += "★" + GetString("AntiAdminerCA");
                        }
                    }

                    /*if(main.AmDebugger.Value && main.BlockKilling.TryGetValue(target.PlayerId, out var isBlocked)) {
                        Mark = isBlocked ? "(true)" : "(false)";
                    }*/
                    if (Utils.IsActive(SystemTypes.Comms) && Options.CommsCamouflage.GetBool())
                        RealName = $"<size=0>{RealName}</size> ";

                    string DeathReason = seer.Data.IsDead && seer.KnowDeathReason(target) ? $"({Utils.ColorString(Utils.GetRoleColor(CustomRoles.Doctor), Utils.GetVitalText(target.PlayerId))})" : "";
                    //Mark・Suffixの適用
                    target.cosmetics.nameText.text = $"{RealName}{DeathReason}{Mark}";

                    if (Suffix != "")
                    {
                        //名前が2行になると役職テキストを上にずらす必要がある
                        RoleText.transform.SetLocalY(0.35f);
                        target.cosmetics.nameText.text += "\r\n" + Suffix;

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
            if (Options.LoverSuicide.GetBool() && CustomRoles.Lovers.IsEnable() && Main.isLoversDead == false)
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
                                CheckForEndVotingPatch.TryAddAfterMeetingDeathPlayers(partnerPlayer.PlayerId, PlayerState.DeathReason.FollowingSuicide);
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
            if (!Main.PlayerStates[seer.PlayerId].IsDead && !Main.PlayerStates[target.PlayerId].IsDead)
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
            if (AmongUsClient.Instance.AmHost)
            {
                if (Main.LastEnteredVent.ContainsKey(pc.PlayerId))
                    Main.LastEnteredVent.Remove(pc.PlayerId);
                Main.LastEnteredVent.Add(pc.PlayerId, __instance);
                if (Main.LastEnteredVentLocation.ContainsKey(pc.PlayerId))
                    Main.LastEnteredVentLocation.Remove(pc.PlayerId);
                Main.LastEnteredVentLocation.Add(pc.PlayerId, pc.GetTruePosition());
            }

            if (Options.CurrentGameMode == CustomGameMode.HideAndSeek && Options.IgnoreVent.GetBool())
                pc.MyPhysics.RpcBootFromVent(__instance.Id);
            if (pc.Is(CustomRoles.Paranoia))
            {
                if (Main.ParaUsedButtonCount.TryGetValue(pc.PlayerId, out var count) && count < Options.ParanoiaNumOfUseButton.GetInt())
                {
                    Main.ParaUsedButtonCount[pc.PlayerId] += 1;
                    new LateTask(() =>
                    {
                        Utils.SendMessage(GetString("SkillUsedLeft") + (Options.ParanoiaNumOfUseButton.GetInt() - Main.ParaUsedButtonCount[pc.PlayerId]).ToString(), pc.PlayerId);
                    }, 4.0f, "Skill Remain Message");
                    pc?.MyPhysics?.RpcBootFromVent(__instance.Id);
                    pc?.NoCheckStartMeeting(pc?.Data);
                }
            }
            if (pc.Is(CustomRoles.Mayor))
            {
                if (Main.MayorUsedButtonCount.TryGetValue(pc.PlayerId, out var count) && count < Options.MayorNumOfUseButton.GetInt())
                {
                    pc?.MyPhysics?.RpcBootFromVent(__instance.Id);
                    pc?.ReportDeadBody(null);
                }
            }
            if (pc.Is(CustomRoles.Mario))
            {
                if (!Main.MarioVentCount.ContainsKey(pc.PlayerId)) Main.MarioVentCount.Add(pc.PlayerId, 0);
                Main.MarioVentCount[pc.PlayerId]++;
                Utils.NotifyRoles(SpecifySeer: pc);
                if (Main.MarioVentCount[pc.PlayerId] >= Options.MarioVentNumWin.GetInt())
                {
                    CustomWinnerHolder.ShiftWinnerAndSetWinner(CustomWinner.Mario); //马里奥这个多动症赢了
                    CustomWinnerHolder.WinnerIds.Add(pc.PlayerId);
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
                (__instance.myPlayer.Is(CustomRoles.Mayor) && Main.MayorUsedButtonCount.TryGetValue(__instance.myPlayer.PlayerId, out var count) && count >= Options.MayorNumOfUseButton.GetInt())
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
                if ((__instance.myPlayer.Data.Role.Role != RoleTypes.Engineer && //エンジニアでなく
                !__instance.myPlayer.CanUseImpostorVentButton()) || //インポスターベントも使えない
                (__instance.myPlayer.Is(CustomRoles.Paranoia) && Main.ParaUsedButtonCount.TryGetValue(__instance.myPlayer.PlayerId, out var count2) && count2 >= Options.ParanoiaNumOfUseButton.GetInt())
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
            Logger.Info($"TaskComplete:{pc.GetNameWithRole()}", "CompleteTask");
            Main.PlayerStates[pc.PlayerId].UpdateTask(pc);
            Utils.NotifyRoles();
            if ((pc.GetPlayerTaskState().IsTaskFinished &&
                pc.GetCustomRole() is CustomRoles.Lighter or CustomRoles.Doctor) ||
                pc.GetCustomRole() is CustomRoles.SpeedBooster)
            {
                //ライターもしくはスピードブースターもしくはドクターがいる試合のみタスク終了時にCustomSyncAllSettingsを実行する
                Utils.MarkEveryoneDirtySettings();
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
    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.RpcSetRole))]
    class PlayerControlSetRolePatch
    {
        public static bool Prefix(PlayerControl __instance, ref RoleTypes roleType)
        {
            var target = __instance;
            Logger.Info($"{__instance.GetNameWithRole()} =>{roleType}", "PlayerControl.RpcSetRole");
            if (!ShipStatus.Instance.enabled) return true;
            if (roleType is RoleTypes.CrewmateGhost or RoleTypes.ImpostorGhost)
            {
                foreach (var seer in Main.AllPlayerControls)
                {
                    var self = seer.PlayerId == target.PlayerId;
                    var seerIsKiller = seer.Is(RoleType.Impostor) || Main.ResetCamPlayerList.Contains(seer.PlayerId);
                    var targetIsKiller = target.Is(RoleType.Impostor) || Main.ResetCamPlayerList.Contains(target.PlayerId);
                    if ((self && targetIsKiller) || (!seerIsKiller && target.Is(RoleType.Impostor)))
                    {
                        Logger.Info($"Desync {target.GetNameWithRole()} =>ImpostorGhost for{seer.GetNameWithRole()}", "PlayerControl.RpcSetRole");
                        target.RpcSetRoleDesync(RoleTypes.ImpostorGhost, seer.GetClientId());
                    }
                    else
                    {
                        Logger.Info($"Desync {target.GetNameWithRole()} =>CrewmateGhost for{seer.GetNameWithRole()}", "PlayerControl.RpcSetRole");
                        target.RpcSetRoleDesync(RoleTypes.CrewmateGhost, seer.GetClientId());
                    }
                }
                return false;
            }
            return true;
        }
    }
}