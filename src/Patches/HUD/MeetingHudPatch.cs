using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using TownOfHost.Extensions;
using TownOfHost.Managers;
using TownOfHost.ReduxOptions;
using TownOfHost.Roles;
using UnityEngine;
using static TownOfHost.Translator;

namespace TownOfHost.Patches.HUD;

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
                if (pc.Is(Dictator.Ref<Dictator>()) && pva.DidVote && pc.PlayerId != pva.VotedFor && pva.VotedFor < 253 && !pc.Data.IsDead)
                {
                    var voteTarget = Utils.GetPlayerById(pva.VotedFor);
                    TryAddAfterMeetingDeathPlayers(pc.PlayerId, PlayerStateOLD.DeathReason.Suicide);
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
                    /*FollowingSuicideOnExile(pva.VotedFor);*/
                    RevengeOnExile(pva.VotedFor);
                    Logger.Info("ディクテーターによる強制会議終了", "Special Phase");
                    /*voteTarget.SetRealKiller(pc);*/
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
                Logger.Info(string.Format("{0,-2}{1}:{2,-3}{3}", ps.TargetPlayerId, Utils.PadRightV2($"({Utils.GetVoteName(ps.TargetPlayerId)})", 40), ps.VotedFor, $"({Utils.GetVoteName(ps.VotedFor)})"), "Vote");
                var voter = Utils.GetPlayerById(ps.TargetPlayerId);
                if (voter == null || voter.Data == null || voter.Data.Disconnected) continue;
                if (StaticOptions.VoteMode)
                {
                    if (ps.VotedFor == 253 && !voter.Data.IsDead && //スキップ
                        !(StaticOptions.WhenSkipVoteIgnoreFirstMeeting && MeetingStates.FirstMeeting) && //初手会議を除く
                        !(StaticOptions.WhenSkipVoteIgnoreNoDeadBody && !MeetingStates.IsExistDeadBody) && //死体がない時を除く
                        !(StaticOptions.WhenSkipVoteIgnoreEmergency && MeetingStates.IsEmergencyMeeting) //緊急ボタンを除く
                       )
                    {
                        switch (StaticOptions.WhenSkipVote)
                        {
                            case "Suicide":
                                TryAddAfterMeetingDeathPlayers(ps.TargetPlayerId, PlayerStateOLD.DeathReason.Suicide);
                                Logger.Info($"スキップしたため{voter.GetNameWithRole()}を自殺させました", "Vote");
                                break;
                            case "Self Vote":
                                ps.VotedFor = ps.TargetPlayerId;
                                Logger.Info($"スキップしたため{voter.GetNameWithRole()}に自投票させました", "Vote");
                                break;
                            default:
                                break;
                        }
                    }
                    if (ps.VotedFor == 254 && !voter.Data.IsDead)//無投票
                    {
                        switch (StaticOptions.WhenNonVote)
                        {
                            case "Suicide":
                                TryAddAfterMeetingDeathPlayers(ps.TargetPlayerId, PlayerStateOLD.DeathReason.Suicide);
                                Logger.Info($"無投票のため{voter.GetNameWithRole()}を自殺させました", "Vote");
                                break;
                            case "Self Vote":
                                ps.VotedFor = ps.TargetPlayerId;
                                Logger.Info($"無投票のため{voter.GetNameWithRole()}に自投票させました", "Vote");
                                break;
                            case "Skip Vote":
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
                    for (var i2 = 0; i2 < StaticOptions.MayorAdditionalVote; i2++)
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

            if (StaticOptions.VoteMode && StaticOptions.WhenTie && tie)
            {
                switch (StaticOptions.WhenTieVote)
                {
                    case "Default":
                        exiledPlayer = GameData.Instance.AllPlayers.ToArray().FirstOrDefault(info => info.PlayerId == exileId);
                        break;
                    case "All":
                        VotingData.DoIf(x => x.Key < 15 && x.Value == max, x =>
                        {
                            TryAddAfterMeetingDeathPlayers(x.Key, PlayerStateOLD.DeathReason.Vote);
                            /*Utils.GetPlayerById(x.Key).SetRealKiller(null);*/
                        });
                        exiledPlayer = null;
                        break;
                    case "Random":
                        exiledPlayer = GameData.Instance.AllPlayers.ToArray().OrderBy(_ => Guid.NewGuid()).FirstOrDefault(x => VotingData.TryGetValue(x.PlayerId, out int vote) && vote == max);
                        tie = false;
                        break;
                }
            }
            else
                exiledPlayer = GameData.Instance.AllPlayers.ToArray().FirstOrDefault(info => !tie && info.PlayerId == exileId);
            /*if (exiledPlayer != null)
                exiledPlayer.Object.SetRealKiller(null);*/

            //RPC
            if (AntiBlackout.OverrideExiledPlayer)
            {
                __instance.RpcVotingComplete(states, null, true);
                ExileControllerWrapUpPatch.AntiBlackout_LastExiled = exiledPlayer;
            }
            else __instance.RpcVotingComplete(states, exiledPlayer, tie); //通常処理


            RevengeOnExile(exileId);

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
        var player = PlayerControl.AllPlayerControls.ToArray().FirstOrDefault(pc => pc.PlayerId == id);
        return player != null && player.Is(Mayor.Ref<Mayor>());
    }
    public static void TryAddAfterMeetingDeathPlayers(byte playerId, PlayerStateOLD.DeathReason deathReason)
    {
        if (TOHPlugin.AfterMeetingDeathPlayers.TryAdd(playerId, deathReason))
        {
            RevengeOnExile(playerId, deathReason);
        }
    }

    public static void RevengeOnExile(byte playerId, PlayerStateOLD.DeathReason deathReason = PlayerStateOLD.DeathReason.Vote)
    {
        if (deathReason == PlayerStateOLD.DeathReason.Suicide) return;
        var player = Utils.GetPlayerById(playerId);
        if (player == null) return;
        var target = PickRevengeTarget(player);
        if (target == null) return;
        TryAddAfterMeetingDeathPlayers(target.PlayerId, PlayerStateOLD.DeathReason.Revenge);
        /*target.SetRealKiller(player);*/
        Logger.Info($"{player.GetNameWithRole()}の道連れ先:{target.GetNameWithRole()}", "MadmatesRevengeOnExile");
    }
    public static PlayerControl PickRevengeTarget(PlayerControl exiledplayer)//道連れ先選定
    {
        List<PlayerControl> TargetList = new();
        foreach (var candidate in PlayerControl.AllPlayerControls)
        {
            if (candidate == exiledplayer || candidate.Data.IsDead || TOHPlugin.AfterMeetingDeathPlayers.ContainsKey(candidate.PlayerId)) continue;
            switch (exiledplayer.GetCustomRole())
            {
                //ここに道連れ役職を追加
                default:
                    if (exiledplayer.Is(Roles.RoleType.Madmate) && StaticOptions.MadmateRevengeCrewmate //黒猫オプション
                                                                && !candidate.Is(Roles.RoleType.Impostor))
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
        Game.State = GameState.InMeeting;
        Logger.Info("------------会議開始------------", "Phase");
        ActionHandle handle = ActionHandle.NoInit();
        Game.TriggerForAll(RoleActionType.RoundEnd, ref handle, false);
        ChatUpdatePatch.DoBlockChat = true;
        GameStates.AlreadyDied |= GameData.Instance.AllPlayers.ToArray().Any(x => x.IsDead);
        MeetingStates.MeetingCalled = true;
        Game.RenderAllForAll();
        "Meeting Call Done".DebugLog();
    }
    public static void Postfix(MeetingHud __instance)
    {
        SoundManager.Instance.ChangeMusicVolume(0f);
        if (StaticOptions.SyncButtonMode)
        {
            Utils.SendMessage(string.Format(GetString("Message.SyncButtonLeft"), StaticOptions.SyncedButtonCount - OldOptions.UsedButtonCount));
            Logger.Info("緊急会議ボタンはあと" + (StaticOptions.SyncedButtonCount - OldOptions.UsedButtonCount) + "回使用可能です。", "SyncButtonMode");
        }
        if (AntiBlackout.OverrideExiledPlayer)
            Utils.SendMessage(GetString("Warning.OverrideExiledPlayer"));

        if (AmongUsClient.Instance.AmHost)
            DTask.Schedule(() => ChatUpdatePatch.DoBlockChat = false, 3f);
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
                TOHPlugin.PlayerStates[player.PlayerId].deathReason = PlayerStateOLD.DeathReason.Execution;
                TOHPlugin.PlayerStates[player.PlayerId].SetDead();
                Utils.SendMessage(string.Format(GetString("Message.Executed"), player.Data.PlayerName));
                Logger.Info($"{player.GetNameWithRole()}を処刑しました", "Execution");
            });
        }
    }
}

[HarmonyPatch(typeof(ShipStatus), nameof(ShipStatus.CoStartMeeting))]
public class HostMeetingPatch
{
    public static void Prefix(ShipStatus __instance, [HarmonyArgument(0)] PlayerControl reporter)
    {
        if (!AmongUsClient.Instance.AmHost) return;
        reporter.GetDynamicName().RenderFor(PlayerControl.LocalPlayer);
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
        Game.State = GameState.Roaming;
        MeetingStates.FirstMeeting = false;
        Logger.Info("------------会議終了------------", "Phase");
        if (AmongUsClient.Instance.AmHost)
        {
            AntiBlackout.SetIsDead();
            PlayerControl.AllPlayerControls.ToArray().Do(pc => RandomSpawn.CustomNetworkTransformPatch.NumOfTP[pc.PlayerId] = 0);
        }
        ActionHandle handle = ActionHandle.NoInit();
        Game.TriggerForAll(RoleActionType.RoundStart, ref handle, false);
    }
}