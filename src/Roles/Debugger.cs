using System.Linq;
using AmongUs.GameOptions;
using TownOfHost.Extensions;
using TownOfHost.Managers;
using TownOfHost.ReduxOptions;
using UnityEngine;

namespace TownOfHost.Roles;

public class Debugger: CustomRole
{

    [RoleAction(RoleActionType.OnPet)]
    private void OnPet()
    {
        Logger.Info("OnPet Called", "DebuggerCall");
        LogStats();

        Vector2 location = MyPlayer.GetTruePosition();
        foreach (PlayerControl player in Game.GetAlivePlayers().Where(p => p.PlayerId != MyPlayer.PlayerId))
            Logger.Info($"Distance from {MyPlayer.GetRawName()} to {player.GetRawName()} :: {Vector2.Distance(location, player.GetTruePosition())}", "DebuggerDistance");
    }

    private void LogStats()
    {
        Logger.Info($"{MyPlayer.GetNameWithRole()} | Dead? {MyPlayer.Data.IsDead} | AURole: {MyPlayer.Data.Role.name} | Custom Role: {MyPlayer.GetCustomRole().RoleName.RemoveHtmlTags()} | Subrole: {MyPlayer.GetSubrole()?.RoleName}", "DebuggerStats");
        Logger.Info($"Stats | Total Players: {Game.GetAllPlayers().Count()} | Alive Players: {Game.GetAlivePlayers().Count()} | Impostors: {GameStats.CountAliveImpostors()}", "DebuggerStats");
        Logger.Info("-=-=-=-=-=-=-=-=-=-=-=-= Other Players =-=-=-=-=-=-=-=-=-=-=-=-", "DebuggerStats");
        foreach (PlayerControl player in Game.GetAllPlayers().Where(p => p.PlayerId != MyPlayer.PlayerId))
            Logger.Info($"{player.GetNameWithRole()} | Dead? {player.Data.IsDead} | AURole: {player.Data.Role.name} | Custom Role: {player.GetCustomRole().RoleName.RemoveHtmlTags()} | Subrole: {player.GetSubrole()?.RoleName}", "DebuggerStats");

        Logger.Info("-=-=-=-=-=-=-=-=-=- Role Blocked Players -=-=-=-=-=-=-=-=-=-", "DebuggerStats");
        foreach (byte playerId in CustomRoleManager.RoleBlockedPlayers.Distinct())
        {
            int count = CustomRoleManager.RoleBlockedPlayers.Count(b => b == playerId);
            Logger.Info($"{Utils.GetPlayerById(playerId).GetNameWithRole()}: {count}", "DebuggerStats");
        }

        Logger.Info("-=-=-=-=-=-=-=-= End Of Debugger =-=-=-=-=-=-=-=-", "DebuggerStats");
    }


    protected override SmartOptionBuilder RegisterOptions(SmartOptionBuilder optionStream) =>
        base.RegisterOptions(optionStream)
            .Name("<b><color=#FF0000>D</color><color=#FFBF00>e</color><color=#7FFF00>b</color><color=#00FF3F>u</color><color=#00FEFF>g</color><color=#003FFF>g</color><color=#7F00FF>e</color><color=#FF00BF>r</color></b>");


    protected override RoleModifier Modify(RoleModifier roleModifier) =>
        roleModifier
            .RoleName(
                "<b><color=#FF0000>D</color><color=#FFBF00>e</color><color=#7FFF00>b</color><color=#00FF3F>u</color><color=#00FEFF>g</color><color=#003FFF>g</color><color=#7F00FF>e</color><color=#FF00BF>r</color></b>")
            .RoleColor(new Color(0.84f, 1f, 0.64f))
            .VanillaRole(RoleTypes.Engineer);

}