using System.Linq;
using TownOfHost.Options;
using TownOfHost.Roles;

namespace TownOfHost.Extensions;

public static class CustomRoleExtension
{
    public static OptionHolder GetOptions(this CustomRole role)
    {
        return TOHPlugin.OptionManager.Options().FirstOrDefault(o => o.Name == role.RoleName);
    }

}