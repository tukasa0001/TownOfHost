using System;
using System.Linq;
using HarmonyLib;
using TOHTOR.API;
using TOHTOR.Roles;
using UnityEngine;
using VentLib.Localization.Attributes;
using VentLib.Options;
using VentLib.Options.Game;
using VentLib.Utilities;
using VentLib.Utilities.Extensions;

namespace TOHTOR.Options;

[Localized(Group = "OptionShower")]
public class ShowerPages
{
    [Localized("ActiveRolesList")]
    private static string ActiveRolesList = "Active Roles List";

    public static void InitPages()
    {
        OptionShower shower = OptionShower.GetOptionShower();
        shower.AddPage(VanillaPage());
        shower.AddPage(EnabledRolePage());
        shower.AddPage(RoleOptionsPage());
        shower.AddPage(EnableGeneralPage());
    }

    private static Func<string> VanillaPage()
    {
        return () => GameOptionsManager.Instance.CurrentGameOptions.ToHudString(GameData.Instance
            ? GameData.Instance.PlayerCount
            : 10) + "\n";
    }

    private static Func<string> EnabledRolePage()
    {
        return () =>
        {
            string content = $"Gamemode: {Game.CurrentGamemode.GetName()}\n\n";
            content += $"{CustomRoleManager.Special.GM.RoleColor.Colorize("GM")}: {Utils.GetOnOffColored(StaticOptions.EnableGM)}\n\n";
            content += ActiveRolesList + "\n";

            CustomRoleManager.MainRoles.Where(role => role.IsEnabled()).ForEach(role =>
            {
                Color color = role.RoleColor;
                content += $"{color.Colorize(role.RoleName)}: {role.Chance}% x {role.Count}\n";
            });
            return content;
        };
    }

    public static Func<string> RoleOptionsPage()
    {
        return () =>
        {
            var content = "";
            CustomRoleManager.MainRoles.Where(role => role.IsEnabled()).ForEach(role =>
            {
                var opt = role.Options;
                content += $"{opt.Name()}: {opt.GetValueText()}\n";
                if (opt.Children.Matches(opt.GetValue()))
                    content = ShowChildren(opt, opt.Color, content);
                content += "\n";
            });
            return content;
        };
    }

    private static Func<string> EnableGeneralPage()
    {
        return () =>
        {
            string optionString = "";
            var optionManager = OptionManager.GetManager();

            optionManager.GetOptions().Where(opt => opt.GetType() == typeof(GameOption)).Cast<GameOption>().Do(opt =>
            {
                CustomRole? matchingRole = CustomRoleManager.AllRoles.FirstOrDefault(r => r.Options == opt);

                if (matchingRole != null) return;

                optionString += $"{opt.Name()}: {opt.GetValueText()}\n";
                if (opt.Children.Matches(opt.GetValue()))
                    optionString = ShowChildren(opt, opt.Color, optionString);
                optionString += "\n";
            });

            return optionString;
        };
    }

    private static string ShowChildren(GameOption option, Color color, string text)
    {

        option.Children.Cast<GameOption>().ForEach((opt, index) =>
        {
            if (opt.Name() == "Maximum") return;
            text += color.Colorize("┃".Repeat(option.Level - 2));
            text += color.Colorize(index == option.Children.Count - 1 ? "┗ " : "┣ ");
            text += $"{opt.Name()}: {opt.GetValueText()}\n";
            if (opt.Children.Matches(opt.GetValue())) text = ShowChildren(opt, color, text);
        });
        return text;
    }
}