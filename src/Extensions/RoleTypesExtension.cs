using AmongUs.GameOptions;

namespace TOHTOR.Extensions;

public static class RoleTypesExtension
{
    public static bool IsImpostor(this RoleTypes roleTypes) => roleTypes is RoleTypes.Impostor or RoleTypes.Shapeshifter;
    public static bool IsCrewmate(this RoleTypes roleTypes) => roleTypes is not (RoleTypes.Impostor or RoleTypes.Shapeshifter);
}