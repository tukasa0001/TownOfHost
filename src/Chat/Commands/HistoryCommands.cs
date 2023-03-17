using System.Linq;
using HarmonyLib;
using TOHTOR.API;
using VentLib.Commands;
using VentLib.Commands.Attributes;
using VentLib.Commands.Interfaces;

namespace TOHTOR.Chat.Commands;

[Command("history")]
public class HistoryCommands : ICommandReceiver
{
    public void Receive(PlayerControl source, CommandContext context)
    {
        if (Game.GameHistory == null!) return;
        Utils.SendMessage(Game.GameHistory.Events.Where(e => e.IsCompletion()).Select(e => e.GenerateMessage()).Join(delimiter: "\n"), source.PlayerId);
    }
}