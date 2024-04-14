using System.Collections.Generic;
using System.Text;

using HarmonyLib;
using UnityEngine;

using TownOfHostForE.Modules;
using TownOfHostForE.Roles;
using TownOfHostForE.Roles.Core;
using static TownOfHostForE.Translator;
using TownOfHostForE.Roles.AddOns.Common;
using TownOfHostForE.Roles.Crewmate;
using TownOfHostForE.Roles.Core.Interfaces;
using TownOfHostForE.Roles.Animals;
using System.Linq;
using TownOfHostForE.Roles.Impostor;
using TMPro;
using static TownOfHostForE.GameMode.WordLimit;
using TownOfHostForE.GameMode;

namespace TownOfHostForE;

[HarmonyPatch]
public static class MeetingHudPatch
{
    [HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.CheckForEndVoting))]
    class CheckForEndVotingPatch
    {
        public static bool Prefix()
        {
            if (!AmongUsClient.Instance.AmHost) return true;
            MeetingVoteManager.Instance?.CheckAndEndMeeting();
            Utils.NotifyRoles();
            return false;
        }
    }
    [HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.CastVote))]
    public static class CastVotePatch
    {
        public static bool Prefix(MeetingHud __instance, [HarmonyArgument(0)] byte srcPlayerId /* 投票した人 */ , [HarmonyArgument(1)] byte suspectPlayerId /* 投票された人 */ )
        {
            var voter = Utils.GetPlayerById(srcPlayerId);
            var voted = Utils.GetPlayerById(suspectPlayerId);

            //インポスターチャット
            if (ImposterChat.ImposterChats(voter,voted) == false)
            {
                __instance.RpcClearVote(voter.GetClientId());
                return false;
            }

            //ロールの奴
            if (voter.GetRoleClass()?.CheckVoteAsVoter(voted) == false)
            {
                __instance.RpcClearVote(voter.GetClientId());
                Logger.Info($"{voter.GetNameWithRole()} は投票しない", nameof(CastVotePatch));
                return false;
            }

            MeetingVoteManager.Instance?.SetVote(srcPlayerId, suspectPlayerId);
            return true;
        }
    }
    [HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.Start))]
    class StartPatch
    {
        public static void Prefix()
        {
            Logger.Info("------------会議開始------------", "Phase");
            BGMSettings.SetMeetingBGM();
            ChatUpdatePatch.DoBlockChat = true;
            GameStates.AlreadyDied |= !Utils.IsAllAlive;
            Main.AllPlayerControls.Do(x => ReportDeadBodyPatch.WaitReport[x.PlayerId].Clear());
            Sending.OnStartMeeting();
            MeetingStates.MeetingCalled = true;
        }
        public static void Postfix(MeetingHud __instance)
        {
            MeetingVoteManager.Start();

            SoundManager.Instance.ChangeAmbienceVolume(0f);
            if (!GameStates.IsModHost) return;
            var myRole = PlayerControl.LocalPlayer.GetRoleClass();
            foreach (var pva in __instance.playerStates)
            {
                var pc = Utils.GetPlayerById(pva.TargetPlayerId);
                if (pc == null) continue;
                var roleTextMeeting = Object.Instantiate(pva.NameText);
                roleTextMeeting.transform.SetParent(pva.NameText.transform);
                roleTextMeeting.transform.localPosition = new Vector3(0f, -0.18f, 0f);
                roleTextMeeting.fontSize = 1.5f;
                (roleTextMeeting.enabled, roleTextMeeting.text)
                    = Utils.GetRoleNameAndProgressTextData(true, PlayerControl.LocalPlayer, pc);
                roleTextMeeting.gameObject.name = "RoleTextMeeting";
                roleTextMeeting.enableWordWrapping = false;

                if (BetWinTeams.BetWinTeamMode.GetBool() && BetWinTeams.BetPoint.ContainsKey(pc.FriendCode) && !BetWinTeams.DisableShogo.GetBool())
                {

                    if (BetWinTeams.IsCamofuluge())
                    {
                        //カモフラージュのコミュサボ中は何もしない。
                    }
                    else if (BetWinTeams.BetPoint[pc.FriendCode].Syougo != null && BetWinTeams.BetPoint[pc.FriendCode].Syougo != "")
                    {
                        var ShogoTextMeeting = Object.Instantiate(pva.NameText);
                        ShogoTextMeeting.transform.SetParent(pva.NameText.transform);
                        ShogoTextMeeting.fontSize = 1.5f;
                        ShogoTextMeeting.text = BetWinTeams.BetPoint[pc.FriendCode].Syougo;

                        //後で全体的にあげるので、もうちょっと下へ
                        ShogoTextMeeting.transform.localPosition = new Vector3(0f, -0.16f, 0f);
                        roleTextMeeting.transform.localPosition = new Vector3(0f, -0.28f, 0f);
                    }
                }
                else
                {
                    roleTextMeeting.transform.localPosition = new Vector3(0f, -0.18f, 0f);
                }

                ////改行コードの有無で位置調整
                //if (roleTextMeeting.text.Contains("\n"))
                //{
                //    Logger.Info("改行コードがあるからさらに下げる", "debug");
                //    roleTextMeeting.transform.localPosition = new Vector3(0f,  -0.4f, 0f);
                //}

                // 役職とサフィックスを同時に表示する必要が出たら要改修
                var suffixBuilder = new StringBuilder(32);
                if (myRole != null)
                {
                    suffixBuilder.Append(myRole.GetSuffix(PlayerControl.LocalPlayer, pc, isForMeeting: true));
                }
                suffixBuilder.Append(CustomRoleManager.GetSuffixOthers(PlayerControl.LocalPlayer, pc, isForMeeting: true));
                if (suffixBuilder.Length > 0)
                {
                    roleTextMeeting.text = suffixBuilder.ToString();
                    roleTextMeeting.enabled = true;
                }
            }
            CustomRoleManager.AllActiveRoles.Values.Do(role => role.OnStartMeeting());
            if (Options.SyncButtonMode.GetBool())
            {
                Utils.SendMessage(string.Format(GetString("Message.SyncButtonLeft"), Options.SyncedButtonCount.GetFloat() - Options.UsedButtonCount));
                Logger.Info("緊急会議ボタンはあと" + (Options.SyncedButtonCount.GetFloat() - Options.UsedButtonCount) + "回使用可能です。", "SyncButtonMode");
            }
            if (Options.ShowReportReason.GetBool())
            {
                if (ReportDeadBodyPatch.ReportTarget == null)
                    Utils.SendMessage(GetString("Message.isButton"));
                else
                    Utils.SendMessage(string.Format(GetString("Message.isReport"), ReportDeadBodyPatch.ReportTarget.PlayerName));
            }
            if (Options.ShowRevengeTarget.GetBool())
            {
                foreach (var Exiled_Target in RevengeTargetPlayer)
                {
                    Utils.SendMessage(string.Format(GetString("Message.RevengeText"), Exiled_Target.Item1.name, Exiled_Target.Item2.name));
                }
                RevengeTargetPlayer.Clear();
            }

            if (AntiBlackout.OverrideExiledPlayer)
            {
                Utils.SendMessage(GetString("Warning.OverrideExiledPlayer"));
            }
            if (MeetingStates.FirstMeeting) TemplateManager.SendTemplate("OnFirstMeeting", noErr: true);
            TemplateManager.SendTemplate("OnMeeting", noErr: true);



            if (AmongUsClient.Instance.AmHost)
            {
                //言葉制限があれば初手会議で報告
                if (Options.GetWordLimitMode() != regulation.None && MeetingStates.FirstMeeting)
                {
                    IsFirstMeetingCheck(Options.GetWordLimitMode());
                }

                _ = new LateTask(() =>
                {
                    foreach (var seen in Main.AllPlayerControls)
                    {
                        var seenName = seen.GetRealName(isMeeting: true);
                        var coloredName = Utils.ColorString(seen.GetRoleColor(), seenName);
                        foreach (var seer in Main.AllPlayerControls)
                        {
                            seen.RpcSetNamePrivate(
                                seer == seen ? coloredName : seenName,
                                true,
                                seer);
                        }
                    }
                    ChatUpdatePatch.DoBlockChat = false;
                    //foreach (var seer in Main.AllPlayerControls)
                    //{
                    //    foreach (var target in Main.AllPlayerControls)
                    //    {
                    //        var seerName = seer.GetRealName(isMeeting: true);
                    //        var coloredName = Utils.ColorString(seer.GetRoleColor(), seerName);
                    //        seer.RpcSetNamePrivate(
                    //            seer == target ? coloredName : seerName,
                    //            true);
                    //    }
                    //}
                    //ChatUpdatePatch.DoBlockChat = false;
                }, 3f, "SetName To Chat");
            }

            foreach (var pva in __instance.playerStates)
            {
                if (pva == null) continue;
                var seer = PlayerControl.LocalPlayer;
                var seerRole = seer.GetRoleClass();

                var target = Utils.GetPlayerById(pva.TargetPlayerId);
                if (target == null) continue;

                // 初手会議での役職説明表示
                if (Options.ShowRoleInfoAtFirstMeeting.GetBool() && MeetingStates.FirstMeeting)
                {
                    string RoleInfoTitleString = $"{GetString("RoleInfoTitle")}";
                    string RoleInfoTitle = $"{Utils.ColorString(Utils.GetRoleColor(target.GetCustomRole()), RoleInfoTitleString)}";
                    Utils.SendMessage(Utils.GetMyRoleInfo(target), sendTo: pva.TargetPlayerId, title: RoleInfoTitle);
                }

                var sb = new StringBuilder();

                //会議画面での名前変更
                //自分自身の名前の色を変更
                //NameColorManager準拠の処理
                pva.NameText.text = pva.NameText.text.ApplyNameColorData(seer, target, true);

                //とりあえずSnitchは会議中にもインポスターを確認することができる仕様にしていますが、変更する可能性があります。

                if (seer.KnowDeathReason(target))
                    sb.Append($"({Utils.ColorString(Utils.GetRoleColor(CustomRoles.Doctor), Utils.GetVitalText(target.PlayerId))})");

                sb.Append(seerRole?.GetMark(seer, target, true));
                sb.Append(CustomRoleManager.GetMarkOthers(seer, target, true));

                var cRole = seer.GetCustomRole();
                switch (cRole)
                {
                    case CustomRoles.Leopard:
                        if (!seer.Data.IsDead && !target.Data.IsDead)
                            pva.NameText.text = Utils.ColorString(Utils.GetRoleColor(CustomRoles.Leopard), target.PlayerId.ToString()) + " " + pva.NameText.text;
                        break;
                    case CustomRoles.NiceGuesser:
                    case CustomRoles.EvilGuesser:
                        if (!seer.Data.IsDead && !target.Data.IsDead)
                            pva.NameText.text = Utils.ColorString(Utils.GetRoleColor(seer.Is(CustomRoles.NiceGuesser) ? CustomRoles.NiceGuesser : CustomRoles.EvilGuesser), target.PlayerId.ToString()) + " " + pva.NameText.text;
                        break;
                    default:
                        if (ImposterChat.CheckImposterChat(cRole))
                        {
                            pva.NameText.text = Utils.ColorString(Utils.GetRoleColor(CustomRoles.EvilGuesser), target.PlayerId.ToString()) + " " + pva.NameText.text;
                        }
                        break;
                }

                //相手の属性
                foreach (var subRole in target.GetCustomSubRoles())
                {
                    switch (subRole)
                    {
                        case CustomRoles.Lovers:
                            //if (seer.Is(CustomRoles.Lovers) || seer.Data.IsDead)
                            if (Utils.CheckMyLovers(seer.PlayerId,target.PlayerId) || seer.Data.IsDead)
                                sb.Append(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Lovers), "♥"));
                            break;
                    }
                }

                //シーアの属性
                foreach (var subRole in seer.GetCustomSubRoles())
                {
                    switch (subRole)
                    {
                        case CustomRoles.Gambler:
                            if (!seer.Data.IsDead && !target.Data.IsDead)
                                pva.NameText.text = Utils.ColorString(Utils.GetRoleColor(CustomRoles.Gambler), target.PlayerId.ToString()) + " " + pva.NameText.text;
                            break;
                    }
                }

                //最後に称号付けれたらつけるよ
                if (BetWinTeams.BetWinTeamMode.GetBool() && BetWinTeams.BetPoint.ContainsKey(target.FriendCode) && !BetWinTeams.DisableShogo.GetBool())
                {

                    if (BetWinTeams.IsCamofuluge())
                    {
                        //カモフラージュのコミュサボ中は何もしない。
                    }
                    else if (BetWinTeams.BetPoint[target.FriendCode].Syougo != null && BetWinTeams.BetPoint[target.FriendCode].Syougo != "")
                    {
                        //全体的に上にずらす
                        pva.NameText.transform.SetLocalY(0.15f);
                    }
                }

                //会議画面ではインポスター自身の名前にSnitchマークはつけません。

                pva.NameText.text += sb.ToString();
            }
        }
    }
    [HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.Update))]
    class UpdatePatch
    {
        public static void Postfix(MeetingHud __instance)
        {
            if (!AmongUsClient.Instance.AmHost) return;
            //if (firstmeeting)
            //{
            //    new LateTask(() =>
            //    {
            //        Utils.MeetingStartOnlyNotifyRoles();
            //    }, 5f, "FirstMeetingDelay");
            //    firstmeeting = false;
            //}
            if (Input.GetMouseButtonUp(1) && Input.GetKey(KeyCode.LeftControl))
            {
                __instance.playerStates.DoIf(x => x.HighlightedFX.enabled, x =>
                {
                    var player = Utils.GetPlayerById(x.TargetPlayerId);
                    player.RpcExileV2();
                    var state = PlayerState.GetByPlayerId(player.PlayerId);
                    state.DeathReason = CustomDeathReason.Execution;
                    state.SetDead();
                    Utils.SendMessage(string.Format(GetString("Message.Executed"), player.Data.PlayerName));
                    Logger.Info($"{player.GetNameWithRole()}を処刑しました", "Execution");
                    __instance.CheckForEndVoting();
                });
            }
        }
    }
    [HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.OnDestroy))]
    class OnDestroyPatch
    {
        public static void Postfix()
        {
            BGMSettings.SetTaskBGM();
            MeetingStates.FirstMeeting = false;
            Logger.Info("------------会議終了------------", "Phase");
            if (AmongUsClient.Instance.AmHost)
            {
                AntiBlackout.SetIsDead();
            }
            // MeetingVoteManagerを通さずに会議が終了した場合の後処理
            MeetingVoteManager.Instance?.Destroy();
        }
    }

    public static void TryAddAfterMeetingDeathPlayers(CustomDeathReason deathReason, params byte[] playerIds)
    {
        var AddedIdList = new List<byte>();
        foreach (var playerId in playerIds)
            if (Main.AfterMeetingDeathPlayers.TryAdd(playerId, deathReason))
                AddedIdList.Add(playerId);
        CheckForDeathOnExile(deathReason, AddedIdList.ToArray());
    }
    public static void CheckForDeathOnExile(CustomDeathReason deathReason, params byte[] playerIds)
    {
        foreach (var playerId in playerIds)
        {
            //Loversの後追い
            //if ((CustomRoles.Lovers.IsPresent() || CustomRoles.PlatonicLover.IsPresent() || CustomRoles.OtakuPrincess.IsPresent()) && !Main.isLoversDead && Main.LoversPlayers.Find(lp => lp.PlayerId == playerId) != null)
            if(CheckLoversSuicide(playerId))
                FixedUpdatePatch.LoversSuicide(playerId, true);
            //道連れチェック
            RevengeOnExile(playerId, deathReason);
        }
    }

    private static bool CheckLoversSuicide(byte playerId)
    {
        //ラバーズ系がいない
        if (!(CustomRoles.Lovers.IsPresent() || CustomRoles.PlatonicLover.IsPresent() || CustomRoles.OtakuPrincess.IsPresent()))
             return false;

        byte ownerId = byte.MaxValue;

        foreach (var list in Main.LoversPlayersV2)
        {
            foreach (var id in list.Value)
            {
                if (id == playerId)
                {
                    ownerId = list.Key;
                    break;
                }
            }
            if (ownerId != byte.MaxValue)
                break;
        }

        //ラバーズに所属していないなら抜ける
        if (ownerId == byte.MaxValue)
            return false;


        return !Main.isLoversDeadV2[ownerId];
    }

    //道連れ
    public static List<(PlayerControl, PlayerControl)> RevengeTargetPlayer;
    private static void RevengeOnExile(byte playerId, CustomDeathReason deathReason)
    {
        var player = Utils.GetPlayerById(playerId);
        if (player == null) return;
        //道連れ能力持たない時は下を通さない
        if (!((player.Is(CustomRoles.SKMadmate) && Options.MadmateRevengeCrewmate.GetBool())
            || player.Is(CustomRoles.EvilNekomata) || player.Is(CustomRoles.Nekomata) || player.Is(CustomRoles.Revenger) || player.Is(CustomRoles.NekoKabocha))) return;

        var target = PickRevengeTarget(player, deathReason);
        if (target == null) return;
        TryAddAfterMeetingDeathPlayers(CustomDeathReason.Revenge, target.PlayerId);
        target.SetRealKiller(player);
        Logger.Info($"{player.GetNameWithRole()}の道連れ先:{target.GetNameWithRole()}", "RevengeOnExile");
    }
    private static PlayerControl PickRevengeTarget(PlayerControl exiledplayer, CustomDeathReason deathReason)//道連れ先選定
    {
        List<PlayerControl> TargetList = new();

        if (exiledplayer.GetRoleClass() is INekomata nekomata)
        {
            // 道連れしない状態ならnull
            if (!nekomata.DoRevenge(deathReason))
            {
                return null;
            }
            TargetList = Main.AllAlivePlayerControls.Where(candidate => candidate != exiledplayer && !Main.AfterMeetingDeathPlayers.ContainsKey(candidate.PlayerId) && nekomata.IsCandidate(candidate)).ToList();
        }
        else
        {
            foreach (var candidate in Main.AllAlivePlayerControls)
            {
                if (candidate == exiledplayer || Main.AfterMeetingDeathPlayers.ContainsKey(candidate.PlayerId)) continue;

                //対象とならない人を判定
                if (exiledplayer.Is(CustomRoleTypes.Madmate) || exiledplayer.Is(CustomRoleTypes.Impostor)) //インポスター陣営の場合
                {
                    if (candidate.Is(CustomRoleTypes.Impostor)) continue; //インポスター
                    if (candidate.Is(CustomRoleTypes.Madmate) && !Options.RevengeMadByImpostor.GetBool()) continue; //マッドメイト（設定）
                }
                if (candidate.Is(CustomRoleTypes.Neutral) && !Options.RevengeNeutral.GetBool()) continue; //第三陣営（設定）

                TargetList.Add(candidate);
                //switch (exiledplayer.GetCustomRole())
                //{
                //    //ここに道連れ役職を追加
                //    default:
                //        if (exiledplayer.Is(CustomRoleTypes.Madmate) && deathReason == CustomDeathReason.Vote && Options.MadmateRevengeCrewmate.GetBool() //黒猫オプション
                //        && !candidate.Is(CustomRoleTypes.Impostor))
                //            TargetList.Add(candidate);
                //        break;
                //}
            }
        }
        if (TargetList == null || TargetList.Count == 0) return null;
        var rand = IRandom.Instance;
        var target = TargetList[rand.Next(TargetList.Count)];
        // 道連れする側とされる側をセットでリストに追加
        RevengeTargetPlayer.Add((exiledplayer, target));
        return target;
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
