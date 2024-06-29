using TownOfHost.Modules.OptionItems.Interfaces;
using TownOfHost.Roles.Core;
using UnityEngine;

namespace TownOfHost.Modules.OptionItems;

public sealed class RoleSpawnChanceOptionItem : IntegerOptionItem, IRoleOptionItem
{
    public RoleSpawnChanceOptionItem(
        int id,
        string name,
        int defaultValue,
        TabGroup tab,
        bool isSingleValue,
        IntegerValueRule rule,
        CustomRoles roleId,
        Color roleColor) : base(id, name, defaultValue, tab, isSingleValue, rule)
    {
        RoleId = roleId;
        RoleColor = roleColor;
    }
    public RoleSpawnChanceOptionItem(
        int id,
        string name,
        int defaultValue,
        TabGroup tab,
        bool isSingleValue,
        IntegerValueRule rule,
        SimpleRoleInfo roleInfo) : this(id, name, defaultValue, tab, isSingleValue, rule, roleInfo.RoleName, roleInfo.RoleColor) { }

    public CustomRoles RoleId { get; }
    public Color RoleColor { get; }

    public override void Refresh()
    {
        base.Refresh();
        if (OptionBehaviour != null && OptionBehaviour.TitleText != null)
        {
            OptionBehaviour.TitleText.text = GetName(true);
        }
    }
}
