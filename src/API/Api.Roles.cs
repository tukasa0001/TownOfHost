using System.Collections.Generic;
using System.Linq;
using TOHTOR.Roles;

namespace TOHTOR.API;

public partial class Api
{
    public class Roles
    {
        public static List<CustomRole> GetEnabledRoles() =>
            CustomRoleManager.AllRoles.Where(r => r.IsEnabled()).ToList();

    }

}