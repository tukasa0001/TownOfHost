using System;
using System.Linq;
using TOHTOR.Managers;
using TOHTOR.Roles;
using VentLib.Commands;
using VentLib.Commands.Attributes;
using VentLib.Commands.Interfaces;
using VentLib.Localization;
using VentLib.Localization.Attributes;

namespace TOHTOR.Chat.Commands;

[Localized(Group = "Commands", Subgroup = "Help")]
[Command(new[] {"Commands.Help.Alias"}, "h", "help")]
public class HelpCmd: ICommandReceiver
{
    [Command(new[] {"Commands.Help.Addons.Alias"},"a", "addons")]
    public static void Addons(PlayerControl source, CommandContext _)
    {
        Utils.SendMessage("Addon Info");
    }

    [Command(new[] {"Commands.Help.Gamemodes.Alias"},"m", "modes")]
    public class Gamemodes
    {
        [Command("cw", "colorwars")]
        public static void ColorWars(PlayerControl source, CommandContext _) => Utils.SendMessage("Color wars info", source.PlayerId);

        [Command("nge", "nogameend")]
        public static void NoGameEnd(PlayerControl source, CommandContext _) => Utils.SendMessage("NoGameEnd Info", source.PlayerId);
    }

    [Command(new[] {"Commands.Help.Roles.Alias"}, "r", "roles")]
    public static void Roles(PlayerControl source, CommandContext context)
    {
        if (context.Args.Length == 0)
            Utils.SendMessage(Localizer.Get("Commands.Help.Roles.Usage"), source.PlayerId);
        else {
            string roleName = String.Join(" ", context.Args);
            CustomRole? matchingRole = CustomRoleManager.AllRoles.FirstOrDefault(r => Localizer.GetAll($"Roles.{r.EnglishRoleName}.RoleName").Contains(roleName));
            if (matchingRole == null) {
                Utils.SendMessage(Localizer.Get("Commands.Help.Roles.RoleNotFound"), source.PlayerId);
                return;
            }
            Language? language = Localizer.GetLanguages($"Roles.{matchingRole.EnglishRoleName}.RoleName", roleName).FirstOrDefault();

            Utils.SendMessage(
                language == null
                    ? Localizer.Get($"Roles.{matchingRole.EnglishRoleName}.Description")
                    : language.Translate($"Roles.{matchingRole.EnglishRoleName}.Description"), source.PlayerId);
        }
    }

    // This is triggered when just using /help
    public void Receive(PlayerControl source, CommandContext context)
    {
        if (context.Args.Length > 0) return;
        string help = Localizer.Get("Commands.Help.Alias");
        Utils.SendMessage(
                Localizer.Get("Commands.Help.CommandList")
                + $"\n/{help} {Localizer.Get("Commands.Help.Roles.Alias")} - {Localizer.Get("Commands.Help.Roles.Info")}"
                + $"\n/{help} {Localizer.Get("Commands.Help.Addons.Alias")} - {Localizer.Get("Commands.Help.Addons.Info")}"
                + $"\n/{help} {Localizer.Get("Commands.Help.Gamemodes.Alias")} - {Localizer.Get("Commands.Help.Gamemodes.Info")}",
                source.PlayerId
            );
    }
}