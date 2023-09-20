using HarmonyLib;
using Il2CppSystem;
using Il2CppSystem.Collections.Generic;
using TownOfHost.Roles.Core;

namespace TownOfHost.Patches;

[HarmonyPatch(typeof(LogicRoleSelectionNormal), nameof(LogicRoleSelectionNormal.AssignRolesForTeam))]
public static class LogicRoleSelectionNormalAssignRolesForTeamPatch
{
    public static void Prefix([HarmonyArgument(0)] List<GameData.PlayerInfo> players)
    {
        // 先にdesyncロールを割り当て済みの人をバニラロールの抽選から除外
        players.RemoveAll((Predicate<GameData.PlayerInfo>)(player => player.GetCustomRole().GetRoleInfo()?.RequireResetCam == true));
    }
}
