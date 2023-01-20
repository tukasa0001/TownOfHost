using System;
using AmongUs.GameOptions;
using TownOfHost.Factions;
using TownOfHost.GUI;

namespace TownOfHost.Roles;

public class Crewmate : CustomRole
{
    public int TotalTasks => taskSupplier?.Invoke() ?? 0;
    public int TasksComplete;
    public bool HasAllTasksDone => TasksComplete >= TotalTasks;

    // Used in subclasses but setup here
    public bool HasOverridenTasks;
    public bool HasCommonTasks;
    public int ShortTasks;
    public int LongTasks;

    private Func<int>? taskSupplier;

    // TODO: Maybe make color customizable idk that's pretty extreme
    [DynElement(UI.Counter)]
    protected string TaskTracker() => RoleUtils.Counter(TasksComplete, TotalTasks);

    [RoleAction(RoleActionType.TaskComplete)]
    protected void InternalTaskComplete(PlayerControl player)
    {
        if (player.PlayerId != MyPlayer.PlayerId) return;
        TasksComplete++;
        this.OnTaskComplete();
    }

    /// <summary>
    /// Sets up the task counter for crewmate roles. If you extend this class and want this done automatically please call base.Setup()
    /// </summary>
    /// <param name="player">Player wrapped into this role's class instance</param>
    protected override void Setup(PlayerControl player) => taskSupplier = () => player.Data?.Tasks?.Count ?? 0;

    /// <summary>
    /// Called automatically when this player completes a task
    /// </summary>
    protected virtual void OnTaskComplete() { }

    protected override RoleModifier Modify(RoleModifier roleModifier) =>
        roleModifier.VanillaRole(RoleTypes.Crewmate).Factions(Faction.Crewmates).RoleColor("#b6f0ff");
}

