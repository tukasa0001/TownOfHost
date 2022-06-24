using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using UnityEngine;

namespace TownOfHost
{
    [HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.CheckForEndVoting))]
    class CheckForEndVotingPatch
    {
        public static bool Prefix(MeetingHud __instance)
        {
            if (!AmongUsClient.Instance.AmHost) return true;
            try
            {
                foreach (var pva in __instance.playerStates)
                {
                    if (pva == null) continue;
                    PlayerControl pc = Utils.GetPlayerById(pva.TargetPlayerId);
                    if (pc == null) continue;
                    //死んでいないディクテーターが投票済み
                    if (pc.Is(CustomRoles.Dictator) && pva.DidVote && pc.PlayerId != pva.VotedFor && pva.VotedFor < 253 && !pc.Data.IsDead)
                    {
                        var voteTarget = Utils.GetPlayerById(pva.VotedFor);
                        Main.AfterMeetingDeathPlayers.TryAdd(pc.PlayerId, PlayerState.DeathReason.Suicide);
                        __instance.RpcVotingComplete(new MeetingHud.VoterState[]{ new ()
                        {
                            VoterId = pva.TargetPlayerId,
                            VotedForId = pva.VotedFor
                        }}, voteTarget.Data, false); //RPC
                        Logger.Info($"{voteTarget.GetNameWithRole()}を追放", "Dictator");
                        Logger.Info("ディクテーターによる強制会議終了", "Special Phase");
                        return true;
                    }
                }
                foreach (var ps in __instance.playerStates)
                {
                    //死んでいないプレイヤーが投票していない
                    if (!(ps.AmDead || ps.DidVote)) return false;
                }

                MeetingHud.VoterState[] states;
                GameData.PlayerInfo exiledPlayer = PlayerControl.LocalPlayer.Data;
                bool tie = false;

                List<MeetingHud.VoterState> statesList = new();
                for (var i = 0; i < __instance.playerStates.Length; i++)
                {
                    PlayerVoteArea ps = __instance.playerStates[i];
                    if (ps == null) continue;
                    Logger.Info(string.Format("{0,-2}{1}:{2,-3}{3}", ps.TargetPlayerId, Utils.PadRightV2($"({Utils.GetVoteName(ps.TargetPlayerId)})", 40), ps.VotedFor, $"({Utils.GetVoteName(ps.VotedFor)})"), "Vote");
                    var voter = Utils.GetPlayerById(ps.TargetPlayerId);
                    if (voter == null || voter.Data == null || voter.Data.Disconnected) continue;
                    if (Options.VoteMode.GetBool())
                    {
                        if (ps.VotedFor == 253 && !voter.Data.IsDead)//スキップ
                        {
                            switch (Options.GetWhenSkipVote())
                            {
                                case VoteMode.Suicide:
                                    Main.AfterMeetingDeathPlayers.TryAdd(ps.TargetPlayerId, PlayerState.DeathReason.Suicide);
                                    Logger.Info($"スキップしたため{voter.GetNameWithRole()}を自殺させました", "Vote");
                                    break;
                                case VoteMode.SelfVote:
                                    ps.VotedFor = ps.TargetPlayerId;
                                    Logger.Info($"スキップしたため{voter.GetNameWithRole()}に自投票させました", "Vote");
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
                                    Main.AfterMeetingDeathPlayers.TryAdd(ps.TargetPlayerId, PlayerState.DeathReason.Suicide);
                                    Logger.Info($"無投票のため{voter.GetNameWithRole()}を自殺させました", "Vote");
                                    break;
                                case VoteMode.SelfVote:
                                    ps.VotedFor = ps.TargetPlayerId;
                                    Logger.Info($"無投票のため{voter.GetNameWithRole()}に自投票させました", "Vote");
                                    break;
                                case VoteMode.Skip:
                                    ps.VotedFor = 253;
                                    Logger.Info($"無投票のため{voter.GetNameWithRole()}にスキップさせました", "Vote");
                                    break;
                                default:
                                    break;
                            }
                        }
                    }
                    statesList.Add(new MeetingHud.VoterState()
                    {
                        VoterId = ps.TargetPlayerId,
                        VotedForId = ps.VotedFor
                    });
                    if (IsMayor(ps.TargetPlayerId))//Mayorの投票数
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
                Logger.Info("===追放者確認処理開始===", "Vote");
                foreach (var data in VotingData)
                {
                    Logger.Info($"{data.Key}({Utils.GetVoteName(data.Key)}):{data.Value}票", "Vote");
                    if (data.Value > max)
                    {
                        Logger.Info(data.Key + "番が最高値を更新(" + data.Value + ")", "Vote");
                        exileId = data.Key;
                        max = data.Value;
                        tie = false;
                    }
                    else if (data.Value == max)
                    {
                        Logger.Info(data.Key + "番が" + exileId + "番と同数(" + data.Value + ")", "Vote");
                        exileId = byte.MaxValue;
                        tie = true;
                    }
                    Logger.Info($"exileId: {exileId}, max: {max}票", "Vote");
                }

                Logger.Info($"追放者決定: {exileId}({Utils.GetVoteName(exileId)})", "Vote");
                exiledPlayer = GameData.Instance.AllPlayers.ToArray().FirstOrDefault(info => !tie && info.PlayerId == exileId);

                __instance.RpcVotingComplete(states, exiledPlayer, tie); //RPC
                if (!Utils.GetPlayerById(exileId).Is(CustomRoles.Witch))
                {
                    foreach (var p in Main.SpelledPlayer)
                        Main.AfterMeetingDeathPlayers.TryAdd(p.PlayerId, PlayerState.DeathReason.Spell);
                }
                Main.SpelledPlayer.Clear();


                if (CustomRoles.Lovers.IsEnable() && Main.isLoversDead == false && Main.LoversPlayers.Find(lp => lp.PlayerId == exileId) != null)
                {
                    FixedUpdatePatch.LoversSuicide(exiledPlayer.PlayerId, true);
                }

                //霊界用暗転バグ対処
                foreach (var pc in PlayerControl.AllPlayerControls)
                {
                    if (Main.ResetCamPlayerList.Contains(pc.PlayerId) && (pc.Data.IsDead || pc.PlayerId == exiledPlayer?.PlayerId)) pc.ResetPlayerCam(19f);
                }

                return false;
            }
            catch (Exception ex)
            {
                Logger.SendInGame("エラー:" + ex.Message + "\r\nSHIFT+M+ENTERで会議を強制終了してください", true);
                throw;
            }
        }
        public static bool IsMayor(byte id)
        {
            var player = PlayerControl.AllPlayerControls.ToArray().Where(pc => pc.PlayerId == id).FirstOrDefault();
            return player != null && player.Is(CustomRoles.Mayor);
        }
    }

    static class ExtendedMeetingHud
    {
        public static Dictionary<byte, int> CustomCalculateVotes(this MeetingHud __instance)
        {
            Logger.Info("CustomCalculateVotes開始", "Vote");
            Dictionary<byte, int> dic = new();
            //| 投票された人 | 投票された回数 |
            for (int i = 0; i < __instance.playerStates.Length; i++)
            {
                PlayerVoteArea ps = __instance.playerStates[i];
                if (ps == null) continue;
                if (ps.VotedFor is not ((byte)252) and not byte.MaxValue and not ((byte)254))
                {
                    int VoteNum = 1;
                    if (CheckForEndVotingPatch.IsMayor(ps.TargetPlayerId)) VoteNum += Options.MayorAdditionalVote.GetInt();
                    //投票を1追加 キーが定義されていない場合は1で上書きして定義
                    dic[ps.VotedFor] = !dic.TryGetValue(ps.VotedFor, out int num) ? VoteNum : num + VoteNum;
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
            Logger.Info("------------会議開始------------", "Phase");
            Main.witchMeeting = true;
            Utils.NotifyRoles(isMeeting: true, ForceLoop: true);
            Main.witchMeeting = false;
        }
        public static void Postfix(MeetingHud __instance)
        {
            foreach (var pva in __instance.playerStates)
            {
                var pc = Utils.GetPlayerById(pva.TargetPlayerId);
                if (pc == null) continue;
                var RoleTextData = Utils.GetRoleText(pc);
                var roleTextMeeting = UnityEngine.Object.Instantiate(pva.NameText);
                roleTextMeeting.transform.SetParent(pva.NameText.transform);
                roleTextMeeting.transform.localPosition = new Vector3(0f, -0.18f, 0f);
                roleTextMeeting.fontSize = 1.5f;
                roleTextMeeting.text = RoleTextData.Item1;
                if (Main.VisibleTasksCount) roleTextMeeting.text += Utils.GetProgressText(pc);
                roleTextMeeting.color = RoleTextData.Item2;
                roleTextMeeting.gameObject.name = "RoleTextMeeting";
                roleTextMeeting.enableWordWrapping = false;
                roleTextMeeting.enabled = pva.TargetPlayerId == PlayerControl.LocalPlayer.PlayerId ||
                    (Main.VisibleTasksCount && PlayerControl.LocalPlayer.Data.IsDead && Options.GhostCanSeeOtherRoles.GetBool());
            }
            if (Options.SyncButtonMode.GetBool())
            {
                if (AmongUsClient.Instance.AmHost) PlayerControl.LocalPlayer.RpcSetName("test");
                Utils.SendMessage("緊急会議ボタンはあと" + (Options.SyncedButtonCount.GetFloat() - Options.UsedButtonCount) + "回使用可能です。");
                Logger.Info("緊急会議ボタンはあと" + (Options.SyncedButtonCount.GetFloat() - Options.UsedButtonCount) + "回使用可能です。", "SyncButtonMode");
            }

            if (AmongUsClient.Instance.AmHost)
            {
                _ = new LateTask(() =>
                {
                    foreach (var pc in PlayerControl.AllPlayerControls)
                    {
                        pc.RpcSetNameEx(pc.GetRealName(isMeeting: true));
                    }
                }, 3f, "SetName To Chat");
            }

            foreach (var pva in __instance.playerStates)
            {
                if (pva == null) continue;
                PlayerControl seer = PlayerControl.LocalPlayer;
                PlayerControl target = Utils.GetPlayerById(pva.TargetPlayerId);
                if (target == null) continue;

                //会議画面での名前変更
                //とりあえずSnitchは会議中にもインポスターを確認することができる仕様にしていますが、変更する可能性があります。
                //変更する場合でも、このコードはMadSnitchで使うと思うので消さないでください。

                //インポスター表示
                bool LocalPlayerKnowsImpostor = false; //203行目のif文で使う trueの時にインポスターの名前を赤くする
                if ((seer.Is(CustomRoles.Snitch) || seer.Is(CustomRoles.MadSnitch)) && //seerがSnitch/MadSnitch
                    seer.GetPlayerTaskState().IsTaskFinished) //seerがタスクを終えている
                {
                    LocalPlayerKnowsImpostor = true;
                }

                if (LocalPlayerKnowsImpostor)
                {
                    if (target != null && target.GetCustomRole().IsImpostor()) //変更先がインポスター
                    {
                        //変更対象の名前を赤くする
                        pva.NameText.text = "<color=#ff0000>" + pva.NameText.text + "</color>";
                    }
                }

                //呪われている場合
                if (Main.SpelledPlayer.Find(x => x.PlayerId == target.PlayerId) != null)
                    pva.NameText.text += "<color=#ff0000>†</color>";

                if (seer.GetCustomRole().IsImpostor() && //LocalPlayerがImpostor
                    target.Is(CustomRoles.Snitch) && //変更対象がSnitch
                    target.GetPlayerTaskState().DoExpose //変更対象のタスクが終わりそう
                )
                {
                    //変更対象にSnitchマークをつける
                    pva.NameText.text += $"<color={Utils.GetRoleColorCode(CustomRoles.Snitch)}>★</color>";
                }
                if (PlayerControl.LocalPlayer.Is(CustomRoles.Lovers) && target.Is(CustomRoles.Lovers))
                {
                    pva.NameText.text += $"<color={Utils.GetRoleColorCode(CustomRoles.Lovers)}>♡</color>";
                }
                else if (PlayerControl.LocalPlayer.Data.IsDead && target.Is(CustomRoles.Lovers))
                {
                    pva.NameText.text += $"<color={Utils.GetRoleColorCode(CustomRoles.Lovers)}>♡</color>";
                }
                if (seer.GetCustomRole().IsImpostor() && //LocalPlayerがImpostor
                    target.Is(CustomRoles.Egoist) //変更対象がEgoist
                )
                {
                    //変更対象の名前をエゴイスト色にする
                    pva.NameText.text = $"<color={Utils.GetRoleColorCode(CustomRoles.Egoist)}>{pva.NameText.text}</color>";
                }
                if (seer.Is(CustomRoles.EgoSchrodingerCat) && //LocalPlayerがEgoSchrodingerCat
                    target.Is(CustomRoles.Egoist) //変更対象がEgoist
                )
                {
                    //変更対象の名前をエゴイスト色にする
                    pva.NameText.text = $"<color={Utils.GetRoleColorCode(CustomRoles.Egoist)}>{pva.NameText.text}</color>";
                }

                if (seer.Is(CustomRoles.Arsonist) && //seerがアーソニストの時
                    seer.IsDousedPlayer(target)) //seerがtargetに既にオイルを塗っている(完了)
                {
                    pva.NameText.text += $"<color={Utils.GetRoleColorCode(CustomRoles.Arsonist)}>▲</color>";
                }

                //会議画面ではインポスター自身の名前にSnitchマークはつけません。

                //自分自身の名前の色を変更
                if (target != null && target.AmOwner && AmongUsClient.Instance.IsGameStarted) //変更先が自分自身
                {
                    pva.NameText.text = $"<color={seer.GetRoleColorCode()}>{pva.NameText.text}</color>"; //名前の色を変更
                }
                foreach (var ExecutionerTarget in Main.ExecutionerTarget)
                {
                    if ((seer.PlayerId == ExecutionerTarget.Key || seer.Data.IsDead) && //seerがKey or Dead
                    target.PlayerId == ExecutionerTarget.Value) //targetがValue
                        pva.NameText.text += $"<color={Utils.GetRoleColorCode(CustomRoles.Executioner)}>♦</color>";
                }
                if (seer.Is(CustomRoles.Doctor) && //LocalPlayerがDoctor
                target.Data.IsDead) //変更対象が死人
                    pva.NameText.text = $"{pva.NameText.text}(<color={Utils.GetRoleColorCode(CustomRoles.Doctor)}>{Utils.GetVitalText(target.PlayerId)}</color>)";
            }
        }
    }
    [HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.Update))]
    class MeetingHudUpdatePatch
    {
        public static void Postfix(MeetingHud __instance)
        {
            if (!AmongUsClient.Instance.AmHost) return;
            if (Input.GetMouseButtonUp(1) && Input.GetKey(KeyCode.LeftControl))
            {
                __instance.playerStates.DoIf(x => x.HighlightedFX.enabled, x =>
                {
                    var player = Utils.GetPlayerById(x.TargetPlayerId);
                    player.RpcExileV2();
                    PlayerState.SetDeathReason(player.PlayerId, PlayerState.DeathReason.Execution);
                    PlayerState.SetDead(player.PlayerId);
                    Utils.SendMessage($"{player.Data.PlayerName}を処刑しました");
                    Logger.Info($"{player.GetNameWithRole()}を処刑しました", "Execution");
                });
            }
        }
    }
    [HarmonyPatch(typeof(PlayerVoteArea), nameof(PlayerVoteArea.SetHighlighted))]
    class SetHighlightedPatch
    {
        public static bool Prefix(PlayerVoteArea __instance, bool value)
        {
            if (!AmongUsClient.Instance.AmHost) return true;
            if (!__instance.HighlightedFX) return false;
            __instance.HighlightedFX.enabled = value;
            return false;
        }
    }
    [HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.OnDestroy))]
    class MeetingHudOnDestroyPatch
    {
        public static void Postfix()
        {
            Logger.Info("------------会議終了------------", "Phase");
        }
    }
}