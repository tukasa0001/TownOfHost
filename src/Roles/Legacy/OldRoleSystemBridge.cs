using AmongUs.GameOptions;
using TOHTOR.Extensions;
using TOHTOR.Roles.Internals;

namespace TOHTOR.Roles;

public static class OldRoleSystemBridge
{
    public static RoleType GetRoleType(this CustomRole customRole)
    {
        if (customRole.SpecialType is SpecialType.Coven) return RoleType.Coven;
        if (customRole.SpecialType is SpecialType.Neutral or SpecialType.NeutralKilling) return RoleType.Neutral;
        if (customRole is Madmate) return RoleType.Madmate;
        return customRole.VirtualRole
            is RoleTypes.Impostor
            or RoleTypes.ImpostorGhost
            or RoleTypes.Shapeshifter
                ? RoleType.Impostor
                : RoleType.Crewmate;
    }

    public static CustomRole GetCustomRole(this GameData.PlayerInfo? playerInfo)
    {
        if (playerInfo == null || playerInfo.Object == null) return CustomRoleManager.Default;
        return playerInfo.Object.GetCustomRole();
    }

    public static bool IsEnable(this CustomRole role) => role.Count > 0 && role.Chance > 0;

    public static bool IsImpostor(this CustomRole customRole) => customRole.VirtualRole is RoleTypes.Impostor or RoleTypes.ImpostorGhost or RoleTypes.Shapeshifter && !customRole.IsNeutral() && !customRole.IsNeutralKilling();

    public static bool IsCrewmate(this CustomRole customRole) =>
        customRole.VirtualRole is RoleTypes.Crewmate or RoleTypes.Engineer or RoleTypes.Scientist
            or RoleTypes.CrewmateGhost or RoleTypes.GuardianAngel &&
        customRole.SpecialType is not SpecialType.NeutralKilling;

    public static bool IsVanilla(this CustomRole customRole) => false;

    public static bool IsMadmate(this CustomRole customRole) => customRole is Madmate;

    public static bool IsNeutral(this CustomRole customRole) => customRole.SpecialType is SpecialType.Neutral;

    public static bool IsNeutralKilling(this CustomRole customRole) =>
        customRole.SpecialType is SpecialType.NeutralKilling;

    public static bool IsCoven(this CustomRole customRole) => customRole.SpecialType is SpecialType.Coven;

    public static bool CanMakeMadmate(this CustomRole role)
        => role switch
        {
            Morphling => true,
            // TODO: EvilTracker => EvilTracker.CanCreateMadMate.GetBool(),
            /*Egoist => EgoistOLD.CanCreateMadMate.GetBool(),*/
            _ => false,
        };

    // TODO;
}

public enum RoleType
{
    Crewmate,
    Impostor,
    Neutral,
    Madmate,
    Coven
}