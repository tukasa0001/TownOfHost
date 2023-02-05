using System.Linq;
using VentLib.Options;
using TownOfHost.Roles;

namespace TownOfHost.Extensions;

public static class CustomRoleExtension
{
    public static Option GetOptions(this CustomRole role)
    {
        return TOHPlugin.OptionManager.Options().FirstOrDefault(o => o.Name == role.RoleName);
    }

}