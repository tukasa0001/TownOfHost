using TownOfHost.Roles.Core;
using UnityEngine;

namespace TownOfHost.Modules.OptionItems.Interfaces;

public interface IRoleOptionItem
{
    public CustomRoles RoleId { get; }
    public Color RoleColor { get; }
}
