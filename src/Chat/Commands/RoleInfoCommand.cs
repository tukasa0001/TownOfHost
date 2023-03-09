using System.Linq;
using TOHTOR.API;
using TOHTOR.Extensions;
using TOHTOR.Roles;
using VentLib.Commands;
using VentLib.Commands.Attributes;
using VentLib.Commands.Interfaces;
using VentLib.Options;
using VentLib.Options.Game;
using VentLib.Utilities;
using VentLib.Utilities.Extensions;

namespace TOHTOR.Chat.Commands;

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

        Option? optionMatch = OptionManager.GetManager(file: "role_options.txt").GetOptions().FirstOrDefault(h => h.Name().RemoveHtmlTags() == role.RoleName);
        if (optionMatch == null) { ShowRoleDescription(source); return; }

        foreach (var child in optionMatch.Children) UpdateOutput(ref output, child);

        Utils.SendMessage(output, source.PlayerId);
    }

    private void UpdateOutput(ref string output, Option options)
    {
        if (options is not GameOption gameOption) return;
        if (gameOption.Level < previousLevel)
            output += "\n";
        previousLevel = gameOption.Level;
        output += $"\n{gameOption.Name()} => {gameOption.Color.Colorize(options.GetValueText())}";

    }
}