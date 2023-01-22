using TownOfHost.Factions;
using UnityEngine;
using TownOfHost.Extensions;
using TownOfHost.Options;
using System.Collections.Generic;
using System.Linq;
using VentLib.Extensions;
using TownOfHost.Roles;

namespace TownOfHost.Roles;

public class CrewPostor : Crewmate
{
    protected override void OnTaskComplete()
    {
        if (MyPlayer.Data.IsDead) return;
        List<PlayerControl> inRangePlayers = RoleUtils.GetPlayersWithinDistance(MyPlayer, 999).Where(p => !p.GetCustomRole().IsAllied(MyPlayer)).ToList();
        if (inRangePlayers.Count == 0) return;
        MyPlayer.RpcMurderPlayer(inRangePlayers.GetRandom());
    }
    protected override SmartOptionBuilder RegisterOptions(SmartOptionBuilder optionStream) =>
        base.RegisterOptions(optionStream)
        .Tab(DefaultTabs.NeutralTab)
            .AddSubOption(sub => sub
                .Name("Override CrewPostor's Tasks")
                .Bind(v => HasOverridenTasks = (bool)v)
                .ShowSubOptionsWhen(v => (bool)v)
                .AddOnOffValues(false)
                .AddSubOption(sub2 => sub2
                    .Name("Allow Common Tasks")
                    .Bind(v => HasCommonTasks = (bool)v)
                    .AddOnOffValues()
                    .Build())
                .AddSubOption(sub2 => sub2
                    .Name("CrewPostor Long Tasks")
                    .Bind(v => LongTasks = (int)v)
                    .AddIntRangeValues(0, 20, 1, 5)
                    .Build())
                .AddSubOption(sub2 => sub2
                    .Name("CrewPostor Short Tasks")
                    .Bind(v => ShortTasks = (int)v)
                    .AddIntRangeValues(1, 20, 1, 5)
                    .Build())
                .Build());

    protected override RoleModifier Modify(RoleModifier roleModifier) =>
        base.Modify(roleModifier).RoleColor("#DC6601").Factions(Faction.Solo);
}