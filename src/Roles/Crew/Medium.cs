using System.Linq;
using TownOfHost.Extensions;
using TownOfHost.Managers;
using TownOfHost.Managers.History;
using VentLib.Localization.Attributes;
using VentLib.Utilities;

namespace TownOfHost.Roles;

[Localized(Group = "Roles", Subgroup = "Medium")]
public class Medium: Crewmate
{
    [Localized("MediumMessage")]
    private static string _mediumMessage = null!;


    [RoleAction(RoleActionType.AnyReportedBody)]
    private void MediumDetermineRole(PlayerControl reporter, GameData.PlayerInfo reported)
    {
        DeathEvent? deathEvent = Game.GameHistory.GetEvents<DeathEvent>().FirstOrDefault(e => e.Killed.PlayerId == reported.PlayerId);
        if (deathEvent == null) return;
        CustomRole killerRole = deathEvent.Killer!.GetCustomRole();
        Async.ScheduleThreaded(() => MediumSendMessage(killerRole), 2f);
    }

    private void MediumSendMessage(CustomRole killerRole)
    {
        Utils.SendMessage($"{_mediumMessage} {killerRole}", MyPlayer.PlayerId);
    }

    protected override RoleModifier Modify(RoleModifier roleModifier) =>
        base.Modify(roleModifier)
            .RoleColor("#A680FF");
}