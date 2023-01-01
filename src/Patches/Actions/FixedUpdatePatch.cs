using System.Collections.Generic;
using System.Linq;
using AmongUs.GameOptions;
using HarmonyLib;
using TownOfHost.Extensions;
using UnityEngine;

namespace TownOfHost.Patches.Actions;

[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.FixedUpdate))]
public static class FixedUpdatePatch
{
    public static void Postfix(PlayerControl __instance)
    {
        var player = __instance;

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
                                OldRPC.PlaySoundRPC(vampireID, Sounds.KillSound);
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
                        Utils.MarkEveryoneDirtySettings();
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
                    OldRPC.ResetCurrentDousingTarget(player.PlayerId);
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
                        Utils.MarkEveryoneDirtySettings();//同期
                        player.RpcGuardAndKill(ar_target);//通知とクールリセット
                        Main.ArsonistTimer.Remove(player.PlayerId);//塗が完了したのでDictionaryから削除
                        Main.isDoused[(player.PlayerId, ar_target.PlayerId)] = true;//塗り完了
                        player.RpcSetDousedPlayer(ar_target, true);
                        Utils.NotifyRoles();//名前変更
                        OldRPC.ResetCurrentDousingTarget(player.PlayerId);
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
                            OldRPC.ResetCurrentDousingTarget(player.PlayerId);

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
                        var KillRange = NormalGameOptionsV07.KillDistances[Mathf.Clamp(Main.NormalOptions.KillDistance, 0, 2)];
                        if (min.Value <= KillRange && player.CanMove && target.CanMove)
                        {
                            var puppeteerId = Main.PuppeteerList[player.PlayerId];
                            OldRPC.PlaySoundRPC(puppeteerId, Sounds.KillSound);
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
                        RealName = Utils.ColorString(Utils.GetRoleColor(CustomRoles.Arsonist), Translator.GetString("EnterVentToWin"));
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