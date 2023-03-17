using AmongUs.GameOptions;
using TOHTOR.API;
using TOHTOR.Extensions;
using TOHTOR.Factions;
using TOHTOR.Managers;
using TOHTOR.Options;
using TOHTOR.Roles.Internals;
using TOHTOR.Roles.Internals.Attributes;
using TOHTOR.Roles.Legacy;
using TOHTOR.Roles.RoleGroups.Impostors;
using UnityEngine;
using VentLib.Logging;
using VentLib.Options.Game;

namespace TOHTOR.Roles.RoleGroups.Neutral;

public class Amnesiac : CustomRole {
    private bool stealExactRole;

    [RoleAction(RoleActionType.AnyReportedBody)]
    public void AmnesiacRememberAction(PlayerControl reporter, GameData.PlayerInfo reported, ActionHandle handle)
    {
        VentLogger.Old($"Reporter: {reporter.GetRawName()} | Reported: {reported.GetNameWithRole()} | Self: {MyPlayer.GetRawName()}", "");

        if (reporter.PlayerId != MyPlayer.PlayerId) return;
        CustomRole newRole = reported.GetCustomRole();
        if (!stealExactRole)
        {
            if (newRole.SpecialType == SpecialType.NeutralKilling) { }
            else if (newRole.SpecialType == SpecialType.Neutral)
                newRole = CustomRoleManager.Static.Opportunist;
            else if (newRole.IsCrewmate())
                newRole = CustomRoleManager.Static.Sheriff;
            else
                newRole = Ref<Traitor>();
        }

        Game.AssignRole(MyPlayer, newRole);

        CustomRole role = MyPlayer.GetCustomRole();
        role.DesyncRole = RoleTypes.Impostor;

        handle.Cancel();
    }

    protected override GameOptionBuilder RegisterOptions(GameOptionBuilder optionStream) =>
        base.RegisterOptions(optionStream)
            .Tab(DefaultTabs.NeutralTab)
            .SubOption(sub => sub.Name("Steals Exact Role")
                .Bind(v => stealExactRole = (bool)v)
                .AddOnOffValues(false).Build());

    protected override RoleModifier Modify(RoleModifier roleModifier) =>
        roleModifier.RoleColor(new Color(0.51f, 0.87f, 0.99f)).DesyncRole(RoleTypes.Impostor).Factions(Faction.Solo);
}