using System;
using System.Collections.Generic;

namespace TOHE;

class LateTask
{
    public string name;
    public float timer;
    public Action action;
    public static List<LateTask> Tasks = new();
    public bool Run(float deltaTime)
    {
        timer -= deltaTime;
        if (timer <= 0)
        {
            action();
            return true;
        }
        return false;
    }
    public LateTask(Action action, float time, string name = "No Name Task")
    {
        this.action = action;
        this.timer = time;
        this.name = name;
        Tasks.Add(this);
        if (name != "")
            Logger.Info("\"" + name + "\" is created", "LateTask");
    }
    public static void Update(float deltaTime)
    {
        var TasksToRemove = new List<LateTask>();
        for (int i = 0; i < Tasks.Count; i++)
        {
            var task = Tasks[i];
            try
            {
                if (task.Run(deltaTime))
                {
                    if (task.name != "")
                        Logger.Info($"\"{task.name}\" is finished", "LateTask");
                    TasksToRemove.Add(task);
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"{ex.GetType()}: {ex.Message}  in \"{task.name}\"\n{ex.StackTrace}", "LateTask.Error", false);
                TasksToRemove.Add(task);
            }
        }
        TasksToRemove.ForEach(task => Tasks.Remove(task));
    }
}