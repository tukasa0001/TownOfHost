using AmongUs.Data;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using static TOHE.Translator;

namespace TOHE;

public static class TemplateManager
{
    private static readonly string TEMPLATE_FILE_PATH = @"./TOHE_DATA/template.txt";
    private static Dictionary<string, Func<string>> _replaceDictionary = new()
    {
        ["RoomCode"] = () => InnerNet.GameCode.IntToGameName(AmongUsClient.Instance.GameId),
        ["PlayerName"] = () => DataManager.Player.Customization.Name,
        ["AmongUsVersion"] = () => UnityEngine.Application.version,
        ["ModVersion"] = () => Main.PluginVersion,
        ["Map"] = () => Constants.MapNames[Main.NormalOptions.MapId],
        ["NumEmergencyMeetings"] = () => Main.NormalOptions.NumEmergencyMeetings.ToString(),
        ["EmergencyCooldown"] = () => Main.NormalOptions.EmergencyCooldown.ToString(),
        ["DiscussionTime"] = () => Main.NormalOptions.DiscussionTime.ToString(),
        ["VotingTime"] = () => Main.NormalOptions.VotingTime.ToString(),
        ["PlayerSpeedMod"] = () => Main.NormalOptions.PlayerSpeedMod.ToString(),
        ["CrewLightMod"] = () => Main.NormalOptions.CrewLightMod.ToString(),
        ["ImpostorLightMod"] = () => Main.NormalOptions.ImpostorLightMod.ToString(),
        ["KillCooldown"] = () => Main.NormalOptions.KillCooldown.ToString(),
        ["NumCommonTasks"] = () => Main.NormalOptions.NumCommonTasks.ToString(),
        ["NumLongTasks"] = () => Main.NormalOptions.NumLongTasks.ToString(),
        ["NumShortTasks"] = () => Main.NormalOptions.NumShortTasks.ToString(),
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
                if (!Directory.Exists("TOHE_DATA")) Directory.CreateDirectory("TOHE_DATA");
                if (File.Exists(@"./template.txt"))
                {
                    File.Move(@"./template.txt", TEMPLATE_FILE_PATH);
                }
                else
                {
                    Logger.Warn("创建新的 Template 文件", "TemplateManager");
                    File.WriteAllText(TEMPLATE_FILE_PATH, GetResourcesTxt("TOHE.Resources.template.txt"));
                }
            }
            catch (Exception ex)
            {
                Logger.Exception(ex, "TemplateManager");
            }
        }
    }
    private static string GetResourcesTxt(string path)
    {
        var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(path);
        stream.Position = 0;
        using StreamReader reader = new(stream, Encoding.UTF8);
        return reader.ReadToEnd();
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
                if (tmp[0].ToLower() == str.ToLower()) sendList.Add(tmp.Skip(1).Join(delimiter: ":").Replace("\\n", "\n"));
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