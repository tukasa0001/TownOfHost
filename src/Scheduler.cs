using System;
using System.Collections.Generic;
using TownOfHost.Extensions;
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

    public bool Execute(float deltaTime)
    {
        instanceDelay -= deltaTime;
        if (instanceDelay > 0) return false;
        action();
        return true;
    }

    public static void Schedule(Action action, float delay, bool repeat = false)
    {
        DTask _ = new(action, delay, repeat: repeat);
    }

    [Obsolete("Use DTask.Schedule instead. In the future this constructor will not automatically register tasks.")]
    public DTask(Action action, float delay, string name = null, bool repeat = false)
    {
        this.action = action;
        this.delay = delay;
        this.instanceDelay = delay;
        this.name = name;
        this.repeat = repeat;
        Tasks.Add(this);
        if (name != null)
            VentLogger.Old("\"" + name + "\" is created", "LateTask");
    }

    public static void Update(float deltaTime)
    {
        int i = 0;
        while (i < Tasks.Count)
        {
            DTask dTask = Tasks[i];
            if (!dTask.Execute(deltaTime)) i++;
            else {
                if (DebugScheduler) VentLogger.Old($"\"{dTask.name ?? "No Name Task"}\" is finished", "LateTask");
                if (dTask.repeat) dTask.instanceDelay = dTask.delay;
                else Tasks.Pop(i);
            }
        }
    }
}