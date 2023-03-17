using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using AmongUs.Data;
using HarmonyLib;
using InnerNet;
using TOHTOR.API;
using TOHTOR.Extensions;
using VentLib.Options;
using VentLib.Utilities.Extensions;

namespace TOHTOR.Managers;

public class TemplateManager
{
    internal List<Template> Templates;
    private Dictionary<string, (string, int)> taggedTemplates = new();
    private readonly FileInfo templateFile;
    private readonly Regex regex = new("(?:\\$|@|%)((?:[A-Za-z0-9]|\\.\\S)*)");
    private readonly Regex tagRegex = new("^\\[[^]]*]");

    public TemplateManager(FileInfo templateFile)
    {
        this.templateFile = templateFile;
        Reload();
    }

    public void Reload()
    {
        string[] content = this.templateFile.ReadAll(true).Split("\n");
        Templates = content.Select((s, i) =>
        {
            var match = tagRegex.Match(s);
            if (match == null) throw new FormatException($"Error parsing template file on line {i + 1}: {s}");
            try
            {
                string extracted = match.Value.TrimStart('[').TrimEnd(']');
                string[] tagInfo = extracted.Split(", ");
                return new Template(tagInfo.Length > 1 ? tagInfo[1] : null, s[(match.Length + 1)..]);
            }
            catch
            {
                return default!;
            }
        }).Where(t => t != null).ToList();
        taggedTemplates = Templates.ToDict((tuple, _) => tuple.Tag ?? "NONE", (tuple, i) => (text: tuple.Text, i));
    }

    public bool SetTemplate(int index, string tag)
    {
        if (index >= Templates.Count) return false;
        (string _, int eIndex) = taggedTemplates.GetValueOrDefault(tag, ("", -1));
        if (eIndex != -1)
            Templates[eIndex].Text = "NONE";
        var template = Templates[index];
        template.Tag = tag;
        taggedTemplates[tag] = (template.Text, index);
        WriteFile();
        return true;
    }

    public bool TryFormat(PlayerControl player, string tag, out string formatted)
    {
        formatted = "";
        if (!taggedTemplates.ContainsKey(tag)) return false;
        string template = taggedTemplates[tag].Item1.Replace("\\n", "\n");
        formatted = regex.Replace(template, match =>
            {
                var split = match.Value.Split(".");
                if (split.Length > 1)
                    return VariableValues.TryGetValue(split[0], out var dynSupplier)
                        ? dynSupplier(player, split[1])
                        : match.Value;
                return TemplateValues.TryGetValue(match.Value, out var funcSupplier)
                    ? funcSupplier(player)
                    : match.Value;
            }
        );
        return true;
    }

    private void WriteFile()
    {
        string content = Templates.Select((template, i) =>
        {
            string tag = template.Tag != null ? ", " + template.Tag : "";
            return $"[{i + 1}{tag}] {template.Text}";
        }).Join(delimiter: "\n");
        File.WriteAllText(this.templateFile.FullName, content);
    }

    private static readonly Dictionary<string, Func<PlayerControl, String>> TemplateValues = new()
    {
        { "$RoomCode", _ => GameCode.IntToGameName(AmongUsClient.Instance.GameId) },
        { "$Host", _ => DataManager.Player.Customization.name },
        { "$AUVersion", _ => UnityEngine.Application.version },
        { "$ModVersion", _ => TOHPlugin.PluginVersion + (TOHPlugin.DevVersion ? " " + TOHPlugin.DevVersionStr : "") },
        { "$Map", _ => Constants.MapNames[GameOptionsManager.Instance.CurrentGameOptions.MapId] },
        { "$Gamemode", _ => Game.CurrentGamemode.GetName() },
        { "$Date", _ => DateTime.Now.ToShortDateString() },
        { "$Time", _ => DateTime.Now.ToShortTimeString() },
        { "$Players", _ => PlayerControl.AllPlayerControls.ToArray().Select(p => p.GetRawName()).StrJoin() },
        { "$PlayerCount", _ => PlayerControl.AllPlayerControls.Count.ToString() },
        { "@Name", player => player.GetRawName() },
        { "@Color", player => ModConstants.ColorNames[player.cosmetics.bodyMatProperties.ColorId] },
        { "@Role", player => player.GetCustomRole().RoleName },
        { "@Blurb", player => player.GetCustomRole().Blurb },
        { "@Description", player => player.GetCustomRole().Description },
        { "%CEnd", _ => "</color>"}
    };

    private static readonly Dictionary<string, Func<PlayerControl, string, string>> VariableValues = new()
    {
        { "%Option", (_, qualifier) => OptionManager.GetManager().GetOption(qualifier)?.GetValueText() ?? "Unknown Option" },
    };

    internal class Template
    {
        public string? Tag;
        public string Text;

        public Template(string? tag, string text)
        {
            this.Tag = tag;
            this.Text = text;
        }
    }
}
