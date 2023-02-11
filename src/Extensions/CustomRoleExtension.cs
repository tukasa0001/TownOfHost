using System.Linq;
using TownOfHost.Roles;
using VentLib.Options.OptionElement;

namespace TownOfHost.Extensions;

public static class CustomRoleExtension
{
    public static Option GetOptions(this CustomRole role)
    {
        return TOHPlugin.OptionManager.Options().FirstOrDefault(o => o.Name == role.RoleName);
    }

}