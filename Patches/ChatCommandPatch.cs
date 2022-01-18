using BepInEx;
using BepInEx.Configuration;
using BepInEx.IL2CPP;
using Hazel;
using System;
using HarmonyLib;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnhollowerBaseLib;
using TownOfHost;
using System.Threading.Tasks;
using System.Threading;

namespace TownOfHost
{
    [HarmonyPatch(typeof(ChatController), nameof(ChatController.SendChat))]
    class ChatCommands
    {
        public static bool Prefix(ChatController __instance)
        {
            var text = __instance.TextArea.text;
            string arg;
            var canceled = false;
            var cancelVal = "";
            if (getCommand("/list", text, out arg))
            {
                canceled = true;
                __instance.AddChat(PlayerControl.LocalPlayer,
$@"{main.getLang(lang.roleListStart)}
{main.getLang(lang.Jester)}: {getOnOff(main.currentScientist == ScientistRole.Jester)}
{main.getLang(lang.Madmate)}: {getOnOff(main.currentEngineer == EngineerRole.Madmate)}
{main.getLang(lang.Bait)}: {getOnOff(main.currentScientist == ScientistRole.Bait)}
{main.getLang(lang.Terrorist)}: {getOnOff(main.currentEngineer == EngineerRole.Terrorist)}"
                );
            }
            if (AmongUsClient.Instance.AmHost)
            {
                if (getCommand("/winner", text, out arg))
                {
                    canceled = true;
                    main.SendToAll(main.winnerList);
                }
                if (getCommand("/h roles", text, out arg))
                {
                    canceled = true;
                    if (arg == "jester")
                    {
                        main.SendToAll("Jester(Scientist):投票で追放されたときに単独勝利となる第三陣営の役職。追放されずにゲームが終了するか、キルされると敗北となる。");
                    }
                    else if (arg == "madmate")
                    {
                        main.SendToAll("Madmate(Engineer):インポスター陣営に属するが、Madmateからはインポスターが誰なのかはわからない。インポスターからもMadmateが誰なのかはわからない。キルやサボタージュはできないが、ベントに入ることができる。");
                    }
                    else if (arg == "bait")
                    {
                        main.SendToAll("Bait(Scientist):キルされたときに、自分をキルした人に強制的に自分の死体を通報させることができる。");
                    }
                    else if (arg == "terrorist")
                    {
                        main.SendToAll("Terrorist(Engineer):自身のタスクを全て完了させた状態で死亡したときに単独勝利となる第三陣営の役職。死因はキルと追放のどちらでもよい。タスクを完了させずに死亡したり、死亡しないまま試合が終了すると敗北する。");
                    }
                    else if (arg == "sidekick")
                    {
                        main.SendToAll("Sidekick(Shapeshifter):初期状態でベントやサボタージュ、変身は可能だが、キルはできない。Sidekickではないインポスターが全員死亡すると、Sidekickもキルが可能となる。");
                    }
                    else if (arg == "vampire")
                    {
                        main.SendToAll("Vampire(Impostor):キルボタンを押してから10秒経って実際にキルが発生する役職。キルをしたときのテレポートは発生しない。また、キルボタンを押してから10秒経つまでに会議が始まるとその瞬間にキルが発生する。");
                    }
                    else if (arg == "fox")
                    {
                        main.SendToAll("Fox(Hide And Seek):Troll陣営を除くいずれかの陣営が勝利したときに生き残っていれば追加勝利となる。");
                    }
                    else if (arg == "troll")
                    {
                        main.SendToAll("Troll(Hide And Seek):インポスターにキルされたときに単独勝利となる。この場合、Foxが生き残っていてもFoxは追加勝利することができない。");
                    }
                    else
                    {
                        main.SendToAll("Error:入力された役職は存在しません。");
                    }
                }
                if (getCommand("/h modes", text, out arg))
                {
                    canceled = true;
                    if (arg == "hideandseek")
                    {
                        main.SendToAll("HideAndSeek:会議を開くことはできず、クルーはタスク完了、インポスターは全クルー殺害でのみ勝利することができる。サボタージュ、アドミン、カメラ、待ち伏せなどは禁止事項である。");
                    }
                    else if (arg == "nogameend")
                    {
                        main.SendToAll("HideAndSeek:勝利判定が存在しないデバッグ用のモード。ホストのSHIFT+L以外でのゲーム終了ができない。");
                    }
                    else
                    {
                        main.SendToAll("Error:入力されたモードは存在しません。");
                    }
                }
                if (getCommand("/endgame", text, out arg))
                {
                    canceled = true;
                    MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.EndGame, Hazel.SendOption.Reliable, -1);
                    AmongUsClient.Instance.FinishRpcImmediately(writer);
                    RPCProcedure.EndGame();
                }
                if (getCommand("/dis", text, out arg))
                {
                    canceled = true;
                    if (arg == "crewmate")
                    {
                        ShipStatus.Instance.enabled = false;
                        ShipStatus.RpcEndGame(GameOverReason.HumansDisconnect, false);
                    }
                    else
                    if (arg == "impostor")
                    {
                        ShipStatus.Instance.enabled = false;
                        ShipStatus.RpcEndGame(GameOverReason.ImpostorDisconnect, false);
                    }
                    else
                    {
                        __instance.AddChat(PlayerControl.LocalPlayer, "crewmate | impostor");
                        cancelVal = "/dis";
                    }
                    ShipStatus.Instance.RpcRepairSystem(SystemTypes.Admin, 0);
                }
            }
            if (canceled)
            {
                __instance.TextArea.Clear();
                __instance.TextArea.SetText(cancelVal);
                __instance.quickChatMenu.ResetGlyphs();
            }
            return !canceled;
        }
        public static bool getCommand(string command, string text, out string arg)
        {
            arg = "";
            var isValid = text.StartsWith(command + " ");
            if (isValid)
                arg = text.Substring(command.Length + 1);
            if (text == command) isValid = true;
            return isValid;
        }
        public static string CommandReturn(lang prefixID, lang textID)
        {
            var text = "";
            text = main.getLang(prefixID);
            return text.Replace("%1$", main.getLang(textID));
        }
        public static string getOnOff(bool value)
        {
            if (value) return main.getLang(lang.ON);
            else return main.getLang(lang.OFF);
        }
    }
    [HarmonyPatch(typeof(ChatController), nameof(ChatController.AddChat))]
    class AddChatPatch
    {
        public static void Postfix(ChatController __instance, [HarmonyArgument(1)] string chatText)
        {
            Logger.SendToFile(__instance.name + ":" + chatText, LogLevel.Message);
            if (chatText == "/winner" && AmongUsClient.Instance.AmHost && main.IgnoreWinnerCommand.Value == false)
            {
                main.SendToAll(main.winnerList);
            }
        }
    }
    [HarmonyPatch(typeof(ChatController), nameof(ChatController.Update))]
    class ChatUpdatePatch
    {
        public static void Postfix(ChatController __instance)
        {
            if (!AmongUsClient.Instance.AmHost) return;
            float num = 3f - __instance.TimeSinceLastMessage;
            if (main.MessagesToSend.Count > 0 && num <= 0.0f)
            {
                string msg = main.MessagesToSend[0];
                main.MessagesToSend.RemoveAt(0);
                __instance.TimeSinceLastMessage = 0.0f;
                PlayerControl.LocalPlayer.RpcSendChat(msg);
            }
        }
    }
}
