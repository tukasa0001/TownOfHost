using System;
using HarmonyLib;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace TownOfHost
{
    [HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.CheckForEndVoting))]
    class CheckForEndVotingPatch
    {
        public static bool recall = false;
        public static bool Prefix(MeetingHud __instance)
        {
            if (MeetingHudUpdatePatch.isDictatorVote)
            {
                MeetingHudUpdatePatch.isDictatorVote = false;
                return true;
            }
            try
            {
                if (!AmongUsClient.Instance.AmHost) return true;
                foreach (var ps in __instance.playerStates)
                {
                    //死んでいないプレイヤーが投票していない
                    if (!(ps.AmDead || ps.DidVote)) return false;
                }

                MeetingHud.VoterState[] states;
                GameData.PlayerInfo exiledPlayer = PlayerControl.LocalPlayer.Data;
                bool tie = false;
                recall = false;

                List<MeetingHud.VoterState> statesList = new List<MeetingHud.VoterState>();
                for (var i = 0; i < __instance.playerStates.Length; i++)
                {
                    PlayerVoteArea ps = __instance.playerStates[i];
                    if (ps == null) continue;
                    Logger.info($"{ps.TargetPlayerId}:{ps.VotedFor}");
                    var voter = Utils.getPlayerById(ps.TargetPlayerId);
                    if (voter == null || voter.Data == null || voter.Data.Disconnected) continue;
                    if (ps.VotedFor == 253 && !voter.Data.IsDead)//スキップ
                    {
                        switch (Options.GetWhenSkipVote())
                        {
                            case VoteMode.Suicide:
                                PlayerState.setDeathReason(ps.TargetPlayerId, PlayerState.DeathReason.Suicide);
                                voter.RpcMurderPlayer(voter);
                                main.IgnoreReportPlayers.Add(voter.PlayerId);
                                recall = true;
                                break;
                            case VoteMode.SelfVote:
                                ps.VotedFor = ps.TargetPlayerId;
                                break;
                            default:
                                break;
                        }
                    }
                    if (ps.VotedFor == 254 && !voter.Data.IsDead)//無投票
                    {
                        switch (Options.GetWhenNonVote())
                        {
                            case VoteMode.Suicide:
                                PlayerState.setDeathReason(ps.TargetPlayerId, PlayerState.DeathReason.Suicide);
                                voter.RpcMurderPlayer(voter);
                                main.IgnoreReportPlayers.Add(voter.PlayerId);
                                recall = true;
                                break;
                            case VoteMode.SelfVote:
                                ps.VotedFor = ps.TargetPlayerId;
                                break;
                            case VoteMode.Skip:
                                ps.VotedFor = 253;
                                break;
                            default:
                                break;
                        }
                    }
                    statesList.Add(new MeetingHud.VoterState()
                    {
                        VoterId = ps.TargetPlayerId,
                        VotedForId = ps.VotedFor
                    });
                    if (isMayor(ps.TargetPlayerId))//Mayorの投票数
                    {
                        for (var i2 = 0; i2 < Options.MayorAdditionalVote.GetFloat(); i2++)
                        {
                            statesList.Add(new MeetingHud.VoterState()
                            {
                                VoterId = ps.TargetPlayerId,
                                VotedForId = ps.VotedFor
                            });
                        }
                    }
                }
                states = statesList.ToArray();

                var VotingData = __instance.CustomCalculateVotes();
                byte exileId = byte.MaxValue;
                int max = 0;
                Logger.info("===追放者確認処理開始===");
                foreach (var data in VotingData)
                {
                    Logger.info(data.Key + ": " + data.Value);
                    if (data.Value > max)
                    {
                        Logger.info(data.Key + "番が最高値を更新(" + data.Value + ")");
                        exileId = data.Key;
                        max = data.Value;
                        tie = false;
                    }
                    else if (data.Value == max)
                    {
                        Logger.info(data.Key + "番が" + exileId + "番と同数(" + data.Value + ")");
                        exileId = byte.MaxValue;
                        tie = true;
                    }
                    Logger.info("exileId: " + exileId + ", max: " + max);
                }

                Logger.info("追放者決定: " + exileId);
                exiledPlayer = GameData.Instance.AllPlayers.ToArray().FirstOrDefault(info => !tie && info.PlayerId == exileId);

                __instance.RpcVotingComplete(states, exiledPlayer, tie); //RPC
                if (!Utils.getPlayerById(exileId).Is(CustomRoles.Witch))
                {
                    foreach (var p in main.SpelledPlayer)
                    {
                        PlayerState.setDeathReason(p.PlayerId, PlayerState.DeathReason.Spell);
                        main.IgnoreReportPlayers.Add(p.PlayerId);
                        p.RpcMurderPlayer(p);
                        recall = true;
                    }
                }
                main.SpelledPlayer.Clear();


                if (CustomRoles.Lovers.isEnable() && main.isLoversDead == false && main.LoversPlayers.Find(lp => lp.PlayerId == exileId) != null)
                {
                    FixedUpdatePatch.LoversSuicide(exiledPlayer);
                }

                //霊界用暗転バグ対処
                foreach (var pc in PlayerControl.AllPlayerControls)
                {
                    if ((pc.Is(CustomRoles.Sheriff) || pc.Is(CustomRoles.Arsonist)) && (pc.Data.IsDead || pc.PlayerId == exiledPlayer?.PlayerId)) pc.ResetPlayerCam(19f);
                }

                return false;
            }
            catch (Exception ex)
            {
                Logger.SendInGame("エラー:" + ex.Message + "\r\nSHIFT+M+ENTERで会議を強制終了してください", true);
                throw;
            }
        }
        public static bool isMayor(byte id)
        {
            var player = PlayerControl.AllPlayerControls.ToArray().Where(pc => pc.PlayerId == id).FirstOrDefault();
            if (player == null) return false;
            return player.Is(CustomRoles.Mayor);
        }
    }

    static class ExtendedMeetingHud
    {
        public static Dictionary<byte, int> CustomCalculateVotes(this MeetingHud __instance)
        {
            Logger.info("CustomCalculateVotes開始");
            Dictionary<byte, int> dic = new Dictionary<byte, int>();
            //| 投票された人 | 投票された回数 |
            for (int i = 0; i < __instance.playerStates.Length; i++)
            {
                PlayerVoteArea ps = __instance.playerStates[i];
                if (ps == null) continue;
                if (ps.VotedFor != (byte)252 && ps.VotedFor != byte.MaxValue && ps.VotedFor != (byte)254)
                {
                    int num;
                    int VoteNum = 1;
                    if (CheckForEndVotingPatch.isMayor(ps.TargetPlayerId)) VoteNum = Options.MayorAdditionalVote.GetSelection() + 1;
                    //投票を1追加 キーが定義されていない場合は1で上書きして定義
                    dic[ps.VotedFor] = !dic.TryGetValue(ps.VotedFor, out num) ? VoteNum : num + VoteNum;
                }
            }
            return dic;
        }
    }
    [HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.Start))]
    class MeetingHudStartPatch
    {
        public static void Prefix(MeetingHud __instance)
        {
            Logger.info("会議が開始", "Phase");
            main.witchMeeting = true;
            Utils.NotifyRoles(isMeeting: true);
            main.witchMeeting = false;
        }
        public static void Postfix(MeetingHud __instance)
        {
            foreach (var pva in __instance.playerStates)
            {
                var roleTextMeeting = UnityEngine.Object.Instantiate(pva.NameText);
                roleTextMeeting.transform.SetParent(pva.NameText.transform);
                roleTextMeeting.transform.localPosition = new Vector3(0f, -0.18f, 0f);
                roleTextMeeting.fontSize = 1.5f;
                roleTextMeeting.text = "RoleTextMeeting";
                roleTextMeeting.gameObject.name = "RoleTextMeeting";
                roleTextMeeting.enableWordWrapping = false;
                roleTextMeeting.enabled = false;
            }
            if (Options.SyncButtonMode.GetBool())
            {
                if (AmongUsClient.Instance.AmHost) PlayerControl.LocalPlayer.RpcSetName("test");
                Utils.SendMessage("緊急会議ボタンはあと" + (Options.SyncedButtonCount.GetFloat() - Options.UsedButtonCount) + "回使用可能です。");
                Logger.SendToFile("緊急会議ボタンはあと" + (Options.SyncedButtonCount.GetFloat() - Options.UsedButtonCount) + "回使用可能です。", LogLevel.Message);
            }

            if (AmongUsClient.Instance.AmHost)
            {
                _ = new LateTask(() =>
                {
                    foreach (var pc in PlayerControl.AllPlayerControls)
                    {
                        pc.RpcSetNameEx(pc.getRealName(isMeeting: true));
                    }
                }, 3f, "SetName To Chat");
            }

            foreach (var pva in __instance.playerStates)
            {
                if (pva == null) continue;
                PlayerControl seer = PlayerControl.LocalPlayer;
                PlayerControl target = Utils.getPlayerById(pva.TargetPlayerId);
                if (target == null) continue;

                //会議画面での名前変更
                //とりあえずSnitchは会議中にもインポスターを確認することができる仕様にしていますが、変更する可能性があります。
                //変更する場合でも、このコードはMadSnitchで使うと思うので消さないでください。

                //インポスター表示
                bool LocalPlayerKnowsImpostor = false; //203行目のif文で使う trueの時にインポスターの名前を赤くする
                if ((seer.Is(CustomRoles.Snitch) || seer.Is(CustomRoles.MadSnitch)) && //seerがSnitch/MadSnitch
                    seer.getPlayerTaskState().isTaskFinished) //seerがタスクを終えている
                {
                    LocalPlayerKnowsImpostor = true;
                }

                if (LocalPlayerKnowsImpostor)
                {
                    if (target != null && target.getCustomRole().isImpostor()) //変更先がインポスター
                    {
                        //変更対象の名前を赤くする
                        pva.NameText.text = "<color=#ff0000>" + pva.NameText.text + "</color>";
                    }
                }

                //呪われている場合
                if (main.SpelledPlayer.Find(x => x.PlayerId == target.PlayerId) != null)
                    pva.NameText.text += "<color=#ff0000>†</color>";

                if (seer.getCustomRole().isImpostor() && //LocalPlayerがImpostor
                    target.Is(CustomRoles.Snitch) && //変更対象がSnitch
                    target.getPlayerTaskState().doExpose //変更対象のタスクが終わりそう
                )
                {
                    //変更対象にSnitchマークをつける
                    pva.NameText.text += $"<color={Utils.getRoleColorCode(CustomRoles.Snitch)}>★</color>";
                }
                if (PlayerControl.LocalPlayer.isLovers() && target.isLovers())
                {
                    pva.NameText.text += $"<color={Utils.getRoleColorCode(CustomRoles.Lovers)}>♡</color>";
                }
                else if (PlayerControl.LocalPlayer.Data.IsDead && target.isLovers())
                {
                    pva.NameText.text += $"<color={Utils.getRoleColorCode(CustomRoles.Lovers)}>♡</color>";
                }
                if (seer.getCustomRole().isImpostor() && //LocalPlayerがImpostor
                    target.Is(CustomRoles.Egoist) //変更対象がEgoist
                )
                {
                    //変更対象の名前をエゴイスト色にする
                    pva.NameText.text = $"<color={Utils.getRoleColorCode(CustomRoles.Egoist)}>{pva.NameText.text}</color>";
                }
                if (seer.Is(CustomRoles.EgoSchrodingerCat) && //LocalPlayerがEgoSchrodingerCat
                    target.Is(CustomRoles.Egoist) //変更対象がEgoist
                )
                {
                    //変更対象の名前をエゴイスト色にする
                    pva.NameText.text = $"<color={Utils.getRoleColorCode(CustomRoles.Egoist)}>{pva.NameText.text}</color>";
                }

                //会議画面ではインポスター自身の名前にSnitchマークはつけません。

                //自分自身の名前の色を変更
                if (target != null && target.AmOwner && AmongUsClient.Instance.IsGameStarted) //変更先が自分自身
                {
                    pva.NameText.text = $"<color={seer.getRoleColorCode()}>{pva.NameText.text}</color>"; //名前の色を変更
                }
                if (seer.Is(CustomRoles.Executioner)) //seerがエクスキューショナー
                    foreach (var ExecutionerTarget in main.ExecutionerTarget)
                    {
                        if (seer.PlayerId == ExecutionerTarget.Key && //seerがKey
                        target.PlayerId == ExecutionerTarget.Value) //targetがValue
                            pva.NameText.text += $"<color={Utils.getRoleColorCode(CustomRoles.Executioner)}>♦</color>";
                    }
                if (seer.Is(CustomRoles.Doctor) && //LocalPlayerがDoctor
                target.Data.IsDead) //変更対象が死人
                    pva.NameText.text = $"{pva.NameText.text}(<color={Utils.getRoleColorCode(CustomRoles.Doctor)}>{Utils.getVitalText(target.PlayerId)}</color>)";
            }
        }
    }
    [HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.Update))]
    class MeetingHudUpdatePatch
    {
        public static bool isDictatorVote = false;
        public static void Postfix(MeetingHud __instance)
        {
            if (AmongUsClient.Instance.GameMode == GameModes.FreePlay) return;

            foreach (var pva in __instance.playerStates)
            {
                if (pva == null) continue;
                PlayerControl pc = Utils.getPlayerById(pva.TargetPlayerId);
                if (pc == null) continue;

                //役職表示系
                var RoleTextMeetingTransform = pva.NameText.transform.Find("RoleTextMeeting");
                TMPro.TextMeshPro RoleTextMeeting = null;
                if (RoleTextMeetingTransform != null) RoleTextMeeting = RoleTextMeetingTransform.GetComponent<TMPro.TextMeshPro>();
                if (RoleTextMeeting != null)
                {

                    var RoleTextData = Utils.GetRoleText(pc);
                    RoleTextMeeting.text = RoleTextData.Item1;
                    if (main.VisibleTasksCount && Utils.hasTasks(pc.Data, false)) RoleTextMeeting.text += Utils.getTaskText(pc);
                    RoleTextMeeting.color = RoleTextData.Item2;
                    if (pva.TargetPlayerId == PlayerControl.LocalPlayer.PlayerId) RoleTextMeeting.enabled = true;
                    else if (main.VisibleTasksCount && PlayerControl.LocalPlayer.Data.IsDead) RoleTextMeeting.enabled = true;
                    else RoleTextMeeting.enabled = false;
                }
                //死んでいないディクテーターが投票済み
                if (pc.Is(CustomRoles.Dictator) && pva.DidVote && !pc.Data.IsDead)
                {
                    var voteTarget = Utils.getPlayerById(pva.VotedFor);
                    MeetingHud.VoterState[] states;
                    List<MeetingHud.VoterState> statesList = new List<MeetingHud.VoterState>();
                    statesList.Add(new MeetingHud.VoterState()
                    {
                        VoterId = pva.TargetPlayerId,
                        VotedForId = pva.VotedFor
                    });
                    states = statesList.ToArray();
                    isDictatorVote = true;
                    pc.RpcMurderPlayer(pc); //自殺
                    __instance.RpcVotingComplete(states, voteTarget.Data, false); //RPC
                    main.IgnoreReportPlayers.Add(pc.PlayerId);
                    CheckForEndVotingPatch.recall = true;
                    Logger.info("ディクテーターによる強制会議終了", "Special Phase");
                }
            }
        }
    }
    [HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.OnDestroy))]
    class MeetingHudOnDestroyPatch
    {
        public static void Postfix(MeetingHud __instance)
        {
            Logger.info("会議が終了", "Phase");
            if (!AmongUsClient.Instance.AmHost) return;

            //エアシップの場合スポーン位置選択が発生するため死体消し用の会議を5秒遅らせる。
            var additional = PlayerControl.GameOptions.MapId == 4 ? 5f : 0f;

            if (CheckForEndVotingPatch.recall)
            {
                foreach (var pc in PlayerControl.AllPlayerControls)
                {
                    if (!pc.Data.IsDead)
                    {
                        new LateTask(() =>
                        {
                            pc.ReportDeadBody(Utils.getPlayerById(main.IgnoreReportPlayers.Last()).Data);
                        },
                            0.2f + additional, "Recall Meeting");
                        new LateTask(() =>
                        {
                            MeetingHud.Instance.RpcClose();
                            CheckForEndVotingPatch.recall = false;
                        },
                            0.5f + additional, "Cancel Meeting");
                        break;
                    }
                }
            }
        }
    }
}
