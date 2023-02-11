using System;
using TownOfHost.API;
using TownOfHost.Extensions;
using TownOfHost.Options;
using VentLib.Commands;
using VentLib.Commands.Attributes;
using VentLib.Commands.Interfaces;

namespace TownOfHost.Chat.Commands;

[Command("r", "rename")]
public class Rename: ICommandReceiver
{
    public void Receive(PlayerControl source, CommandContext context)
    {
        if (!(StaticOptions.AllowCustomizeCommands || source.IsHost())) return;
        string name = String.Join(" ", context.Args);
        source.RpcSetName(name);
    }
}

[Command("w", "winner")]
public class Winner : ICommandReceiver
{
    public void Receive(PlayerControl source, CommandContext _)
    {
        Utils.SendMessage($"Winners: {String.Join(", ", Game.GameHistory.LastWinners)}", source.PlayerId);
    }
}