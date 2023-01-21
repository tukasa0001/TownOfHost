using TownOfHost.Extensions;
using VentLib.Commands;
using VentLib.Commands.Attributes;
using VentLib.Commands.Interfaces;
using VentLib.Extensions;
using VentLib.Logging;

namespace TownOfHost.Chat.Commands;

[Command(new[] {"r", "role"}, user: CommandUser.Host)]
public class RoleInfoCommand: ICommandReceiver
{
    public void Receive(PlayerControl source, CommandContext context)
    {
        VentLogger.Fatal($"Received Message: {source.GetRawName()} => {context.Args.StrJoin()}", "TESTEST");
    }

    [Command(new[] { "set" }, user: CommandUser.Host)]
    public class Subcommand : ICommandReceiver
    {
        public void Receive(PlayerControl source, CommandContext context)
        {
            VentLogger.Fatal($"Received Message: {source.GetRawName()} => {context.Args.StrJoin()}", "TESTEST");
        }
    }
}