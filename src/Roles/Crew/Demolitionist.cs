using TownOfHost.Options;
using System.Collections.Generic;
using TownOfHost.Managers;
using TownOfHost.Patches.Systems;
using TownOfHost.Roles.Internals.Attributes;
using VentLib.Utilities;

namespace TownOfHost.Roles;

public class Demolitionist : Crewmate
{
    private float demoTime;
    private List<byte> inDemoTime;

    [RoleAction(RoleActionType.RoundStart)]
    private void Reset() => inDemoTime = new List<byte>();

    [RoleAction(RoleActionType.MyDeath)]
    private void DemoDeath(PlayerControl killer)
    {
        inDemoTime.Add(killer.PlayerId);
        Async.Schedule(() => DelayedDeath(killer), demoTime);
    }

    private void DelayedDeath(PlayerControl killer)
    {
        inDemoTime.Remove(killer.PlayerId);
        if (Game.State is GameState.InMeeting) return;
        if (killer.Data.IsDead || killer.inVent)
        {
            if (SabotagePatch.CurrentSabotage is SabotageType.Reactor) return;
            RoleUtils.PlayReactorsForPlayer(MyPlayer);
            Async.Schedule(() => RoleUtils.EndReactorsForPlayer(MyPlayer), 1f);
        }
        else
        {
            //  if (killer.Is(CustomRoleManager.Static.Pestilence)) // role not implemented yet
            RoleUtils.RoleCheckedMurder(killer, killer);
            // SET DEATH REASON
        }
    }

    protected override SmartOptionBuilder RegisterOptions(SmartOptionBuilder optionStream) =>
        base.RegisterOptions(optionStream)
            .Tab(DefaultTabs.CrewmateTab)
            .AddSubOption(sub => sub
                .Name("Demo Time")
                .BindFloat(v => demoTime = v)
                .AddFloatRangeValues(0.5f, 10f, 0.5f, 2)
                .Build());

    protected override RoleModifier Modify(RoleModifier roleModifier) =>
        base.Modify(roleModifier).RoleColor("#5e2801");
}