using System.Linq;
using TOHTOR.API;
using TOHTOR.Managers.History.Events;
using TOHTOR.Roles.Internals.Attributes;
using TOHTOR.Roles.Internals.Interfaces;
using TOHTOR.Roles.RoleGroups.Vanilla;
using VentLib.Localization.Attributes;
using VentLib.Utilities;

namespace TOHTOR.Roles.RoleGroups.Crew;

[Localized(Group = "Roles", Subgroup = "Medium")]
public partial class Medium: Crewmate, IModdable
{
    [Localized("MediumMessage")]
    private static string _mediumMessage = null!;


    [RoleAction(RoleActionType.AnyReportedBody)]
    private void MediumDetermineRole(PlayerControl reporter, GameData.PlayerInfo reported)
    {
        if (reporter.PlayerId != MyPlayer.PlayerId) return;
        IDeathEvent? deathEvent = Game.GameHistory.GetEvents<IDeathEvent>().FirstOrDefault(e => e.Player().PlayerId == reported.PlayerId);
        deathEvent?.InstigatorRole().IfPresent(killerRole => Async.Schedule(() => MediumSendMessage(killerRole), 2f));
    }

    private void MediumSendMessage(CustomRole killerRole)
    {
        Utils.SendMessage($"{_mediumMessage} {killerRole}", MyPlayer.PlayerId);
    }

    protected override RoleModifier Modify(RoleModifier roleModifier) =>
        base.Modify(roleModifier)
            .RoleColor("#A680FF");
}

