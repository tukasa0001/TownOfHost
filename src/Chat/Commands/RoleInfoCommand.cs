using System.Linq;
using TownOfHost.Extensions;
using TownOfHost.Managers;
using VentLib.Options;
using TownOfHost.Roles;
using VentLib.Commands;
using VentLib.Commands.Attributes;
using VentLib.Commands.Interfaces;
using VentLib.Utilities;
using VentLib.Utilities.Extensions;

namespace TownOfHost.Chat.Commands;

[Command("m", "myrole")]
public class RoleInfoCommand: ICommandReceiver
{
    private int previousLevel = 0;

    public void Receive(PlayerControl source, CommandContext context)
    {
        if (Game.State is GameState.InLobby) return;
        if (context.Args.Length == 0) {
            ShowRoleDescription(source);
            return;
        }
        string pageString = context.Args[0];
        if (!int.TryParse(pageString, out int page) || page <= 1) ShowRoleDescription(source);
        else ShowRoleOptions(source, page);
    }

    private void ShowRoleDescription(PlayerControl source)
    {
        CustomRole role = source.GetCustomRole();
        string output = $"{role} {role.Factions.StrJoin()}:";
        output += $"\n{role.Description}";
        Utils.SendMessage(output, source.PlayerId);
    }

    private void ShowRoleOptions(PlayerControl source, int page)
    {
        CustomRole role = source.GetCustomRole();
        string output = $"{role} {role.Factions.StrJoin()}:";

        Option? optionMatch = TOHPlugin.OptionManager.Options().FirstOrDefault(h => h.Name == role.RoleName);
        if (optionMatch == null) { ShowRoleDescription(source); return; }

        /*foreach (var child in optionMatch.GetHoldersRecursive().Where(child => child != optionMatch))
            UpdateOutput(ref output, child);*/

        Utils.SendMessage(output, source.PlayerId);
    }

    private void UpdateOutput(ref string output, Option options)
    {
        if (options.Level < previousLevel)
            output += "\n";
        previousLevel = options.Level;
        if (options.Color != null)
            output += $"\n{options.Name} => {options.Color.Colorize(options.GetValueAsString())}";
        else
            output += $"\n{options.Name} => {options.GetValueAsString()}";
    }
}