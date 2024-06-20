using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Assets.CoreScripts;
using HarmonyLib;
using Hazel;

using TownOfHost.Roles.Core;
using static TownOfHost.Translator;

namespace TownOfHost
{
    [HarmonyPatch(typeof(ChatController), nameof(ChatController.SendChat))]
    class ChatCommands
    {
        public static List<string> ChatHistory = new();
        private static Dictionary<CustomRoles, string> roleCommands;

        public static bool Prefix(ChatController __instance)
        {
            // クイックチャットなら横流し
            if (__instance.quickChatField.Visible)
            {
                return true;
            }
            // 入力欄に何も書かれてなければブロック
            if (__instance.freeChatField.textArea.text == "")
            {
                return false;
            }
            __instance.timeSinceLastMessage = 3f;
            var text = __instance.freeChatField.textArea.text;
            if (ChatHistory.Count == 0 || ChatHistory[^1] != text) ChatHistory.Add(text);
            ChatControllerUpdatePatch.CurrentHistorySelection = ChatHistory.Count;
            string[] args = text.Split(' ');
            string subArgs = "";
            var canceled = false;
            var cancelVal = "";
            Main.isChatCommand = true;
            Logger.Info(text, "SendChat");
            switch (args[0])
            {
                case "/dump":
                    canceled = true;
                    Utils.DumpLog();
                    break;
                case "/v":
                case "/version":
                    canceled = true;
                    string version_text = "";
                    foreach (var kvp in Main.playerVersion.OrderBy(pair => pair.Key))
                    {
                        version_text += $"{kvp.Key}:{Utils.GetPlayerById(kvp.Key)?.Data?.PlayerName}:{kvp.Value.forkId}/{kvp.Value.version}({kvp.Value.tag})\n";
                    }
                    if (version_text != "") HudManager.Instance.Chat.AddChat(PlayerControl.LocalPlayer, version_text);
                    break;
                default:
                    Main.isChatCommand = false;
                    break;
            }
            if (AmongUsClient.Instance.AmHost)
            {
                Main.isChatCommand = true;
                switch (args[0])
                {
                    case "/win":
                    case "/winner":
                        canceled = true;
                        Utils.SendMessage("Winner: " + string.Join(",", Main.winnerList.Select(b => Main.AllPlayerNames[b])));
                        break;

                    case "/l":
                    case "/lastresult":
                        canceled = true;
                        Utils.ShowLastResult();
                        break;

                    case "/kl":
                    case "/killlog":
                        canceled = true;
                        Utils.ShowKillLog();
                        break;

                    case "/r":
                    case "/rename":
                        canceled = true;
                        Main.nickName = args.Length > 1 ? Main.nickName = args[1] : "";
                        break;

                    case "/hn":
                    case "/hidename":
                        canceled = true;
                        Main.HideName.Value = args.Length > 1 ? args.Skip(1).Join(delimiter: " ") : Main.HideName.DefaultValue.ToString();
                        GameStartManagerPatch.HideName.text = Main.HideName.Value;
                        break;

                    case "/n":
                    case "/now":
                        canceled = true;
                        subArgs = args.Length < 2 ? "" : args[1];
                        switch (subArgs)
                        {
                            case "r":
                            case "roles":
                                Utils.ShowActiveRoles();
                                break;
                            default:
                                Utils.ShowActiveSettings();
                                break;
                        }
                        break;

                    case "/dis":
                        canceled = true;
                        subArgs = args.Length < 2 ? "" : args[1];
                        switch (subArgs)
                        {
                            case "crewmate":
                                GameManager.Instance.enabled = false;
                                GameManager.Instance.RpcEndGame(GameOverReason.HumansDisconnect, false);
                                break;

                            case "impostor":
                                GameManager.Instance.enabled = false;
                                GameManager.Instance.RpcEndGame(GameOverReason.ImpostorDisconnect, false);
                                break;

                            default:
                                __instance.AddChat(PlayerControl.LocalPlayer, "crewmate | impostor");
                                cancelVal = "/dis";
                                break;
                        }
                        break;

                    case "/h":
                    case "/help":
                        canceled = true;
                        subArgs = args.Length < 2 ? "" : args[1];
                        switch (subArgs)
                        {
                            case "r":
                            case "roles":
                                subArgs = args.Length < 3 ? "" : args[2];
                                GetRolesInfo(subArgs);
                                break;

                            case "a":
                            case "addons":
                                subArgs = args.Length < 3 ? "" : args[2];
                                switch (subArgs)
                                {
                                    case "lastimpostor":
                                    case "limp":
                                        Utils.SendMessage(Utils.GetRoleName(CustomRoles.LastImpostor) + GetString("LastImpostorInfoLong"));
                                        break;

                                    default:
                                        Utils.SendMessage($"{GetString("Command.h_args")}:\n lastimpostor(limp)");
                                        break;
                                }
                                break;

                            case "m":
                            case "modes":
                                subArgs = args.Length < 3 ? "" : args[2];
                                switch (subArgs)
                                {
                                    case "hideandseek":
                                    case "has":
                                        Utils.SendMessage(GetString("HideAndSeekInfo"));
                                        break;

                                    case "nogameend":
                                    case "nge":
                                        Utils.SendMessage(GetString("NoGameEndInfo"));
                                        break;

                                    case "syncbuttonmode":
                                    case "sbm":
                                        Utils.SendMessage(GetString("SyncButtonModeInfo"));
                                        break;

                                    case "randommapsmode":
                                    case "rmm":
                                        Utils.SendMessage(GetString("RandomMapsModeInfo"));
                                        break;

                                    default:
                                        Utils.SendMessage($"{GetString("Command.h_args")}:\n hideandseek(has), nogameend(nge), syncbuttonmode(sbm), randommapsmode(rmm)");
                                        break;
                                }
                                break;

                            case "n":
                            case "now":
                                Utils.ShowActiveSettingsHelp();
                                break;

                            default:
                                Utils.ShowHelp();
                                break;
                        }
                        break;

                    case "/m":
                    case "/myrole":
                        canceled = true;
                        if (GameStates.IsInGame)
                        {
                            var role = PlayerControl.LocalPlayer.GetCustomRole();
                            HudManager.Instance.Chat.AddChat(
                                PlayerControl.LocalPlayer,
                                role.GetRoleInfo()?.Description?.FullFormatHelp ??
                                // roleInfoがない役職
                                GetString(role.ToString()) + PlayerControl.LocalPlayer.GetRoleInfo(true));
                        }
                        break;

                    case "/t":
                    case "/template":
                        canceled = true;
                        if (args.Length > 1) TemplateManager.SendTemplate(args[1]);
                        else HudManager.Instance.Chat.AddChat(PlayerControl.LocalPlayer, $"{GetString("ForExample")}:\n{args[0]} test");
                        break;

                    case "/mw":
                    case "/messagewait":
                        canceled = true;
                        if (args.Length > 1 && int.TryParse(args[1], out int sec))
                        {
                            Main.MessageWait.Value = sec;
                            Utils.SendMessage(string.Format(GetString("Message.SetToSeconds"), sec), 0);
                        }
                        else Utils.SendMessage($"{GetString("Message.MessageWaitHelp")}\n{GetString("ForExample")}:\n{args[0]} 3", 0);
                        break;

                    case "/say":
                        canceled = true;
                        if (args.Length > 1)
                            Utils.SendMessage(args.Skip(1).Join(delimiter: " "), title: $"<color=#ff0000>{GetString("MessageFromTheHost")}</color>");
                        break;

                    case "/exile":
                        canceled = true;
                        if (args.Length < 2 || !int.TryParse(args[1], out int id)) break;
                        Utils.GetPlayerById(id)?.RpcExileV2();
                        break;

                    case "/kill":
                        canceled = true;
                        if (args.Length < 2 || !int.TryParse(args[1], out int id2)) break;
                        Utils.GetPlayerById(id2)?.RpcMurderPlayer(Utils.GetPlayerById(id2));
                        break;

                    default:
                        Main.isChatCommand = false;
                        break;
                }
            }
            if (canceled)
            {
                Logger.Info("Command Canceled", "ChatCommand");
                __instance.freeChatField.textArea.Clear();
                __instance.freeChatField.textArea.SetText(cancelVal);
            }
            return !canceled;
        }

        public static void GetRolesInfo(string role)
        {
            // 初回のみ処理
            if (roleCommands == null)
            {
#pragma warning disable IDE0028  // Dictionary初期化の簡素化をしない
                roleCommands = new Dictionary<CustomRoles, string>();

                // GM
                roleCommands.Add(CustomRoles.GM, "gm");

                // Impostor役職
                roleCommands.Add((CustomRoles)(-1), $"== {GetString("Impostor")} ==");  // 区切り用
                ConcatCommands(CustomRoleTypes.Impostor);

                // Madmate役職
                roleCommands.Add((CustomRoles)(-2), $"== {GetString("Madmate")} ==");  // 区切り用
                ConcatCommands(CustomRoleTypes.Madmate);
                roleCommands.Add(CustomRoles.SKMadmate, "sm");

                // Crewmate役職
                roleCommands.Add((CustomRoles)(-3), $"== {GetString("Crewmate")} ==");  // 区切り用
                ConcatCommands(CustomRoleTypes.Crewmate);

                // Neutral役職
                roleCommands.Add((CustomRoles)(-4), $"== {GetString("Neutral")} ==");  // 区切り用
                ConcatCommands(CustomRoleTypes.Neutral);

                // 属性
                roleCommands.Add((CustomRoles)(-5), $"== {GetString("Addons")} ==");  // 区切り用
                roleCommands.Add(CustomRoles.Lovers, "lo");
                roleCommands.Add(CustomRoles.Watcher, "wat");
                roleCommands.Add(CustomRoles.Workhorse, "wh");

                // HAS
                roleCommands.Add((CustomRoles)(-6), $"== {GetString("HideAndSeek")} ==");  // 区切り用
                roleCommands.Add(CustomRoles.HASFox, "hfo");
                roleCommands.Add(CustomRoles.HASTroll, "htr");
#pragma warning restore IDE0028
            }

            var msg = "";
            var rolemsg = $"{GetString("Command.h_args")}";
            foreach (var r in roleCommands)
            {
                var roleName = r.Key.ToString();
                var roleShort = r.Value;

                if (String.Compare(role, roleName, true) == 0 || String.Compare(role, roleShort, true) == 0)
                {
                    var roleInfo = r.Key.GetRoleInfo();
                    if (roleInfo != null && roleInfo.Description != null)
                    {
                        Utils.SendMessage(roleInfo.Description.FullFormatHelp, removeTags: false);
                    }
                    // RoleInfoがない役職は従来の処理
                    else
                    {
                        Utils.SendMessage(GetString(roleName) + GetString($"{roleName}InfoLong"));
                    }
                    return;
                }

                var roleText = $"{roleName.ToLower()}({roleShort.ToLower()}), ";
                if ((int)r.Key < 0)
                {
                    msg += rolemsg + "\n" + roleShort + "\n";
                    rolemsg = "";
                }
                else if ((rolemsg.Length + roleText.Length) > 40)
                {
                    msg += rolemsg + "\n";
                    rolemsg = roleText;
                }
                else
                {
                    rolemsg += roleText;
                }
            }
            msg += rolemsg;
            Utils.SendMessage(msg);
        }
        private static void ConcatCommands(CustomRoleTypes roleType)
        {
            var roles = CustomRoleManager.AllRolesInfo.Values.Where(role => role.CustomRoleType == roleType);
            foreach (var role in roles)
            {
                if (role.ChatCommand is null) continue;

                roleCommands[role.RoleName] = role.ChatCommand;
            }
        }
        public static void OnReceiveChat(PlayerControl player, string text)
        {
            if (!AmongUsClient.Instance.AmHost) return;
            string[] args = text.Split(' ');
            string subArgs = "";
            switch (args[0])
            {
                case "/l":
                case "/lastresult":
                    Utils.ShowLastResult(player.PlayerId);
                    break;

                case "/kl":
                case "/killlog":
                    Utils.ShowKillLog(player.PlayerId);
                    break;

                case "/n":
                case "/now":
                    subArgs = args.Length < 2 ? "" : args[1];
                    switch (subArgs)
                    {
                        case "r":
                        case "roles":
                            Utils.ShowActiveRoles(player.PlayerId);
                            break;

                        default:
                            Utils.ShowActiveSettings(player.PlayerId);
                            break;
                    }
                    break;

                case "/h":
                case "/help":
                    subArgs = args.Length < 2 ? "" : args[1];
                    switch (subArgs)
                    {
                        case "n":
                        case "now":
                            Utils.ShowActiveSettingsHelp(player.PlayerId);
                            break;
                    }
                    break;

                case "/m":
                case "/myrole":
                    if (GameStates.IsInGame)
                    {
                        var role = player.GetCustomRole();
                        if (role.GetRoleInfo()?.Description is { } description)
                        {
                            Utils.SendMessage(description.FullFormatHelp, player.PlayerId, removeTags: false);
                        }
                        // roleInfoがない役職
                        else
                        {
                            Utils.SendMessage(GetString(role.ToString()) + player.GetRoleInfo(true), player.PlayerId);
                        }
                    }
                    break;

                case "/t":
                case "/template":
                    if (args.Length > 1) TemplateManager.SendTemplate(args[1], player.PlayerId);
                    else Utils.SendMessage($"{GetString("ForExample")}:\n{args[0]} test", player.PlayerId);
                    break;

                default:
                    break;
            }
        }
    }
    [HarmonyPatch(typeof(ChatController), nameof(ChatController.Update))]
    class ChatUpdatePatch
    {
        public static bool DoBlockChat = false;
        public static void Postfix(ChatController __instance)
        {
            if (!AmongUsClient.Instance.AmHost || Main.MessagesToSend.Count < 1 || (Main.MessagesToSend[0].Item2 == byte.MaxValue && Main.MessageWait.Value > __instance.timeSinceLastMessage)) return;
            if (DoBlockChat) return;
            var player = Main.AllAlivePlayerControls.OrderBy(x => x.PlayerId).FirstOrDefault();
            if (player == null) return;
            (string msg, byte sendTo, string title) = Main.MessagesToSend[0];
            Main.MessagesToSend.RemoveAt(0);
            int clientId = sendTo == byte.MaxValue ? -1 : Utils.GetPlayerById(sendTo).GetClientId();
            var name = player.Data.PlayerName;
            if (clientId == -1)
            {
                player.SetName(title);
                DestroyableSingleton<HudManager>.Instance.Chat.AddChat(player, msg);
                player.SetName(name);
            }
            var writer = CustomRpcSender.Create("MessagesToSend", SendOption.None);
            writer.StartMessage(clientId);
            writer.StartRpc(player.NetId, (byte)RpcCalls.SetName)
                .Write(player.Data.NetId)
                .Write(title)
                .EndRpc();
            writer.StartRpc(player.NetId, (byte)RpcCalls.SendChat)
                .Write(msg)
                .EndRpc();
            writer.StartRpc(player.NetId, (byte)RpcCalls.SetName)
                .Write(player.Data.NetId)
                .Write(player.Data.PlayerName)
                .EndRpc();
            writer.EndMessage();
            writer.SendMessage();
            __instance.timeSinceLastMessage = 0f;
        }
    }

    [HarmonyPatch(typeof(ChatController), nameof(ChatController.AddChat))]
    class AddChatPatch
    {
        public static void Postfix(string chatText)
        {
            switch (chatText)
            {
                default:
                    break;
            }
            if (!AmongUsClient.Instance.AmHost) return;
        }
    }
    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.RpcSendChat))]
    class RpcSendChatPatch
    {
        public static bool Prefix(PlayerControl __instance, string chatText, ref bool __result)
        {
            if (string.IsNullOrWhiteSpace(chatText))
            {
                __result = false;
                return false;
            }
            int return_count = PlayerControl.LocalPlayer.name.Count(x => x == '\n');
            chatText = new StringBuilder(chatText).Insert(0, "\n", return_count).ToString();
            if (AmongUsClient.Instance.AmClient && DestroyableSingleton<HudManager>.Instance)
                DestroyableSingleton<HudManager>.Instance.Chat.AddChat(__instance, chatText);
            if (chatText.Contains("who", StringComparison.OrdinalIgnoreCase))
                DestroyableSingleton<UnityTelemetry>.Instance.SendWho();
            MessageWriter messageWriter = AmongUsClient.Instance.StartRpc(__instance.NetId, (byte)RpcCalls.SendChat, SendOption.None);
            messageWriter.Write(chatText);
            messageWriter.EndMessage();
            __result = true;
            return false;
        }
    }
}