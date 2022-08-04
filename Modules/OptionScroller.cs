using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;
using HarmonyLib;
using System.Reflection;
using System.Text;

namespace TownOfHost
{

    [HarmonyPatch]
    class GameOptionsDataPatch
    {
        private static IEnumerable<MethodBase> TargetMethods()
        {
            return typeof(GameOptionsData).GetMethods().Where(x => x.ReturnType == typeof(string) && x.GetParameters().Length == 1 && x.GetParameters()[0].ParameterType == typeof(int));
        }

        private static string BuildRoleOptions()
        {
            var impRoles = GameOptionsDataPatch.BuildOptionsOfType(CustomOption.CustomOptionType.Impostor, true) + "\n";
            var neutralRoles = BuildOptionsOfType(CustomOption.CustomOptionType.Neutral, true) + "\n";
            var crewRoles = BuildOptionsOfType(CustomOption.CustomOptionType.Crewmate, true) + "\n";
            var modifiers = BuildOptionsOfType(CustomOption.CustomOptionType.Modifier, true);
            return impRoles + neutralRoles + crewRoles + modifiers;
        }

        private static string BuildOptionsOfType(CustomOption.CustomOptionType type, bool headerOnly)
        {
            StringBuilder sb = new("\n");
            var options = CustomOption.Options.Where(o => o.type == type);
            foreach (var option in options)
            {
                if (option.Parent == null)
                {
                    sb.AppendLine($"{option.Name}: {option.Selections[option.Selection]}");
                }
            }
            if (headerOnly) return sb.ToString();
            else sb = new StringBuilder();

            sb.AppendLine(Helpers.ColorString(new Color(204f / 255f, 204f / 255f, 0, 1f), "Crewmate Roles"));
            sb.AppendLine(Helpers.ColorString(new Color(204f / 255f, 204f / 255f, 0, 1f), "Neutral Roles"));
            sb.AppendLine(Helpers.ColorString(new Color(204f / 255f, 204f / 255f, 0, 1f), "Impostor Roles"));
            sb.AppendLine(Helpers.ColorString(new Color(204f / 255f, 204f / 255f, 0, 1f), "Modifiers"));

            foreach (var option in options)
            {
                if (option.Parent != null)
                {
                    bool isIrrelevant = option.Parent.GetSelection() == 0 || (option.Parent.Parent != null && option.Parent.Parent.GetSelection() == 0);

                    Color c = isIrrelevant ? Color.grey : Color.white;  // No use for now
                    if (isIrrelevant) continue;
                    sb.AppendLine(Helpers.ColorString(c, $"{option.Name}: {option.Selections[option.Selection]}"));
                }
                else
                {
                    sb.AppendLine($"\n{option.Name}: {option.Selections[option.Selection].ToString()}");
                }
            }
            return sb.ToString();
        }

        private static void Postfix(ref string __result)
        {
            int counter = Options.OptionsPage;
            string hudString = counter != 0 ? Helpers.ColorString(DateTime.Now.Second % 2 == 0 ? Color.white : Color.red, "(Use scroll wheel if necessary)\n\n") : "";

            switch (counter)
            {
                case 0:
                    hudString += "Page 1: Vanilla Settings \n\n" + __result;
                    break;
                case 1:
                    hudString += "Page 2: The Other Roles Settings \n" + BuildOptionsOfType(CustomOption.CustomOptionType.General, false);
                    break;
                case 2:
                    hudString += "Page 3: Role and Modifier Rates \n" + BuildRoleOptions();
                    break;
                case 3:
                    hudString += "Page 4: Impostor Role Settings \n" + BuildOptionsOfType(CustomOption.CustomOptionType.Impostor, false);
                    break;
                case 4:
                    hudString += "Page 5: Neutral Role Settings \n" + BuildOptionsOfType(CustomOption.CustomOptionType.Neutral, false);
                    break;
                case 5:
                    hudString += "Page 6: Crewmate Role Settings \n" + BuildOptionsOfType(CustomOption.CustomOptionType.Crewmate, false);
                    break;
                case 6:
                    hudString += "Page 7: Modifier Settings \n" + BuildOptionsOfType(CustomOption.CustomOptionType.Modifier, false);
                    break;
            }

            hudString += $"\n Press TAB or Page Number for more... ({counter + 1}/7)";
            __result = hudString;
        }
    }
}