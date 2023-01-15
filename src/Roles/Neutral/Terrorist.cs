using TownOfHost.Factions;
using UnityEngine;
using TownOfHost.Options;

namespace TownOfHost.Roles;

// Inherits from crewmate because crewmate has task setup
public class Terrorist : Crewmate
{
    private bool canWinBySuicide;

    [RoleAction(RoleActionType.MyDeath)]
    private void OnTerroristDeath() => TerroristWinCheck();


    //   [RoleAction(RoleActionType.SelfExiled)]
    //  private void OnTerroristExiled() => TerroristWinCheck();

    private void TerroristWinCheck()
    {
        if (this.HasAllTasksDone)
        {
            // I know we are going to redo death reasons but I will still like it here for reasons.
            /*if (canWinBySuicide || TOHPlugin.PlayerStates[MyPlayer.PlayerId].deathReason != (PlayerStateOLD.DeathReason.Suicide | PlayerStateOLD.DeathReason.FollowingSuicide))
            {
                // TERRORIST WIN
            }*/
        }
        //OldRPC.TerroristWin(MyPlayer.PlayerId);
    }

    protected override SmartOptionBuilder RegisterOptions(SmartOptionBuilder optionStream) =>
        base.RegisterOptions(optionStream)
        .Tab(DefaultTabs.NeutralTab)
            .AddSubOption(sub => sub
                .Name("Can Win By Suicide")
                .Bind(v => canWinBySuicide = (bool)v)
                .AddOnOffValues(false).Build())
            .AddSubOption(sub => sub
                .Name("Override Terrorist's Tasks")
                .Bind(v => HasOverridenTasks = (bool)v)
                .ShowSubOptionsWhen(v => (bool)v)
                .AddOnOffValues(false)
                .AddSubOption(sub2 => sub2
                    .Name("Allow Common Tasks")
                    .Bind(v => HasCommonTasks = (bool)v)
                    .AddOnOffValues()
                    .Build())
                .AddSubOption(sub2 => sub2
                    .Name("Terrorist Long Tasks")
                    .Bind(v => LongTasks = (int)v)
                    .AddIntRangeValues(0, 20, 1, 5)
                    .Build())
                .AddSubOption(sub2 => sub2
                    .Name("Terrorist Short Tasks")
                    .Bind(v => ShortTasks = (int)v)
                    .AddIntRangeValues(1, 20, 1, 5)
                    .Build())
                .Build());

    protected override RoleModifier Modify(RoleModifier roleModifier) =>
        base.Modify(roleModifier).RoleColor(Color.green).Factions(Faction.Solo);
}