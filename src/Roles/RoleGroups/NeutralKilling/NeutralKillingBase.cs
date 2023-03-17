using AmongUs.GameOptions;
using TOHTOR.Factions;
using TOHTOR.Roles.Internals;
using TOHTOR.Roles.Internals.Interfaces;

namespace TOHTOR.Roles.RoleGroups.NeutralKilling;

public partial class NeutralKillingBase: Vanilla.Impostor, IModdable
{
    public override bool IsAllied(PlayerControl player) => false;

    protected override RoleModifier Modify(RoleModifier roleModifier) =>
        roleModifier.VanillaRole(RoleTypes.Impostor).SpecialType(SpecialType.NeutralKilling).Factions(Faction.Solo);
}