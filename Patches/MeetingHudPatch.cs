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

            //Sheriff用RPCの送信
            foreach(var pc in PlayerControl.AllPlayerControls) {
                if(tie) break;
                if(pc.getCustomRole() != CustomRoles.Sheriff) continue;
                if(exiledPlayer.PlayerId != pc.PlayerId) continue;
                var clientId = pc.getClientId();
                MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(__instance.NetId, (byte)23, SendOption.Reliable, clientId);
                writer.WritePacked(states.Length);
                foreach(var state in states) {
                    state.Serialize(writer);
                }
                writer.Write(byte.MaxValue);
                writer.Write(tie);
                AmongUsClient.Instance.FinishRpcImmediately(writer);
            }
            //実際のRPC
            new LateTask(() => MeetingHud.Instance.RpcVotingComplete(states, exiledPlayer, tie), 0.5f, "RpcVotingCompleteTask");
            return false;
        }
        public static bool isMayor(byte id) {
            var player = PlayerControl.AllPlayerControls.ToArray().Where(pc => pc.PlayerId == id).FirstOrDefault();
            if(player == null) return false;
            return player.isMayor();
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
                    if(CheckForEndVotingPatch.isMayor(ps.TargetPlayerId)) VoteNum = main.MayorAdditionalVote + 1;
                    //投票を1追加 キーが定義されていない場合は1で上書きして定義
                    dic[ps.VotedFor] = !dic.TryGetValue(ps.VotedFor, out num) ? VoteNum : num + VoteNum;
                }
            }
            return dic;
        }
    }
}
