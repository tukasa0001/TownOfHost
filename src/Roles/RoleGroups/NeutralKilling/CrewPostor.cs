using System.Collections.Generic;
using System.Linq;
using TOHTOR.API;
using TOHTOR.Extensions;
using TOHTOR.Factions;
using TOHTOR.Managers.History.Events;
using TOHTOR.Options;
using TOHTOR.Roles.RoleGroups.Vanilla;
using VentLib.Options.Game;
using VentLib.Utilities.Extensions;

namespace TOHTOR.Roles.RoleGroups.NeutralKilling;

public class CrewPostor : Crewmate
{
    protected override void OnTaskComplete()
    {
        if (MyPlayer.Data.IsDead) return;
        List<PlayerControl> inRangePlayers = RoleUtils.GetPlayersWithinDistance(MyPlayer, 999).Where(p => !p.GetCustomRole().IsAllied(MyPlayer)).ToList();
        if (inRangePlayers.Count == 0) return;
        PlayerControl target = inRangePlayers.GetRandom();
        bool death = MyPlayer.Attack(target, () => new TaskDeathEvent(target, MyPlayer));
        Game.GameHistory.AddEvent(new TaskKillEvent(MyPlayer, target, death));
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

    protected override RoleModifier Modify(RoleModifier roleModifier) => base.Modify(roleModifier).RoleColor("#DC6601").Factions(Faction.Solo);

    class TaskKillEvent : KillEvent, IRoleEvent
    {
        public TaskKillEvent(PlayerControl killer, PlayerControl victim, bool successful = true) : base(killer, victim, successful)
        {
        }

        public override string Message() => $"{Game.GetName(Player())} viciously completed his task and killed {Game.GetName(Target())}.";
    }

    class TaskDeathEvent : DeathEvent
    {
        public TaskDeathEvent(PlayerControl deadPlayer, PlayerControl? killer) : base(deadPlayer, killer)
        {
        }
    }
}