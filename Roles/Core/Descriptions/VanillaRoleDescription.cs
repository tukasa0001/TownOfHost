using AmongUs.GameOptions;

namespace TownOfHost.Roles.Core.Descriptions;

/// <summary>
/// バニラ役職の説明文
/// </summary>
public class VanillaRoleDescription : RoleDescription
{
    public VanillaRoleDescription(SimpleRoleInfo roleInfo, RoleTypes vanillaRoleType) : base(roleInfo)
    {
        this.vanillaRoleType = vanillaRoleType;
    }
    private readonly RoleTypes vanillaRoleType;

    public override string Blurb => DestroyableSingleton<RoleManager>.Instance.GetRole(vanillaRoleType).Blurb;
    public override string Description => DestroyableSingleton<RoleManager>.Instance.GetRole(vanillaRoleType).BlurbLong;
}
