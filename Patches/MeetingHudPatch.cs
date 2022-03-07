using BepInEx;
using BepInEx.Configuration;
using BepInEx.IL2CPP;
using System;
using HarmonyLib;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnhollowerBaseLib;
using TownOfHost;
using System.Linq;
using Il2CppSystem.Linq;
using Hazel;

namespace TownOfHost
{
    [HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.CheckForEndVoting))]
    class CheckForEndVotingPatch {
        public static bool Prefix(MeetingHud __instance) {
            try {
            
            
            if(!AmongUsClient.Instance.AmHost) return true;
            foreach(var ps in __instance.playerStates) {
                if(!(ps.AmDead || ps.DidVote))//死んでいないプレイヤーが投票していない
                    return false;
            }
            

            
            MeetingHud.VoterState[] states;
            GameData.PlayerInfo exiledPlayer = PlayerControl.LocalPlayer.Data;
            bool tie = false;

            List<MeetingHud.VoterState> statesList = new List<MeetingHud.VoterState>();
            for(var i = 0; i < __instance.playerStates.Length; i++) {
                PlayerVoteArea ps = __instance.playerStates[i];
                if(ps == null) continue;
                Logger.info($"{ps.TargetPlayerId}:{ps.VotedFor}");
                var voter = main.getPlayerById(ps.TargetPlayerId);
                if(voter == null || voter.Data == null || voter.Data.Disconnected) continue;
                if(ps.VotedFor == 253 && !voter.Data.IsDead)//スキップ
                {
                    switch (main.whenSkipVote)
                    {
                        case VoteMode.Suicide:
                            main.ps.setDeathReason(ps.TargetPlayerId,PlayerState.DeathReason.Suicide);
                            voter.RpcMurderPlayer(voter);
                            main.IgnoreReportPlayers.Add(voter.PlayerId);
                            break;
                        case VoteMode.SelfVote:
                            ps.VotedFor = ps.TargetPlayerId;
                            break;
                        default:
                            break;
                    }
                }
                if(ps.VotedFor == 254 && !voter.Data.IsDead)//無投票
                {
                    switch (main.whenNonVote)
                    {
                        case VoteMode.Suicide:
                            main.ps.setDeathReason(ps.TargetPlayerId,PlayerState.DeathReason.Suicide);
                            voter.RpcMurderPlayer(voter);
                            main.IgnoreReportPlayers.Add(voter.PlayerId);
                            break;
                        case VoteMode.SelfVote:
                            ps.VotedFor = ps.TargetPlayerId;
                            break;
                        default:
                            break;
                    }
                }
                statesList.Add(new MeetingHud.VoterState() {
                    VoterId = ps.TargetPlayerId,
                    VotedForId = ps.VotedFor
                });
                if(isMayor(ps.TargetPlayerId))//Mayorの投票数
                for(var i2 = 0; i2 < main.MayorAdditionalVote; i2++) {
                    statesList.Add(new MeetingHud.VoterState() {
                        VoterId = ps.TargetPlayerId,
                        VotedForId = ps.VotedFor
                    });
                }
            }
            states = statesList.ToArray();

            var VotingData = __instance.CustomCalculateVotes();
            byte exileId = byte.MaxValue;
            int max = 0;
            Logger.info("===追放者確認処理開始===");
            foreach(var data in VotingData) {
                Logger.info(data.Key + ": " + data.Value);
                if(data.Value > max)
                {
                    Logger.info(data.Key + "番が最高値を更新(" + data.Value + ")");
                    exileId = data.Key;
                    max = data.Value;
                    tie = false;
                } else if(data.Value == max) {
                    Logger.info(data.Key + "番が" + exileId + "番と同数(" + data.Value + ")");
                    exileId = byte.MaxValue;
                    tie = true;
                }
                Logger.info("exileId: " + exileId + ", max: " + max);
            }

            Logger.info("追放者決定: " + exileId);
            exiledPlayer = GameData.Instance.AllPlayers.ToArray().FirstOrDefault(info => !tie && info.PlayerId == exileId);

            __instance.RpcVotingComplete(states, exiledPlayer, tie); //RPC

            //霊界用暗転バグ対処
            foreach(var pc in PlayerControl.AllPlayerControls)
                if(pc.isSheriff() && (pc.Data.IsDead || pc.PlayerId == exiledPlayer?.PlayerId)) pc.ResetPlayerCam(19f);
            
            return false;


            }
            catch(Exception ex) {
                Logger.SendInGame("エラー:" + ex.Message + "\r\nSHIFT+M+ENTERで会議を強制終了してください", true);
                throw;
            }
        }
        public static bool isMayor(byte id) {
            var player = PlayerControl.AllPlayerControls.ToArray().Where(pc => pc.PlayerId == id).FirstOrDefault();
            if(player == null) return false;
            return player.isMayor();
        }
    }

    static class ExtendedMeetingHud {
        public static Dictionary<byte, int> CustomCalculateVotes(this MeetingHud __instance) {
            Logger.info("CustomCalculateVotes開始");
            Dictionary<byte, int> dic = new Dictionary<byte, int>();
            //| 投票された人 | 投票された回数 |
            for(int i = 0; i < __instance.playerStates.Length; i++) {
                PlayerVoteArea ps = __instance.playerStates[i];
                if(ps == null) continue;
                if(ps.VotedFor != (byte) 252 && ps.VotedFor != byte.MaxValue && ps.VotedFor != (byte) 254) {
                    int num;
                    int VoteNum = 1;
                    if(CheckForEndVotingPatch.isMayor(ps.TargetPlayerId)) VoteNum = main.MayorAdditionalVote + 1;
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
            main.witchMeeting = true;
            main.NotifyRoles(isMeeting:true);
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
                roleTextMeeting.enabled = false;
            }
            if (main.SyncButtonMode)
            {
                if(AmongUsClient.Instance.AmHost) PlayerControl.LocalPlayer.RpcSetName("test");
                main.SendToAll("緊急会議ボタンはあと" + (main.SyncedButtonCount - main.UsedButtonCount) + "回使用可能です。");
                Logger.SendToFile("緊急会議ボタンはあと" + (main.SyncedButtonCount - main.UsedButtonCount) + "回使用可能です。", LogLevel.Message);
            }

            if (AmongUsClient.Instance.AmHost)
            {
                _ = new LateTask(() =>
                {
                    foreach (var pc in PlayerControl.AllPlayerControls)
                    {
                        pc.RpcSetName(pc.getRealName(isMeeting: true));
                    }
                }, 3f, "SetName To Chat");
            }

            foreach(var pva in __instance.playerStates) {
                if(pva == null) continue;
                PlayerControl pc = main.getPlayerById(pva.TargetPlayerId);
                if(pc == null) continue;

                //会議画面での名前変更
                //とりあえずSnitchは会議中にもインポスターを確認することができる仕様にしていますが、変更する可能性があります。
                //変更する場合でも、このコードはMadSnitchで使うと思うので消さないでください。

                //インポスター表示
                bool LocalPlayerKnowsImpostor = false; //203行目のif文で使う trueの時にインポスターの名前を赤くする
                if(PlayerControl.LocalPlayer.isSnitch() && //LocalPlayerがSnitch
                PlayerControl.LocalPlayer.getPlayerTaskState().isTaskFinished) //LocalPlayerがタスクを終えている
                    LocalPlayerKnowsImpostor = true;
                
                if(LocalPlayerKnowsImpostor) {
                    if(pc != null && pc.getCustomRole().isImpostor()) //変更先がインポスター
                        //変更対象の名前を赤くする
                        pva.NameText.text = "<color=#ff0000>" + pva.NameText.text + "</color>";
                }

                if(PlayerControl.LocalPlayer.getCustomRole().isImpostor() && //LocalPlayerがImpostor
                pc.isSnitch() && //変更対象がSnitch
                pc.getPlayerTaskState().doExpose //変更対象のタスクが終わりそう
                ) {
                    //変更対象にSnitchマークをつける
                    pva.NameText.text += $"<color={main.getRoleColorCode(CustomRoles.Snitch)}>★</color>";
                }

                //会議画面ではインポスター自身の名前にSnitchマークはつけません。

                //自分自身の名前の色を変更
                if(pc != null && pc.AmOwner && AmongUsClient.Instance.IsGameStarted) //変更先が自分自身
                    pva.NameText.text  = $"<color={PlayerControl.LocalPlayer.getRoleColorCode()}>{pva.NameText.text}</color>"; //名前の色を変更
            }
        }
    }
    [HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.Update))]
    class MeetingHudUpdatePatch
    {
        public static void Postfix(MeetingHud __instance)
        {
            if(AmongUsClient.Instance.GameMode == GameModes.FreePlay) return;
            foreach (var pva in __instance.playerStates)
            {
                if(pva == null) continue;
                PlayerControl pc = main.getPlayerById(pva.TargetPlayerId);
                if(pc == null) continue;

                //役職表示系
                var RoleTextMeetingTransform = pva.NameText.transform.Find("RoleTextMeeting");
                TMPro.TextMeshPro RoleTextMeeting = null;
                if(RoleTextMeetingTransform != null) RoleTextMeeting = RoleTextMeetingTransform.GetComponent<TMPro.TextMeshPro>();
                if (RoleTextMeeting != null)
                {

                    var RoleTextData = main.GetRoleText(pc);
                    RoleTextMeeting.text = RoleTextData.Item1;
                    if (main.VisibleTasksCount && main.hasTasks(pc.Data, false)) RoleTextMeeting.text += " <color=#e6b422>(" + main.getTaskText(pc.Data.Tasks) + ")</color>";
                    RoleTextMeeting.color = RoleTextData.Item2;
                    if (pva.TargetPlayerId == PlayerControl.LocalPlayer.PlayerId) RoleTextMeeting.enabled = true;
                    else if (main.VisibleTasksCount && PlayerControl.LocalPlayer.Data.IsDead) RoleTextMeeting.enabled = true;
                    else RoleTextMeeting.enabled = false;
                }
            }
        }
    }
}
