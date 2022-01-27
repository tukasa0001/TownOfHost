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

namespace TownOfHost
{
    [HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.CheckForEndVoting))]
    class CheckForEndVotingPatch {
        public static bool Prefix(MeetingHud __instance) {
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
                statesList.Add(new MeetingHud.VoterState() {
                    VoterId = ps.TargetPlayerId,
                    VotedForId = ps.VotedFor
                });
            }
            states = statesList.ToArray();

            var VotingData = __instance.CustomCalculateVotes();
            byte exileId = byte.MaxValue;
            int max = 0;
            Logger.info("===追放者確認処理開始===");
            foreach(var data in VotingData) {
                Logger.info(data.Key + ": " + data.Value);
                if(data.Value > max) {
                    Logger.info(data.Key + "番が最高値を更新(" + data.Value + ")");
                    exileId = data.Key;
                    max = data.Value;
                    tie = false;
                } else
                if(data.Value == max) {
                    Logger.info(data.Key + "番が" + exileId + "番と同数(" + data.Value + ")");
                    exileId = byte.MaxValue;
                    tie = true;
                }
                Logger.info("exileId: " + exileId + ", max: " + max);
            }
            
            Logger.info("追放者決定: " + exileId);
            exiledPlayer = GameData.Instance.AllPlayers.ToArray().FirstOrDefault(info => !tie && info.PlayerId == exileId);
            MeetingHud.Instance.RpcVotingComplete(states, exiledPlayer, tie);
            return false;
        }
        
    }

    static class ExtendedMeetingHud {
        public static Dictionary<byte, int> CustomCalculateVotes(this MeetingHud __instance) {
            Dictionary<byte, int> dic = new Dictionary<byte, int>();
            //| 投票された人 | 投票された回数 |
            for(int i = 0; i < __instance.playerStates.Length; i++) {
                PlayerVoteArea ps = __instance.playerStates[i];
                if(ps.VotedFor != (byte) 252 && ps.VotedFor != byte.MaxValue && ps.VotedFor != (byte) 254) {
                    int num;
                    int VoteNum = 1;
                    //投票を1追加 キーが定義されていない場合は1で上書きして定義
                    dic[ps.VotedFor] = !dic.TryGetValue(ps.VotedFor, out num) ? VoteNum : num + VoteNum;
                }
            }
            return dic;
        }
    }
}