using System;
using System.Collections.Generic;
using TownOfHost.Extensions;
using VentLib.Extensions;
using VentLib.Logging;

namespace TownOfHost;

// Abbreviated name not preferable but since this is used so much and is already ugly it's *okay* here
// DelayedTask
public class DTask
{
    // TODO: make into setting
    public const bool DebugScheduler = true;

    private static List<DTask> Tasks = new();

    private readonly Action action;
    private float delay;
    private float instanceDelay;
    private bool repeat;
    private readonly string name;

    private bool Execute(float deltaTime)
    {
        instanceDelay -= deltaTime;
        if (instanceDelay > 0) return false;
        action();
        return true;
    }

    [Obsolete("Use Async.ScheduleInStep instead. In the future this constructor will not automatically register tasks.")]
    public DTask(Action action, float delay, string name = null, bool repeat = false)
    {
        this.action = action;
        this.delay = delay;
        this.instanceDelay = delay;
        this.name = name;
        this.repeat = repeat;
        Tasks.Add(this);
        if (name != null)
            VentLogger.Trace("\"" + name + "\" is created", "LateTask");
    }

    public static void Update(float deltaTime)
    {
        int i = 0;
        while (i < Tasks.Count)
        {
            DTask dTask = Tasks[i];
            if (!dTask.Execute(deltaTime)) i++;
            else {
                if (DebugScheduler) VentLogger.Trace($"\"{dTask.name ?? "No Name Task"}\" is finished", "LateTask");
                if (dTask.repeat) dTask.instanceDelay = dTask.delay;
                else Tasks.Pop(i);
            }
        }
    }
}