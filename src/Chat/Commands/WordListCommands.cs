using System.Linq;
using HarmonyLib;
using TownOfHost.Managers;
using VentLib.Commands;
using VentLib.Commands.Attributes;

namespace TownOfHost.Chat.Commands;

[Command(new[] { "wordlist", "wl" }, user: CommandUser.Host)]
public class WordListCommands
{
    [Command("list")]
    private void ListWords(PlayerControl source)
    {
        Utils.SendMessage(
            ChatManager.BannedWords.Select((w, i) => $"{i+1}) {w}").Join(delimiter: "\n"),
            source.PlayerId
            );
    }

    [Command("add")]
    private void AddWord(PlayerControl source, CommandContext context, string word)
    {
        ChatManager.AddWord(word);
    }

    [Command("reload")]
    private void Reload(PlayerControl source)
    {
        ChatManager.Reload();
        Utils.SendMessage("Successfully Reloaded Wordlist", source.PlayerId);
    }

    private ChatManager ChatManager => TOHPlugin.PluginDataManager.ChatManager;

}