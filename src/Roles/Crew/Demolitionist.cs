using TownOfHost.Factions;
using UnityEngine;
using TownOfHost.Extensions;
using TownOfHost.Options;
using System.Collections.Generic;
using System.Linq;
using TownOfHost.Managers;
using VentLib.Utilities;

namespace TownOfHost.Roles;

public class Demolitionist : Crewmate
{
    private float demoTime;
    public List<byte> inDemoTime;

    [RoleAction(RoleActionType.RoundStart)]
    public void Reset()
    {
        inDemoTime = new List<byte>();
    }

    [RoleAction(RoleActionType.MyDeath)]
    public void DemoDeath(PlayerControl killer)
    {
        inDemoTime.Add(killer.PlayerId);
        Async.ScheduleInStep(() =>
        {
            inDemoTime.Remove(killer.PlayerId);
            if (Game.State is not GameState.InMeeting)
                if (killer.Data.IsDead || killer.inVent)
                {
                    // Well you died by other causes. F
                    // or you are just in a vent :grin:
                }
                else
                {
                    //  if (killer.Is(CustomRoleManager.Static.Pestilence)) // role not implemented yet
                    killer.RpcMurderPlayer(killer);
                    // SET DEATH REASON
                }
        }, demoTime);
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