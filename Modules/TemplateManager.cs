using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using HarmonyLib;
using static TownOfHost.Translator;

namespace TownOfHost
{
    public static class TemplateManager
    {
        private static readonly string TEMPLATE_FILE_PATH = "./TOH_DATA/template.txt";
        private static Dictionary<string, Func<string>> _replaceDictionary = new()
        {
            ["RoomCode"] = () => InnerNet.GameCode.IntToGameName(AmongUsClient.Instance.GameId),
            ["PlayerName"] = () => PlayerControl.LocalPlayer.Data.PlayerName,
            ["AmongUsVersion"] = () => UnityEngine.Application.version,
            ["ModVersion"] = () => Main.PluginVersion,
            ["Map"] = () => Constants.MapNames[PlayerControl.GameOptions.MapId],
            ["NumEmergencyMeetings"] = () => PlayerControl.GameOptions.NumEmergencyMeetings.ToString(),
            ["EmergencyCooldown"] = () => PlayerControl.GameOptions.EmergencyCooldown.ToString(),
            ["DiscussionTime"] = () => PlayerControl.GameOptions.DiscussionTime.ToString(),
            ["VotingTime"] = () => PlayerControl.GameOptions.VotingTime.ToString(),
            ["PlayerSpeedMod"] = () => PlayerControl.GameOptions.PlayerSpeedMod.ToString(),
            ["CrewLightMod"] = () => PlayerControl.GameOptions.CrewLightMod.ToString(),
            ["ImpostorLightMod"] = () => PlayerControl.GameOptions.ImpostorLightMod.ToString(),
            ["KillCooldown"] = () => PlayerControl.GameOptions.KillCooldown.ToString(),
            ["NumCommonTasks"] = () => PlayerControl.GameOptions.NumCommonTasks.ToString(),
            ["NumLongTasks"] = () => PlayerControl.GameOptions.NumLongTasks.ToString(),
            ["NumShortTasks"] = () => PlayerControl.GameOptions.NumShortTasks.ToString(),
            ["Date"] = () => DateTime.Now.ToShortDateString(),
            ["Time"] = () => DateTime.Now.ToShortTimeString(),
        };

        public static void Init()
        {
            CreateIfNotExists();
        }

        public static void CreateIfNotExists()
        {
            if (!File.Exists(TEMPLATE_FILE_PATH))
            {
                try
                {
                    if (!Directory.Exists(@"TOH_DATA")) Directory.CreateDirectory(@"TOH_DATA");
                    if (File.Exists(@"./template.txt"))
                    {
                        File.Move(@"./template.txt", TEMPLATE_FILE_PATH);
                    }
                    else
                    {
                        Logger.Info("Among Us.exeと同じフォルダにtemplate.txtが見つかりませんでした。新規作成します。", "TemplateManager");
                        File.WriteAllText(TEMPLATE_FILE_PATH, "test:This is template text.\\nLine breaks are also possible.\ntest:これは定型文です。\\n改行も可能です。");
                    }
                }
                catch (Exception ex)
                {
                    Logger.Exception(ex, "TemplateManager");
                }
            }
        }

        public static void SendTemplate(string str = "", byte playerId = 0xff, bool noErr = false)
        {
            CreateIfNotExists();
            using StreamReader sr = new(TEMPLATE_FILE_PATH, Encoding.GetEncoding("UTF-8"));
            string text;
            string[] tmp = { };
            List<string> sendList = new();
            HashSet<string> tags = new();
            while ((text = sr.ReadLine()) != null)
            {
                tmp = text.Split(":");
                if (tmp.Length > 1 && tmp[1] != "")
                {
                    tags.Add(tmp[0]);
                    if (tmp[0] == str) sendList.Add(tmp.Skip(1).Join(delimiter: ":").Replace("\\n", "\n"));
                }
            }
            if (sendList.Count == 0 && !noErr)
            {
                if (playerId == 0xff)
                    HudManager.Instance.Chat.AddChat(PlayerControl.LocalPlayer, string.Format(GetString("Message.TemplateNotFoundHost"), str, tags.Join(delimiter: ", ")));
                else Utils.SendMessage(string.Format(GetString("Message.TemplateNotFoundClient"), str), playerId);
            }
            else for (int i = 0; i < sendList.Count; i++) Utils.SendMessage(ApplyReplaceDictionary(sendList[i]), playerId);
        }

        private static string ApplyReplaceDictionary(string text)
        {
            foreach (var kvp in _replaceDictionary)
            {
                text = Regex.Replace(text, "{{" + kvp.Key + "}}", kvp.Value.Invoke() ?? "", RegexOptions.IgnoreCase);
            }
            return text;
        }
    }
}