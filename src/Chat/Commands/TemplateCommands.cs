using System.Linq;
using HarmonyLib;
using TownOfHost.Managers;
using VentLib.Commands;
using VentLib.Commands.Attributes;
using VentLib.Commands.Interfaces;

namespace TownOfHost.Chat.Commands;

[Command(new[] { "template", "t" }, user: CommandUser.Host)]
public class TemplateCommands: ICommandReceiver
{
    [Command("list")]
    private void ListTemplates(PlayerControl source)
    {
        Utils.SendMessage(TOHPlugin.PluginDataManager.TemplateManager.Templates
                .Select((template, i) =>
                {
                    string tag = template.Tag != null ? ", " + template.Tag : "";
                    return $"[{i + 1}{tag}] {template.Text}";
                })
            .Join(delimiter: "\n"),
            source.PlayerId
        );
    }

    [Command("preview")]
    private void PreviewTemplate(PlayerControl source, CommandContext context, string tag)
    {
        if (context.Errored)
        {
            Utils.SendMessage("Error - Missing argument \"tag\"", source.PlayerId);
            return;
        }

        Utils.SendMessage(
            !templateManager.TryFormat(source, tag, out string message) ? $"Error Displaying Template \"{tag}\"" : message,
            source.PlayerId
            );
    }

    [Command("set")]
    private void SetTemplate(PlayerControl source, CommandContext context, int index, string tag)
    {
        if (context.Errored && context.ErroredParameters[0] == 2)
            Utils.SendMessage("Error - Invalid template id argument", source.PlayerId);
        else if (context.Errored && context.ErroredParameters[0] == 3)
            Utils.SendMessage("Error - Invalid tag argument", source.PlayerId);
        else if (!templateManager.SetTemplate(index, tag))
            Utils.SendMessage($"Error - Template ID {index} does not exist", source.PlayerId);
        else
            Utils.SendMessage("Successfully set template", source.PlayerId);
    }

    [Command("reload")]
    private void Reload(PlayerControl source)
    {
        templateManager.Reload();
        Utils.SendMessage("Reloaded Template Manager", source.PlayerId);
    }

    public void Receive(PlayerControl source, CommandContext context)
    {
        if (context.Args.Length == 0) Utils.SendMessage("Incorrect usage", source.PlayerId);
    }

    private TemplateManager2 templateManager => TOHPlugin.PluginDataManager.TemplateManager;
}