using HarmonyLib;
using Hazel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using TMPro;
using TownOfHostForE.Roles.Core;
using TownOfHostForE.Attributes;
using TownOfHostForE.Roles.Impostor;
using TownOfHostForE.Roles.Animals;
using TownOfHostForE.Roles.Crewmate;
using UnityEngine;
using static TownOfHostForE.Translator;
using TownOfHostForE.Roles.AddOns.NotCrew;
using static UnityEngine.GraphicsBuffer;

namespace TownOfHostForE;

public static class GuessManager
{

    public static Dictionary<byte, int> GuesserGuessed = new();

    [GameModuleInitializer]
    public static void GameInit()
    {
        GuesserGuessed.Clear();
    }
    public static string GetFormatString()
    {
        string text = GetString("PlayerIdList");
        foreach (var pc in Main.AllAlivePlayerControls)
        {
            string id = pc.PlayerId.ToString();
            string name = pc.GetRealName();
            text += $"\n{id} → {name}";
        }
        return text;
    }

    public static bool CheckCommond(ref string msg, string command, bool exact = true)
    {
        var comList = command.Split('|');
        for (int i = 0; i < comList.Count(); i++)
        {
            if (exact)
            {
                if (msg == "/" + comList[i]) return true;
            }
            else
            {
                if (msg.StartsWith("/" + comList[i]))
                {
                    msg = msg.Replace("/" + comList[i], string.Empty);
                    return true;
                }
            }
        }
        return false;
    }

    public static byte GetColorFromMsg(string msg)
    {
        if (ComfirmIncludeMsg(msg, "赤|レッド|red")) return 0;
        if (ComfirmIncludeMsg(msg, "青|ブルー|blue")) return 1;
        if (ComfirmIncludeMsg(msg, "緑|グリーン|green")) return 2;
        if (ComfirmIncludeMsg(msg, "桃|ピンク|pink")) return 3;
        if (ComfirmIncludeMsg(msg, "橙|オレンジ|orange")) return 4;
        if (ComfirmIncludeMsg(msg, "黄|イエロー|yellow")) return 5;
        if (ComfirmIncludeMsg(msg, "黒|ブラック|black")) return 6;
        if (ComfirmIncludeMsg(msg, "白|ホワイト|white")) return 7;
        if (ComfirmIncludeMsg(msg, "紫|パープル|perple")) return 8;
        if (ComfirmIncludeMsg(msg, "茶|ブラウン|brown")) return 9;
        if (ComfirmIncludeMsg(msg, "水|シアン|cyan")) return 10;
        if (ComfirmIncludeMsg(msg, "黄緑|ライム|lime")) return 11;
        if (ComfirmIncludeMsg(msg, "栗|マルーン|maroon")) return 12;
        if (ComfirmIncludeMsg(msg, "薔薇|ローズ|rose")) return 13;
        if (ComfirmIncludeMsg(msg, "ﾊﾞﾅﾅ|バナナ|banana")) return 14;
        if (ComfirmIncludeMsg(msg, "灰|グレー|gray")) return 15;
        if (ComfirmIncludeMsg(msg, "ﾀﾝ|タン|tan")) return 16;
        if (ComfirmIncludeMsg(msg, "珊瑚|コーラル|coral")) return 17;
        else return byte.MaxValue;
    }

    private static bool ComfirmIncludeMsg(string msg, string key)
    {
        var keys = key.Split('|');
        for (int i = 0; i < keys.Count(); i++)
        {
            if (msg.Contains(keys[i])) return true;
        }
        return false;
    }

    public static bool NotGueesFlag = false;
    public static bool GuesserMsg(PlayerControl pc, string msg, bool isUI = false)
    {
        var originMsg = msg;

        if (!AmongUsClient.Instance.AmHost) return false;
        if (!GameStates.IsInGame || pc == null) return false;
        if (!pc.Is(CustomRoles.NiceGuesser) && !pc.Is(CustomRoles.EvilGuesser) && !pc.Is(CustomRoles.Leopard) && !pc.Is(CustomRoles.Gambler)) return false;

        int operate = 0; // 1:ID 2:猜测
        msg = msg.ToLower().TrimStart().TrimEnd();
        if (CheckCommond(ref msg, "id|guesslist|g")) operate = 1;
        else if (CheckCommond(ref msg, "shoot|guess|bet|st|gs|bt", false)) operate = 2;
        else return false;

        if (!pc.IsAlive())
        {
            if (!isUI) Utils.SendMessage(GetString("GuessDead"), pc.PlayerId);
            else pc.ShowPopUp(GetString("GuessDead"));
            return true;
        }

        if (operate == 1)
        {
            Utils.SendMessage(GetFormatString(), pc.PlayerId);
            return true;
        }
        else if (operate == 2)
        {
            if (
            (pc.Is(CustomRoles.NiceGuesser) && NiceGuesser.GGTryHideMsg.GetBool()) ||
            (pc.Is(CustomRoles.Leopard) && Leopard.AGTryHideMsg.GetBool()) ||
            (pc.Is(CustomRoles.EvilGuesser) && EvilGuesser.EGTryHideMsg.GetBool()) ||
            (pc.Is(CustomRoles.Gambler) && Gambler.GaTryHideMsg.GetBool())
            )
            {
                new LateTask(() =>
                {
                    TryHideMsg();
                }, 0.1f, "TryGuessHide");
            }
            else if (pc.AmOwner && !isUI) Utils.SendMessage(originMsg, 255, pc.GetRealName());

            if (!MsgToPlayerAndRole(msg, out byte targetId, out CustomRoles role, out string error))
            {
                if (!isUI) Utils.SendMessage(error, pc.PlayerId);
                else pc.ShowPopUp(error);
                return true;
            }

            //ニムロッドなど、指定条件で賭けれないときに呼ばれる。
            if (NotGueesFlag) return true;

            var target = Utils.GetPlayerById(targetId);
            if (target != null)
            {
                bool guesserSuicide = false;
                if (!GuesserGuessed.ContainsKey(pc.PlayerId)) GuesserGuessed.Add(pc.PlayerId, 0);
                if (pc.Is(CustomRoles.NiceGuesser) && GuesserGuessed[pc.PlayerId] >= NiceGuesser.GGCanGuessTime.GetInt())
                {
                    if (!isUI) Utils.SendMessage(GetString("GGGuessMax"), pc.PlayerId);
                    else pc.ShowPopUp(GetString("GGGuessMax"));
                    return true;
                }
                if (pc.Is(CustomRoles.EvilGuesser) && GuesserGuessed[pc.PlayerId] >= EvilGuesser.EGCanGuessTime.GetInt())
                {
                    if (!isUI) Utils.SendMessage(GetString("EGGuessMax"), pc.PlayerId);
                    else pc.ShowPopUp(GetString("EGGuessMax"));
                    return true;
                }
                if (pc.Is(CustomRoles.Leopard) && GuesserGuessed[pc.PlayerId] >= Leopard.AGCanGuessTime.GetInt())
                {
                    if (!isUI) Utils.SendMessage(GetString("EGGuessMax"), pc.PlayerId);
                    else pc.ShowPopUp(GetString("EGGuessMax"));
                    return true;
                }
                if (pc.Is(CustomRoles.Gambler) && GuesserGuessed[pc.PlayerId] >= Gambler.GaCanGuessTime.GetInt())
                {
                    if (!isUI) Utils.SendMessage(GetString("EGGuessMax"), pc.PlayerId);
                    else pc.ShowPopUp(GetString("EGGuessMax"));
                    return true;
                }
                if (role == CustomRoles.GM || target.Is(CustomRoles.GM))
                {
                    Utils.SendMessage(GetString("GuessGM"), pc.PlayerId);
                    return true;
                }
                if (target.Is(CustomRoles.Snitch) && target.AllTasksCompleted() && !EvilGuesser.EGCanGuessTaskDoneSnitch.GetBool())
                {
                    if (!isUI) Utils.SendMessage(GetString("EGGuessSnitchTaskDone"), pc.PlayerId);
                    else pc.ShowPopUp(GetString("EGGuessSnitchTaskDone"));
                    return true;
                }
                if (target.Is(CustomRoles.Snitch) && target.AllTasksCompleted() && !Leopard.AGCanGuessTaskDoneSnitch.GetBool())
                {
                    if (!isUI) Utils.SendMessage(GetString("EGGuessSnitchTaskDone"), pc.PlayerId);
                    else pc.ShowPopUp(GetString("EGGuessSnitchTaskDone"));
                    return true;
                }
                if (target.Is(CustomRoles.Snitch) && target.AllTasksCompleted() && !Gambler.GaCanGuessTaskDoneSnitch.GetBool())
                {
                    if (!isUI) Utils.SendMessage(GetString("EGGuessSnitchTaskDone"), pc.PlayerId);
                    else pc.ShowPopUp(GetString("EGGuessSnitchTaskDone"));
                    return true;
                }
                if (role.IsVanilla())
                {
                    if (
                        (pc.Is(CustomRoles.NiceGuesser) && !NiceGuesser.GGCanGuessVanilla.GetBool()) ||
                        (pc.Is(CustomRoles.Leopard) && !Leopard.AGCanGuessVanilla.GetBool()) ||
                        (pc.Is(CustomRoles.EvilGuesser) && !EvilGuesser.EGCanGuessVanilla.GetBool()) ||
                        (pc.Is(CustomRoles.Gambler) && !Gambler.GaCanGuessVanilla.GetBool())
                        )
                    {
                        Utils.SendMessage(GetString("GuessVanillaRole"), pc.PlayerId);
                        return true;
                    }
                }
                if (role.IsAddOn())
                {
                    return true;
                }
                if (pc.Is(CustomRoles.EvilGuesser) && role == CustomRoles.Egoist)
                {
                    //イビルゲッサーはエゴイストを撃ち抜けない。
                    return true;
                }
                if (pc.PlayerId == target.PlayerId)
                {
                    if (!isUI) Utils.SendMessage(GetString("LaughToWhoGuessSelf"), pc.PlayerId, Utils.ColorString(Color.cyan, GetString("MessageFromKPD")));
                    else pc.ShowPopUp(Utils.ColorString(Color.cyan, GetString("MessageFromKPD")) + "\n" + GetString("LaughToWhoGuessSelf"));
                    guesserSuicide = true;
                }
                else if (pc.Is(CustomRoles.NiceGuesser) && role.IsCrewmate() && !NiceGuesser.GGCanGuessCrew.GetBool() && !pc.Is(CustomRoles.Madmate))
                {
                    guesserSuicide = true;
                }
                else if (pc.Is(CustomRoles.EvilGuesser) &&
                         (role.IsImpostor() && !EvilGuesser.EGCanGuessImp.GetBool()) ||
                         (role.IsWhiteCrew() && !EvilGuesser.EGCantWhiteCrew.GetBool()))
                {
                    guesserSuicide = true;
                }
                else if (pc.Is(CustomRoles.Leopard) &&
                         (role.IsAnimals() && !Leopard.AGCanGuessAnim.GetBool()) ||
                         (role.IsWhiteCrew() && !Leopard.AGCantWhiteCrew.GetBool()))
                {
                    guesserSuicide = true;
                }
                else if (pc.Is(CustomRoles.Gambler) &&
                         (role.IsWhiteCrew() && !Gambler.GaCantWhiteCrew.GetBool()))
                {
                    guesserSuicide = true;
                }
                else if (CheckTargetRoles(target,role))
                {
                    guesserSuicide = true;
                }
                Logger.Info($"{pc.GetNameWithRole()} が {target.GetNameWithRole()} をやったぜ", "Guesser");

                var dp = guesserSuicide ? pc : target;
                var tempDeathReason = CustomDeathReason.Gambled;
                if (NiceGuesser.ChangeGuessDeathReason.GetBool()||
                    EvilGuesser.ChangeGuessDeathReason.GetBool()||
                    Leopard.ChangeGuessDeathReason.GetBool() ||
                    Gambler.ChangeGuessDeathReason.GetBool())
                {
                    tempDeathReason = guesserSuicide ? CustomDeathReason.Misfire : CustomDeathReason.Gambled;
                }

                target = dp;

                Logger.Info($"ゲッサーがやったぜ：{target.GetNameWithRole()} 死亡", "Guesser");

                string Name = dp.GetRealName();

                GuesserGuessed[pc.PlayerId]++;

                BGMSettings.PlaySoundSERPC("Gunfire");

                new LateTask(() =>
                {
                    var playerState = PlayerState.GetByPlayerId(dp.PlayerId);
                    playerState.DeathReason = tempDeathReason;
                    dp.SetRealKiller(pc);
                    RpcGuesserMurderPlayer(dp);

                    //死者检查
                    //Utils.AfterPlayerDeathTasks(dp, true);
                    CustomRoleManager.OnMurderPlayer(pc, target);

                    Utils.NotifyRoles(isForMeeting: true, NoCache: true);

                    new LateTask(() =>
                    {
                        foreach (var pc in Main.AllPlayerControls)
                        {
                            Utils.SendMessage(Name + "がやられた。いい奴だったよ。",pc.PlayerId,Utils.ColorString(Utils.GetRoleColor(CustomRoles.NiceGuesser), GetString("GuessKillTitle")));
                        }
                    }, 0.6f, "Guess Msg");

                }, 0.2f, "Guesser Kill");
            }
        }
        return true;
    }

    private static bool CheckTargetRoles(PlayerControl target,CustomRoles role)
    {
        bool result = !target.Is(role);

        switch (target.GetCustomRole())
        {
            case CustomRoles.NormalEngineer:
                if (role == CustomRoles.Engineer) result = false;
                break;
            case CustomRoles.NormalScientist:
                if (role == CustomRoles.Scientist) result = false;
                break;
            case CustomRoles.NormalImpostor:
                if (role == CustomRoles.Impostor) result = false;
                break;
            case CustomRoles.NormalShapeshifter:
                if (role == CustomRoles.Shapeshifter) result = false;
                break;
        }

        return result;
    }

    public static TextMeshPro nameText(this PlayerControl p) => p.cosmetics.nameText;
    public static TextMeshPro NameText(this PoolablePlayer p) => p.cosmetics.nameText;
    public static void RpcGuesserMurderPlayer(this PlayerControl pc, float delay = 0f) //ゲッサー用の殺し方
    {
        // DEATH STUFF //
        var amOwner = pc.AmOwner;
        pc.Data.IsDead = true;
        pc.RpcExileV2();
        var playerState = PlayerState.GetByPlayerId(pc.PlayerId);
        playerState.SetDead();
        //Main.PlayerStates[pc.PlayerId].SetDead();
        var meetingHud = MeetingHud.Instance;
        var hudManager = DestroyableSingleton<HudManager>.Instance;
        SoundManager.Instance.PlaySound(pc.KillSfx, false, 0.8f);
        hudManager.KillOverlay.ShowKillAnimation(pc.Data, pc.Data);
        if (amOwner)
        {
            hudManager.ShadowQuad.gameObject.SetActive(false);
            pc.nameText().GetComponent<MeshRenderer>().material.SetInt("_Mask", 0);
            pc.RpcSetScanner(false);
            ImportantTextTask importantTextTask = new GameObject("_Player").AddComponent<ImportantTextTask>();
            importantTextTask.transform.SetParent(AmongUsClient.Instance.transform, false);
            meetingHud.SetForegroundForDead();
        }
        PlayerVoteArea voteArea = MeetingHud.Instance.playerStates.First(
            x => x.TargetPlayerId == pc.PlayerId
        );
        if (voteArea == null) return;
        if (voteArea.DidVote) voteArea.UnsetVote();
        voteArea.AmDead = true;
        voteArea.Overlay.gameObject.SetActive(true);
        voteArea.Overlay.color = Color.white;
        voteArea.XMark.gameObject.SetActive(true);
        voteArea.XMark.transform.localScale = Vector3.one;
        foreach (var playerVoteArea in meetingHud.playerStates)
        {
            if (playerVoteArea.VotedFor != pc.PlayerId) continue;
            playerVoteArea.UnsetVote();
            var voteAreaPlayer = Utils.GetPlayerById(playerVoteArea.TargetPlayerId);
            if (!voteAreaPlayer.AmOwner) continue;
            meetingHud.ClearVote();
        }
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.GuessKill, SendOption.Reliable, -1);
        writer.Write(pc.PlayerId);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public static void RpcClientGuess(PlayerControl pc)
    {
        var amOwner = pc.AmOwner;
        var meetingHud = MeetingHud.Instance;
        var hudManager = DestroyableSingleton<HudManager>.Instance;
        SoundManager.Instance.PlaySound(pc.KillSfx, false, 0.8f);
        hudManager.KillOverlay.ShowKillAnimation(pc.Data, pc.Data);
        if (amOwner)
        {
            hudManager.ShadowQuad.gameObject.SetActive(false);
            pc.nameText().GetComponent<MeshRenderer>().material.SetInt("_Mask", 0);
            pc.RpcSetScanner(false);
            ImportantTextTask importantTextTask = new GameObject("_Player").AddComponent<ImportantTextTask>();
            importantTextTask.transform.SetParent(AmongUsClient.Instance.transform, false);
            meetingHud.SetForegroundForDead();
        }
        PlayerVoteArea voteArea = MeetingHud.Instance.playerStates.First(
            x => x.TargetPlayerId == pc.PlayerId
        );
        //pc.Die(DeathReason.Kill);
        if (voteArea == null) return;
        if (voteArea.DidVote) voteArea.UnsetVote();
        voteArea.AmDead = true;
        voteArea.Overlay.gameObject.SetActive(true);
        voteArea.Overlay.color = Color.white;
        voteArea.XMark.gameObject.SetActive(true);
        voteArea.XMark.transform.localScale = Vector3.one;
        foreach (var playerVoteArea in meetingHud.playerStates)
        {
            if (playerVoteArea.VotedFor != pc.PlayerId) continue;
            playerVoteArea.UnsetVote();
            var voteAreaPlayer = Utils.GetPlayerById(playerVoteArea.TargetPlayerId);
            if (!voteAreaPlayer.AmOwner) continue;
            meetingHud.ClearVote();
        }
    }
    private static bool MsgToPlayerAndRole(string msg, out byte id, out CustomRoles role, out string error)
    {
        if (msg.StartsWith("/")) msg = msg.Replace("/", string.Empty);

        Regex r = new("\\d+");
        MatchCollection mc = r.Matches(msg);
        string result = string.Empty;
        for (int i = 0; i < mc.Count; i++)
        {
            result += mc[i];//匹配结果是完整的数字，此处可以不做拼接的
        }

        if (int.TryParse(result, out int num))
        {
            id = Convert.ToByte(num);
        }
        else
        {
            //并不是玩家编号，判断是否颜色
            //byte color = GetColorFromMsg(msg);
            //好吧我不知道怎么取某位玩家的颜色，等会了的时候再来把这里补上
            id = byte.MaxValue;
            error = GetString("GuessHelp");
            role = new();
            return false;
        }

        //判断选择的玩家是否合理
        PlayerControl target = Utils.GetPlayerById(id);
        if (target == null || target.Data.IsDead)
        {
            error = GetString("GuessNull");
            role = new();
            return false;
        }

        if (!ChatCommands.GetRoleByInputName(msg, out role, true))
        {
            error = GetString("GuessHelp");
            return false;
        }

        error = string.Empty;
        return true;
    }

    public static string ChangeNormal2Vanilla(CustomRoles role)
    {
        //ノーマル系役職ならバニラに変える
        switch (role)
        {
            case CustomRoles.NormalEngineer:
                role = CustomRoles.Engineer;
                break;
            case CustomRoles.NormalScientist:
                role = CustomRoles.Scientist;
                break;
            case CustomRoles.NormalImpostor:
                role = CustomRoles.Impostor;
                break;
            case CustomRoles.NormalShapeshifter:
                role = CustomRoles.Shapeshifter;
                break;
        }

        return GetString(Enum.GetName(typeof(CustomRoles), role)).TrimStart('*').ToLower().Trim().Replace(" ", string.Empty).RemoveHtmlTags();
    }

    public static void TryHideMsg()
    {
        ChatUpdatePatch.DoBlockChat = true;
        List<CustomRoles> roles = Enum.GetValues(typeof(CustomRoles)).Cast<CustomRoles>().Where(x => x is not CustomRoles.NotAssigned).ToList();
        var rd = IRandom.Instance;
        string msg;
        string[] command = new string[] {"bt"};
        for (int i = 0; i < 20; i++)
        {
            msg = "/";
            if (rd.Next(1, 100) < 20)
            {
                msg += "id";
            }
            else
            {
                msg += command[rd.Next(0, command.Length - 1)];
                msg += rd.Next(1, 100) < 50 ? string.Empty : " ";
                msg += rd.Next(0, 15).ToString();
                msg += rd.Next(1, 100) < 50 ? string.Empty : " ";
                CustomRoles role = roles[rd.Next(0, roles.Count())];
                msg += rd.Next(1, 100) < 50 ? string.Empty : " ";
                msg += Utils.GetRoleName(role);
            }
            var player = Main.AllAlivePlayerControls.ToArray()[rd.Next(0, Main.AllAlivePlayerControls.Count())];
            DestroyableSingleton<HudManager>.Instance.Chat.AddChat(player, msg);
            var writer = CustomRpcSender.Create("MessagesToSend", SendOption.None);
            writer.StartMessage(-1);
            writer.StartRpc(player.NetId, (byte)RpcCalls.SendChat)
                .Write(msg)
                .EndRpc();
            writer.EndMessage();
            writer.SendMessage();
        }
        ChatUpdatePatch.DoBlockChat = false;
    }

    private static void SendRPC(int playerId, CustomRoles role)
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.Guess, SendOption.Reliable, -1);
        writer.Write(playerId);
        writer.Write((byte)role);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public static void ReceiveRPC(MessageReader reader, PlayerControl pc)
    {
        int PlayerId = reader.ReadInt32();
        CustomRoles role = (CustomRoles)reader.ReadByte();
        GuesserMsg(pc, $"/bt {PlayerId} {GetString(role.ToString())}", true);
    }
}
