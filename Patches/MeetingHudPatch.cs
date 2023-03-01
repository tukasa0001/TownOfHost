using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HarmonyLib;
using UnityEngine;

using TownOfHost.Roles.Impostor;
using TownOfHost.Roles.Crewmate;
using TownOfHost.Roles.Neutral;
using static TownOfHost.Translator;

namespace TownOfHost
{
    [HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.CheckForEndVoting))]
    class CheckForEndVotingPatch
    {
        public static bool Prefix(MeetingHud __instance)
        {
            if (!AmongUsClient.Instance.AmHost) return true;
            var voteLog = Logger.Handler("Vote");
            try
            {
                List<MeetingHud.VoterState> statesList = new();
                MeetingHud.VoterState[] states;
                foreach (var pva in __instance.playerStates)
                {
                    if (pva == null) continue;
                    PlayerControl pc = Utils.GetPlayerById(pva.TargetPlayerId);
                    if (pc == null) continue;
                    //死んでいないディクテーターが投票済み
                    if (pc.Is(CustomRoles.Dictator) && pva.DidVote && pc.PlayerId != pva.VotedFor && pva.VotedFor < 253 && !pc.Data.IsDead)
                    {
                        var voteTarget = Utils.GetPlayerById(pva.VotedFor);
                        TryAddAfterMeetingDeathPlayers(PlayerState.DeathReason.Suicide, pc.PlayerId);
                        statesList.Add(new()
                        {
                            VoterId = pva.TargetPlayerId,
                            VotedForId = pva.VotedFor
                        });
                        states = statesList.ToArray();
                        if (AntiBlackout.OverrideExiledPlayer)
                        {
                            __instance.RpcVotingComplete(states, null, true);
                            ExileControllerWrapUpPatch.AntiBlackout_LastExiled = voteTarget.Data;
                        }
                        else __instance.RpcVotingComplete(states, voteTarget.Data, false); //通常処理

                        Logger.Info($"{voteTarget.GetNameWithRole()}を追放", "Dictator");
                        CheckForDeathOnExile(PlayerState.DeathReason.Vote, pva.VotedFor);
                        Logger.Info("ディクテーターによる強制会議終了", "Special Phase");
                        voteTarget.SetRealKiller(pc);
                        return true;
                    }
                }
                foreach (var ps in __instance.playerStates)
                {
                    //死んでいないプレイヤーが投票していない
                    if (!(Main.PlayerStates[ps.TargetPlayerId].IsDead || ps.DidVote)) return false;
                }

                GameData.PlayerInfo exiledPlayer = PlayerControl.LocalPlayer.Data;
                bool tie = false;

                for (var i = 0; i < __instance.playerStates.Length; i++)
                {
                    PlayerVoteArea ps = __instance.playerStates[i];
                    if (ps == null) continue;
                    voteLog.Info(string.Format("{0,-2}{1}:{2,-3}{3}", ps.TargetPlayerId, Utils.PadRightV2($"({Utils.GetVoteName(ps.TargetPlayerId)})", 40), ps.VotedFor, $"({Utils.GetVoteName(ps.VotedFor)})"));
                    var voter = Utils.GetPlayerById(ps.TargetPlayerId);
                    if (voter == null || voter.Data == null || voter.Data.Disconnected) continue;
                    if (Options.VoteMode.GetBool())
                    {
                        if (ps.VotedFor == 253 && !voter.Data.IsDead && //スキップ
                            !(Options.WhenSkipVoteIgnoreFirstMeeting.GetBool() && MeetingStates.FirstMeeting) && //初手会議を除く
                            !(Options.WhenSkipVoteIgnoreNoDeadBody.GetBool() && !MeetingStates.IsExistDeadBody) && //死体がない時を除く
                            !(Options.WhenSkipVoteIgnoreEmergency.GetBool() && MeetingStates.IsEmergencyMeeting) //緊急ボタンを除く
                            )
                        {
                            switch (Options.GetWhenSkipVote())
                            {
                                case VoteMode.Suicide:
                                    TryAddAfterMeetingDeathPlayers(PlayerState.DeathReason.Suicide, ps.TargetPlayerId);
                                    voteLog.Info($"スキップしたため{voter.GetNameWithRole()}を自殺させました");
                                    break;
                                case VoteMode.SelfVote:
                                    ps.VotedFor = ps.TargetPlayerId;
                                    voteLog.Info($"スキップしたため{voter.GetNameWithRole()}に自投票させました");
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
                                    TryAddAfterMeetingDeathPlayers(PlayerState.DeathReason.Suicide, ps.TargetPlayerId);
                                    voteLog.Info($"無投票のため{voter.GetNameWithRole()}を自殺させました");
                                    break;
                                case VoteMode.SelfVote:
                                    ps.VotedFor = ps.TargetPlayerId;
                                    voteLog.Info($"無投票のため{voter.GetNameWithRole()}に自投票させました");
                                    break;
                                case VoteMode.Skip:
                                    ps.VotedFor = 253;
                                    voteLog.Info($"無投票のため{voter.GetNameWithRole()}にスキップさせました");
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
                voteLog.Info("===追放者確認処理開始===");
                foreach (var data in VotingData)
                {
                    voteLog.Info($"{data.Key}({Utils.GetVoteName(data.Key)}):{data.Value}票");
                    if (data.Value > max)
                    {
                        voteLog.Info(data.Key + "番が最高値を更新(" + data.Value + ")");
                        exileId = data.Key;
                        max = data.Value;
                        tie = false;
                    }
                    else if (data.Value == max)
                    {
                        voteLog.Info(data.Key + "番が" + exileId + "番と同数(" + data.Value + ")");
                        exileId = byte.MaxValue;
                        tie = true;
                    }
                    voteLog.Info($"exileId: {exileId}, max: {max}票");
                }

                voteLog.Info($"追放者決定: {exileId}({Utils.GetVoteName(exileId)})");

                if (Options.VoteMode.GetBool() && Options.WhenTie.GetBool() && tie)
                {
                    switch ((TieMode)Options.WhenTie.GetValue())
                    {
                        case TieMode.Default:
                            exiledPlayer = GameData.Instance.AllPlayers.ToArray().FirstOrDefault(info => info.PlayerId == exileId);
                            break;
                        case TieMode.All:
                            var exileIds = VotingData.Where(x => x.Key < 15 && x.Value == max).Select(kvp => kvp.Key).ToArray();
                            foreach (var playerId in exileIds)
                                Utils.GetPlayerById(playerId).SetRealKiller(null);
                            TryAddAfterMeetingDeathPlayers(PlayerState.DeathReason.Vote, exileIds);
                            exiledPlayer = null;
                            break;
                        case TieMode.Random:
                            exiledPlayer = GameData.Instance.AllPlayers.ToArray().OrderBy(_ => Guid.NewGuid()).FirstOrDefault(x => VotingData.TryGetValue(x.PlayerId, out int vote) && vote == max);
                            tie = false;
                            break;
                    }
                }
                else
                    exiledPlayer = GameData.Instance.AllPlayers.ToArray().FirstOrDefault(info => !tie && info.PlayerId == exileId);
                if (exiledPlayer != null)
                    exiledPlayer.Object.SetRealKiller(null);

                //RPC
                if (AntiBlackout.OverrideExiledPlayer)
                {
                    __instance.RpcVotingComplete(states, null, true);
                    ExileControllerWrapUpPatch.AntiBlackout_LastExiled = exiledPlayer;
                }
                else __instance.RpcVotingComplete(states, exiledPlayer, tie); //通常処理

                CheckForDeathOnExile(PlayerState.DeathReason.Vote, exileId);

                return false;
            }
            catch (Exception ex)
            {
                Logger.SendInGame(string.Format(GetString("Error.MeetingException"), ex.Message), true);
                throw;
            }
        }
        public static bool IsMayor(byte id)
        {
            var player = Main.AllPlayerControls.Where(pc => pc.PlayerId == id).FirstOrDefault();
            return player != null && player.Is(CustomRoles.Mayor);
        }
        public static void TryAddAfterMeetingDeathPlayers(PlayerState.DeathReason deathReason, params byte[] playerIds)
        {
            var AddedIdList = new List<byte>();
            foreach (var playerId in playerIds)
                if (Main.AfterMeetingDeathPlayers.TryAdd(playerId, deathReason))
                    AddedIdList.Add(playerId);
            CheckForDeathOnExile(deathReason, AddedIdList.ToArray());
        }
        public static void CheckForDeathOnExile(PlayerState.DeathReason deathReason, params byte[] playerIds)
        {
            Witch.OnCheckForEndVoting(deathReason, playerIds);
            foreach (var playerId in playerIds)
            {
                //Loversの後追い
                if (CustomRoles.Lovers.IsEnable() && !Main.isLoversDead && Main.LoversPlayers.Find(lp => lp.PlayerId == playerId) != null)
                    FixedUpdatePatch.LoversSuicide(playerId, true);
                //道連れチェック
                RevengeOnExile(playerId, deathReason);
            }
        }
        private static void RevengeOnExile(byte playerId, PlayerState.DeathReason deathReason)
        {
            var player = Utils.GetPlayerById(playerId);
            if (player == null) return;
            var target = PickRevengeTarget(player, deathReason);
            if (target == null) return;
            TryAddAfterMeetingDeathPlayers(PlayerState.DeathReason.Revenge, target.PlayerId);
            target.SetRealKiller(player);
            Logger.Info($"{player.GetNameWithRole()}の道連れ先:{target.GetNameWithRole()}", "RevengeOnExile");
        }
        private static PlayerControl PickRevengeTarget(PlayerControl exiledplayer, PlayerState.DeathReason deathReason)//道連れ先選定
        {
            List<PlayerControl> TargetList = new();
            foreach (var candidate in Main.AllAlivePlayerControls)
            {
                if (candidate == exiledplayer || Main.AfterMeetingDeathPlayers.ContainsKey(candidate.PlayerId)) continue;
                switch (exiledplayer.GetCustomRole())
                {
                    //ここに道連れ役職を追加
                    default:
                        if (exiledplayer.Is(CustomRoleTypes.Madmate) && deathReason == PlayerState.DeathReason.Vote && Options.MadmateRevengeCrewmate.GetBool() //黒猫オプション
                        && !candidate.Is(CustomRoleTypes.Impostor))
                            TargetList.Add(candidate);
                        break;
                }
            }
            if (TargetList == null || TargetList.Count == 0) return null;
            var rand = IRandom.Instance;
            var target = TargetList[rand.Next(TargetList.Count)];
            return target;
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
            ChatUpdatePatch.DoBlockChat = true;
            GameStates.AlreadyDied |= !Utils.IsAllAlive;
            Main.AllPlayerControls.Do(x => ReportDeadBodyPatch.WaitReport[x.PlayerId].Clear());
            Utils.NotifyRoles(isMeeting: true, NoCache: true);
            MeetingStates.MeetingCalled = true;
        }
        public static void Postfix(MeetingHud __instance)
        {
            SoundManager.Instance.ChangeAmbienceVolume(0f);
            if (!GameStates.IsModHost) return;
            foreach (var pva in __instance.playerStates)
            {
                var pc = Utils.GetPlayerById(pva.TargetPlayerId);
                if (pc == null) continue;
                var RoleTextData = Utils.GetRoleText(pc.PlayerId);
                var roleTextMeeting = UnityEngine.Object.Instantiate(pva.NameText);
                roleTextMeeting.transform.SetParent(pva.NameText.transform);
                roleTextMeeting.transform.localPosition = new Vector3(0f, -0.18f, 0f);
                roleTextMeeting.fontSize = 1.5f;
                roleTextMeeting.text = RoleTextData.Item1;
                if (Main.VisibleTasksCount) roleTextMeeting.text += Utils.GetProgressText(pc);
                roleTextMeeting.color = RoleTextData.Item2;
                roleTextMeeting.gameObject.name = "RoleTextMeeting";
                roleTextMeeting.enableWordWrapping = false;
                roleTextMeeting.enabled =
                    pc.AmOwner || //対象がLocalPlayer
                    (Main.VisibleTasksCount && PlayerControl.LocalPlayer.Data.IsDead && Options.GhostCanSeeOtherRoles.GetBool()) || //LocalPlayerが死亡していて幽霊が他人の役職を見れるとき
                    pc.Is(CustomRoles.GM); //対象がGMのとき
                if (EvilTracker.IsTrackTarget(PlayerControl.LocalPlayer, pc) && EvilTracker.CanSeeLastRoomInMeeting)
                {
                    roleTextMeeting.text = EvilTracker.GetArrowAndLastRoom(PlayerControl.LocalPlayer, pc);
                    roleTextMeeting.enabled = true;
                }
            }
            if (Options.SyncButtonMode.GetBool())
            {
                Utils.SendMessage(string.Format(GetString("Message.SyncButtonLeft"), Options.SyncedButtonCount.GetFloat() - Options.UsedButtonCount));
                Logger.Info("緊急会議ボタンはあと" + (Options.SyncedButtonCount.GetFloat() - Options.UsedButtonCount) + "回使用可能です。", "SyncButtonMode");
            }
            if (AntiBlackout.OverrideExiledPlayer)
            {
                Utils.SendMessage(Translator.GetString("Warning.OverrideExiledPlayer"));
            }
            if (MeetingStates.FirstMeeting) TemplateManager.SendTemplate("OnFirstMeeting", noErr: true);
            TemplateManager.SendTemplate("OnMeeting", noErr: true);

            if (AmongUsClient.Instance.AmHost)
            {
                _ = new LateTask(() =>
                {
                    foreach (var pc in Main.AllPlayerControls)
                    {
                        pc.RpcSetNameEx(pc.GetRealName(isMeeting: true));
                    }
                    ChatUpdatePatch.DoBlockChat = false;
                }, 3f, "SetName To Chat");
            }

            foreach (var pva in __instance.playerStates)
            {
                if (pva == null) continue;
                PlayerControl seer = PlayerControl.LocalPlayer;
                PlayerControl target = Utils.GetPlayerById(pva.TargetPlayerId);
                if (target == null) continue;

                var sb = new StringBuilder();

                //会議画面での名前変更
                //自分自身の名前の色を変更
                //NameColorManager準拠の処理
                pva.NameText.text = pva.NameText.text.ApplyNameColorData(seer, target, true);

                //とりあえずSnitchは会議中にもインポスターを確認することができる仕様にしていますが、変更する可能性があります。

                if (seer.KnowDeathReason(target))
                    sb.Append($"({Utils.ColorString(Utils.GetRoleColor(CustomRoles.Doctor), Utils.GetVitalText(target.PlayerId))})");

                //インポスター表示
                switch (seer.GetCustomRole().GetCustomRoleTypes())
                {
                    case CustomRoleTypes.Impostor:
                        if (target.Is(CustomRoles.MadSnitch) && target.GetPlayerTaskState().IsTaskFinished && Options.MadSnitchCanAlsoBeExposedToImpostor.GetBool())
                            sb.Append(Utils.ColorString(Utils.GetRoleColor(CustomRoles.MadSnitch), "★")); //変更対象にSnitchマークをつける
                        sb.Append(Snitch.GetWarningMark(seer, target));
                        break;
                }
                switch (seer.GetCustomRole())
                {
                    case CustomRoles.Arsonist:
                        if (seer.IsDousedPlayer(target)) //seerがtargetに既にオイルを塗っている(完了)
                            sb.Append(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Arsonist), "▲"));
                        break;
                    case CustomRoles.Executioner:
                        sb.Append(Executioner.TargetMark(seer, target));
                        break;
                    case CustomRoles.Egoist:
                    case CustomRoles.Jackal:
                        sb.Append(Snitch.GetWarningMark(seer, target));
                        break;
                    case CustomRoles.EvilTracker:
                        sb.Append(EvilTracker.GetTargetMark(seer, target));
                        break;
                }

                foreach (var subRole in target.GetCustomSubRoles())
                {
                    switch (subRole)
                    {
                        case CustomRoles.Lovers:
                            if (seer.Is(CustomRoles.Lovers) || seer.Data.IsDead)
                                sb.Append(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Lovers), "♡"));
                            break;
                    }
                }

                //呪われている場合
                sb.Append(Witch.GetSpelledMark(target.PlayerId, true));

                //会議画面ではインポスター自身の名前にSnitchマークはつけません。

                pva.NameText.text += sb.ToString();
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
                    Main.PlayerStates[player.PlayerId].deathReason = PlayerState.DeathReason.Execution;
                    Main.PlayerStates[player.PlayerId].SetDead();
                    Utils.SendMessage(string.Format(GetString("Message.Executed"), player.Data.PlayerName));
                    Logger.Info($"{player.GetNameWithRole()}を処刑しました", "Execution");
                    __instance.CheckForEndVoting();
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
            MeetingStates.FirstMeeting = false;
            Logger.Info("------------会議終了------------", "Phase");
            if (AmongUsClient.Instance.AmHost)
            {
                AntiBlackout.SetIsDead();
                Main.AllPlayerControls.Do(pc => RandomSpawn.CustomNetworkTransformPatch.NumOfTP[pc.PlayerId] = 0);
            }
        }
    }
}