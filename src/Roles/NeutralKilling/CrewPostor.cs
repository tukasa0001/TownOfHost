using TOHTOR.Factions;
using TOHTOR.Extensions;
using System.Collections.Generic;
using System.Linq;
using TOHTOR.Options;
using VentLib.Options.Game;
using VentLib.Utilities.Extensions;

namespace TOHTOR.Roles;

public class CrewPostor : Crewmate
{
    protected override void OnTaskComplete()
    {
        if (MyPlayer.Data.IsDead) return;
        List<PlayerControl> inRangePlayers = RoleUtils.GetPlayersWithinDistance(MyPlayer, 999).Where(p => !p.GetCustomRole().IsAllied(MyPlayer)).ToList();
        if (inRangePlayers.Count == 0) return;
        MyPlayer.RpcMurderPlayer(inRangePlayers.GetRandom());
    }
    protected override GameOptionBuilder RegisterOptions(GameOptionBuilder optionStream) =>
        base.RegisterOptions(optionStream)
        .Tab(DefaultTabs.NeutralTab)
            .SubOption(sub => sub
                .Name("Override CrewPostor's Tasks")
                .Bind(v => HasOverridenTasks = (bool)v)
                .ShowSubOptionPredicate(v => (bool)v)
                .AddOnOffValues(false)
                .SubOption(sub2 => sub2
                    .Name("Allow Common Tasks")
                    .Bind(v => HasCommonTasks = (bool)v)
                    .AddOnOffValues()
                    .Build())
                .SubOption(sub2 => sub2
                    .Name("CrewPostor Long Tasks")
                    .Bind(v => LongTasks = (int)v)
                    .AddIntRange(0, 20, 1, 5)
                    .Build())
                .SubOption(sub2 => sub2
                    .Name("CrewPostor Short Tasks")
                    .Bind(v => ShortTasks = (int)v)
                    .AddIntRange(1, 20, 1, 5)
                    .Build())
                .Build());

    protected override RoleModifier Modify(RoleModifier roleModifier) =>
        base.Modify(roleModifier).RoleColor("#DC6601").Factions(Faction.Solo);
}