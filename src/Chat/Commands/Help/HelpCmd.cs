using VentLib.Commands;
using VentLib.Commands.Attributes;

namespace TownOfHost.Chat.Commands.Help;

[Command(new[] {"Commands.Aliases.Help"}, "h", "help")]
public class HelpCmd
{
    [Command("r", "roles")]
    public static void Roles(PlayerControl source, CommandContext _)
    {
        Utils.SendMessage("Roles Info");
    }

    [Command("a", "addons")]
    public static void Addons(PlayerControl source, CommandContext _)
    {
        Utils.SendMessage("Addon Info");
    }

    [Command("m", "modes")]
    public class Gamemodes
    {
        [Command("cw", "colorwars")]
        public static void ColorWars(PlayerControl source, CommandContext _) => Utils.SendMessage("Color wars info", source.PlayerId);

        [Command("nge", "nogameend")]
        public static void NoGameEnd(PlayerControl source, CommandContext _) => Utils.SendMessage("NoGameEnd Info", source.PlayerId);
    }
}