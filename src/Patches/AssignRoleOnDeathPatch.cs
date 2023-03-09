using AmongUs.GameOptions;
using HarmonyLib;
using TOHTOR.Extensions;
using VentLib.Logging;

namespace TOHTOR.Patches;

[HarmonyPatch(typeof(RoleManager), nameof(RoleManager.AssignRoleOnDeath))]
public class AssignRoleOnDeathPatch
{
    public static bool Prefix(RoleManager __instance)
    {
        return false;
    }


    public static void Postfix(RoleManager __instance, [HarmonyArgument(0)] PlayerControl player, [HarmonyArgument(1)] bool specialRolesAllowed)
    {
        bool impostor = player.GetCustomRole().RealRole.IsImpostor();
        switch (player.Data.Role.Role)
        {
            case RoleTypes.GuardianAngel:
                break;
            case RoleTypes.CrewmateGhost:
                if (impostor) player.RpcSetRole(RoleTypes.ImpostorGhost);
                break;
            case RoleTypes.ImpostorGhost:
                if (!impostor) player.RpcSetRole(RoleTypes.CrewmateGhost);
                break;
            case RoleTypes.Shapeshifter:
            case RoleTypes.Crewmate:
            case RoleTypes.Impostor:
            case RoleTypes.Scientist:
            case RoleTypes.Engineer:
            default:
                return;
        }

        VentLogger.Debug($"Dead Player {player.GetRawName()} => {player.Data.Role.Role}");
    }
}