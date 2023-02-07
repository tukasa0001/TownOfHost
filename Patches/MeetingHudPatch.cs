using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using UnityEngine;
using static TownOfHost.Translator;

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
                        TryAddAfterMeetingDeathPlayers(pc.PlayerId, PlayerState.DeathReason.Suicide);
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

                        if (CustomRoles.Witch.IsEnable())
                        {
                            Witch.OnCheckForEndVoting(pva.VotedFor);
                        }
                        Logger.Info($"{voteTarget.GetNameWithRole()}を追放", "Dictator");
                        FollowingSuicideOnExile(pva.VotedFor);
                        RevengeOnExile(pva.VotedFor);
                        Logger.Info("ディクテーターによる強制会議終了", "Special Phase");
                        voteTarget.SetRealKiller(pc);
                        return true;
                    }
                }
                foreach (var ps in __instance.playerStates)
                {
                    //死んでいないプレイヤーが投票していない
                    if (!(ps.AmDead || ps.DidVote)) return false;
                }

                GameData.PlayerInfo exiledPlayer = PlayerControl.LocalPlayer.Data;
                bool tie = false;

                for (var i = 0; i < __instance.playerStates.Length; i++)
                {
                    PlayerVoteArea ps = __instance.playerStates[i];
                    if (ps == null) continue;
                    Logger.Info(string.Format("{0,-2}{1}:{2,-3}{3}", ps.TargetPlayerId, Utils.PadRightV2($"({Utils.GetVoteName(ps.TargetPlayerId)})", 40), ps.VotedFor, $"({Utils.GetVoteName(ps.VotedFor)})"), "PlayerVote");
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
                                    TryAddAfterMeetingDeathPlayers(ps.TargetPlayerId, PlayerState.DeathReason.Suicide);
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
                                    TryAddAfterMeetingDeathPlayers(ps.TargetPlayerId, PlayerState.DeathReason.Suicide);
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
                    if (IsMayor(ps.TargetPlayerId) && !Options.MayorHideVote.GetBool())//Mayorの投票数
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

                if (Options.VoteMode.GetBool() && Options.WhenTie.GetBool() && tie)
                {
                    switch ((TieMode)Options.WhenTie.GetValue())
                    {
                        case TieMode.Default:
                            exiledPlayer = GameData.Instance.AllPlayers.ToArray().FirstOrDefault(info => info.PlayerId == exileId);
                            break;
                        case TieMode.All:
                            VotingData.DoIf(x => x.Key < 15 && x.Value == max, x =>
                            {
                                TryAddAfterMeetingDeathPlayers(x.Key, PlayerState.DeathReason.Vote);
                                Utils.GetPlayerById(x.Key).SetRealKiller(null);
                            });
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

                if (CustomRoles.Witch.IsEnable())
                {
                    Witch.OnCheckForEndVoting(exileId);
                }
                
                FollowingSuicideOnExile(exileId);
                RevengeOnExile(exileId);

                // 参考：https://github.com/music-discussion/TownOfHost-TheOtherRoles
                if (exiledPlayer != null)
                {
                    var realName = exiledPlayer.Object.GetRealName(isMeeting: true);
                    Main.LastVotedPlayer = realName;
                    if (exiledPlayer.PlayerId == exileId)
                    {
                        var player = Utils.GetPlayerById(exiledPlayer.PlayerId);
                        var role = GetString(exiledPlayer.GetCustomRole().ToString());
                        var crole = exiledPlayer.GetCustomRole();
                        var coloredRole = Utils.ColorString(Utils.GetRoleColor(exiledPlayer.GetCustomRole()), $"{role}");
                        var name = "";
                        int impnum = 0;
                        int neutralnum = 0;
                        foreach (var pc in PlayerControl.AllPlayerControls)
                        {
                            if (pc == null || pc.Data.IsDead || pc.Data.Disconnected) continue;
                            var pc_role = pc.GetCustomRole();
                            if (pc_role.IsImpostor() && pc != exiledPlayer.Object)
                                impnum++;
                            else if (pc_role.IsNeutralKilling() && pc != exiledPlayer.Object)
                                neutralnum++;
                        }
                        if (Options.ConfirmEjectionsRoles.GetBool())
                        {
                            name = string.Format(GetString("PlayerIsRole"), realName, coloredRole);
                        }
                        else if (Options.ConfirmEjections.GetBool())
                        {
                            if (CustomRolesHelper.IsImpostor(player.GetCustomRole()))
                            {
                                name = $"{realName} 属于 " + Utils.ColorString(Utils.GetRoleColor(CustomRoles.Impostor), "内鬼阵营");
                            }
                            else
                            {
                                name = string.Format(GetString("IsGood"), realName);
                            }
                            if (Options.ConfirmEjectionsNK.GetBool() && CustomRolesHelper.IsNK(player.GetCustomRole()))
                            {
                                if (Options.ConfirmEjectionsNKAsImp.GetBool())
                                {
                                    name = $"{realName} 属于 " + Utils.ColorString(Utils.GetRoleColor(CustomRoles.Impostor), "内鬼阵营");
                                }
                                else
                                {
                                    name = $"{realName} 属于 " + Utils.ColorString(Color.cyan, "中立阵营");
                                }
                            }
                            if (Options.ConfirmEjectionsNonNK.GetBool() && CustomRolesHelper.IsNNK(player.GetCustomRole()))
                            {
                                if (Options.ConfirmEjectionsNKAsImp.GetBool())
                                {
                                    name = $"{realName} 属于 " + Utils.ColorString(Utils.GetRoleColor(CustomRoles.Impostor), "内鬼阵营");
                                }
                                else
                                {
                                    name = $"{realName} 属于 " + Utils.ColorString(Color.cyan, "中立阵营");
                                }
                            }
                        }
                        else
                        {
                            name = string.Format(GetString("PlayerExiled"), realName);
                        }
                        if (crole == CustomRoles.Jester)
                            name = string.Format(GetString("ExiledJester"), realName, coloredRole) + "<size=0>";

                        if (Executioner.Target.ContainsValue(exileId))
                            name = string.Format(GetString("ExiledExeTarget"), realName, coloredRole) + "<size=0>";
                        if (Options.ShowImpRemainOnEject.GetBool() && crole != CustomRoles.Jester && !Executioner.Target.ContainsValue(exileId))
                        {
                            name += "\n";
                            string comma = neutralnum != 0 ? "，" : "";
                            if (impnum == 0)
                            {
                                name += GetString("NoImpRemain") + comma;
                            }
                            else
                            {
                                name += string.Format(GetString("ImpRemain"), impnum) + comma;
                            }

                            if (Options.ShowNKRemainOnEject.GetBool() && neutralnum != 0)
                                name += string.Format(GetString("NeutralRemain"), neutralnum);
                        }
                        name += "<size=0>";
                        new LateTask(() =>
                        {
                            player.RpcSetName(name);
                        }, 3.0f, "Change Exiled Player Name");
                        new LateTask(() =>
                        {
                            player.RpcSetName(realName);
                        }, 11.5f, "Change Exiled Player Name Back");
                    }
                }

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
        public static void TryAddAfterMeetingDeathPlayers(byte playerId, PlayerState.DeathReason deathReason)
        {
            if (Main.AfterMeetingDeathPlayers.TryAdd(playerId, deathReason))
            {
                FollowingSuicideOnExile(playerId);
                RevengeOnExile(playerId, deathReason);
            }
        }
        public static void FollowingSuicideOnExile(byte playerId)
        {
            //Loversの後追い
            if (CustomRoles.Lovers.IsEnable() && !Main.isLoversDead && Main.LoversPlayers.Find(lp => lp.PlayerId == playerId) != null)
                FixedUpdatePatch.LoversSuicide(playerId, true);
        }
        public static void RevengeOnExile(byte playerId, PlayerState.DeathReason deathReason = PlayerState.DeathReason.Vote)
        {
            if (deathReason == PlayerState.DeathReason.Suicide) return;
            var player = Utils.GetPlayerById(playerId);
            if (player == null) return;
            var target = PickRevengeTarget(player);
            if (target == null) return;
            TryAddAfterMeetingDeathPlayers(target.PlayerId, PlayerState.DeathReason.Revenge);
            target.SetRealKiller(player);
            Logger.Info($"{player.GetNameWithRole()}の道連れ先:{target.GetNameWithRole()}", "MadmatesRevengeOnExile");
        }
        public static PlayerControl PickRevengeTarget(PlayerControl exiledplayer)//道連れ先選定
        {
            List<PlayerControl> TargetList = new();
            foreach (var candidate in Main.AllPlayerControls)
            {
                if (candidate == exiledplayer || candidate.Data.IsDead || Main.AfterMeetingDeathPlayers.ContainsKey(candidate.PlayerId)) continue;
                switch (exiledplayer.GetCustomRole())
                {
                    //ここに道連れ役職を追加
                    default:
                        if (exiledplayer.Is(RoleType.Madmate) && Options.MadmateRevengeCrewmate.GetBool() //黒猫オプション
                        && !candidate.Is(RoleType.Impostor))
                            TargetList.Add(candidate);
                        break;
                }
            }
            if (TargetList == null || TargetList.Count == 0) return null;
            var rand = IRandom.Instance;
            var target = TargetList[rand.Next(TargetList.Count)];
            Logger.Info($"{exiledplayer.GetNameWithRole()}の道連れ先:{target.GetNameWithRole()}", "PickRevengeTarget");
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
                    var target = Utils.GetPlayerById(ps.VotedFor);
                    if (target != null && target.Is(CustomRoles.Zombie)) VoteNum = 0;
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
        public static void NoticeMafiaSkill()
        {
            if (!AmongUsClient.Instance.AmHost) return;
            foreach (var pc in PlayerControl.AllPlayerControls)
            {
                switch (pc.GetCustomRole())
                {
                    case CustomRoles.Mafia:
                        new LateTask(() =>
                        {
                            Utils.SendMessage(GetString("MafiaDeadMsg"), pc.PlayerId);
                        }, 5.0f, "Notice Mafia Skill");
                    break;
                }
            }
        }
        public static void NoticeCyberStarSkill()
        {
            if (!AmongUsClient.Instance.AmHost) return;
            foreach (var pc in PlayerControl.AllPlayerControls)
            {
                if (!Options.ImpKnowCyberStarDead.GetBool() && CustomRolesHelper.IsImpostor(pc.GetCustomRole())) continue;
                if (!Options.NeutralKnowCyberStarDead.GetBool() && CustomRolesHelper.IsNeutral(pc.GetCustomRole())) continue;
                foreach (var csId in Main.CyberStarDead)
                    {
                    var cs = Utils.GetPlayerById(csId);
                    if (cs == null) continue;
                        new LateTask(() =>
                        {
                            Utils.SendMessage(string.Format(GetString("CyberStarDead"), cs.GetRealName()), pc.PlayerId, Utils.ColorString(    Utils.GetRoleColor(CustomRoles.CyberStar)  , GetString("CyberStarNewsTitle")));
                        }, 5.0f, "Notice CyberStar Skill");
                    }
            }
        }
        public static void NoticeDetectiveSkill()
        {
            if (!AmongUsClient.Instance.AmHost) return;
            foreach (var pc in PlayerControl.AllPlayerControls)
            {
                if (Main.DetectiveNotify.ContainsKey(pc.PlayerId))
                {
                    new LateTask(() =>
                    {
                        Utils.SendMessage(Main.DetectiveNotify[pc.PlayerId], pc.PlayerId, Utils.ColorString(Utils.GetRoleColor(CustomRoles.Detective), GetString("DetecticeNotiveTitle")));
                    }, 5.0f, "Notice Detective Skill");
                }
            }
        }

        public static void NoticeGodAlive()
        {
            foreach (var pc in PlayerControl.AllPlayerControls)
            {
                if (pc.Is(CustomRoles.God) && pc.IsAlive())
                {
                    new LateTask(() =>
                    {
                        Utils.SendMessage(GetString("GodNoticeAlive"), 255, Utils.ColorString(Utils.GetRoleColor(CustomRoles.God), GetString("GodAliveTitle")));
                    }, 5.0f, "Notice God Alive");
                    return;
                }
            }
        }

        public static void NoticeSkill()
        {
            if (!AmongUsClient.Instance.AmHost) return;
            if (Options.MafiaCanKillNum.GetInt() > 0) NoticeMafiaSkill();
            if (Main.CyberStarDead.Count > 0) NoticeCyberStarSkill();
            if (Main.DetectiveNotify.Count > 0) NoticeDetectiveSkill();
            if (Options.NotifyGodAlive.GetBool()) NoticeGodAlive();
        }

        public static void Prefix(MeetingHud __instance)
        {
            Logger.Info("------------会議開始------------", "Phase");
            ChatUpdatePatch.DoBlockChat = true;
            GameStates.AlreadyDied |= GameData.Instance.AllPlayers.ToArray().Any(x => x.IsDead);
            Main.AllPlayerControls.Do(x => ReportDeadBodyPatch.WaitReport[x.PlayerId].Clear());
            MeetingStates.MeetingCalled = true;
            Main.GuesserGuessed.Clear();
            NoticeSkill();
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
                    pva.TargetPlayerId == PlayerControl.LocalPlayer.PlayerId ||
                    (Main.VisibleTasksCount && PlayerControl.LocalPlayer.Data.IsDead && Options.GhostCanSeeOtherRoles.GetBool()) ||
                    (AmongUsClient.Instance.AmHost && PlayerControl.LocalPlayer.Is(CustomRoles.GM)) ||
                    (pc.GetCustomRole().IsImpostor() && PlayerControl.LocalPlayer.GetCustomRole().IsImpostor() && !PlayerControl.LocalPlayer.Data.IsDead && Options.ImpKnowAlliesRole.GetBool()) ||
                    (PlayerControl.LocalPlayer.Is(CustomRoles.God) && !PlayerControl.LocalPlayer.Data.IsDead);
                if (EvilTracker.IsTrackTarget(PlayerControl.LocalPlayer, pc) && EvilTracker.CanSeeLastRoomInMeeting.GetBool())
                {
                    roleTextMeeting.text = EvilTracker.GetArrowAndLastRoom(PlayerControl.LocalPlayer, pc);
                    roleTextMeeting.enabled = true;
                }
            }

            if(AmongUsClient.Instance.AmHost && PlayerControl.LocalPlayer.Is(CustomRoles.Psychic))
            {
                System.Random rd = new();
                int numOfPsychicBad = 0;
                for (int i = 0; i < Options.PsychicCanSeeNum.GetInt(); i++)
                {
                    if (rd.Next(1, 100) < 18) numOfPsychicBad++;
                }
                if (numOfPsychicBad > Options.PsychicCanSeeNum.GetInt() || numOfPsychicBad < 1) numOfPsychicBad = 1;

                List<byte> goodids = new();
                List<byte> badids = new();
                Dictionary<byte, bool> isGood = new();
                if (!PlayerControl.LocalPlayer.Data.IsDead)
                {
                    if (PlayerControl.LocalPlayer.Is(CustomRoles.Psychic))
                    {
                        if (Options.PsychicFresh.GetBool() || !Main.PsychicTarget.ContainsKey(PlayerControl.LocalPlayer.PlayerId))
                        {
                            List<PlayerControl> badPlayers = new();
                            List<PlayerControl> goodPlayers = new();
                            foreach (var pc in PlayerControl.AllPlayerControls)
                            {
                                if (pc == null || pc.Data.IsDead || pc.Data.Disconnected || pc.PlayerId == PlayerControl.LocalPlayer.PlayerId) continue;
                                isGood.Add(pc.PlayerId, true);
                                var role = pc.GetCustomRole();
                                switch (role.GetRoleType())
                                {
                                    case RoleType.Crewmate:
                                        if (Options.CkshowEvil.GetBool())
                                            if (!role.IsCK())
                                            {
                                                badPlayers.Add(pc);
                                                isGood[pc.PlayerId] = false;
                                            }
                                        break;
                                    case RoleType.Impostor:
                                        badPlayers.Add(pc); isGood[pc.PlayerId] = false;
                                        break;
                                    case RoleType.Neutral:
                                        if (Options.NBshowEvil.GetBool())
                                            if (!role.IsNeutralKilling())
                                            {
                                                badPlayers.Add(pc);
                                                isGood[pc.PlayerId] = false;
                                            }
                                        if (Options.NEshowEvil.GetBool())
                                        {
                                            if (role.IsNeutralKilling())
                                            {
                                                badPlayers.Add(pc);
                                                isGood[pc.PlayerId] = false;
                                            }
                                        }
                                        break;
                                }
                                if (isGood[pc.PlayerId]) goodPlayers.Add(pc);
                            }
                            List<byte> badpcids = new();
                            foreach (var p in badPlayers)
                            {
                                badpcids.Add(p.PlayerId);
                            }
                            if (numOfPsychicBad > Options.PsychicCanSeeNum.GetInt() || numOfPsychicBad < 1) numOfPsychicBad = 1;
                            int goodPeople = Options.PsychicCanSeeNum.GetInt() - numOfPsychicBad;

                            if (numOfPsychicBad != 0)
                                for (var i = 0; i < numOfPsychicBad; i++)
                                {
                                    if (badPlayers.Count <= 0) break;
                                    var rando = new System.Random();
                                    var player = badPlayers[rando.Next(0, badPlayers.Count)];
                                    badPlayers.Remove(player);
                                    badids.Add(player.PlayerId);
                                }
                            if (goodPeople != 0)
                                for (var i = 0; i < goodPeople; i++)
                                {
                                    if (goodPlayers.Count <= 0) break;
                                    var rando = new System.Random();
                                    var player = goodPlayers[rando.Next(0, goodPlayers.Count)];
                                    goodPlayers.Remove(player);
                                    goodids.Add(player.PlayerId);
                                }

                            byte pcid = PlayerControl.LocalPlayer.PlayerId;
                            if (!Main.PsychicTarget.ContainsKey(pcid)) Main.PsychicTarget.Add(pcid, new List<byte>());
                            Main.PsychicTarget[pcid] = new();
                            Main.PsychicTarget[pcid].AddRange(goodids);
                            Main.PsychicTarget[pcid].AddRange(badids);
                        }

                    }
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
                    Utils.NotifyRoles(isMeeting: true, NoCache: true);
                    ChatUpdatePatch.DoBlockChat = false;
                }, 3.5f, "SetName To Chat");
            }

            foreach (var pva in __instance.playerStates)
            {
                if (pva == null) continue;
                PlayerControl seer = PlayerControl.LocalPlayer;
                PlayerControl target = Utils.GetPlayerById(pva.TargetPlayerId);
                if (target == null) continue;

                //会議画面での名前変更
                //自分自身の名前の色を変更
                if (target != null && target.AmOwner && AmongUsClient.Instance.IsGameStarted) //変更先が自分自身
                    pva.NameText.color = seer.GetRoleColor();//名前の色を変更

                //とりあえずSnitchは会議中にもインポスターを確認することができる仕様にしていますが、変更する可能性があります。

                if (seer.KnowDeathReason(target))
                    pva.NameText.text += $"({Utils.ColorString(Utils.GetRoleColor(CustomRoles.Doctor), Utils.GetVitalText(target.PlayerId))})";

                //インポスター表示
                bool LocalPlayerKnowsImpostor = false; //203行目のif文で使う trueの時にインポスターの名前を赤くする
                bool LocalPlayerKnowsJackal = false; //trueの時にジャッカルの名前の色を変える
                bool LocalPlayerKnowsEgoist = false; //trueの時にエゴイストの名前の色を変える
                switch (seer.GetCustomRole().GetRoleType())
                {
                    case RoleType.Impostor:
                        LocalPlayerKnowsEgoist = true;
                        if (target.Is(CustomRoles.MadSnitch) && target.GetPlayerTaskState().IsTaskFinished && Options.MadSnitchCanAlsoBeExposedToImpostor.GetBool())
                            pva.NameText.text += Utils.ColorString(Utils.GetRoleColor(CustomRoles.MadSnitch), "★"); //変更対象にSnitchマークをつける
                        else if (target.Is(CustomRoles.Snitch) && //変更対象がSnitch
                        target.GetPlayerTaskState().DoExpose) //変更対象のタスクが終わりそう)
                            pva.NameText.text += Utils.ColorString(Utils.GetRoleColor(CustomRoles.Snitch), "★"); //変更対象にSnitchマークをつける
                        break;
                }
                switch (seer.GetCustomRole())
                {
                    case CustomRoles.MadSnitch:
                    case CustomRoles.Snitch:
                        if (seer.GetPlayerTaskState().IsTaskFinished) //seerがタスクを終えている
                        {
                            LocalPlayerKnowsImpostor = true;
                            if (seer.Is(CustomRoles.Snitch) && Options.SnitchCanFindNeutralKiller.GetBool())
                            {
                                LocalPlayerKnowsJackal = true;
                                LocalPlayerKnowsEgoist = true;
                            }
                        }
                        break;
                    case CustomRoles.Arsonist:
                        if (seer.IsDousedPlayer(target)) //seerがtargetに既にオイルを塗っている(完了)
                            pva.NameText.text += Utils.ColorString(Utils.GetRoleColor(CustomRoles.Arsonist), "▲");
                        break;
                    case CustomRoles.Executioner:
                        pva.NameText.text += Executioner.TargetMark(seer, target);
                        break;
                    case CustomRoles.Egoist:
                    case CustomRoles.Jackal:
                        if (Options.SnitchCanFindNeutralKiller.GetBool() &&
                        target.Is(CustomRoles.Snitch) && //変更対象がSnitch
                        target.GetPlayerTaskState().DoExpose) //変更対象のタスクが終わりそう)
                            pva.NameText.text += Utils.ColorString(Utils.GetRoleColor(CustomRoles.Snitch), "★"); //変更対象にSnitchマークをつける
                        break;
                    case CustomRoles.EvilTracker:
                        pva.NameText.text += EvilTracker.GetTargetMark(seer, target);
                        break;
                    case CustomRoles.EgoSchrodingerCat:
                        LocalPlayerKnowsEgoist = true;
                        break;
                    case CustomRoles.JSchrodingerCat:
                        LocalPlayerKnowsJackal = true;
                        break;
                    case CustomRoles.Psychic:
                        foreach (var id in Main.PsychicTarget[seer.PlayerId])
                        {
                            if (target.PlayerId == id) pva.NameText.text = Utils.ColorString(Utils.GetRoleColor(CustomRoles.Impostor), pva.NameText.text);
                        }
                        break;
                    case CustomRoles.Mafia:
                        if (seer.Data.IsDead && !target.Data.IsDead)
                        {
                            pva.NameText.text = Utils.ColorString(Utils.GetRoleColor(CustomRoles.Mafia), target.PlayerId.ToString()) + " " + pva.NameText.text;
                        }
                        break;
                    case CustomRoles.NiceGuesser:
                    case CustomRoles.EvilGuesser:
                        if (!seer.Data.IsDead && !target.Data.IsDead)
                        {
                            pva.NameText.text = Utils.ColorString(Utils.GetRoleColor(seer.Is(CustomRoles.NiceGuesser) ? CustomRoles.NiceGuesser : CustomRoles.EvilGuesser), target.PlayerId.ToString()) + " " + pva.NameText.text;
                        }
                        break;
                }

                switch (target.GetCustomRole())
                {
                    case CustomRoles.Egoist:
                        if (LocalPlayerKnowsEgoist)
                            pva.NameText.color = Utils.GetRoleColor(CustomRoles.Egoist); //変更対象の名前の色変更
                        break;
                    case CustomRoles.Jackal:
                        if (LocalPlayerKnowsJackal)
                            pva.NameText.color = Utils.GetRoleColor(CustomRoles.Jackal); //変更対象の名前をジャッカル色にする
                        break;
                }

                bool isLover = false;
                foreach (var subRole in target.GetCustomSubRoles())
                {
                    switch (subRole)
                    {
                        case CustomRoles.Lovers:
                            if (seer.Is(CustomRoles.Lovers) || seer.Data.IsDead)
                            {
                                pva.NameText.text += Utils.ColorString(Utils.GetRoleColor(CustomRoles.Lovers), "♡");
                                isLover = true;
                            }
                            break;
                    }
                }
                if ((seer.Is(CustomRoles.Ntr) || target.Is(CustomRoles.Ntr)) && !seer.Data.IsDead && !isLover)
                    pva.NameText.text += Utils.ColorString(Utils.GetRoleColor(CustomRoles.Lovers), "♡");
                else if (seer == target && CustomRolesHelper.RoleExist(CustomRoles.Ntr))
                    pva.NameText.text += Utils.ColorString(Utils.GetRoleColor(CustomRoles.Lovers), "♡");

                if (LocalPlayerKnowsImpostor)
                {
                    if (target != null && target.GetCustomRole().IsImpostor()) //変更先がインポスター
                        pva.NameText.color = Palette.ImpostorRed; //変更対象の名前を赤くする
                }
                //呪われている場合
                if (Witch.IsSpelled(target.PlayerId))
                    pva.NameText.text += Utils.ColorString(Utils.GetRoleColor(CustomRoles.Impostor), "†");

                if (target.Is(CustomRoles.SuperStar) && Options.EveryOneKnowSuperStar.GetBool())
                    pva.NameText.text += Utils.ColorString(Utils.GetRoleColor(CustomRoles.SuperStar), "★");

                //会議画面ではインポスター自身の名前にSnitchマークはつけません。
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
                Main.CyberStarDead.Clear();
                Main.DetectiveNotify.Clear();
            }
        }
    }
}